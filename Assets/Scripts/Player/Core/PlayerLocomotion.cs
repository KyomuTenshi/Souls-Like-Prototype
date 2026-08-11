// PlayerLocomotion.cs
using UnityEngine;

namespace SG 
{
    public class PlayerLocomotion : MonoBehaviour
    {
        CameraHandler cameraHandler;
        PlayerManager playerManager;
        PlayerStats playerStats;
        Transform cameraObject;
        InputHandler inputHandler;
        public Vector3 moveDirection;

        [HideInInspector]
        public Transform myTransform;
        [HideInInspector]
        public AnimatorHandler animatorHandler;

        public Rigidbody rb; 
        public GameObject normalCamera;

        [Header("Ground & Air Detection Stats")]
        [SerializeField] float groundDetectionRayStartPoint = 0.5f;
        [SerializeField] float minimunDistanceNeededToBeginFall = 1f;
        [SerializeField] float grounfDirectionRayDistance = 0.2f;
        LayerMask ignoreForGroundCheck;
        public float inAirTimer;

        [Header("Movement Stats")]
        [SerializeField] float movementSpeed = 5;
        [SerializeField] float rotationSpeed = 10;
        [SerializeField] float rollSpeed = 6;
        [SerializeField] float sprintSpeed = 7;
        [SerializeField] float fallingSpeed = 45;

        [Header("Jump Stats")]
        [SerializeField] float jumpForce = 9f;
        [SerializeField] float jumpAscendGraceTime = 0.35f;
        [SerializeField] string jumpStartAnimation = "Jump";
        [SerializeField] float airTimeForLandAnimation = 0.5f;

        [Header("Stamina Costs")]
        [SerializeField] int rollStaminaCost = 15;
        [SerializeField] int jumpStaminaCost = 10;
        [SerializeField] float sprintStaminaCostPerSecond = 8f;

        [Header("NieR Dodge — Attack Cancel")]
        [SerializeField] bool allowAttackDodgeCancel = true;
        [Range(0f, 1f)]
        [SerializeField] float attackDodgeCancelTime = 0.25f;

        WeaponSlotManager weaponSlotManager;

        float jumpGraceTimer;

        public float RollSpeed { get { return rollSpeed; } }

        public void Awake()
        {
            cameraHandler = FindFirstObjectByType<CameraHandler>();
        }
        void Start()
        {
            playerManager = GetComponent<PlayerManager>();
            playerStats = GetComponent<PlayerStats>();
            rb = GetComponent<Rigidbody>();
            inputHandler = GetComponent<InputHandler>();
            animatorHandler = GetComponentInChildren<AnimatorHandler>();
            weaponSlotManager = GetComponentInChildren<WeaponSlotManager>();
            cameraObject = Camera.main.transform;
            myTransform = transform;
            animatorHandler.Initialize();

            playerManager.isGrounded = true;
            ignoreForGroundCheck = ~(1 << 8 | 1 << 11);

            rb.useGravity = false;
        }

        #region Movement
        Vector3 normalVector;
        Vector3 targetPosition;

        private void HandleRotation(float delta)
        {
            if (inputHandler.lockOnFlag)
            {
                if (inputHandler.sprintFlag || inputHandler.rollFlag)
                {
                    Vector3 targetDirection = Vector3.zero;
                    targetDirection = cameraHandler.cameraTransform.forward * inputHandler.vertical;
                    targetDirection += cameraHandler.cameraTransform.right * inputHandler.horizontal;
                    targetDirection.Normalize();
                    targetDirection.y = 0;

                    if (targetDirection == Vector3.zero)
                    {
                        targetDirection = myTransform.forward;
                    }

                    Quaternion tr = Quaternion.LookRotation(targetDirection);
                    Quaternion targetRotation = Quaternion.Slerp(myTransform.rotation, tr, rotationSpeed * Time.deltaTime);

                    myTransform.rotation = targetRotation;
                }
                else
                {
                    // ИСПРАВЛЕНО: если lockOnFlag уже true, а
                    // currentLockOnTarget ещё/уже null (переходный кадр,
                    // цель уничтожена, рассинхрон флага и цели) — раньше
                    // здесь был прямой NRE каждый кадр, который обрывал
                    // Update() ДО HandleRollingAndSprinting/HandleFalling/
                    // HandleJumping и до всех вызовов аниматора — этим и
                    // объяснялись "не работающие анимации". Раннее return
                    // делает лишний кадр без поворота, а не крэш всего кадра.
                    if (cameraHandler.currentLockOnTarget == null)
                        return;

                    Vector3 rotationDirection = cameraHandler.currentLockOnTarget.transform.position - transform.position;
                    rotationDirection.y = 0;
                    rotationDirection.Normalize();
                    Quaternion tr = Quaternion.LookRotation(rotationDirection);
                    Quaternion targetRotation = Quaternion.Slerp(myTransform.rotation, tr, rotationSpeed * Time.deltaTime);
                    myTransform.rotation = targetRotation;
                }
            }
            else
            {
                Vector3 targetDir = Vector3.zero;

                targetDir = cameraHandler.cameraTransform.forward * inputHandler.vertical;
                targetDir += cameraHandler.cameraTransform.right * inputHandler.horizontal;

                targetDir.y = 0;
                targetDir.Normalize();

                if (targetDir == Vector3.zero)
                    targetDir = myTransform.forward;

                float rs = rotationSpeed;

                Quaternion tr = Quaternion.LookRotation(targetDir);
                Quaternion targetRotation = Quaternion.Slerp(myTransform.rotation, tr, rs * Time.deltaTime);

                myTransform.rotation = targetRotation;
            }
        }

