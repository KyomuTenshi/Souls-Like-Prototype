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
        public bool sprintFlag;
        public float rollInputTimer;

        [Header("Roll")]
        [Tooltip("Максимальная длительность нажатия, при которой выполняется перекат, с. Если кнопка удерживается дольше, выполняется спринт.")]
        public float rollTapTime = 0.25f;

        [Header("Camera")]
        [Tooltip("Скорость камеры со стика геймпада. Стик выдаёт не смещение за кадр, а отклонение, поэтому умножается на время.")]
        public float gamepadCameraSpeed = 150f;

        PlayerControls inputActions;

        Vector2 movementInput;
        Vector2 cameraInput;

        public void OnEnable()
        {
            if (inputActions == null)
            {
                inputActions = new PlayerControls();

                // Подписки на canceled сбрасывают ввод при отпускании: performed с нулевым значением не приходит,
                // и без сброса персонаж продолжал бы движение.
                inputActions.PlayerMovement.Movement.performed += i => movementInput = i.ReadValue<Vector2>();
                inputActions.PlayerMovement.Movement.canceled += i => movementInput = Vector2.zero;

                // Камера читается напрямую в MoveInput (в туториале — через performed):
                // колбэк сохранял только последнее событие мыши за кадр.
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

            InputAction cameraAction = inputActions.PlayerMovement.Camera;
            cameraInput = cameraAction.ReadValue<Vector2>();

            if (cameraAction.activeControl != null && cameraAction.activeControl.device is Gamepad)
            {
                cameraInput *= gamepadCameraSpeed * delta;
            }

            mouseX = cameraInput.x;
            mouseY = cameraInput.y;
        }

        private void HandleRollInput(float delta)
        {
            // В актуальной версии Input System фаза Button-действия сразу переходит в Performed,
            // поэтому проверка phase == Started из туториала ненадёжна. IsPressed() возвращает true,
            // пока кнопка удерживается.
            b_input = inputActions.PlayerActions.Roll.IsPressed();

            if (b_input)
            {
                rollInputTimer += delta;
                sprintFlag = true;
            }
            else
            {
                if (rollInputTimer > 0 && rollInputTimer < rollTapTime)
                {
                    sprintFlag = false;
                    rollFlag = true;
                }

                rollInputTimer = 0;
            }
        }
    }
}