using System.Collections;
using UnityEngine;

namespace SG {
    public class PlayerManager : CharacterManager
    {
        InputHandler inputHandler;
        Animator anim;
        CameraHandler cameraHandler;
        PlayerLocomotion playerLocomotion;
        InteractableUI interactableUI;
        public GameObject interactableUIGameObject;
        public GameObject itemInteractableGameObject;

        [SerializeField] private float itemNotificationDuration = 2f;
        Coroutine itemNotificationCoroutine;

        public bool isIntetacting;

        [Header("Player Flags")]
        public bool isSprinting;
        public bool isInAir;
        public bool isGrounded;
        public bool canDoConbo;

        static readonly int IsInteractingHash = Animator.StringToHash("isInteracting");
        static readonly int CanDoComboHash = Animator.StringToHash("canDoCombo");
        // Параметр в Animator должен называться ровно "IsInAir" (регистр важен).
        static readonly int IsInAirHash = Animator.StringToHash("IsInAir");

        private void Awake()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            inputHandler = GetComponent<InputHandler>();
            anim = GetComponentInChildren<Animator>();
            playerLocomotion = GetComponent<PlayerLocomotion>();

            interactableUI = FindFirstObjectByType<InteractableUI>(FindObjectsInactive.Include);
        }

        void Start()
        {
            cameraHandler = CameraHandler.singleton;
        }

        void Update()
        {
            float delta = Time.deltaTime;
            isIntetacting = anim.GetBool(IsInteractingHash);
            canDoConbo = anim.GetBool(CanDoComboHash);
            anim.SetBool(IsInAirHash, isInAir);

            inputHandler.TickInput(delta);
            playerLocomotion.HandleMovement(delta);
            playerLocomotion.HandleRollingAndSprinting(delta);
            playerLocomotion.HandleFalling(delta, playerLocomotion.moveDirection);
            playerLocomotion.HandleJumping();

            CheckForInteractableObject();
        }

        private void LateUpdate()
        {
            float delta = Time.deltaTime;

            if (cameraHandler != null)
            {
                cameraHandler.FollowTarget(delta);

                // При открытом инвентаре камеру не крутим: курсор разлочен
                // для кликов по UI, его путь к кнопке вращал бы камеру за
                // меню. Слежение оставляем — с открытым меню можно ходить.
                if (!inputHandler.inventoryFlag)
                {
                    cameraHandler.HandleCameraRotation(delta, inputHandler.mouseX, inputHandler.mouseY);
                }
            }

            inputHandler.rollFlag = false;
            inputHandler.sprintFlag = false;
            inputHandler.lightAttack_Input = false;
            inputHandler.heavyAttack_Input = false;
            inputHandler.quickSlotRight_Input = false;
            inputHandler.quickSlotLeft_Input = false;
            inputHandler.interact_Input = false;
            inputHandler.jump_Input = false;
            inputHandler.inventory_Input = false;

            if (isInAir)
            {
                playerLocomotion.inAirTimer = playerLocomotion.inAirTimer + Time.deltaTime;
            }
        }

        public void CheckForInteractableObject()
        {
            if (cameraHandler == null)
                return;

            RaycastHit hit;

            if (Physics.SphereCast(transform.position, 0.3f, transform.forward, out hit, 1f, cameraHandler.ignoreLayers))
            {
                Interactable interactableObject = hit.collider.GetComponent<Interactable>();

                if (interactableObject != null)
                {
                    string interactableText = interactableObject.interactbleText;

                    if (interactableUI != null && interactableUI.interactionText != null)
                    {
                        interactableUI.interactionText.text = interactableText;
                    }

                    if (interactableUIGameObject != null)
                    {
                        interactableUIGameObject.SetActive(true);
                    }

                    if (inputHandler.interact_Input)
                    {
                        interactableObject.Interact(this);
                    }
                }
            }
            else
            {
                if (interactableUIGameObject != null)
                {
                    interactableUIGameObject.SetActive(false);
                }

                // Ручное скрытие уведомления по нажатию — быстрый способ
                // отмахнуться, не дожидаясь таймера.
                if (itemInteractableGameObject != null && inputHandler.interact_Input)
                {
                    if (itemNotificationCoroutine != null)
                    {
                        StopCoroutine(itemNotificationCoroutine);
                        itemNotificationCoroutine = null;
                    }
                    itemInteractableGameObject.SetActive(false);
                }
            }
        }

        // Единая точка показа "подобрано: <название>" с иконкой; само
        // скрывается по таймеру.
        public void ShowItemPickupNotification(string itemName, Sprite itemIcon)
        {
            if (interactableUI != null)
            {
                if (interactableUI.itemText != null)
                {
                    interactableUI.itemText.text = itemName;
                }

                if (interactableUI.itemImage != null && itemIcon != null)
                {
                    interactableUI.itemImage.texture = itemIcon.texture;
                }
            }

            if (itemInteractableGameObject == null)
                return;

            itemInteractableGameObject.SetActive(true);

            if (itemNotificationCoroutine != null)
            {
                StopCoroutine(itemNotificationCoroutine);
            }
            itemNotificationCoroutine = StartCoroutine(HideItemNotificationAfterDelay());
        }

        private IEnumerator HideItemNotificationAfterDelay()
        {
            yield return new WaitForSeconds(itemNotificationDuration);

            if (itemInteractableGameObject != null)
            {
                itemInteractableGameObject.SetActive(false);
            }
            itemNotificationCoroutine = null;
        }
    }
}