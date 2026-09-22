using UnityEngine;

namespace SG {
    public class PlayerLocomotion : MonoBehaviour
    {
        Transform cameraObject;
        InputHandler inputHandler;
        Vector3 moveDirection;

        [HideInInspector]
        public Transform myTransform;
        [HideInInspector]
        public AnimatorHandler animatorHandler;

        public new Rigidbody rigidbody;
        public GameObject normalCamera;

        [Header("Stats")]
        [SerializeField]
        float movementSpeed = 5;
        [SerializeField]
        float rotationSpeed = 10;

        [Header("Roll (используется, если у анимации нет root motion)")]
        [SerializeField]
        float rollSpeed = 6f;      // скорость смещения во время ролла
        public bool useManualRollMovement = true; // должен быть public/виден из AnimatorHandler.OnAnimatorMove

        Vector3 normalVector;
        Vector3 targetPosition;
        Vector3 rollDirection; // направление, зафиксированное в момент старта ролла

        void Start()
        {
            rigidbody = GetComponent<Rigidbody>();
            inputHandler = GetComponent<InputHandler>();
            animatorHandler = GetComponentInChildren<AnimatorHandler>();
            cameraObject = Camera.main.transform;
            myTransform = transform;
            animatorHandler.Initialize();

            normalVector = Vector3.up;
            animatorHandler.canRotate = true;
        }

        public void Update()
        {
            float delta = Time.deltaTime;

            inputHandler.TickInput(delta);
            HandleMovement(delta);
            HandleRollingAndSprinting(delta);
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
            animatorHandler.UpdateAnimatorValues(inputHandler.moveAmount, 0);

            if (animatorHandler.anim.GetBool("isInteracting"))
                return;

            moveDirection = cameraObject.forward * inputHandler.vertical;
            moveDirection += cameraObject.right * inputHandler.horizontal;

            moveDirection.y = 0;
            moveDirection.Normalize();

            float speed = movementSpeed;
            moveDirection *= speed;

            Vector3 projectedVelocity = Vector3.ProjectOnPlane(moveDirection, normalVector);
            rigidbody.linearVelocity = projectedVelocity;

            if (animatorHandler.canRotate)
            {
                HandleRotation(delta);
            }
        }

        public void HandleRollingAndSprinting(float delta)
        {
            if (animatorHandler.anim.GetBool("isInteracting"))
            {
                if (useManualRollMovement && rollDirection != Vector3.zero)
                {
                    rigidbody.linearVelocity = rollDirection * rollSpeed;
                }
                return;
            }

            if (inputHandler.rollFlag)
            {
                moveDirection = cameraObject.forward * inputHandler.vertical;
                moveDirection += cameraObject.right * inputHandler.horizontal;
                moveDirection.y = 0;

                if (inputHandler.moveAmount > 0)
                {
                    moveDirection.Normalize();
                    rollDirection = moveDirection; // фиксируем направление на весь ролл

                    animatorHandler.PlayTargetAnimation("Rolling", true);
                    Quaternion rollRotation = Quaternion.LookRotation(moveDirection);
                    myTransform.rotation = rollRotation;
                }
                else
                {
                    rollDirection = myTransform.forward * -1; // Backstep — назад
                    animatorHandler.PlayTargetAnimation("Backstep", true);
                }
            }
        }
        #endregion
    }
}