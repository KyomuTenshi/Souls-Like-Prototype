using UnityEngine;

namespace SG {
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

        public new Rigidbody rigidbody;
        public GameObject normalCamera;

        [Header("Ground & Air Detection Stats")]
        [SerializeField]
        float groundDetectionRayStartPoint = 0.5f;
        [SerializeField]
        float minimumDistanceNeededToBeginFall = 1f;
        [SerializeField]
        float groundDirectionRayDistance = 0.2f;
        LayerMask ignoreForGroundCheck;
        public float inAirTimer;

        [Header("Movement Stats")]
        [SerializeField]
        float movementSpeed = 5;
        [SerializeField]
        float sprintSpeed = 7;
        [SerializeField]
        float rotationSpeed = 10;
        [SerializeField]
        float fallingSpeed = 45;

        Vector3 normalVector;
        Vector3 targetPosition;

        #region Action Settings
        // Параметры действий вынесены в Inspector: дистанция и длительность задаются явно
        // и не зависят от длины анимационных клипов и переходов в Animator.
        [Header("Manual Movement")]
        [Tooltip("Вкл — ролл, бэкстеп и торможение двигаются по параметрам ниже. Выкл — root motion анимаций.")]
        public bool useManualRollMovement = true;

        [Header("Roll")]
        [Tooltip("Дистанция ролла, м")]
        [SerializeField]
        float rollDistance = 4f;
        [Tooltip("Длительность перемещения, с")]
        [SerializeField]
        float rollDuration = 0.6f;

        [Header("Backstep")]
        [Tooltip("Дистанция бэкстепа, м")]
        [SerializeField]
        float backstepDistance = 1.5f;
        [Tooltip("Длительность перемещения, с")]
        [SerializeField]
        float backstepDuration = 0.35f;

        [Header("Sprint Stop")]
        [Tooltip("Тормозной путь, м")]
        [SerializeField]
        float sprintStopDistance = 2f;
        [Tooltip("Время торможения до полной остановки, с")]
        [SerializeField]
        float sprintStopDuration = 0.6f;
        [Tooltip("Минимальная длительность спринта для анимации торможения, с")]
        [SerializeField]
        float minSprintTimeForStop = 0.3f;
        [Tooltip("Допустимый интервал между отпусканием спринта и отпусканием движения, с")]
        [SerializeField]
        float sprintReleaseGrace = 0.25f;
        #endregion

        void Start()
        {
            playerManager = GetComponent<PlayerManager>();
            rigidbody = GetComponent<Rigidbody>();
            inputHandler = GetComponent<InputHandler>();
            animatorHandler = GetComponentInChildren<AnimatorHandler>();
            cameraObject = Camera.main.transform;
            myTransform = transform;
            animatorHandler.Initialize();

            normalVector = Vector3.up;
            animatorHandler.canRotate = true;

            playerManager.isGrounded = true;
            ignoreForGroundCheck = ~(1 << 8 | 1 << 11); // Игнорируем слои Player и Enemy.
        }

        #region Movement
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
            // Вызов перенесён выше ранних return, чтобы параметры Animator обновлялись и во время действий.
            animatorHandler.UpdateAnimatorValues(inputHandler.moveAmount, 0, playerManager.isSprinting);

            // Во время действия скорость и поворот задаёт само действие, а не ввод игрока.
            if (animatorHandler.anim.GetBool("isInteracting"))
                return;

            if (inputHandler.rollFlag)
                return;

            if (playerManager.isInAir)
                return;

            moveDirection = cameraObject.forward * inputHandler.vertical;
            moveDirection += cameraObject.right * inputHandler.horizontal;

            moveDirection.y = 0;
            moveDirection.Normalize();

            float speed = movementSpeed;

            if (inputHandler.sprintFlag && inputHandler.moveAmount > 0.5)
            {
                speed = sprintSpeed;
                playerManager.isSprinting = true;
                moveDirection *= speed;
            }
            else
            {
                if (inputHandler.moveAmount < 0.5)
                {
                    moveDirection *= movementSpeed;
                    playerManager.isSprinting = false;
                }
                else
                {
                    moveDirection *= speed;
                    playerManager.isInteracting = false;
                }
            }

            Vector3 projectedVelocity = Vector3.ProjectOnPlane(moveDirection, normalVector);
            rigidbody.linearVelocity = projectedVelocity; // Unity 6: Rigidbody.velocity переименован в linearVelocity.

