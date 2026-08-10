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
            // Врагу не нужен player-специфичный AnimatorHandler (он завязан на
            // InputHandler/PlayerManager через GetComponentInParent) — работаем
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

        // БЫЛО: void. Теперь возвращает, ПРОШЁЛ ли урон (false — цель уже
        // мертва). Нужно хитстопу в DamageCollider: удар по трупу не должен
        // замораживать игру, а по isDead после вызова DamageCollider решает,
        // сыграть ли удлинённый kill-хитстоп. Смена void -> bool обратно
        // совместима по исходникам — старые вызовы вида
        // enemyStats.TakeDamage(x); компилируются без изменений.
        public bool TakeDamage(int damage)
        {
            // Мёртвый враг больше не реагирует на удары: без этой проверки
            // каждый удар по трупу заново проигрывал CrossFade("Dead") и
            // труп "дёргался", а currentHealth уходил в минус.
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
                // else, а не два независимых if: при смертельном ударе раньше
                // в одном кадре запускались ДВА CrossFade подряд
                // (BetaDamage, затем Dead) — лишний вызов и грязный бленд.
                anim.SetBool("isInteracting", true);
                anim.CrossFade("BetaDamage", 0.2f);
            }

            return true;
        }
    }
}