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
        public bool isInteracting;

        [Header("Roll")]
        [Tooltip("Максимальная длительность нажатия, при которой выполняется перекат, с. Если кнопка удерживается дольше, выполняется спринт.")]
        public float rollTapTime = 0.25f;

        PlayerControls inputActions;
        CameraHandler cameraHandler;

        Vector2 movementInput;
        Vector2 cameraInput;

        private void Awake()
        {
            cameraHandler = CameraHandler.singleton;
        }

        // Камера обновляется в LateUpdate (в туториале — FixedUpdate), после перемещения персонажа,
        // чтобы исключить дрожание изображения.
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

                // Подписки на canceled сбрасывают ввод при отпускании: performed с нулевым значением не приходит,
                // и без сброса персонаж и камера продолжали бы движение.
                inputActions.PlayerMovement.Movement.performed += i => movementInput = i.ReadValue<Vector2>();
                inputActions.PlayerMovement.Movement.canceled += i => movementInput = Vector2.zero;

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