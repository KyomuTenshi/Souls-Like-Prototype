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
        public bool rb_Input;
        public bool rt_Input;
        public bool d_Pad_Up;
        public bool d_Pad_Down;
        public bool d_Pad_Left;
        public bool d_Pad_Right;

        public bool rollFlag;
        public bool sprintFlag;
        public bool comboFlag;
        public float rollInputTimer;
        public bool isIntetacting;

        [SerializeField] private float rollInputThreshold = 0.5f;

        PlayerControls inputActions;
        PlayerAttacker playerAttacker;
        PlayerInventory playerInventory;
        PlayerManager playerManager;

        Vector2 movementInput;
        Vector2 cameraInput;

        private void Awake()
        {
            playerAttacker = GetComponent<PlayerAttacker>();
            playerInventory = GetComponent<PlayerInventory>();
            playerManager = GetComponent<PlayerManager>();
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

                // Подписки строго ОДИН РАЗ здесь, в OnEnable (см. историю бага
                // с размножением подписчиков при подписке внутри TickInput).
                inputActions.PlayerActions.RB.performed += i => rb_Input = true;
                inputActions.PlayerActions.RT.performed += i => rt_Input = true;

                inputActions.PlayerQuistSlots.DPadRight.performed += i => d_Pad_Right = true;
                inputActions.PlayerQuistSlots.DPadLeft.performed += i => d_Pad_Left = true;
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
            // Флаги читаются и гасятся сразу в месте использования — атака не
            // может сработать дважды в одном "живом" окне кадра.
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
                    // Проверка isIntetacting выполняется внутри HandleLightAttack —
                    // дублировать её здесь ранним return нельзя: он заодно
                    // пропускал обработку rt_Input ниже, и нажатие тяжёлой
                    // атаки в этом кадре терялось.
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
            // Гасим флаг сразу здесь, как rb_Input/rt_Input выше, а не ждём
            // PlayerManager.LateUpdate() — иначе одно нажатие могло бы
            // обработаться дважды при изменении порядка Update'ов.
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
    }
}