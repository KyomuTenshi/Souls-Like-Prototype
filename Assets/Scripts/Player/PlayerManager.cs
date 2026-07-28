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

        private void Awake()
        {
            // Блокируем курсор в центре экрана для удобного управления мышью
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            // БЫЛО: тут стояла cameraHandler = FindObjectsByType<CameraHandler>();
            // Это не компилировалось (FindObjectsByType возвращает массив и требует
            // параметр FindObjectsSortMode) и было лишним — cameraHandler надёжно
            // выставляется ниже, в Start(), через CameraHandler.singleton (см.
            // комментарий там про порядок Awake()/Start()).
        }

        void Start()
        {
            // CameraHandler.singleton переносим сюда, в Start(), а не в Awake().
            // Unity не гарантирует порядок вызова Awake() между разными объектами:
            // если Awake() этого скрипта отрабатывал раньше, чем Awake() самого
            // CameraHandler (где singleton = this), то cameraHandler навсегда
            // оставался null, и блок с FollowTarget/HandleCameraRotation в
            // LateUpdate() молча пропускался — камера переставала следовать за
            // игроком, без единой ошибки в консоли.
            // Unity гарантирует другое: ВСЕ Awake() всех активных объектов сцены
            // отрабатывают раньше, чем ЛЮБОЙ Start(). Поэтому к моменту этого
            // Start() CameraHandler.Awake() уже железно выполнился, и singleton
            // точно заполнен.
            cameraHandler = CameraHandler.singleton;

            inputHandler = GetComponent<InputHandler>();
            anim = GetComponentInChildren<Animator>();
            playerLocomotion = GetComponent<PlayerLocomotion>();
        }

        void Update()
        {
            float delta = Time.deltaTime;
            isIntetacting = anim.GetBool("isInteracting");
            canDoConbo = anim.GetBool("canDoCombo");

            // rollFlag и sprintFlag пересчитываются заново каждый кадр внутри
            // InputHandler.TickInput() -> HandleRollInput() (по текущему состоянию
            // кнопки), причём TickInput вызывается прямо здесь, перед тем как эти
            // флаги читаются ниже. Поэтому не важно, в каком порядке Unity вызовет
            // Update() этого скрипта относительно других — свежее значение всё
            // равно выставлено до чтения.
            inputHandler.TickInput(delta);
            playerLocomotion.HandleMovement(delta);
            playerLocomotion.HandleRollingAndSprinting(delta);
            playerLocomotion.HandleFalling(delta, playerLocomotion.moveDirection);
        }

        private void LateUpdate()
        {
            float delta = Time.deltaTime;

            // Камера обрабатывается именно в LateUpdate (после того как игрок уже
            // переместился и повернулся в Update), чтобы избежать дерганий за
            // игроком.
            if (cameraHandler != null)
            {
                cameraHandler.FollowTarget(delta);
                cameraHandler.HandleCameraRotation(delta, inputHandler.mouseX, inputHandler.mouseY);
            }

            inputHandler.rollFlag = false;
            inputHandler.sprintFlag = false;
            inputHandler.rb_Input = false;
            inputHandler.rt_Input = false;

            if (isInAir)
            {
                playerLocomotion.inAirTimer = playerLocomotion.inAirTimer + Time.deltaTime;
            }
        }
    }
}