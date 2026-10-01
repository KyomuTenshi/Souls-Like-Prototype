using UnityEngine;

namespace SG {
    public class EnemyStats : MonoBehaviour
    {
        public int healthLevel = 10;
        public int maxHealth;
        public int currentHealth;

        Animator animator;

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>();
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

        // Мёртвый враг урон не получает: иначе Damage_01 прерывал анимацию смерти.
        public void TakeDamage(int damage)
        {
            if (currentHealth <= 0)
                return;

            currentHealth = currentHealth - damage;

            if (currentHealth <= 0)
            {
                currentHealth = 0;
                animator.Play("Death_02");
            }
            else
            {
                animator.Play("Damage_01");
            }
        }
    }
}
