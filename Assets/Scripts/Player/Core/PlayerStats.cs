using UnityEngine;

namespace SG {
    public class PlayerStats : MonoBehaviour
    {
        public int healthLevel = 10;
        public int maxHealth;
        public int currentHealth;

        public bool isDead;

        public HealthBar healthBar;

        AnimatorHandler animatorHandler;

        private void Awake()
        {
            animatorHandler = GetComponentInChildren<AnimatorHandler>();
        }

        void Start()
        {
            maxHealth = SetMaxHealthFromHealthLevel();
            currentHealth = maxHealth;

            if (healthBar != null)
            {
                healthBar.SetMaxHealth(maxHealth);
            }
            else
            {
                Debug.LogWarning("PlayerStats: HealthBar не назначен в инспекторе — HP-бар обновляться не будет.");
            }
        }

        private int SetMaxHealthFromHealthLevel()
        {
            maxHealth = healthLevel * 10;
            return maxHealth;
        }

        public void TakeDamage(int damage)
        {
            // После смерти урон игнорируется — иначе каждый последующий удар
            // заново запускал анимацию Dead.
            if (isDead)
                return;

            currentHealth = currentHealth - damage;

            if (healthBar != null)
            {
                healthBar.SetCurrentHealth(currentHealth);
            }

            if (currentHealth <= 0)
            {
                currentHealth = 0;
                isDead = true;
                animatorHandler.PlayeTargetAnimation("Dead", true);
            }
            else
            {
                // else, а не второй независимый if: на смертельном ударе раньше
                // в одном кадре игрались и BetaDamage, и Dead.
                animatorHandler.PlayeTargetAnimation("BetaDamage", true);
            }
        }
    }
}