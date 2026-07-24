using UnityEngine;

namespace SG 
{
    public class InputHandler : MonoBehaviour
    {
        public float horizontal;
        public float vertical;
        public float moveAmount;
        public float mouseX;
        public float mouseY;

        public bool b_Input;

        public bool rollFlag;
        public bool isIntetacting;

        PlayerControls inputActions;
        CameraHandler cameraHandler;

        Vector2 movementInput;
        Vector2 cameraInput;

        private void Start()
        {
            cameraHandler = CameraHandler.singleton;
            
            // Блокируем курсор в центре экрана для удобного управления мыгой
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            float delta = Time.deltaTime;
            TickInput(delta);
        }

        private void LateUpdate()
        {
            float delta = Time.deltaTime;

            // Вся камера обрабатывается в LateUpdate, чтобы избежать дерганий за игроком
            if (cameraHandler != null)
            {
                cameraHandler.FollowTarget(delta);
                cameraHandler.HandleCameraRotation(delta, mouseX, mouseY);
            }
        }

        public void OnEnable()
        {
            if (inputActions == null)
            {
                inputActions = new PlayerControls();
                
                inputActions.PlayerMovement.Movement.performed += inputActions => movementInput = inputActions.ReadValue<Vector2>();
                inputActions.PlayerMovement.Movement.canceled += inputActions => movementInput = Vector2.zero;

                inputActions.PlayerMovement.Camera.performed += i => cameraInput = i.ReadValue<Vector2>();
                inputActions.PlayerMovement.Camera.canceled += i => cameraInput = Vector2.zero;

                inputActions.PlayerActions.Roll.performed += i => rollFlag = true;
            }

            inputActions.Enable();
        }

        private void OnDisable()
        {
            inputActions.Disable();
        }

        public void TickInput(float delta)
        {
            MoveInput(delta);
        }

        private void MoveInput(float delta)
        {
            horizontal = movementInput.x;
            vertical = movementInput.y;
            moveAmount = Mathf.Clamp01(Mathf.Abs(horizontal) + Mathf.Abs(vertical));
            
            mouseX = cameraInput.x;
            mouseY = cameraInput.y;
        }

        // Раньше здесь опрашивался inputActions.PlayerActions.Roll.phase == InputActionPhase.Started,
        // но для Button-действия без interactions фаза почти сразу переходит в Performed,
        // поэтому Started ловился ненадёжно. Теперь rollFlag ставится через событие Roll.performed
        // в OnEnable(). Метод оставлен на случай, если понадобится позже.
        private void HandleRollInput(float delta)
        {
        }
    }
}