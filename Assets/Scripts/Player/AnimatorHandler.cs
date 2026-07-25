using UnityEngine;

namespace SG {
    public class AnimatorHandler : MonoBehaviour
    {
        public Animator anim;
        public InputHandler inputHandler;
        public PlayerLocomotion playerLocomotion;
        int vertical;
        int horizontal;
        public bool canRotate;

        public void Initialize()
        {
            anim = GetComponent<Animator>();
            inputHandler = GetComponentInParent<InputHandler>();
            playerLocomotion = GetComponentInParent<PlayerLocomotion>();
            vertical = Animator.StringToHash("Vertical");
            horizontal = Animator.StringToHash("Horizontal");

            // Апply Root Motion по умолчанию включён Unity-ом на новом Animator-компоненте.
            // Пока он включён, а OnAnimatorMove() ничего не делает во время обычного
            // перемещения (isInteracting == false), аниматор всё равно считается
            // "ответственным" за Rigidbody на этот кадр, и rb.linearVelocity,
            // выставленный в PlayerLocomotion.HandleMovement, эффективно перебивается —
            // отсюда "анимации Walk/Run идут, а персонаж стоит на месте".
            // Root motion нам нужен только во время действий (Rolling и т.п.),
            // это уже включается точечно в PlayeTargetAnimation(). Поэтому по
            // умолчанию выключаем его здесь.
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
                h = 0.5f;
            } else if (horizontalMovement < -0.55f)
            {
                h = 1;
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
            if (inputHandler.isIntetacting == false)
                return;

            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);

            float delta = Time.deltaTime;
            playerLocomotion.GetComponent<Rigidbody>().linearDamping = 0;

            Vector3 deltaPosition = anim.deltaPosition;
            deltaPosition.y = 0;

            Vector3 velocity;

            if (stateInfo.normalizedTime >= 1f)
            {
                // Клип переката уже фактически доиграл (в том числе во время
                // 0.2с CrossFade-перехода в Locomotion), а isInteracting может
                // сброситься на кадр-другой позже. Раньше в этом окне Rigidbody
                // продолжал получать скорость, и персонажа "доносило" по инерции
                // после конца анимации — поэтому тут скорость жёстко глушим,
                // не дожидаясь флага.
                velocity = Vector3.zero;
            }
            else if (deltaPosition.sqrMagnitude > 0.0001f)
            {
                // Если для какой-то другой анимации (например атаки) root motion
                // реально есть — используем его как и раньше.
                velocity = deltaPosition / delta;
            }
            else
            {
                // У клипа Roll (Universal Animation Library) Average Velocity =
                // (0,0,0) — root motion curves есть, но реального смещения вперёд
                // не дают. Двигаем персонажа вручную.
                // Раньше скорость плавно менялась по синусоиде (0 -> максимум в
                // середине -> 0), но там RollSpeed достигался лишь на мгновение,
                // из-за чего суммарная дистанция переката получалась заметно
                // меньше, чем задано в RollSpeed, и рывок ощущался слабым.
                // Теперь — "трапеция": быстрый разгон, RollSpeed держится почти
                // весь перекат, короткое торможение в конце. Резкого рывка
                // по-прежнему нет, но пройденное расстояние заметно больше.
                const float rampFraction = 0.15f; // доля длительности на разгон/торможение
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

            playerLocomotion.GetComponent<Rigidbody>().linearVelocity = velocity;
        }
    }
}