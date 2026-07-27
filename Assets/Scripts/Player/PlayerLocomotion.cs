using UnityEngine;

namespace SG 
{
    public class PlayerLocomotion : MonoBehaviour
    {
        PlayerManager playerManager;
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

        // Используется в AnimatorHandler.OnAnimatorMove() как запасной вариант,
        // когда у клипа анимации (например Roll) нет собственного смещения
        // вперёд (Average Velocity == 0), и двигать персонажа приходится вручную.
        public float RollSpeed { get { return rollSpeed; } }

        void Start()
        {
            playerManager = GetComponent<PlayerManager>();
            rb = GetComponent<Rigidbody>();
            inputHandler = GetComponent<InputHandler>();
            animatorHandler = GetComponentInChildren<AnimatorHandler>();
            cameraObject = Camera.main.transform;
            myTransform = transform;
            animatorHandler.Initialize();

            playerManager.isGrounded = true;
            ignoreForGroundCheck = ~(1 << 8 | 1 << 11);

            // БЫЛО: Rigidbody.useGravity оставался включённым по умолчанию (true),
            // а HandleFalling() при этом сам добавлял силу вниз через
            // rb.AddForce(-Vector3.up * fallingSpeed). Это двойная гравитация:
            // встроенная физика Unity + ручная сила одновременно, из-за чего
            // падение получалось заметно быстрее и резче, чем задаёт fallingSpeed,
            // и подобрать ощущение падения "как в souls-like" через инспектор было
            // невозможно — реальное ускорение всегда было больше выставленного.
            // Отключаем встроенную гравитику: падение теперь полностью и
            // предсказуемо считает HandleFalling().
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

            if (inputHandler.sprintFlag &&inputHandler.moveAmount > 0.5)
            {
                speed = sprintSpeed;
                playerManager.isSprinting = true;
                moveDirection *= speed;
            }
            else
            {
                if(inputHandler.moveAmount < 0.5)
                {
                    moveDirection *= movementSpeed;
                    playerManager.isSprinting = false;
                } else
                {
                    moveDirection *= speed;
                    playerManager.isSprinting = false;
                }
                
            }

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
                // Сбрасываем флаг сразу тут, в месте использования, а не в
                // PlayerManager.Update() безусловно каждый кадр — порядок вызова
                // Update() между разными скриптами Unity не гарантирован, и если
                // PlayerManager отрабатывал раньше PlayerLocomotion, флаг мог
                // обнулиться до того, как Rolling вообще успевал его увидеть.
                inputHandler.rollFlag = false;

                moveDirection = cameraObject.forward * inputHandler.vertical;
                moveDirection += cameraObject.right * inputHandler.horizontal;

                if (inputHandler.moveAmount > 0)
                {
                    moveDirection.y = 0;

                    // Защита от Vector3.zero: если персонаж стоит на месте, но
                    // moveAmount по какой-то причине > 0 (например, стик слегка
                    // "плавает"), LookRotation на нулевом векторе кидает предупреждение
                    // в консоль и не меняет поворот. Такого практически не бывает при
                    // Clamp01 выше, но проверка дешёвая и убирает риск полностью.
                    if (moveDirection.sqrMagnitude > 0.0001f)
                    {
                        animatorHandler.PlayeTargetAnimation("Rolling", true);
                        Quaternion rollRotation = Quaternion.LookRotation(moveDirection);
                        myTransform.rotation = rollRotation;
                    }
                }
                else
                {
                    // BackStep: анимации пока нет в проекте. Код сознательно оставлен
                    // закомментированным (не удалён), чтобы включить его одной строкой,
                    // как только анимация BackStep появится в Animator Controller.
                    // animatorHandler.PlayeTargetAnimation("Backstep", true);
                }
            }
        }

        public void HandleFalling(float delta, Vector3 moveDir)
        {
            playerManager.isGrounded = false;
            RaycastHit hit;
            Vector3 origin = myTransform.position;
            origin.y += groundDetectionRayStartPoint;

            if (Physics.Raycast(origin, myTransform.forward, out hit, 0.4f))
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

            Debug.DrawRay(origin, -Vector3.up * minimunDistanceNeededToBeginFall, Color.red, 0.1f, false);
            if (Physics.Raycast(origin, -Vector3.up, out hit, minimunDistanceNeededToBeginFall, ignoreForGroundCheck))
            {
                // normalVector берём из hit.normal (нормаль поверхности), а не
                // hit.point (мировая точка попадания) — ProjectOnPlane в
                // HandleMovement ждёт именно нормаль плоскости (~(0,1,0) на ровном
                // полу). Использование hit.point ломало проекцию скорости в
                // зависимости от координат персонажа на карте.
                normalVector = hit.normal;

                Vector3 tp = hit.point;
                playerManager.isGrounded = true;
                targetPosition.y = tp.y;

                if (playerManager.isInAir)
                {
                    if (inAirTimer > 0.5f)
                    {
                        Debug.Log("You were in the air for " + inAirTimer);
                        animatorHandler.PlayeTargetAnimation("Land", true);
                        inAirTimer = 0;
                    }
                    else
                    {
                        animatorHandler.PlayeTargetAnimation("Locomotion", false);
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
                    myTransform.position = Vector3.Lerp(myTransform.position, targetPosition, Time.deltaTime);
                }
                else
                {
                    myTransform.position = targetPosition;
                }
            }
        }
        #endregion
    }
}