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

                // БЫЛО: эти две подписки жили внутри HandleAttackInput(), которая
                // вызывается каждый кадр из TickInput(). Каждый Update() добавлял
                // ЕЩЁ ОДИН обработчик на "RB.performed" поверх уже висящих —
                // подписчики множились без остановки, и одно нажатие RB спустя
                // время вызывало HandleLightAttack() не один раз, а столько раз,
                // сколько накопилось подписок (отсюда сгруппированные по 6-7
                // повторов одинаковые ошибки CrossFade в консоли).
                // Подписываемся РОВНО ОДИН РАЗ здесь, в OnEnable, как и на
                // остальные действия выше.
                inputActions.PlayerActions.RB.performed += i => rb_Input = true;

                // БЫЛО: вторая строка тоже слушала RB (copy-paste) — из-за этого
                // ЛЮБОЕ нажатие RB запускало одновременно и лёгкую, и тяжёлую
                // атаку. Тяжёлая атака должна триггериться отдельной кнопкой RT.
                inputActions.PlayerActions.RT.performed += i => rt_Input = true;
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
            // Флаги теперь только ЧИТАЮТСЯ и сразу гасятся тут же, в месте
            // использования (как rollFlag в HandleRollingAndSprinting) — не
            // дожидаясь общего сброса в PlayerManager.LateUpdate(). Это не даёт
            // атаке повторно сработать в течение того же "живого" окна кадра.
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
                    if (playerManager.isIntetacting)
                        return;
                    
                    if (playerManager.canDoConbo)
                        return;
                        
                    playerAttacker.HandleLightAttack(playerInventory.rightWeapon);
                }
            }

            if (rt_Input)
            {
                rt_Input = false;
                playerAttacker.HandleHeavytAttack(playerInventory.rightWeapon);
            }
        }
    }
}