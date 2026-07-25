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

        // Момент normalizedTime (0..1), при котором для НЕ-Roll интеракций
        // (Falling, Land и т.п.) управление возвращается игроку. По умолчанию
        // 1 = ждать полный клип, как было. Если "Land" ощущается слишком долгим —
        // можно поставить, например, 0.75, чтобы отдать управление немного раньше
        // конца анимации посадки, не трогая сам клип и не меняя код.
        [Range(0.1f, 1f)]
        public float interactionExitNormalizedTime = 1f;

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

        public void PlayeTargetAnimation(string targetAnim, bool isInteracting)
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

        private void OnAnimatorMove()
        {
            if (playerManager.isIntetacting == false)
                return;

            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);

            float delta = Time.deltaTime;
            Rigidbody rb = playerLocomotion.rb;
            rb.linearDamping = 0;

            // ВАЖНО: раньше вся логика ниже применялась к ЛЮБОЙ анимации с
            // isInteracting == true (включая Falling и Land), а не только к Roll.
            // У Falling/Land нет собственного root motion (deltaPosition ~ 0),
            // и код проваливался в ветку, написанную специально под Roll, толкая
            // персонажа вперёд на RollSpeed каждый кадр — отсюда и рывок вперёд
            // в падении, и при приземлении. Явно проверяем, что это именно
            // состояние "Rolling" (имя состояния в Animator Controller должно
            // совпадать со строкой, переданной в PlayeTargetAnimation("Rolling", true)).
            bool isRollingState = stateInfo.IsName("Rolling");

            // Для Roll порог всегда 1 (ждём Animation Event/конец клипа как раньше).
            // Для остальных интеракций (Falling, Land) используем настраиваемый
            // interactionExitNormalizedTime, чтобы не ждать управлением полный клип,
            // если он длинный.
            float exitThreshold = isRollingState ? 1f : interactionExitNormalizedTime;

            if (stateInfo.normalizedTime >= exitThreshold)
            {
                // Клип доиграл до конца (или до настроенной точки возврата управления).
                rb.linearVelocity = Vector3.zero;

                if (!isRollingState)
                {
                    // Это Falling/Land (или любая другая не-Roll "интеракция") —
                    // явно возвращаем управление игроку и выключаем root motion,
                    // не дожидаясь Animation Event, которого может не быть в
                    // контроллере. Без этого isInteracting навсегда оставался
                    // true, HandleMovement() выходил на первой строке и
                    // управление к игроку не возвращалось.
                    anim.applyRootMotion = false;
                    anim.SetBool("isInteracting", false);
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
                // Специфичный для Roll разгон/торможение (см. предыдущий комментарий).
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
                // Любая другая "интеракция" без собственного root motion
                // (Falling, Land и т.п.) — НЕ толкаем персонажа вперёд.
                // Физику падения уже полностью считает PlayerLocomotion.HandleFalling.
                velocity = Vector3.zero;
            }

            rb.linearVelocity = velocity;
        }
    }
}