        public void HandleMovement(float delta)
        {
            if (inputHandler.rollFlag)
                return;

            if (playerManager.isIntetacting)
                return;

            moveDirection = cameraHandler.cameraTransform.forward * inputHandler.vertical;
            moveDirection += cameraHandler.cameraTransform.right * inputHandler.horizontal;
            moveDirection.y = 0;
            moveDirection.Normalize();

            float speed = movementSpeed;

            bool wantsSprint = inputHandler.sprintFlag && inputHandler.moveAmount > 0.5f;
            bool staminaFree = playerStats == null || playerStats.IsActionMode;
            bool canSprint = staminaFree || playerStats.HasStamina();

            if (wantsSprint && canSprint)
            {
                speed = sprintSpeed;
                playerManager.isSprinting = true;

                if (!staminaFree)
                {
                    playerStats.DrainStamina(sprintStaminaCostPerSecond * delta);
                }
            }
            else
            {
                playerManager.isSprinting = false;
            }

            moveDirection *= speed;

            Vector3 projectedVelocity = Vector3.ProjectOnPlane(moveDirection, normalVector);
            rb.linearVelocity = projectedVelocity;

            if (inputHandler.lockOnFlag && inputHandler.sprintFlag == false)
            {
                animatorHandler.UpdateAnimatorValues(inputHandler.vertical, inputHandler.horizontal, playerManager.isSprinting);
            }
            else 
            {
                animatorHandler.UpdateAnimatorValues(inputHandler.moveAmount, 0, playerManager.isSprinting);
            }

            if (animatorHandler.canRotate)
            {
                HandleRotation(delta);
            }
        }

        public void HandleRollingAndSprinting(float delta)
        {
            if (!inputHandler.rollFlag)
                return;

            bool isCancellingAttack = playerManager.isIntetacting;

            if (isCancellingAttack && !CanCancelAttackIntoRoll())
                return;

            inputHandler.ConsumeRollBuffer();

            if (playerStats != null && !playerStats.IsActionMode && !playerStats.HasStamina())
                return;

            moveDirection = cameraObject.forward * inputHandler.vertical;
            moveDirection += cameraObject.right * inputHandler.horizontal;

            if (inputHandler.moveAmount > 0)
            {
                moveDirection.y = 0;

                if (moveDirection.sqrMagnitude > 0.0001f)
                {
                    if (isCancellingAttack)
                    {
                        animatorHandler.DisableConbo();

                        if (weaponSlotManager != null)
                        {
                            weaponSlotManager.CloseLeftHandDamageCollider();
                            weaponSlotManager.CloseRightHandDamageCollider();
                        }
                    }

                    animatorHandler.PlayeTargetAnimation("Rolling", true);
                    Quaternion rollRotation = Quaternion.LookRotation(moveDirection);
                    myTransform.rotation = rollRotation;

                    if (playerStats != null && !playerStats.IsActionMode)
                    {
                        playerStats.TakeStaminaDamage(rollStaminaCost);
                    }
                }
            }
            else
            {
                // animatorHandler.PlayeTargetAnimation("Backstep", true);
            }
        }

        private bool CanCancelAttackIntoRoll()
        {
            if (!allowAttackDodgeCancel)
                return false;

            if (playerManager.canDoConbo)
                return true;

            AnimatorStateInfo stateInfo = animatorHandler.anim.GetCurrentAnimatorStateInfo(0);

            if (!stateInfo.IsTag("Attack"))
                return false;

            return stateInfo.normalizedTime >= attackDodgeCancelTime;
        }

