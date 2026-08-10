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
        // Теперь это УСКОРЕНИЕ падения в м/с² (см. HandleFalling). 45 ≈
        // прежняя средняя скорость набора при 60 FPS, но теперь одинаково
        // на любом фреймрейте. Ощущается иначе — подкрути под себя.
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
        // БЫЛО: magic number 0.5f прямо в HandleFalling. Порог решает, какая
        // анимация фазы 3 играет: дольше него — полноценный "Land", короче —
        // мгновенный "Empty". Раньше жёсткая привязка к 0.5с молча ломалась
        // при увеличении fallingSpeed: чем быстрее падение, тем меньше
        // суммарное время в воздухе, и "Land" переставал успевать включиться
        // вообще. Теперь порог настраивается отдельно от скорости падения —
        // подгоняй его под конкретную пару jumpForce/fallingSpeed, а не
        // наоборот.
        [SerializeField] float airTimeForLandAnimation = 0.5f;

        [Header("Stamina Costs")]
        // Гейт в духе souls: действие доступно, пока стамина > 0, а стоимость
        // может увести её ровно в ноль (как в Dark Souls).
        [SerializeField] int rollStaminaCost = 15;
        [SerializeField] int jumpStaminaCost = 10;
        [SerializeField] float sprintStaminaCostPerSecond = 8f;

        [Header("NieR Dodge — Attack Cancel")]
        // Ролл может ОТМЕНЯТЬ атаку — ядро ощущения NieR/DMC: уклонение
        // важнее завершения замаха. Работает по ТЕГУ состояния: в Animator
        // Controller у состояний атак в поле Tag должно стоять "Attack"
        // (выдели состояние -> Inspector -> поле Tag, регистр важен).
        // Состояние без тега отменить нельзя — поэтому ролл не отменяет сам
        // себя, Land, Falling или хитстан. Пометишь тегом Attack другие
        // состояния (например, BetaDamage) — кансел распространится и на
        // них: правило целиком в руках Animator'а, код менять не нужно.
        [SerializeField] bool allowAttackDodgeCancel = true;
        // Доля клипа атаки (0..1), ДО которой кансел запрещён: самое начало
        // замаха доигрывает, чтобы отмена не выглядела как "атаки не было".
        // Открытое комбо-окно (canDoCombo) разрешает кансел независимо от
        // порога — оно по смыслу и есть окно отмены.
        [Range(0f, 1f)]
        [SerializeField] float attackDodgeCancelTime = 0.25f;

        WeaponSlotManager weaponSlotManager;

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
            // Нужен канселу атак: при обрыве замаха закрываем хитбоксы оружия.
            weaponSlotManager = GetComponentInChildren<WeaponSlotManager>();
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

            // Сначала прижимаем к горизонту, ПОТОМ нормализуем (см. тот же
            // фикс в HandleMovement).
            targetDir.y = 0;
            targetDir.Normalize();

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
            // БЫЛО: Normalize(), потом y = 0. Камера смотрит сверху вниз, её
            // forward наклонён — после нормализации зануление y ОБРЕЗАЛО
            // длину вектора, и фактическая скорость бега зависела от наклона
            // камеры (чем круче вниз смотришь, тем медленнее бежишь).
            // Правильный порядок: сначала прижать к горизонту, потом
            // нормализовать до единичной длины.
            moveDirection.y = 0;
            moveDirection.Normalize();

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
            if (!inputHandler.rollFlag)
                return;

            // БЫЛО: при isIntetacting — выход всегда, и ролл во время атаки
            // пропадал. Теперь атаку можно ОТМЕНИТЬ роллом (проверка ниже).
            // Если отменять пока нельзя — просто выходим, НЕ трогая буфер:
            // InputHandler поднимет rollFlag и в следующем кадре, попытка
            // повторится сама, пока живо окно буфера.
            bool isCancellingAttack = playerManager.isIntetacting;

            if (isCancellingAttack && !CanCancelAttackIntoRoll())
                return;

            // Дальше ролл точно принят к исполнению — гасим буфер. Даже если
            // ниже его съест стамина-гейт: как в souls, ввод без ресурса
            // пропадает, а не висит в очереди.
            inputHandler.ConsumeRollBuffer();

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
                    if (isCancellingAttack)
                    {
                        // Обрываем атаку аккуратно:
                        // 1) захлопываем комбо-окно, чтобы буферизованный RB
                        //    не продолжил цепочку прямо ИЗ ролла;
                        // 2) закрываем хитбоксы оружия — Close-событие в
                        //    конце клипа атаки после CrossFade может уже не
                        //    сработать, и меч "бил" бы сквозь весь ролл.
                        animatorHandler.DisableConbo();

                        if (weaponSlotManager != null)
                        {
                            weaponSlotManager.CloseLeftHandDamageCollider();
                            weaponSlotManager.CloseRightHandDamageCollider();
                        }
                    }

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

        // Можно ли прямо сейчас оборвать текущую интеракцию роллом.
        // Разрешаем только для состояний с тегом "Attack" на слое 0, и
        // только после attackDodgeCancelTime их длины — ЛИБО когда открыто
        // комбо-окно (тогда кансел мгновенный).
        private bool CanCancelAttackIntoRoll()
        {
            if (!allowAttackDodgeCancel)
                return false;

            if (playerManager.canDoConbo)
                return true;

            AnimatorStateInfo stateInfo = animatorHandler.anim.GetCurrentAnimatorStateInfo(0);

            if (!stateInfo.IsTag("Attack"))
                return false;

            return stateInfo.normalizedTime >= attackDodgeCancelTime;
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
                // БЫЛО: rb.AddForce(...) каждый Update. Update тикает чаще
                // FixedUpdate, и каждая копия силы попадала в следующий
                // физический шаг: на 144 FPS игрок падал ЗАМЕТНО быстрее, чем
                // на 60. Теперь пишем скорость напрямую с масштабом на delta —
                // ускорение падения одинаково на любом фреймрейте.
                rb.linearVelocity += Vector3.down * fallingSpeed * delta;

                // Воздушный контроль (лёгкий снос в сторону ввода) — по той же
                // схеме. Направление нормализуем: раньше сюда прилетал вектор,
                // уже умноженный на скорость бега, и сила зависела от неё.
                Vector3 airDirection = moveDir;
                airDirection.y = 0;
                if (airDirection.sqrMagnitude > 0.0001f)
                {
                    rb.linearVelocity += airDirection.normalized * (fallingSpeed / 10f) * delta;
                }
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
                    if (inAirTimer > airTimeForLandAnimation)
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
                    // Clamp01: на просадке FPS (delta > 0.1с) фактор Lerp
                    // вылетал за 1 — позицию ПЕРЕбрасывало за targetPosition,
                    // и персонаж дёргался на рельефе.
                    float snapFactor = Mathf.Clamp01(Time.deltaTime / 0.1f);
                    myTransform.position = Vector3.Lerp(myTransform.position, targetPosition, snapFactor);
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

                // Спринт-прыжок сохраняет скорость спринта: раньше горизонталь
                // всегда бралась от movementSpeed, и разбег "съедался" в
                // момент отрыва — прыжок с разбега ощущался как с места.
                float jumpHorizontalSpeed = playerManager.isSprinting ? sprintSpeed : movementSpeed;

                Vector3 jumpVelocity = inputHandler.moveAmount > 0
                    ? moveDirection * jumpHorizontalSpeed
                    : Vector3.zero;
                jumpVelocity.y = jumpForce;
                rb.linearVelocity = jumpVelocity;
            }
        }
        #endregion
    }
}