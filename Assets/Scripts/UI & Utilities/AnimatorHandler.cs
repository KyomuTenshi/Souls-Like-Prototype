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

        // Кэш хэшей параметров Animator — эти два дёргаются чаще всего в проекте.
        static readonly int IsInteractingHash = Animator.StringToHash("isInteracting");
        static readonly int CanDoComboHash = Animator.StringToHash("canDoCombo");

        // Момент normalizedTime (0..1), при котором для НЕ-Roll интеракций
        // (Land, Pick Up и т.п.) управление возвращается игроку. 1 = ждать
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

        [Header("Movement Blend")]
        // true (NieR-feel): в blend tree идут СЫРЫЕ значения ввода — на стике
        // скорость анимации плавно растёт с наклоном, без "ступенек".
        // false: лестница туториала (0 / ±0.5 / ±1) — souls-снэппинг.
        // Для клавиатуры разницы нет (WASD и так даёт 0/±1). Blend tree
        // менять не нужно: пороги -1/-0.5/0/0.5/1 интерполируются и так.
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
                // Аналоговый режим: значения как есть (в пределах [-1..1]).
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

        // --- I-Frames уклонения -------------------------------------------
        // Вызываются Animation Event'ами на клипе Rolling (события обязаны
        // жить на объекте с Animator — поэтому методы здесь, а флаг в
        // PlayerStats). Ставь EnableInvulnerability в начале клипа (~5-10%),
        // DisableInvulnerability — ближе к концу (~60-70%): классическое окно
        // неуязвимости уклонения. Пока события не расставлены — методы просто
        // не вызываются, ничего не меняется.
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

            // Мёртвый персонаж: авто-возврат управления по normalizedTime
            // ЗАПРЕЩЁН. Без этого клип "Dead" доигрывал до конца, ветка ниже
            // снимала isInteracting — и трупом можно было бегать до
            // возрождения (или вечно, если PlayerRespawn не повешен).
            // Управление вернёт PlayerRespawn через PlayeTargetAnimation
            // ("Empty", isInteracting: false). Вертикаль оставляем гравитации,
            // горизонталь глушим — труп не должен скользить.
            if (playerStats != null && playerStats.isDead)
            {
                Vector3 deadVelocity = Vector3.zero;
                deadVelocity.y = rb.linearVelocity.y;
                rb.linearVelocity = deadVelocity;
                return;
            }

            // Логика ниже разветвляется по типу интеракции: только Roll
            // получает ручной разгон/торможение; воздушные состояния
            // (Jump/Falling) физику не трогают — её ведёт HandleFalling.
            bool isRollingState = stateInfo.IsName("Rolling");
            bool inAir = playerManager.isInAir;

            // Для Roll порог всегда 1 (ждём конец клипа). Для остальных
            // интеракций — настраиваемый interactionExitNormalizedTime.
            float exitThreshold = isRollingState ? 1f : interactionExitNormalizedTime;

            // В воздухе авто-выход ЗАПРЕЩЁН: зацикленный Falling проходит
            // normalizedTime >= 1 на каждом витке, и без этой проверки
            // управление возвращалось бы игроку прямо в полёте. Момент
            // выхода из воздушных состояний определяет HandleFalling
            // (Land/Empty при касании земли) — это фаза 3.
            if (!inAir && stateInfo.normalizedTime >= exitThreshold)
            {
                rb.linearVelocity = Vector3.zero;

                if (!isRollingState)
                {
                    // Land/Pick Up и т.п.: явно возвращаем управление, не
                    // дожидаясь Animation Event, которого может не быть.
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
                // Реальный root motion есть (например, у атак и старта
                // прыжка) — используем его для горизонтали.
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
            else if (inAir)
            {
                // Воздушное состояние без root motion (Falling-цикл):
                // скорость целиком ведёт физика/HandleFalling — не мешаем,
                // иначе горизонтальный импульс прыжка обнулялся бы каждый кадр.
                velocity = rb.linearVelocity;
            }
            else
            {
                // Наземная интеракция без root motion — стоим на месте.
                velocity = Vector3.zero;
            }

            // Ключ к физическому прыжку: в воздухе вертикальную скорость
            // НИКОГДА не перетираем root motion'ом (у клипов y всё равно
            // занулён выше) — сохраняем импульс прыжка и ускорение падения.
            if (inAir)
            {
                velocity.y = rb.linearVelocity.y;
            }

            rb.linearVelocity = velocity;
        }
    }
}