            if (animatorHandler.canRotate)
            {
                HandleRotation(delta);
            }
        }

        public void HandleRollingAndSprinting(float delta)
        {
            // Перемещение во время активного действия; вызывается до проверки isInteracting.
            HandleManualActionMovement(delta);

            if (animatorHandler.anim.GetBool("isInteracting"))
                return;

            if (inputHandler.rollFlag)
            {
                moveDirection = cameraObject.forward * inputHandler.vertical;
                moveDirection += cameraObject.right * inputHandler.horizontal;
                moveDirection.y = 0;

                if (inputHandler.moveAmount > 0)
                {
                    animatorHandler.PlayTargetAnimation("Rolling", true);
                    Quaternion rollRotation = Quaternion.LookRotation(moveDirection);
                    myTransform.rotation = rollRotation;
                    StartManualActionMovement(moveDirection, rollDistance, rollDuration, false);
                }
                else
                {
                    animatorHandler.PlayTargetAnimation("Backstep", true);
                    StartManualActionMovement(-myTransform.forward, backstepDistance, backstepDuration, false);
                }
            }

            // Анимация торможения после спринта — расширение сверх туториала.
            HandleSprintStop(delta);
        }

        public void HandleFalling(float delta, Vector3 moveDirection)
        {
            playerManager.isGrounded = false;
            RaycastHit hit;
            Vector3 origin = myTransform.position;
            origin.y += groundDetectionRayStartPoint;

            if (Physics.Raycast(origin, myTransform.forward, out hit, 0.4f))
            {
                moveDirection = Vector3.zero;
            }

            if (playerManager.isInAir)
            {
                rigidbody.AddForce(-Vector3.up * fallingSpeed);
                rigidbody.AddForce(moveDirection * fallingSpeed / 10f);
            }

            Vector3 dir = moveDirection;
            dir.Normalize();
            origin = origin + dir * groundDirectionRayDistance;

            targetPosition = myTransform.position;

            Debug.DrawRay(origin, -Vector3.up * minimumDistanceNeededToBeginFall, Color.red, 0.1f, false);
            if (Physics.Raycast(origin, -Vector3.up, out hit, minimumDistanceNeededToBeginFall, ignoreForGroundCheck))
            {
                normalVector = hit.normal;
                Vector3 tp = hit.point;
                playerManager.isGrounded = true;

                targetPosition.y = tp.y;

                if (playerManager.isInAir)
                {
                    if (inAirTimer > 0.5f)
                    {
                        Debug.Log("You were in the air for " + inAirTimer + " seconds.");
                        animatorHandler.PlayTargetAnimation("Land", true);
                        inAirTimer = 0;
                    }
                    else 
                    {
                        animatorHandler.PlayTargetAnimation("Locomotion", false);
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
                    if (playerManager.isInteracting == false)
                    {
                        animatorHandler.PlayTargetAnimation("Falling", true);
                    }

                    Vector3 vel = rigidbody.linearVelocity;
                    vel.Normalize();
                    rigidbody.linearVelocity = vel * (movementSpeed / 2);
                    playerManager.isInAir = true;
                }
            }

            if (playerManager.isGrounded)
            {
                if (playerManager.isInteracting || inputHandler.moveAmount > 0)
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

        #region Manual Action Movement
        // Ручное перемещение для ролла, бэкстепа и торможения вместо root motion.
        // Скорость вычисляется из заданной дистанции и длительности, поэтому
        // пройденный путь не зависит от длины клипа и настроек переходов Animator.

        [HideInInspector]
        public bool isDoingManualAction; // Используется в AnimatorHandler.OnAnimatorMove для отключения root motion.

        Vector3 actionDirection;
        float actionStartSpeed;
        float actionDuration;
        float actionTimer;
        bool actionSlowsDown;

        private void StartManualActionMovement(Vector3 direction, float distance, float duration, bool slowDown)
        {
            isDoingManualAction = useManualRollMovement;

            if (!isDoingManualAction)
                return;

            direction.y = 0;
            actionDirection = direction.normalized;
            actionDuration = Mathf.Max(duration, 0.01f);
            actionTimer = 0;
            actionSlowsDown = slowDown;

            // Равномерное движение: v = d / t. Линейное торможение до нуля: v0 = 2d / t.
            actionStartSpeed = slowDown ? 2f * distance / actionDuration : distance / actionDuration;

            rigidbody.linearVelocity = actionDirection * actionStartSpeed;
        }

        private void HandleManualActionMovement(float delta)
        {
            if (!animatorHandler.anim.GetBool("isInteracting"))
            {
                isDoingManualAction = false;
                return;
            }

            if (!isDoingManualAction)
                return;

            actionTimer += delta;

            // Дистанция пройдена: персонаж стоит на месте до завершения анимации.
            if (actionTimer >= actionDuration)
            {
                rigidbody.linearVelocity = Vector3.zero;
                return;
            }

            float speed = actionStartSpeed;

            if (actionSlowsDown)
                speed *= 1f - actionTimer / actionDuration;

            rigidbody.linearVelocity = actionDirection * speed;
        }
        #endregion

        #region Sprint Stop
        // Проигрывает Sprint_Exit, если игрок резко остановился после достаточно долгого спринта.

        float sprintTime;
        float timeSinceSprint;
        bool wasMoving;

        private void HandleSprintStop(float delta)
        {
            if (inputHandler.rollFlag)
            {
                sprintTime = 0;
                return;
            }

            bool moving = inputHandler.moveAmount > 0;
            bool sprinting = inputHandler.b_input && moving;

            if (sprinting)
            {
                sprintTime += delta;
                timeSinceSprint = 0;
            }
            else
            {
                timeSinceSprint += delta;
            }

            bool justStopped = wasMoving && !moving;
            wasMoving = moving;

            if (justStopped && timeSinceSprint <= sprintReleaseGrace && sprintTime >= minSprintTimeForStop)
            {
                sprintTime = 0;
                animatorHandler.PlayTargetAnimation("Sprint_Exit", true);
                StartManualActionMovement(myTransform.forward, sprintStopDistance, sprintStopDuration, true);
                return;
            }

            if (timeSinceSprint > sprintReleaseGrace)
            {
                sprintTime = 0;
            }
        }
        #endregion
    }
}