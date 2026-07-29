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
        public bool canRotate;

        // Кэш хэшей параметров Animator — эти два дёргаются чаще всего в проекте.
        static readonly int IsInteractingHash = Animator.StringToHash("isInteracting");
        static readonly int CanDoComboHash = Animator.StringToHash("canDoCombo");

        // Момент normalizedTime (0..1), при котором для НЕ-Roll интеракций
        // (Falling, Land и т.п.) управление возвращается игроку. 1 = ждать
        // полный клип. Если "Land" ощущается долгим — поставь, например, 0.75.
        [Range(0.1f, 1f)]
        public float interactionExitNormalizedTime = 1f;

        // Длительность блендинга (CrossFade) между анимациями-интеракциями,
        // включая шаги комбо. Меньше — резче/отзывчивее, больше — мягче.
        [Range(0.05f, 0.5f)]
        public float animationBlendTime = 0.2f;

        // Более быстрый CrossFade для перехода в Rec-анимацию при ОБРЫВЕ комбо
        // (см. ComboWindowClosed) — обрыв должен ощущаться мгновенно.
        [Range(0.02f, 0.3f)]
        public float comboBreakBlendTime = 0.05f;

        public void Initialize()
        {
            playerManager = GetComponentInParent<PlayerManager>();
            anim = GetComponent<Animator>();
            inputHandler = GetComponentInParent<InputHandler>();
            playerLocomotion = GetComponentInParent<PlayerLocomotion>();
            vertical = Animator.StringToHash("Vertical");
            horizontal = Animator.StringToHash("Horizontal");

            anim.applyRootMotion = false;
        }

        public void UpdateAnimatorValues(float verticalMovement, float horizontalMovement, bool isSprinting)
        {
            #region Vertical
            float v = 0;

            if (verticalMovement > 0 && verticalMovement < 0.55f)
            {
                v = 0.5f;
            } else if (verticalMovement > 0.55f)
            {
                v = 1;
            } else if (verticalMovement < 0 && verticalMovement > -0.55f)
            {
                v = -0.5f;
            } else if (verticalMovement < -0.55f)
            {
                v = -1;
            } else
            {
                v = 0;
            }

            #endregion

            #region  Horizontal
            float h = 0;

            if (horizontalMovement > 0 && horizontalMovement < 0.55f)
            {
                h = 0.5f;
            } else if (horizontalMovement > 0.55f)
            {
                h = 1;
            } else if (horizontalMovement < 0 && horizontalMovement > -0.55f)
            {
                h = -0.5f;
            } else if (horizontalMovement < -0.55f)
            {
                h = -1;
            } else
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
        }

        public void PlayeTargetAnimation(string targetAnim, bool isInteracting, float blendTime = -1f)
        {
            anim.applyRootMotion = isInteracting;
            anim.SetBool(IsInteractingHash, isInteracting);
            anim.CrossFade(targetAnim, blendTime >= 0f ? blendTime : animationBlendTime);
        }

        // Вызывается Animation Event'ом (со строковым параметром — именем
        // Rec-анимации) ближе к концу клипа атаки, ПОСЛЕ EnableCombo(). Если
        // canDoCombo всё ещё true — игрок не нажал RB в окне комбо, и окно
        // закрылось само: явно переключаем на Rec с быстрым блендом.
        public void ComboWindowClosed(string recoveryAnim)
        {
            if (anim.GetBool(CanDoComboHash))
            {
                anim.SetBool(CanDoComboHash, false);
                PlayeTargetAnimation(recoveryAnim, true, comboBreakBlendTime);
            }
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
            anim.SetBool(CanDoComboHash, true);
        }

        public void DisableConbo()
        {
            anim.SetBool(CanDoComboHash, false);
        }
        private void OnAnimatorMove()
        {
            if (playerManager.isIntetacting == false)
                return;

            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);

            float delta = Time.deltaTime;
            Rigidbody rb = playerLocomotion.rb;
            rb.linearDamping = 0;

            // Логика ниже разветвляется по типу интеракции: только Roll
            // получает ручной разгон/торможение; Falling/Land без root motion
            // персонажа не толкают (их физику считает HandleFalling).
            bool isRollingState = stateInfo.IsName("Rolling");

            // Для Roll порог всегда 1 (ждём конец клипа). Для остальных
            // интеракций — настраиваемый interactionExitNormalizedTime.
            float exitThreshold = isRollingState ? 1f : interactionExitNormalizedTime;

            if (stateInfo.normalizedTime >= exitThreshold)
            {
                rb.linearVelocity = Vector3.zero;

                if (!isRollingState)
                {
                    // Falling/Land: явно возвращаем управление, не дожидаясь
                    // Animation Event, которого может не быть в контроллере.
                    anim.applyRootMotion = false;
                    anim.SetBool(IsInteractingHash, false);
                }

                return;
            }

            Vector3 deltaPosition = anim.deltaPosition;
            deltaPosition.y = 0;

            Vector3 velocity;

            if (deltaPosition.sqrMagnitude > 0.0001f)
            {
                // Реальный root motion есть (например, у атак) — используем его.
                velocity = deltaPosition / delta;
            }
            else if (isRollingState)
            {
                // Специфичный для Roll разгон/торможение.
                const float rampFraction = 0.15f;
                float normalizedTime = Mathf.Clamp01(stateInfo.normalizedTime);
                float speedMultiplier;

                if (normalizedTime < rampFraction)
                    speedMultiplier = normalizedTime / rampFraction;
                else if (normalizedTime > 1f - rampFraction)
                    speedMultiplier = (1f - normalizedTime) / rampFraction;
                else
                    speedMultiplier = 1f;

                float easedSpeed = playerLocomotion.RollSpeed * speedMultiplier;
                velocity = playerLocomotion.myTransform.forward * easedSpeed;
            }
            else
            {
                // Интеракция без root motion (Falling, Land) — не толкаем вперёд.
                velocity = Vector3.zero;
            }

            rb.linearVelocity = velocity;
        }
    }
}