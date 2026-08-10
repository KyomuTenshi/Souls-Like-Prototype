using UnityEngine;

namespace SG 
{
    public class PlayerLocomotion : MonoBehaviour
    {
        PlayerManager playerManager;
        PlayerStats playerStats;
        Transform cameraObject;
        InputHandler inputHandler;
        public Vector3 moveDirection;

        [HideInInspector]
        public Transform myTransform;
        [HideInInspector]
        public AnimatorHandler animatorHandler;

        public Rigidbody rb; 
        public GameObject normalCamera;

        [Header("Ground & Air Detection Stats")]
        [SerializeField] float groundDetectionRayStartPoint = 0.5f;
        [SerializeField] float minimunDistanceNeededToBeginFall = 1f;
        [SerializeField] float grounfDirectionRayDistance = 0.2f;
        LayerMask ignoreForGroundCheck;
        public float inAirTimer;

        [Header("Movement Stats")]
        [SerializeField] float movementSpeed = 5;
        [SerializeField] float rotationSpeed = 10;
        [SerializeField] float rollSpeed = 6;
        [SerializeField] float sprintSpeed = 7;
        [SerializeField] float fallingSpeed = 45;

        [Header("Jump Stats")]
        // Прыжок теперь ФИЗИЧЕСКИЙ: вертикальный импульс + три фазы анимации.
        // Фаза 1 (jumpStartAnimation) запускается кодом; фаза 2 (Falling,
        // зацикленная) — переходом в Animator Controller по Has Exit Time;
        // фаза 3 (Land/Empty) — кодом в HandleFalling при касании земли.
        [SerializeField] float jumpForce = 9f;
        // Окно "взлёта": пока оно идёт, проверка земли в HandleFalling
        // пропускается — иначе луч вниз тут же "приземлял" бы игрока
        // обратно в первый же кадр прыжка.
        [SerializeField] float jumpAscendGraceTime = 0.35f;
        // Имя состояния фазы 1. По умолчанию "Jump" — как в туториале,
        // чтобы ничего не переименовывать в контроллере.
        [SerializeField] string jumpStartAnimation = "Jump";

        [Header("Stamina Costs")]
        // Гейт в духе souls: действие доступно, пока стамина > 0, а стоимость
        // может увести её ровно в ноль (как в Dark Souls).
        [SerializeField] int rollStaminaCost = 15;
        [SerializeField] int jumpStaminaCost = 10;
        [SerializeField] float sprintStaminaCostPerSecond = 8f;

        float jumpGraceTimer;

        // Используется в AnimatorHandler.OnAnimatorMove() как запасной вариант,
        // когда у клипа анимации (например Roll) нет собственного смещения
        // вперёд (Average Velocity == 0), и двигать персонажа приходится вручную.
        public float RollSpeed { get { return rollSpeed; } }

        void Start()
        {
            playerManager = GetComponent<PlayerManager>();
            playerStats = GetComponent<PlayerStats>();
            rb = GetComponent<Rigidbody>();
            inputHandler = GetComponent<InputHandler>();
            animatorHandler = GetComponentInChildren<AnimatorHandler>();
            cameraObject = Camera.main.transform;
            myTransform = transform;
            animatorHandler.Initialize();

            playerManager.isGrounded = true;
            ignoreForGroundCheck = ~(1 << 8 | 1 << 11);

            // Встроенная гравитация выключена: падение полностью и предсказуемо
            // считает HandleFalling() (иначе получалась двойная гравитация —
            // физика Unity + ручная сила одновременно).
            rb.useGravity = false;
        }

        #region Movement
        Vector3 normalVector;
        Vector3 targetPosition;

        private void HandleRotation(float delta)
        {
            Vector3 targetDir = Vector3.zero;

            targetDir = cameraObject.forward * inputHandler.vertical;
            targetDir += cameraObject.right * inputHandler.horizontal;

            targetDir.Normalize();
            targetDir.y = 0;

            if (targetDir == Vector3.zero)
                targetDir = myTransform.forward;

            float rs = rotationSpeed;

            Quaternion tr = Quaternion.LookRotation(targetDir);
            Quaternion targetRotation = Quaternion.Slerp(myTransform.rotation, tr, rs * delta);

            myTransform.rotation = targetRotation;
        }

