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

            // БЫЛО: ветка else с двумя под-случаями, которые оба умножали на
            // movementSpeed (speed там всегда и был movementSpeed) — мёртвое
            // ветвление. Поведение после схлопывания идентично прежнему.
            float speed = movementSpeed;

            if (inputHandler.sprintFlag && inputHandler.moveAmount > 0.5f)
            {
                speed = sprintSpeed;
                playerManager.isSprinting = true;
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
                    }
                }
                else
                {
                    // BackStep: анимации пока нет в проекте. Включается одной
                    // строкой, когда клип появится в Animator Controller.
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

            Debug.DrawRay(origin, -Vector3.up * minimunDistanceNeededToBeginFall, Color.red, 0.1f, false);
            if (Physics.Raycast(origin, -Vector3.up, out hit, minimunDistanceNeededToBeginFall, ignoreForGroundCheck))
            {
                // hit.normal (нормаль поверхности), а не hit.point — ProjectOnPlane
                // в HandleMovement ждёт именно нормаль плоскости.
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
        #endregion
    }
}