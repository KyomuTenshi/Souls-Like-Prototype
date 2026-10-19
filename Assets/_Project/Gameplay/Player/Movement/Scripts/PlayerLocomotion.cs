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
        [Tooltip("Радиус SphereCast под персонажем, м. Меньше радиуса капсулы, чтобы не цеплять стены.")]
        [SerializeField]
        float groundCheckRadius = 0.25f;
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
        [Tooltip("Длительность перемещения, с. Подбирается под анимацию: персонаж должен двигаться, пока катится.")]
        [SerializeField]
        float rollDuration = 0.6f;
        [Tooltip("Доля длительности в конце, за которую скорость плавно падает до нуля. 0 — резкая остановка, 1 — торможение с самого начала.")]
        [SerializeField, Range(0, 1)]
        float rollSlowDown = 0.5f;

        [Header("Backstep")]
        [Tooltip("Дистанция бэкстепа, м")]
        [SerializeField]
        float backstepDistance = 1.5f;
        [Tooltip("Длительность перемещения, с")]
        [SerializeField]
        float backstepDuration = 0.5f;
        [Tooltip("Доля длительности в конце, за которую скорость плавно падает до нуля. 0 — резкая остановка, 1 — торможение с самого начала.")]
        [SerializeField, Range(0, 1)]
        float backstepSlowDown = 0.5f;

        [Header("Jump")]
        [Tooltip("Дистанция прыжка вперёд на ровном месте, м")]
        [SerializeField]
        float jumpDistance = 3f;
        [Tooltip("Высота прыжка, м. Время в воздухе зависит от высоты и гравитации падения: чем выше прыжок, тем он дольше.")]
        [SerializeField]
        float jumpHeight = 1f;

        [HideInInspector]
        public bool isJumping; // От отрыва до приземления.

        [Header("Landing")]
        [Tooltip("Падение дольше этого времени — тяжёлое приземление: управление заблокировано до конца анимации Land, с. Считается только время движения вниз.")]
        [SerializeField]
        float hardLandingFallTime = 0.5f;
        [Tooltip("Мягкое приземление (обычный прыжок): через сколько секунд движение или ролл прерывают анимацию Land, с")]
        [SerializeField]
        float softLandingRecoveryTime = 0.15f;

        bool isSoftLanding;
        float softLandingTimer;
        int overrideLayer;

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
            overrideLayer = animatorHandler.anim.GetLayerIndex("Override");

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
                    playerManager.isSprinting = false;
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
                    StartManualActionMovement(moveDirection, rollDistance, rollDuration, rollSlowDown);
                }
                else
                {
                    animatorHandler.PlayTargetAnimation("Backstep", true);
                    StartManualActionMovement(-myTransform.forward, backstepDistance, backstepDuration, backstepSlowDown);
                }
            }

            // Анимация торможения после спринта — расширение сверх туториала.
            HandleSprintStop(delta);
        }

        public void HandleFalling(float delta, Vector3 moveDirection)
        {
            playerManager.isGrounded = false;
            RaycastHit hit;

            // Позиция читается и пишется через Rigidbody, а не Transform (в туториале — myTransform.position):
            // метод вызывается в FixedUpdate, а при включённой интерполяции Transform хранит сглаженную позицию
            // с прошлого кадра, и прямую запись в него физика игнорирует — персонаж зависал над землёй в ролле.
            Vector3 currentPosition = rigidbody.position;
            Vector3 origin = currentPosition;
            origin.y += groundDetectionRayStartPoint;

            // Триггеры игнорируются явно: в проекте включено Queries Hit Triggers, и без этого
            // игрок вставал на триггер-зоны (огонь, предметы) как на землю.
            if (Physics.Raycast(origin, myTransform.forward, out hit, 0.4f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                moveDirection = Vector3.zero;
            }

            if (playerManager.isInAir)
            {
                // Действие закончилось раньше, чем персонаж приземлился (например, ролл с уступа), —
                // включаем анимацию падения, иначе он летел бы вниз в позе бега.
                if (!animatorHandler.anim.GetBool("isInteracting"))
                    animatorHandler.PlayTargetAnimation("Falling", true);

                rigidbody.AddForce(-Vector3.up * fallingSpeed);

                // Толчок вперёд помогает сойти с уступа. В прыжке скорость вперёд уже задана,
                // и толчок сделал бы его длиннее jumpDistance.
                if (!isJumping)
                    rigidbody.AddForce(moveDirection * fallingSpeed / 10f);
            }

            // Пока персонаж поднимается в прыжке, землю не ищем: луч достаёт на 0.5 м ниже ног
            // и нашёл бы её сразу после отрыва — прыжок тут же «приземлился» бы.
            if (isJumping && rigidbody.linearVelocity.y > 0)
                return;

            Vector3 dir = moveDirection;
            dir.Normalize();
            origin = origin + dir * groundDirectionRayDistance;

            targetPosition = currentPosition;

            // Луч, смещённый вперёд, на краю уже не попадает в землю, хотя капсула ещё стоит на уступе.
            // Персонаж считался в воздухе, терял управление и застревал на краю, поэтому дополнительно
            // проверяем SphereCast из-под самого персонажа (в туториале — только луч).
            Vector3 sphereOrigin = currentPosition + Vector3.up * groundDetectionRayStartPoint;
            float sphereDistance = minimumDistanceNeededToBeginFall - groundCheckRadius;

            Debug.DrawRay(origin, -Vector3.up * minimumDistanceNeededToBeginFall, Color.red, 0.1f, false);
            if (Physics.Raycast(origin, -Vector3.up, out hit, minimumDistanceNeededToBeginFall, ignoreForGroundCheck, QueryTriggerInteraction.Ignore)
                || Physics.SphereCast(sphereOrigin, groundCheckRadius, -Vector3.up, out hit, sphereDistance, ignoreForGroundCheck, QueryTriggerInteraction.Ignore))
            {
                normalVector = hit.normal;
                Vector3 tp = hit.point;
                playerManager.isGrounded = true;

                targetPosition.y = tp.y;

                if (playerManager.isInAir)
                {
                    // Land проигрывается при любом приземлении (в туториале после короткого падения — сразу Empty):
                    // в состоянии Jump стоит только отталкивание, и без Land прыжок обрывался прямо в бег.
                    // После короткого падения анимацию можно прервать движением — см. HandleSoftLanding.
                    animatorHandler.PlayTargetAnimation("Land", true);
                    isSoftLanding = inAirTimer <= hardLandingFallTime;
                    softLandingTimer = softLandingRecoveryTime;

                    if (!isSoftLanding)
                        Debug.Log("You were in the air for " + inAirTimer + " seconds.");

                    inAirTimer = 0;
                    playerManager.isInAir = false;
                    isJumping = false;
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
                    vel *= movementSpeed / 2;

                    // После прыжка игрок уже летит вниз быстрее movementSpeed / 2 — скорость падения
                    // не урезается, иначе он зависал в воздухе, опустившись ниже точки прыжка.
                    vel.y = Mathf.Min(vel.y, rigidbody.linearVelocity.y);

                    rigidbody.linearVelocity = vel;
                    playerManager.isInAir = true;
                }
            }

            // В воздухе targetPosition совпадает с текущей позицией — двигать тело не нужно.
            if (!playerManager.isGrounded)
                return;

            if (playerManager.isInteracting || inputHandler.moveAmount > 0)
            {
                currentPosition = Vector3.Lerp(currentPosition, targetPosition, delta / 0.1f);
            }
            else
            {
                currentPosition = targetPosition;
            }

            // MovePosition учитывает интерполяцию, в отличие от присваивания rigidbody.position.
            rigidbody.MovePosition(currentPosition);
        }

        public void HandleJumping()
        {
            if (playerManager.isInteracting || playerManager.isInAir)
                return;

            if (!inputHandler.jump_Input)
                return;

            // Без ввода — прыжок на месте (расширение сверх туториала).
            moveDirection = Vector3.zero;

            if (inputHandler.moveAmount > 0)
            {
                moveDirection = cameraObject.forward * inputHandler.vertical;
                moveDirection += cameraObject.right * inputHandler.horizontal;
                moveDirection.y = 0;
                moveDirection.Normalize();
                myTransform.rotation = Quaternion.LookRotation(moveDirection);
            }

            animatorHandler.PlayTargetAnimation("Jump", true);

            // Прыжок задаёт только начальную скорость, дальше персонажа ведёт та же физика, что и при падении.
            // Раньше вертикаль шла по своей параболе с гравитацией слабее, чем у падения, и ниже точки
            // прыжка персонаж резко ускорялся вниз, а скорость вперёд обнулялась.
            float airGravity = Mathf.Abs(Physics.gravity.y) + fallingSpeed / rigidbody.mass;
            float jumpSpeed = Mathf.Sqrt(2f * airGravity * jumpHeight); // v = √(2gh): с такой скоростью тело поднимется на высоту h.
            float airTime = 2f * jumpSpeed / airGravity;                  // Подъём и спуск обратно на высоту отрыва.

            rigidbody.linearVelocity = moveDirection * (jumpDistance / airTime) + Vector3.up * jumpSpeed;
            playerManager.isInAir = true;
            isJumping = true;
        }

        // Мягкое приземление: первые softLandingRecoveryTime секунд персонаж приседает, затем движение
        // или ролл прерывают Land. Без ввода анимация доигрывает до конца сама.
        // Вызывается до HandleRollingAndSprinting, чтобы ролл сработал в том же кадре, что и прерывание.
        public void HandleSoftLanding(float delta)
        {
            if (!isSoftLanding)
                return;

            if (softLandingTimer > 0)
            {
                softLandingTimer -= delta;
                return;
            }

            // Во время перехода смотрим, куда он ведёт: Land уже могла смениться уроном или Empty.
            Animator anim = animatorHandler.anim;
            AnimatorStateInfo state = anim.IsInTransition(overrideLayer)
                ? anim.GetNextAnimatorStateInfo(overrideLayer)
                : anim.GetCurrentAnimatorStateInfo(overrideLayer);

            if (!state.IsName("Land"))
            {
                isSoftLanding = false;
                return;
            }

            if (inputHandler.moveAmount > 0 || inputHandler.rollFlag)
            {
                animatorHandler.PlayTargetAnimation("Empty", false);
                isSoftLanding = false;
            }
        }
        #endregion

        #region Manual Action Movement
        // Ручное перемещение для ролла, бэкстепа и торможения вместо root motion.
        // Скорость вычисляется из заданной дистанции и длительности, поэтому
        // пройденный путь не зависит от длины клипа и настроек переходов Animator.
        // Вертикальная скорость остаётся за физикой: раньше она обнулялась каждый кадр,
        // гравитация не работала, и ролл с уступа летел по воздуху до конца анимации.

        [HideInInspector]
        public bool isDoingManualAction; // Используется в AnimatorHandler.OnAnimatorMove для отключения root motion.

        Vector3 actionDirection;
        float actionStartSpeed;
        float actionDuration;
        float actionTimer;
        float actionSlowDownPortion;

        // slowDownPortion — доля длительности в конце, за которую скорость линейно падает до нуля.
        private void StartManualActionMovement(Vector3 direction, float distance, float duration, float slowDownPortion)
        {
            isDoingManualAction = useManualRollMovement;

            if (!isDoingManualAction)
                return;

            direction.y = 0;
            actionDirection = direction.normalized;
            actionDuration = Mathf.Max(duration, 0.01f);
            actionTimer = 0;
            actionSlowDownPortion = Mathf.Clamp01(slowDownPortion);

            // Путь = площадь под графиком скорости: v0 * T * (1 - k / 2), где k — доля торможения.
            // Отсюда v0 = d / (T * (1 - k / 2)). k = 0 — равномерно (v0 = d / T), k = 1 — тормозим с начала (v0 = 2d / T).
            actionStartSpeed = distance / (actionDuration * (1f - actionSlowDownPortion / 2f));

            rigidbody.linearVelocity = actionDirection * actionStartSpeed + Vector3.up * rigidbody.linearVelocity.y;
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
            // Вертикальная скорость сохраняется, чтобы игрок мог упасть, если под ним нет земли.
            if (actionTimer >= actionDuration)
            {
                rigidbody.linearVelocity = Vector3.up * rigidbody.linearVelocity.y;
                return;
            }

            float speed = actionStartSpeed;
            float slowDownStart = actionDuration * (1f - actionSlowDownPortion);

            if (actionTimer > slowDownStart)
                speed *= (actionDuration - actionTimer) / (actionDuration - slowDownStart);

            rigidbody.linearVelocity = actionDirection * speed + Vector3.up * rigidbody.linearVelocity.y;
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
                StartManualActionMovement(myTransform.forward, sprintStopDistance, sprintStopDuration, 1f);
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