        public void HandleMovement(float delta)
        {
            if (inputHandler.rollFlag)
                return;

            if (playerManager.isIntetacting)
                return;

            moveDirection = cameraObject.forward * inputHandler.vertical;
            moveDirection += cameraObject.right * inputHandler.horizontal;
            moveDirection.Normalize();
            moveDirection.y = 0;

            float speed = movementSpeed;

            // Спринт: нужен зажатый флаг и заметный ввод. Стамина-гейт и
            // списание — только в Souls-режиме; в Action (NieR-style) спринт
            // бесплатный. playerStats == null (компонент не повешен) — фича
            // тихо отключается, ничего не ломая.
            bool wantsSprint = inputHandler.sprintFlag && inputHandler.moveAmount > 0.5f;
            bool staminaFree = playerStats == null || playerStats.IsActionMode;
            bool canSprint = staminaFree || playerStats.HasStamina();

            if (wantsSprint && canSprint)
            {
                speed = sprintSpeed;
                playerManager.isSprinting = true;

                if (!staminaFree)
                {
                    playerStats.DrainStamina(sprintStaminaCostPerSecond * delta);
                }
            }
            else
            {
                playerManager.isSprinting = false;
            }

            moveDirection *= speed;

            Vector3 projectedVelocity = Vector3.ProjectOnPlane(moveDirection, normalVector);
            rb.linearVelocity = projectedVelocity;

            animatorHandler.UpdateAnimatorValues(inputHandler.moveAmount, 0, playerManager.isSprinting);

            if (animatorHandler.canRotate)
            {
                HandleRotation(delta);
            }
        }

        public void HandleRollingAndSprinting(float delta)
        {
            if (animatorHandler.anim.GetBool("isInteracting"))
                return;

            if (inputHandler.rollFlag)
            {
                // Флаг гасим в месте использования — порядок Update() между
                // скриптами Unity не гарантирован.
                inputHandler.rollFlag = false;

                // Souls-режим: выдохся — ролла нет (действие доступно, пока
                // стамина строго больше нуля). Action-режим (NieR-style):
                // уклонение всегда доступно — это ядро боевого ритма NieR,
                // его нельзя отбирать у игрока из-за ресурса.
                if (playerStats != null && !playerStats.IsActionMode && !playerStats.HasStamina())
                    return;

                moveDirection = cameraObject.forward * inputHandler.vertical;
                moveDirection += cameraObject.right * inputHandler.horizontal;

                if (inputHandler.moveAmount > 0)
                {
                    moveDirection.y = 0;

                    // Защита LookRotation от нулевого вектора.
                    if (moveDirection.sqrMagnitude > 0.0001f)
                    {
                        animatorHandler.PlayeTargetAnimation("Rolling", true);
                        Quaternion rollRotation = Quaternion.LookRotation(moveDirection);
                        myTransform.rotation = rollRotation;

                        // Списание — только в Souls-режиме (в Action ролл
                        // бесплатный, см. гейт выше).
                        if (playerStats != null && !playerStats.IsActionMode)
                        {
                            playerStats.TakeStaminaDamage(rollStaminaCost);
                        }
                    }
                }
                else
                {
                    // BackStep: анимации пока нет в проекте. Включается одной
                    // строкой, когда клип появится в Animator Controller.
                    // animatorHandler.PlayeTargetAnimation("Backstep", true);
                    // Не забудь тогда добавить и стамина-кост, как у ролла выше.
                }
            }
        }

