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

        public void UpdateAnimatorValues(float verticalMovement, float horizontalMovement)
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

            float delta = Time.deltaTime;
            playerLocomotion.GetComponent<Rigidbody>().linearDamping = 0;

            Vector3 deltaPosition = anim.deltaPosition;
            deltaPosition.y = 0;

            Vector3 velocity;

            // У клипа Roll (Universal Animation Library) Average Velocity = (0,0,0) —
            // root motion curves есть, но реального смещения вперёд не дают.
            // Поэтому если deltaPosition от аниматора практически нулевой,
            // двигаем персонажа вручную с фиксированной скоростью вперёд по
            // направлению, куда он уже повёрнут (оно выставляется в
            // HandleRollingAndSprinting перед стартом переката).
            // Если для другой анимации (например атаки) root motion реально
            // есть — используем его как и раньше, ничего не ломая.
            if (deltaPosition.sqrMagnitude > 0.0001f)
            {
                velocity = deltaPosition / delta;
            }
            else
            {
                velocity = playerLocomotion.myTransform.forward * playerLocomotion.RollSpeed;
            }

            playerLocomotion.GetComponent<Rigidbody>().linearVelocity = velocity;
        }
    }
}