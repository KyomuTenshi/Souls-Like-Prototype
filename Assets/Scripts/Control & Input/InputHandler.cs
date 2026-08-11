// InputHandler.cs
using UnityEngine;
using UnityEngine.InputSystem;

namespace SG 
{
    public class InputHandler : MonoBehaviour
    {
        public float horizontal;
        public float vertical;
        public float moveAmount;
        public float mouseX;
        public float mouseY;

        public bool dodge_Input;
        public bool lightAttack_Input;
        public bool heavyAttack_Input;
        public bool interact_Input;
        public bool jump_Input;
        public bool inventory_Input;
        public bool lockOn_Input;
        public bool lockOnLeft_Input;
        public bool lockOnRight_Input;
        public bool quickSlotLeft_Input;
        public bool quickSlotRight_Input;

        public bool rollFlag;
        public bool sprintFlag;
        public bool comboFlag;
        public bool lockOnFlag;
        public bool inventoryFlag;
        public float rollInputTimer;

        [SerializeField] private float rollInputThreshold = 0.5f;

        [Header("Attack Input Buffer")]
        [SerializeField] private float attackBufferWindow = 0.4f;
        float lightAttackBufferTimer;
        float heavyAttackBufferTimer;

        [Header("Roll Input Buffer (NieR dodge)")]
        [SerializeField] private float rollBufferWindow = 0.35f;
        float rollBufferTimer;

        PlayerControls inputActions;
        PlayerAttacker playerAttacker;
        PlayerInventory playerInventory;
        PlayerManager playerManager;
        CameraHandler cameraHandler;
        UIManager uiManager;

        Vector2 movementInput;
        Vector2 cameraInput;
        // Камера получает ввод из двух источников с разной природой:
        // mouse delta — это смещение ЗА КАДР (fps-независимо само по себе),
        // стик — удерживаемое значение, которое прибавляется каждый кадр и
        // потому зависит от fps. Флаг позволяет нормализовать только стик.
        bool cameraInputIsAnalog;

        private void Awake()
        {
            playerAttacker = GetComponent<PlayerAttacker>();
            playerInventory = GetComponent<PlayerInventory>();
            playerManager = GetComponent<PlayerManager>();

            uiManager = FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
            cameraHandler = FindFirstObjectByType<CameraHandler>();
        }

        public void OnEnable()
        {
            if (inputActions == null)
            {
                inputActions = new PlayerControls();

                inputActions.PlayerMovement.Movement.performed += ctx => movementInput = ctx.ReadValue<Vector2>();
                inputActions.PlayerMovement.Movement.canceled += ctx => movementInput = Vector2.zero;

                inputActions.PlayerMovement.Camera.performed += ctx =>
                {
                    cameraInput = ctx.ReadValue<Vector2>();
                    cameraInputIsAnalog = ctx.control.device is Gamepad;
                };
                inputActions.PlayerMovement.Camera.canceled += ctx => cameraInput = Vector2.zero;

                inputActions.PlayerActions.LightAttack.performed += i => lightAttack_Input = true;
                inputActions.PlayerActions.HeavyAttack.performed += i => heavyAttack_Input = true;
                inputActions.PlayerActions.Interact.performed += i => interact_Input = true;

                inputActions.PlayerQuickSlots.QuickSlotRight.performed += i => quickSlotRight_Input = true;
                inputActions.PlayerQuickSlots.QuickSlotLeft.performed += i => quickSlotLeft_Input = true;

                inputActions.PlayerActions.Jump.performed += i => jump_Input = true;
                inputActions.PlayerActions.Inventory.performed += i => inventory_Input = true;

                inputActions.PlayerActions.LockOn.performed += i => lockOn_Input = true;

                inputActions.PlayerMovement.LockOnTargetLeft.performed += i => lockOnLeft_Input = true;
                inputActions.PlayerMovement.LockOnTargetRight.performed += i => lockOnRight_Input = true;
            }

            inputActions.Enable();
        }

        private void OnDisable()
        {
            inputActions.Disable();
        }

        public void TickInput(float delta)
        {
            HandleInventoryInput();
            HandleMoveInput(delta);

            if (inventoryFlag)
            {
                lightAttack_Input = false;
                heavyAttack_Input = false;
                jump_Input = false;
                interact_Input = false;
                quickSlotLeft_Input = false;
                quickSlotRight_Input = false;
                lockOnLeft_Input = false;
                lockOnRight_Input = false;
                // Без сброса нажатие lock-on при открытом меню "запоминалось"
                // и срабатывало сразу после его закрытия.
                lockOn_Input = false;
                lightAttackBufferTimer = 0f;
                heavyAttackBufferTimer = 0f;
                rollBufferTimer = 0f;
                rollInputTimer = 0f;
                rollFlag = false;
                sprintFlag = false;
                return;
            }

            HandleRollInput(delta);
            HandleAttackInput(delta);
            HandleQuickSlotsInput(delta);
            HandleLockOnInput();
        }

