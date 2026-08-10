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

        [SerializeField] private float rollInputThreshold = 0.5f;

        [Header("Attack Input Buffer")]
        // Окно буфера атак (сек). Нажатие атаки во время другой анимации
        // (ролл, приземление, чужая атака вне комбо-окна) раньше ПРОПАДАЛО:
        // rb_Input гасился, HandleLightAttack утыкался в isIntetacting — и
        // всё. Игрок жмёт кнопку чуть раньше времени -> ощущение "съеденного"
        // ввода. Теперь нажатие запоминается и исполняется, как только это
        // становится возможно (открылось комбо-окно / кончилась интеракция).
        // Это стандарт отзывчивого action-комбата (NieR/DMC/Souls так делают).
        [SerializeField] private float attackBufferWindow = 0.4f;
        float lightAttackBufferTimer;
        float heavyAttackBufferTimer;

        [Header("Roll Input Buffer (NieR dodge)")]
        // Буфер уклонения — той же природы, что буфер атак выше. Раньше тап
        // ролла во время атаки/приземления пропадал: rollFlag выставлялся на
        // один кадр, HandleRollingAndSprinting утыкался в isIntetacting, а
        // LateUpdate стирал флаг. Теперь нажатие живёт rollBufferWindow
        // секунд и держит rollFlag поднятым, пока PlayerLocomotion не сможет
        // его исполнить — в том числе КАНСЕЛОМ атаки (см. PlayerLocomotion).
        // Исполнитель гасит буфер через ConsumeRollBuffer().
        [SerializeField] private float rollBufferWindow = 0.35f;
        float rollBufferTimer;

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
            // Инвентарь обрабатываем ПЕРВЫМ (раньше был последним): открытие
            // меню должно заглушить боевой ввод в этом же кадре, а закрытие —
            // вернуть его без задержки в кадр.
            HandleInventoryInput();

            MoveInput(delta);

            // Пока открыто меню, геймплейный ввод глотаем: раньше E/R/Shift/
            // Space/X били, роллили и прыгали "за меню", а путь мыши к кнопке
            // крутил камеру (гейт камеры — в PlayerManager.LateUpdate).
            // Движение (WASD) оставляем — как в souls-играх, ходить с
            // открытым меню можно.
            if (inventoryFlag)
            {
                rb_Input = false;
                rt_Input = false;
                jump_Input = false;
                a_Input = false;
                d_Pad_Left = false;
                d_Pad_Right = false;
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
                    // БЫЛО: rollFlag = true напрямую (жил один кадр до
                    // LateUpdate). Теперь тап только взводит буфер —
                    // исполнение ниже.
                    rollBufferTimer = rollBufferWindow;
                }

                rollInputTimer = 0;
            }

            // Пока буфер жив, каждый кадр поднимаем rollFlag заново
            // (PlayerManager.LateUpdate его стирает — это ок, буфер
            // переживает стирание и попытка повторяется). Если ролл возможен
            // прямо сейчас, он выйдет в этот же кадр — прежнее мгновенное
            // поведение полностью сохранено.
            if (rollBufferTimer > 0f)
            {
                rollBufferTimer -= delta;
                rollFlag = true;
            }
        }

        // Зовёт PlayerLocomotion в момент, когда ролл реально принят к
        // исполнению: без этого буфер поднимал бы rollFlag ещё несколько
        // кадров после старта ролла.
        public void ConsumeRollBuffer()
        {
            rollBufferTimer = 0f;
            rollFlag = false;
        }

        private void HandleAttackInput(float delta)
        {
            // Нажатие только взводит таймер буфера — исполнение ниже.
            // Если персонаж свободен, атака выйдет в этот же кадр, т.е.
            // прежнее мгновенное поведение полностью сохраняется.
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

            // Не даём ДВУМ атакам выйти в один кадр: playerManager.isIntetacting
            // и canDoConbo кэшируются в начале кадра (PlayerManager.Update) и
            // не видят атаку, запущенную строчкой выше. Без guard'а RB+RT,
            // зажатые одновременно, запускали два CrossFade подряд — второй
            // молча перетирал первый. Теперь лёгкая имеет приоритет в этом
            // кадре, а тяжёлая остаётся в буфере и выйдет в свой момент.
            bool attackExecutedThisFrame = false;

            // Лёгкая атака / комбо. Комбо-окно проверяем первым: если оно
            // открыто, буферизованное нажатие продолжает цепочку.
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

            // Тяжёлая атака. НОВОЕ (Фаза 3): в комбо-окне RT больше не ждёт
            // конца всей цепочки, а ВЕТВИТ её тяжёлым финишером — строки
            // вида лёгкая-лёгкая-тяжёлая, как в NieR/DMC. Вне комбо-окна —
            // прежнее поведение: ждёт конца текущей интеракции.
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