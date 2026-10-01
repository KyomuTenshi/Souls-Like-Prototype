using UnityEngine;

namespace SG {
    public class AnimatorHandler : MonoBehaviour
    {
        PlayerManager playerManager;
        public Animator anim;
        InputHandler inputHandler;
        PlayerLocomotion playerLocomotion;
        int vertical;
        int horizontal;
        int rightArmLayer;
        int leftArmLayer;
        public bool canRotate;

        // Слои рук проигрывают статичную позу Sword_Idle. С постоянным весом 0.8 руки замирали
        // при ходьбе и беге, поэтому вес снижается по мере ускорения и руки размахиваются в такт шагам.
        [Header("Arm Layers")]
        [Tooltip("Вес слоёв рук на месте: руки держат оружие в позе Idle")]
        [Range(0, 1)] public float armLayerIdleWeight = 0.8f;
        [Tooltip("Вес слоёв рук при ходьбе и беге трусцой")]
        [Range(0, 1)] public float armLayerMoveWeight = 0.35f;
        [Tooltip("Вес слоёв рук при спринте")]
        [Range(0, 1)] public float armLayerSprintWeight = 0.15f;

        public void Initialize()
        {
            playerManager = GetComponentInParent<PlayerManager>();
            anim = GetComponent<Animator>();
            inputHandler = GetComponentInParent<InputHandler>();
            playerLocomotion = GetComponentInParent<PlayerLocomotion>();
            vertical = Animator.StringToHash("Vertical");
            horizontal = Animator.StringToHash("Horizontal");
            rightArmLayer = anim.GetLayerIndex("Right Arm");
            leftArmLayer = anim.GetLayerIndex("Left Arm");
        }

        public void UpdateAnimatorValues(float verticalMovement, float horizontalMovement, bool isSprinting)
        {
            #region Vertical
            float v = 0;

            if (verticalMovement > 0 && verticalMovement < 0.55f)
            {
                v = 0.5f;
            }
            else if (verticalMovement > 0.55f)
            {
                v = 1;
            }
            else if (verticalMovement < 0 && verticalMovement > -0.55f)
            {
                v = -0.5f;
            }
            else if (verticalMovement < -0.55f)
            {
                v = -1;
            }
            else
            {
                v = 0;
            }
            #endregion

            #region Horizontal
            float h = 0;

            if (horizontalMovement > 0 && horizontalMovement < 0.55f)
            {
                h = 0.5f;
            }
            else if (horizontalMovement > 0.55f)
            {
                h = 1;
            }
            else if (horizontalMovement < 0 && horizontalMovement > -0.55f)
            {
                h = -0.5f;
            }
            else if (horizontalMovement < -0.55f)
            {
                h = -1;
            }
            else
            {
                h = 0;
            }
            #endregion

            if (isSprinting)
            {
                v = 2;
                h = horizontalMovement;
            }

            anim.SetFloat(vertical, v, 0.1f, Time.deltaTime);
            anim.SetFloat(horizontal, h, 0.1f, Time.deltaTime);

            UpdateArmLayerWeights();
        }

        private void UpdateArmLayerWeights()
        {
            // Берём уже сглаженное значение Vertical, чтобы вес менялся синхронно с blend tree локомоции.
            float speed = Mathf.Abs(anim.GetFloat(vertical));
            float weight = speed <= 1
                ? Mathf.Lerp(armLayerIdleWeight, armLayerMoveWeight, speed)
                : Mathf.Lerp(armLayerMoveWeight, armLayerSprintWeight, speed - 1);

            if (rightArmLayer >= 0)
                anim.SetLayerWeight(rightArmLayer, weight);

            if (leftArmLayer >= 0)
                anim.SetLayerWeight(leftArmLayer, weight);
        }

        public void PlayTargetAnimation(string targetAnim, bool isInteracting)
        {
            anim.applyRootMotion = isInteracting;
            anim.SetBool("isInteracting", isInteracting);
            anim.CrossFade(targetAnim, 0.2f);
        }

        public void CanRotate()
        {
            canRotate = true;
        }

        public void StopRotation()
        {
            canRotate = false;
        }

        public void EnableCombo()
        {
            anim.SetBool("canDoCombo", true);
        }

        public void DisableCombo()
        {
            anim.SetBool("canDoCombo", false);
        }

        private void OnAnimatorMove()
        {
            if (playerManager.isInteracting == false)
                return;

            if (playerManager.isInAir)
                return;

            // Root motion отключается только на время ручного действия (ролл, бэкстеп, торможение).
            // Остальные анимации с isInteracting двигаются через root motion, как в туториале.
            if (playerLocomotion.isDoingManualAction)
                return;

            float delta = Time.deltaTime;
            playerLocomotion.rigidbody.linearDamping = 0; // Unity 6: Rigidbody.drag переименован в linearDamping.
            Vector3 deltaPosition = anim.deltaPosition;
            deltaPosition.y = 0;
            Vector3 velocity = deltaPosition / delta;
            playerLocomotion.rigidbody.linearVelocity = velocity; // Unity 6: Rigidbody.velocity переименован в linearVelocity.
        }
    }
}
