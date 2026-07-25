using UnityEngine;

namespace SG 
{
    public class PlayerLocomotion : MonoBehaviour
    {
        Transform cameraObject;
        InputHandler inputHandler;
        Vector3 moveDirection;

        [HideInInspector]
        public Transform myTransform;
        [HideInInspector]
        public AnimatorHandler animatorHandler;

        public Rigidbody rb; 
        public GameObject normalCamera;

        [Header("Stats")]
        [SerializeField] float movementSpeed = 5;
        [SerializeField] float rotationSpeed = 10;
        [SerializeField] float rollSpeed = 6;
        [SerializeField] float sprintSpeed = 7;

        public bool isSprinting;

        // Используется в AnimatorHandler.OnAnimatorMove() как запасной вариант,
        // когда у клипа анимации (например Roll) нет собственного смещения
        // вперёд (Average Velocity == 0), и двигать персонажа приходится вручную.
        public float RollSpeed { get { return rollSpeed; } }

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            inputHandler = GetComponent<InputHandler>();
            animatorHandler = GetComponentInChildren<AnimatorHandler>();
            cameraObject = Camera.main.transform;
            myTransform = transform;
            animatorHandler.Initialize();
        }

        public void Update()
        {
            float delta = Time.deltaTime;

            isSprinting = inputHandler.b_Input;
            inputHandler.TickInput(delta);
            HandleMovement(delta);
            HandleRollingAndSprinting(delta);
        }

        #region Movement
        Vector3 normalVector;
        Vector3 targetPosition;

        private void HandleRotation(float delta)
        {
            Vector3 targetDir = Vector3.zero;
            float moveOverride = inputHandler.moveAmount;

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
            
            moveDirection = cameraObject.forward * inputHandler.vertical;
            moveDirection += cameraObject.right * inputHandler.horizontal;
            moveDirection.Normalize();
            moveDirection.y = 0;

            float speed = movementSpeed;

            if (inputHandler.sprintFlag)
            {
                speed = sprintSpeed;
                isSprinting = true;
                moveDirection *= speed;
            } else
            {
                moveDirection *= speed;
            }

            Vector3 projectedVelocity = Vector3.ProjectOnPlane(moveDirection, normalVector);
            rb.linearVelocity = projectedVelocity;
            
            animatorHandler.UpdateAnimatorValues(inputHandler.moveAmount, 0, isSprinting);
            
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
                // Сбрасываем флаг сразу тут, в месте использования. Раньше это делал
                // PlayerManager.Update() безусловно каждый кадр — но порядок вызова
                // Update() между разными скриптами Unity не гарантирует, и если
                // PlayerManager успевал отработать раньше PlayerLocomotion, флаг
                // обнулялся до того, как Rolling вообще успевал его увидеть.
                inputHandler.rollFlag = false;

                moveDirection = cameraObject.forward * inputHandler.vertical;
                moveDirection += cameraObject.right * inputHandler.horizontal;

                if (inputHandler.moveAmount > 0)
                {
                    animatorHandler.PlayeTargetAnimation("Rolling", true);
                    moveDirection.y = 0;
                    Quaternion rollRotation = Quaternion.LookRotation(moveDirection);
                    myTransform.rotation = rollRotation;
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
        #endregion
    }
}