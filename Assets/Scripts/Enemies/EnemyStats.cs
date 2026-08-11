using UnityEngine;

namespace SG {
    public class EnemyStats : MonoBehaviour
    {
        public int healthLevel = 10;
        public int maxHealth;
        public int currentHealth;

        public bool isDead;

        Animator anim;

        private void Awake()
        {
            // Врагу player-специфичный AnimatorHandler не нужен — работаем
            // напрямую со стандартным Animator.
            anim = GetComponent<Animator>();
        }

        void Start()
        {
            maxHealth = SetMaxHealthFromHealthLevel();
            currentHealth = maxHealth;
        }

        private int SetMaxHealthFromHealthLevel()
        {
            maxHealth = healthLevel * 10;
            return maxHealth;
        }

        // Возвращает, прошёл ли урон (false — цель уже мертва). Нужно
        // хитстопу в DamageCollider. Старые вызовы вида TakeDamage(x);
        // компилируются как раньше — результат просто игнорируется.
        public bool TakeDamage(int damage)
        {
            // Труп не реагирует: без guard'а каждый удар заново играл "Dead",
            // а currentHealth уходил в минус.
            if (isDead)
                return false;

            currentHealth = currentHealth - damage;

            if (currentHealth <= 0)
            {
                currentHealth = 0;
                isDead = true;
                anim.SetBool("isInteracting", true);
                anim.CrossFade("Dead", 0.2f);
            }
            else
            {
                anim.SetBool("isInteracting", true);
                anim.CrossFade("BetaDamage", 0.2f);
            }

            return true;
        }
    }
}