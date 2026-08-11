using UnityEngine;

namespace SG {
    public class AnimatorHandler : MonoBehaviour
    {
        PlayerManager playerManager;
        PlayerStats playerStats;
        public Animator anim;
        InputHandler inputHandler;
        PlayerLocomotion playerLocomotion;
        int vertical;
        int horizontal;
        public bool canRotate;

        static readonly int IsInteractingHash = Animator.StringToHash("isInteracting");
        static readonly int CanDoComboHash = Animator.StringToHash("canDoCombo");

        // normalizedTime, при котором не-Roll интеракции (Land, Pick Up)
        // возвращают управление. 1 = ждать полный клип.
        [Range(0.1f, 1f)]
        public float interactionExitNormalizedTime = 1f;

        // CrossFade между интеракциями, включая шаги комбо.
        [Range(0.05f, 0.5f)]
        public float animationBlendTime = 0.2f;

        // Быстрый CrossFade при обрыве комбо — обрыв должен ощущаться мгновенно.
        [Range(0.02f, 0.3f)]
        public float comboBreakBlendTime = 0.05f;

        [Header("Movement Blend")]
        // true (NieR-feel): сырые значения ввода — на стике скорость анимации
        // растёт плавно. false: лестница туториала 0 / ±0.5 / ±1.
        [SerializeField] bool useAnalogMovementBlend = true;

        public void Initialize()
        {
            playerManager = GetComponentInParent<PlayerManager>();
            playerStats = GetComponentInParent<PlayerStats>();
            anim = GetComponent<Animator>();
            inputHandler = GetComponentInParent<InputHandler>();
            playerLocomotion = GetComponentInParent<PlayerLocomotion>();
            vertical = Animator.StringToHash("Vertical");
            horizontal = Animator.StringToHash("Horizontal");

            anim.applyRootMotion = false;
        }

        public void UpdateAnimatorValues(float verticalMovement, float horizontalMovement, bool isSprinting)
        {
            float v;
            float h;

            if (useAnalogMovementBlend)
            {
                v = Mathf.Clamp(verticalMovement, -1f, 1f);
                h = Mathf.Clamp(horizontalMovement, -1f, 1f);
            }
            else
            {
                #region Vertical (лестница туториала)
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

                #region Horizontal (лестница туториала)
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
            }

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

        // Animation Event (строковый параметр — имя Rec-анимации) ближе к
        // концу клипа атаки, после EnableCombo. canDoCombo ещё true — окно
        // закрылось само, явно уходим в Rec с быстрым блендом.
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

        #region I-Frames (Animation Events на клипе Rolling)
        // События обязаны жить на объекте с Animator — поэтому методы здесь,
        // а флаг в PlayerStats. Enable ~5-10% клипа, Disable ~60-70%.
        public void EnableInvulnerability()
        {
            if (playerStats != null)
                playerStats.isInvulnerable = true;
        }

        public void DisableInvulnerability()
        {
            if (playerStats != null)
                playerStats.isInvulnerable = false;
        }
        #endregion

        private void OnAnimatorMove()
        {
            if (playerManager.isIntetacting == false)
                return;

            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);

            float delta = Time.deltaTime;
            if (delta <= 0f)
                return;

            Rigidbody rb = playerLocomotion.rb;
            rb.linearDamping = 0;

            // Мёртвый персонаж: авто-возврат управления запрещён, иначе клип
            // "Dead" доигрывал бы и трупом можно было бегать. Управление
            // вернёт PlayerRespawn. Горизонталь глушим — труп не скользит.
            if (playerStats != null && playerStats.isDead)
            {
                Vector3 deadVelocity = Vector3.zero;
                deadVelocity.y = rb.linearVelocity.y;
                rb.linearVelocity = deadVelocity;
                return;
            }

            bool isRollingState = stateInfo.IsName("Rolling");
            bool inAir = playerManager.isInAir;

            // Roll всегда ждёт конец клипа; остальные — настраиваемый порог.
            float exitThreshold = isRollingState ? 1f : interactionExitNormalizedTime;

            // В воздухе авто-выход запрещён: зацикленный Falling проходит
            // normalizedTime >= 1 каждый виток — управление возвращал бы
            // прямо в полёте. Выход из воздуха решает HandleFalling.
            if (!inAir && stateInfo.normalizedTime >= exitThreshold)
            {
                rb.linearVelocity = Vector3.zero;

                if (!isRollingState)
                {
                    // Явный возврат управления, не дожидаясь Animation Event,
                    // которого может не быть.
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
                // Реальный root motion (атаки, старт прыжка) ведёт горизонталь.
                velocity = deltaPosition / delta;
            }
            else if (isRollingState)
            {
                // Ручной разгон/торможение ролла по фазе клипа.
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
            else if (inAir)
            {
                // Falling-цикл без root motion: скорость ведёт HandleFalling,
                // иначе горизонтальный импульс прыжка обнулялся бы каждый кадр.
                velocity = rb.linearVelocity;
            }
            else
            {
                velocity = Vector3.zero;
            }

            // В воздухе вертикаль никогда не перетираем root motion'ом —
            // сохраняем импульс прыжка и ускорение падения.
            if (inAir)
            {
                velocity.y = rb.linearVelocity.y;
            }

            rb.linearVelocity = velocity;
        }
    }
}