        public void HandleFalling(float delta, Vector3 moveDir)
        {
            playerManager.isGrounded = false;
            RaycastHit hit;
            Vector3 origin = myTransform.position;
            origin.y += groundDetectionRayStartPoint;

            if (Physics.Raycast(origin, myTransform.forward, out hit, 0.4f, ignoreForGroundCheck))
            {
                moveDir = Vector3.zero;
            }

            if (playerManager.isInAir)
            {
                rb.linearVelocity += Vector3.down * fallingSpeed * delta;

                Vector3 airDirection = moveDir;
                airDirection.y = 0;
                if (airDirection.sqrMagnitude > 0.0001f)
                {
                    rb.linearVelocity += airDirection.normalized * (fallingSpeed / 10f) * delta;
                }
            }

            Vector3 dir = moveDir;
            dir.Normalize();
            origin = origin + dir * grounfDirectionRayDistance;

            targetPosition = myTransform.position;

            bool jumpAscending = jumpGraceTimer > 0f;
            if (jumpAscending)
            {
                jumpGraceTimer -= delta;
            }

            Debug.DrawRay(origin, -Vector3.up * minimunDistanceNeededToBeginFall, Color.red, 0.1f, false);
            if (!jumpAscending && Physics.Raycast(origin, -Vector3.up, out hit, minimunDistanceNeededToBeginFall, ignoreForGroundCheck))
            {
                normalVector = hit.normal;

                Vector3 tp = hit.point;
                playerManager.isGrounded = true;
                targetPosition.y = tp.y;

                if (playerManager.isInAir)
                {
                    if (inAirTimer > airTimeForLandAnimation)
                    {
                        Debug.Log("You were in the air for " + inAirTimer);
                        animatorHandler.PlayeTargetAnimation("Land", true);
                        inAirTimer = 0;
                    }
                    else
                    {
                        animatorHandler.PlayeTargetAnimation("Empty", false);
                        inAirTimer = 0;
                    }

                    playerManager.isInAir = false;
                }
            }
            else
            {
                if (playerManager.isGrounded)
                {
                    playerManager.isGrounded = false;
                }

                if (playerManager.isInAir == false)
                {
                    if (playerManager.isIntetacting == false)
                    {
                        animatorHandler.PlayeTargetAnimation("Falling", true);
                    }

                    Vector3 vel = rb.linearVelocity;
                    vel.Normalize();
                    rb.linearVelocity = vel * (movementSpeed / 2);
                    playerManager.isInAir = true;
                }
            }

            if (playerManager.isGrounded)
            {
                if (playerManager.isIntetacting || inputHandler.moveAmount > 0)
                {
                    float snapFactor = Mathf.Clamp01(Time.deltaTime / 0.1f);
                    myTransform.position = Vector3.Lerp(myTransform.position, targetPosition, snapFactor);
                }
                else
                {
                    myTransform.position = targetPosition;
                }
            }
        }
        
        public void HandleJumping()
        {
            if (playerManager.isIntetacting)
                return;

            if (inputHandler.jump_Input)
            {
                inputHandler.jump_Input = false;

                if (!playerManager.isGrounded)
                    return;

                if (playerStats != null && !playerStats.IsActionMode && !playerStats.HasStamina())
                    return;

                moveDirection = cameraObject.forward * inputHandler.vertical;
                moveDirection += cameraObject.right * inputHandler.horizontal;
                moveDirection.y = 0;
                moveDirection.Normalize();

                if (inputHandler.moveAmount > 0 && moveDirection.sqrMagnitude > 0.0001f)
                {
                    Quaternion jumpRotation = Quaternion.LookRotation(moveDirection);
                    myTransform.rotation = jumpRotation;
                }

                animatorHandler.PlayeTargetAnimation(jumpStartAnimation, true);

                if (playerStats != null && !playerStats.IsActionMode)
                {
                    playerStats.TakeStaminaDamage(jumpStaminaCost);
                }

                playerManager.isGrounded = false;
                playerManager.isInAir = true;
                inAirTimer = 0;
                jumpGraceTimer = jumpAscendGraceTime;

                float jumpHorizontalSpeed = playerManager.isSprinting ? sprintSpeed : movementSpeed;

                Vector3 jumpVelocity = inputHandler.moveAmount > 0
                    ? moveDirection * jumpHorizontalSpeed
                    : Vector3.zero;
                jumpVelocity.y = jumpForce;
                rb.linearVelocity = jumpVelocity;
            }
        }
        #endregion
    }
}