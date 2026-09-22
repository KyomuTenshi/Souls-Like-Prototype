using UnityEngine;
using UnityEngine.InputSystem;

namespace SG {
    public class InputHandler : MonoBehaviour
    {
        public float horizontal;
        public float vertical;
        public float moveAmount;
        public float mouseX;
        public float mouseY;

        public bool b_input;

        public bool rollFlag;
        public bool isInteracting;

        PlayerControls inputActions;
        CameraHandler cameraHandler;

        Vector2 movementInput;
        Vector2 cameraInput;

        private void Awake()
        {
            cameraHandler = CameraHandler.singleton;
        }

        private void LateUpdate()
        {
            float delta = Time.deltaTime;

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

                // Считываем значение при нажатии/удержании
                inputActions.PlayerMovement.Movement.performed += i => movementInput = i.ReadValue<Vector2>();
                // Сбрасываем значение при отпускании клавиши
                inputActions.PlayerMovement.Movement.canceled += i => movementInput = Vector2.zero;

                // То же самое для камеры
                inputActions.PlayerMovement.Camera.performed += i => cameraInput = i.ReadValue<Vector2>();
                inputActions.PlayerMovement.Camera.canceled += i => cameraInput = Vector2.zero;
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
            HandleRollInput(delta);
        }

        private void MoveInput(float delta)
        {
            horizontal = movementInput.x;
            vertical = movementInput.y;
            moveAmount = Mathf.Clamp01(Mathf.Abs(horizontal) + Mathf.Abs(vertical));
            mouseX = cameraInput.x;
            mouseY = cameraInput.y;
        }

        private void HandleRollInput(float delta)
        {
            b_input = inputActions.PlayerActions.Roll.WasPressedThisFrame();

            if (b_input)
            {
                rollFlag = true;
            }
        }
    }
}