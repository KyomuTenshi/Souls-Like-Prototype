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

        public bool d_Pad_Up;
        public bool d_Pad_Down;
        public bool d_Pad_Left;
        public bool d_Pad_Right;

        public bool rollFlag;
        public bool sprintFlag;
        public bool comboFlag;
        public bool inventoryFlag;
        public float rollInputTimer;
        public bool isIntetacting;

        [SerializeField] private float rollInputThreshold = 0.5f;

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

            // FindFirstObjectByType вместо устаревшего FindObjectOfType —
            // тот же смысл, без obsolete-warning (см. PlayerStats/PlayerManager).
            uiManager = FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
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

                // Все one-shot подписки — строго ОДИН РАЗ здесь, в OnEnable.
                // Подписка внутри Tick-методов (вызываются каждый кадр из
                // PlayerManager.Update) добавляла бы нового подписчика каждый
                // кадр — та же ошибка, что уже была с RB/RT/D-Pad, и в неё же
                // наступила подписка на Inventory, которая раньше стояла
                // внутри HandleInventoryInput().
                inputActions.PlayerActions.RB.performed += i => rb_Input = true;
                inputActions.PlayerActions.RT.performed += i => rt_Input = true;
                inputActions.PlayerActions.Interactable.performed += i => a_Input = true;

                inputActions.PlayerQuistSlots.DPadRight.performed += i => d_Pad_Right = true;
                inputActions.PlayerQuistSlots.DPadLeft.performed += i => d_Pad_Left = true;

                inputActions.PlayerActions.Jump.performed += i => jump_Input = true;
                inputActions.PlayerActions.Inventory.performed += i => { inventory_Input = true; Debug.Log("Inventory pressed"); };
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
            HandleQuackSlotsInput(delta);
            HandleInventoryInput();
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
                    rollFlag = true;
                }

                rollInputTimer = 0;
            }
        }

        private void HandleAttackInput(float delta)
        {
            if (rb_Input)
            {
                rb_Input = false;

                if (playerManager.canDoConbo)
                {
                    comboFlag = true;
                    playerAttacker.HandleWeaponCombo(playerInventory.rightWeapon);
                    comboFlag = false;
                }
                else
                {
                    playerAttacker.HandleLightAttack(playerInventory.rightWeapon);
                }
            }

            if (rt_Input)
            {
                rt_Input = false;
                playerAttacker.HandleHeavytAttack(playerInventory.rightWeapon);
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
            // Флаг гасим сразу же в месте использования — как и rb_Input/
            // rt_Input/jump_Input — иначе он останется true и откроет/закроет
            // окно повторно в следующем кадре без нового нажатия.
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
    }
}