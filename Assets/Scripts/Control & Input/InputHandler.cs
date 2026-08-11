// InputHandler.cs
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
        public bool a_Input;
        public bool rb_Input;
        public bool rt_Input;
        public bool jump_Input;
        public bool inventory_Input;
        public bool lockOn_Input;
        public bool right_Stick_Left_Input;
        public bool right_Stick_Right_Input;

        public bool d_Pad_Up;
        public bool d_Pad_Down;
        public bool d_Pad_Left;
        public bool d_Pad_Right;

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

                inputActions.PlayerMovement.Camera.performed += ctx => cameraInput = ctx.ReadValue<Vector2>();
                inputActions.PlayerMovement.Camera.canceled += ctx => cameraInput = Vector2.zero;

                inputActions.PlayerActions.RB.performed += i => rb_Input = true;
                inputActions.PlayerActions.RT.performed += i => rt_Input = true;
                inputActions.PlayerActions.Interactable.performed += i => a_Input = true;

                inputActions.PlayerQuistSlots.DPadRight.performed += i => d_Pad_Right = true;
                inputActions.PlayerQuistSlots.DPadLeft.performed += i => d_Pad_Left = true;

                inputActions.PlayerActions.Jump.performed += i => jump_Input = true;
                inputActions.PlayerActions.Inventory.performed += i => { inventory_Input = true; Debug.Log("Inventory pressed"); };

                inputActions.PlayerActions.LockOn.performed += i => lockOn_Input = true;

                inputActions.PlayerMovement.LockOnTargetLeft.performed += i => right_Stick_Left_Input = true;
                inputActions.PlayerMovement.LockOnTargetRight.performed += i => right_Stick_Right_Input = true;
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

            // ИСПРАВЛЕНО: метод объявлен как HandleMoveInput, а вызывался
            // как MoveInput — несуществующее имя, ошибка компиляции.
            HandleMoveInput(delta);

            if (inventoryFlag)
            {
                rb_Input = false;
                rt_Input = false;
                jump_Input = false;
                a_Input = false;
                d_Pad_Left = false;
                d_Pad_Right = false;
                right_Stick_Left_Input = false;
                right_Stick_Right_Input = false;
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
            HandleQuackSlotsInput(delta);
            HandleLockOnInput();
        }

        private void HandleMoveInput(float delta)
        {
            horizontal = movementInput.x;
            vertical = movementInput.y;
            moveAmount = Mathf.Clamp01(Mathf.Abs(horizontal) + Mathf.Abs(vertical));

            mouseX = cameraInput.x;
            mouseY = cameraInput.y;
        }

        private void HandleRollInput(float delta)
        {
            b_Input = inputActions.PlayerActions.Roll.IsPressed();

            if (b_Input)
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
            if (rb_Input)
            {
                rb_Input = false;
                lightAttackBufferTimer = attackBufferWindow;
            }

            if (rt_Input)
            {
                rt_Input = false;
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

        private void HandleQuackSlotsInput(float delta)
        {
            if (d_Pad_Right)
            {
                d_Pad_Right = false;
                playerInventory.ChangeRightWeapon();
            }
            else if (d_Pad_Left)
            {
                d_Pad_Left = false;
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

            if (lockOnFlag && right_Stick_Left_Input)
            {
                right_Stick_Left_Input = false;
                cameraHandler.HandleLockOn();
                if (cameraHandler.leftLockOnTarget != null)
                {
                    cameraHandler.currentLockOnTarget = cameraHandler.leftLockOnTarget;
                }
            }

            if (lockOnFlag && right_Stick_Right_Input)
            {
                right_Stick_Right_Input = false;
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