        private void HandleMoveInput(float delta)
        {
            horizontal = movementInput.x;
            vertical = movementInput.y;
            moveAmount = Mathf.Clamp01(Mathf.Abs(horizontal) + Mathf.Abs(vertical));

            // Стик нормализуем к эталонным 60 fps: CameraHandler прибавляет
            // ввод к углу каждый кадр без deltaTime (для mouse delta это
            // корректно), поэтому удерживаемый стик без нормализации крутил
            // бы камеру тем быстрее, чем выше fps. Множители ScaleVector2
            // в ассете (x=20, y=12) подобраны под 60 fps и остаются верными.
            float cameraScale = cameraInputIsAnalog ? delta * 60f : 1f;
            mouseX = cameraInput.x * cameraScale;
            mouseY = cameraInput.y * cameraScale;
        }

        private void HandleRollInput(float delta)
        {
            dodge_Input = inputActions.PlayerActions.Dodge.IsPressed();

            if (dodge_Input)
            {
                rollInputTimer += delta;
                sprintFlag = true;
            }
            else
            {
                if (rollInputTimer > 0 && rollInputTimer < rollInputThreshold)
                {
                    sprintFlag = false;
                    rollBufferTimer = rollBufferWindow;
                }

                rollInputTimer = 0;
            }

            if (rollBufferTimer > 0f)
            {
                rollBufferTimer -= delta;
                rollFlag = true;
            }
        }

        public void ConsumeRollBuffer()
        {
            rollBufferTimer = 0f;
            rollFlag = false;
        }

        private void HandleAttackInput(float delta)
        {
            if (lightAttack_Input)
            {
                lightAttack_Input = false;
                lightAttackBufferTimer = attackBufferWindow;
            }

            if (heavyAttack_Input)
            {
                heavyAttack_Input = false;
                heavyAttackBufferTimer = attackBufferWindow;
            }

            bool attackExecutedThisFrame = false;

            if (lightAttackBufferTimer > 0f)
            {
                lightAttackBufferTimer -= delta;

                if (playerManager.canDoConbo)
                {
                    comboFlag = true;
                    playerAttacker.HandleWeaponCombo(playerInventory.rightWeapon);
                    comboFlag = false;
                    lightAttackBufferTimer = 0f;
                    attackExecutedThisFrame = true;
                }
                else if (!playerManager.isIntetacting)
                {
                    playerAttacker.HandleLightAttack(playerInventory.rightWeapon);
                    lightAttackBufferTimer = 0f;
                    attackExecutedThisFrame = true;
                }
            }

            if (heavyAttackBufferTimer > 0f)
            {
                heavyAttackBufferTimer -= delta;

                if (!attackExecutedThisFrame)
                {
                    if (playerManager.canDoConbo)
                    {
                        comboFlag = true;
                        playerAttacker.HandleHeavyComboFinisher(playerInventory.rightWeapon);
                        comboFlag = false;
                        heavyAttackBufferTimer = 0f;
                    }
                    else if (!playerManager.isIntetacting)
                    {
                        playerAttacker.HandleHeavytAttack(playerInventory.rightWeapon);
                        heavyAttackBufferTimer = 0f;
                    }
                }
            }
        }

        private void HandleQuickSlotsInput(float delta)
        {
            if (quickSlotRight_Input)
            {
                quickSlotRight_Input = false;
                playerInventory.ChangeRightWeapon();
            }
            else if (quickSlotLeft_Input)
            {
                quickSlotLeft_Input = false;
                playerInventory.ChangeLeftWeapon();
            }
        }

        private void HandleInventoryInput()
        {
            if (inventory_Input)
            {
                inventory_Input = false;
                inventoryFlag = !inventoryFlag;

                if (uiManager == null)
                {
                    Debug.LogWarning("InputHandler: UIManager не найден в сцене — окно инвентаря не откроется.");
                    return;
                }

                if (inventoryFlag)
                {
                    uiManager.OpenSelectWindow();
                }
                else
                {
                    uiManager.CloseSelectWindow();
                }
            }
        }

        private void HandleLockOnInput()
        {
            if (cameraHandler == null)
                return;

            if (lockOn_Input && lockOnFlag == false)
            {
                lockOn_Input = false;
                cameraHandler.HandleLockOn();
                if (cameraHandler.nearestLockOnTarget != null)
                {
                    cameraHandler.currentLockOnTarget = cameraHandler.nearestLockOnTarget;
                    lockOnFlag = true;
                }
            }
            else if (lockOn_Input && lockOnFlag)
            {
                lockOn_Input = false;
                lockOnFlag = false;
                cameraHandler.ClearLockOnTargets();
            }

            if (lockOnFlag && lockOnLeft_Input)
            {
                lockOnLeft_Input = false;
                cameraHandler.HandleLockOn();
                if (cameraHandler.leftLockOnTarget != null)
                {
                    cameraHandler.currentLockOnTarget = cameraHandler.leftLockOnTarget;
                }
            }

            if (lockOnFlag && lockOnRight_Input)
            {
                lockOnRight_Input = false;
                cameraHandler.HandleLockOn();
                if (cameraHandler.rightLockOnTarget != null)
                {
                    cameraHandler.currentLockOnTarget = cameraHandler.rightLockOnTarget;
                }
            }

            cameraHandler.SetCameraHeight();
        }
    }
}