        public void HandleFalling(float delta, Vector3 moveDir)
        {
            playerManager.isGrounded = false;
            RaycastHit hit;
            Vector3 origin = myTransform.position;
            origin.y += groundDetectionRayStartPoint;

            // Маска ignoreForGroundCheck и на переднем луче: без неё, как только
            // на игроке появятся дочерние хитбоксы (уроки про урон по частям
            // тела), луч начнёт попадать в собственные коллайдеры, и персонаж
            // будет "застревать в воздухе" у стен.
            if (Physics.Raycast(origin, myTransform.forward, out hit, 0.4f, ignoreForGroundCheck))
            {
                moveDir = Vector3.zero;
            }

            if (playerManager.isInAir)
            {
                rb.AddForce(-Vector3.up * fallingSpeed);
                rb.AddForce(moveDir * fallingSpeed / 10f);
            }

            Vector3 dir = moveDir;
            dir.Normalize();
            origin = origin + dir * grounfDirectionRayDistance;

            targetPosition = myTransform.position;

            // Окно взлёта после прыжка: пока идёт — землю не ищем вообще,
            // персонаж гарантированно успевает оторваться от неё.
            bool jumpAscending = jumpGraceTimer > 0f;
            if (jumpAscending)
            {
                jumpGraceTimer -= delta;
            }

            Debug.DrawRay(origin, -Vector3.up * minimunDistanceNeededToBeginFall, Color.red, 0.1f, false);
            if (!jumpAscending && Physics.Raycast(origin, -Vector3.up, out hit, minimunDistanceNeededToBeginFall, ignoreForGroundCheck))
            {
                // hit.normal (нормаль поверхности), а не hit.point — ProjectOnPlane
                // в HandleMovement ждёт именно нормаль плоскости.
                normalVector = hit.normal;

                Vector3 tp = hit.point;
                playerManager.isGrounded = true;
                targetPosition.y = tp.y;

                if (playerManager.isInAir)
                {
                    // ФАЗА 3 прыжка/падения: долгий полёт — жёсткое
                    // приземление (Land), короткий — мгновенный выход (Empty).
                    if (inAirTimer > 0.5f)
                    {
                        Debug.Log("You were in the air for " + inAirTimer);
                        animatorHandler.PlayeTargetAnimation("Land", true);
                        inAirTimer = 0;
                    }
                    else
                    {
                        animatorHandler.PlayeTargetAnimation("Empty", false);
                        inAirTimer = 0;
                    }

                    playerManager.isInAir = false;
                }
            }
            else
            {
                if (playerManager.isGrounded)
                {
                    playerManager.isGrounded = false;
                }

                if (playerManager.isInAir == false)
                {
                    // ФАЗА 2 при обычном сходе с обрыва (без прыжка): сразу
                    // включаем зацикленное падение. После прыжка сюда не
                    // попадаем (isInAir уже true) — в Falling переводит
                    // переход Jump -> Falling в самом Animator Controller.
                    if (playerManager.isIntetacting == false)
                    {
                        animatorHandler.PlayeTargetAnimation("Falling", true);
                    }

                    Vector3 vel = rb.linearVelocity;
                    vel.Normalize();
                    rb.linearVelocity = vel * (movementSpeed / 2);
                    playerManager.isInAir = true;
                }
            }

            if (playerManager.isGrounded)
            {
                if (playerManager.isIntetacting || inputHandler.moveAmount > 0)
                {
                    myTransform.position = Vector3.Lerp(myTransform.position, targetPosition, Time.deltaTime / 0.1f);
                }
                else
                {
                    myTransform.position = targetPosition;
                }
            }
        }
        
        public void HandleJumping()
        {
            if (playerManager.isIntetacting)
                return;

            if (inputHandler.jump_Input)
            {
                // Гасим флаг сразу же, иначе он никогда не сбросится (в
                // InputHandler он только выставляется в true) и прыжок будет
                // повторно триггериться на каждом кадре.
                inputHandler.jump_Input = false;

                // Прыгать можно только с земли — никаких прыжков в полёте.
                if (!playerManager.isGrounded)
                    return;

                // Souls-режим: без стамины не прыгаем. Action-режим:
                // прыжок бесплатный и всегда доступен с земли.
                if (playerStats != null && !playerStats.IsActionMode && !playerStats.HasStamina())
                    return;

                moveDirection = cameraObject.forward * inputHandler.vertical;
                moveDirection += cameraObject.right * inputHandler.horizontal;
                moveDirection.y = 0;
                moveDirection.Normalize();

                // Поворот в сторону движения — только если ввод есть.
                // Прыжок с места (moveAmount == 0) теперь тоже работает.
                if (inputHandler.moveAmount > 0 && moveDirection.sqrMagnitude > 0.0001f)
                {
                    Quaternion jumpRotation = Quaternion.LookRotation(moveDirection);
                    myTransform.rotation = jumpRotation;
                }

                // ФАЗА 1: стартовый клип прыжка.
                animatorHandler.PlayeTargetAnimation(jumpStartAnimation, true);

                // Списание — только в Souls-режиме (см. гейт выше).
                if (playerStats != null && !playerStats.IsActionMode)
                {
                    playerStats.TakeStaminaDamage(jumpStaminaCost);
                }

                // Реальный вертикальный импульс. Дальше вертикаль ведёт
                // HandleFalling (ручная гравитация), горизонталь — root
                // motion клипа либо сохранённая скорость (см. OnAnimatorMove).
                playerManager.isGrounded = false;
                playerManager.isInAir = true;
                inAirTimer = 0;
                jumpGraceTimer = jumpAscendGraceTime;

                Vector3 jumpVelocity = inputHandler.moveAmount > 0
                    ? moveDirection * movementSpeed
                    : Vector3.zero;
                jumpVelocity.y = jumpForce;
                rb.linearVelocity = jumpVelocity;
            }
        }
        #endregion
    }
}