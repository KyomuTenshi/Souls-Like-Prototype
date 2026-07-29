using UnityEngine;

namespace SG {
    public class PlayerManager : MonoBehaviour
    {
        InputHandler inputHandler;
        Animator anim;
        CameraHandler cameraHandler;
        PlayerLocomotion playerLocomotion;

        public bool isIntetacting;

        [Header("Player Flags")]
        public bool isSprinting;
        public bool isInAir;
        public bool isGrounded;
        public bool canDoConbo;

        // Кэш хэшей параметров Animator: GetBool(string) хэширует строку при
        // каждом вызове, а эти два читаются каждый кадр.
        static readonly int IsInteractingHash = Animator.StringToHash("isInteracting");
        static readonly int CanDoComboHash = Animator.StringToHash("canDoCombo");

        private void Awake()
        {
            // Блокируем курсор в центре экрана для удобного управления мышью
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Свои компоненты берём в Awake (конвенция Unity: своё — в Awake,
            // зависящее от чужих Awake — в Start). Это ещё и надёжнее: другие
            // скрипты могут обратиться к этим ссылкам уже в своих Start().
            inputHandler = GetComponent<InputHandler>();
            anim = GetComponentInChildren<Animator>();
            playerLocomotion = GetComponent<PlayerLocomotion>();
        }

        void Start()
        {
            // CameraHandler.singleton читаем в Start(), а не в Awake(): Unity не
            // гарантирует порядок Awake() между объектами, но гарантирует, что
            // ВСЕ Awake() отработают раньше ЛЮБОГО Start(). К этому моменту
            // singleton заполнен железно.
            cameraHandler = CameraHandler.singleton;
        }

        void Update()
        {
            float delta = Time.deltaTime;
            isIntetacting = anim.GetBool(IsInteractingHash);
            canDoConbo = anim.GetBool(CanDoComboHash);

            // TickInput вызывается здесь, ПЕРЕД чтением флагов ниже — свежее
            // значение выставлено до использования вне зависимости от порядка
            // Update() между скриптами.
            inputHandler.TickInput(delta);
            playerLocomotion.HandleMovement(delta);
            playerLocomotion.HandleRollingAndSprinting(delta);
            playerLocomotion.HandleFalling(delta, playerLocomotion.moveDirection);
        }

        private void LateUpdate()
        {
            float delta = Time.deltaTime;

            // Камера — в LateUpdate, после перемещения игрока в Update.
            if (cameraHandler != null)
            {
                cameraHandler.FollowTarget(delta);
                cameraHandler.HandleCameraRotation(delta, inputHandler.mouseX, inputHandler.mouseY);
            }

            // Страховочная сетка: one-shot флаги уже гасятся в местах
            // потребления (InputHandler), но сброс здесь безвреден и ловит
            // любой флаг, взведённый колбэком в "мёртвой зоне" кадра.
            inputHandler.rollFlag = false;
            inputHandler.sprintFlag = false;
            inputHandler.rb_Input = false;
            inputHandler.rt_Input = false;
            inputHandler.d_Pad_Up = false;
            inputHandler.d_Pad_Down = false;
            inputHandler.d_Pad_Right = false;
            inputHandler.d_Pad_Left = false;

            if (isInAir)
            {
                playerLocomotion.inAirTimer = playerLocomotion.inAirTimer + Time.deltaTime;
            }
        }
    }
}