using UnityEngine;

namespace SG {
    public class PlayerManager : MonoBehaviour
    {
        InputHandler inputHandler;
        Animator anim;
        CameraHandler cameraHandler;
        PlayerLocomotion playerLocomotion;

        InteractableUI interactableUI;
        public GameObject interactableUIGameObject;
        public GameObject itemInteractableGameObject;

        public bool isInteracting;

        [Header("Player Flags")]
        public bool isSprinting;
        public bool isInAir;
        public bool isGrounded;
        public bool canDoCombo;

        [Header("Interaction")]
        [Tooltip("Радиус зоны поиска предметов перед персонажем, м")]
        public float interactCheckRadius = 0.6f;
        [Tooltip("Смещение зоны поиска вперёд от персонажа, м")]
        public float interactCheckForward = 0.5f;
        [Tooltip("Высота центра зоны поиска над точкой персонажа, м")]
        public float interactCheckHeight = 0.5f;

        public void Awake()
        {
            cameraHandler = FindObjectOfType<CameraHandler>();
        }
        void Start()
        {
            // Синглтон берётся в Start: в Awake CameraHandler мог ещё не инициализироваться.
            cameraHandler = CameraHandler.singleton;
            inputHandler = GetComponent<InputHandler>();
            anim = GetComponentInChildren<Animator>();
            playerLocomotion = GetComponent<PlayerLocomotion>();
            interactableUI = FindFirstObjectByType<InteractableUI>();
        }

        void Update()
        {
            float delta = Time.deltaTime;

            isInteracting = anim.GetBool("isInteracting");
            canDoCombo = anim.GetBool("canDoCombo");

            // Спринт учитывается только при движении: удержание кнопки на месте не должно включать анимацию спринта.
            isSprinting = inputHandler.b_input && inputHandler.moveAmount > 0;
            inputHandler.TickInput(delta);
            playerLocomotion.HandleMovement(delta);
            playerLocomotion.HandleRollingAndSprinting(delta);
            playerLocomotion.HandleFalling(delta, playerLocomotion.moveDirection);

            CheckForInteractableObject();
        }

        // Камера обновляется в LateUpdate (в туториале — FixedUpdate): каждый кадр и после перемещения персонажа.
        // В FixedUpdate часть движения мыши терялась, и камера ощущалась тугой.
        private void LateUpdate()
        {
            float delta = Time.deltaTime;

            if (cameraHandler != null)
            {
                cameraHandler.FollowTarget(delta);
                cameraHandler.HandleCameraRotation(delta, inputHandler.mouseX, inputHandler.mouseY);
            }

            inputHandler.rollFlag = false;
            inputHandler.sprintFlag = false;
            inputHandler.rb_Input = false;
            inputHandler.rt_Input = false;
            inputHandler.d_Pad_Up = false;
            inputHandler.d_Pad_Down = false;
            inputHandler.d_Pad_Left = false;
            inputHandler.d_Pad_Right = false;
            inputHandler.a_Input = false;

            if (isInAir)
            {
                playerLocomotion.inAirTimer = playerLocomotion.inAirTimer + Time.deltaTime;
            }
        }

        // Проверка через OverlapSphere (в туториале — SphereCast): SphereCast не находит коллайдеры,
        // внутри которых начинается, поэтому предмет терялся, если подойти к нему вплотную.
        public void CheckForInteractableObject()
        {
            Vector3 checkPosition = transform.position + Vector3.up * interactCheckHeight + transform.forward * interactCheckForward;
            Collider[] colliders = Physics.OverlapSphere(checkPosition, interactCheckRadius, cameraHandler.ignoreLayers, QueryTriggerInteraction.Collide);

            foreach (Collider collider in colliders)
            {
                if (!collider.CompareTag("Interactable"))
                    continue;

                Interactable interactableObject = collider.GetComponent<Interactable>();

                if (interactableObject != null)
                {
                    string interactableText = interactableObject.interactbleText;
                    interactableUI.interavtableText.text = interactableText;
                    interactableUIGameObject.SetActive(true);

                    if (inputHandler.a_Input && !isInteracting)
                    {
                        interactableObject.Interact(this);
                    }

                    return;
                }
            }

            if (interactableUIGameObject != null)
            {
                interactableUIGameObject.SetActive(false);
            }

            if (itemInteractableGameObject != null && inputHandler.a_Input)
            {
                itemInteractableGameObject.SetActive(false);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * interactCheckHeight + transform.forward * interactCheckForward, interactCheckRadius);
        }
    }
}