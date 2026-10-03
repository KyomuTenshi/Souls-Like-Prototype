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
        public bool a_Input;
        public bool rb_Input;
        public bool rt_Input;
        public bool jump_Input;
        public bool inventory_Input;

        public bool d_Pad_Up;
        public bool d_Pad_Down;
        public bool d_Pad_Left;
        public bool d_Pad_Right;

        public bool rollFlag;
        public bool sprintFlag;
        public bool comboFlag;
        public bool inventoryFlag;
        public float rollInputTimer;

        [Header("Roll")]
        [Tooltip("Максимальная длительность нажатия, при которой выполняется перекат, с. Если кнопка удерживается дольше, выполняется спринт.")]
        public float rollTapTime = 0.25f;

        [Header("Camera")]
        [Tooltip("Скорость камеры со стика геймпада. Стик выдаёт не смещение за кадр, а отклонение, поэтому умножается на время.")]
        public float gamepadCameraSpeed = 150f;

        PlayerControls inputActions;
        PlayerAttacker playerAttacker;
        PlayerInventory playerInventory;
        PlayerManager playerManager;
        UIManager uiManager;

        Vector2 movementInput;
        Vector2 cameraInput;

        private void Awake()
        {
            playerAttacker = GetComponent<PlayerAttacker>();
            playerInventory = GetComponent<PlayerInventory>();
            playerManager = GetComponent<PlayerManager>();

            // UIManager висит не на игроке, а на объекте UI (Canvas), поэтому GetComponent вернул бы null.
            uiManager = FindFirstObjectByType<UIManager>();
        }

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

                // Подписки делаются один раз здесь (в туториале — в Handle*Input каждый кадр):
                // каждый += добавлял ещё один обработчик, и их число росло с каждым кадром.
                inputActions.PlayerActions.RB.performed += i => rb_Input = true;
                inputActions.PlayerActions.RT.performed += i => rt_Input = true;
                inputActions.PlayerActions.Interactable.performed += i => a_Input = true;
                inputActions.PlayerActions.Jump.performed += i => jump_Input = true;
                inputActions.PlayerActions.Inventory.performed += i => inventory_Input = true;
                inputActions.PlayerQuickSlots.DPadRight.performed += i => d_Pad_Right = true;
                inputActions.PlayerQuickSlots.DPadLeft.performed += i => d_Pad_Left = true;
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
            HandleAttackInput(delta);
            HandleQuickSlotsInput();
            HandleInventoryInput();
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

        private void HandleAttackInput(float delta)
        {
            if (rb_Input)
            {
                if (playerManager.canDoCombo)
                {
                    comboFlag = true;
                    playerAttacker.HandleWeaponCombo(playerInventory.rightWeapon);
                    comboFlag = false;
                }
                else
                {
                    if (playerManager.isInteracting)
                        return;

                    if (playerManager.canDoCombo)
                        return;
                    playerAttacker.HandleLightAttack(playerInventory.rightWeapon);
                }
            }

            if (rt_Input)
            {
                if (playerManager.canDoCombo)
                {
                    comboFlag = true;
                    playerAttacker.HandleWeaponCombo(playerInventory.rightWeapon);
                    comboFlag = false;
                }
                else
                {
                    if (playerManager.isInteracting)
                        return;

                    if (playerManager.canDoCombo)
                        return;
                    playerAttacker.HandleHeavyAttack(playerInventory.rightWeapon);
                }
            }
        }

        private void HandleQuickSlotsInput()
        {
            if (d_Pad_Right)
            {
                playerInventory.ChangeRightWeapon();
            }
            else if (d_Pad_Left)
            {
                playerInventory.ChangeLeftWeapon();
            }
        }

        private void HandleInventoryInput()
        {
            if (inventory_Input)
            {
                inventoryFlag = !inventoryFlag;

                if (uiManager != null)
                {
                    if (inventoryFlag)
                    {
                        uiManager.OpenSelectWindow();
                        uiManager.UpdateUI();
                        uiManager.hudWindow.SetActive(false);
                    }
                    else
                    {
                        uiManager.CloseSelectWindow();
                        uiManager.CloseAllInventoryWindows();
                        uiManager.hudWindow.SetActive(true);
                    }
                }
            }
        }
    }
}