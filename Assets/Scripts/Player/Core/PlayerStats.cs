using UnityEngine;

namespace SG {
    public class PlayerStats : MonoBehaviour
    {
        public int healthLevel = 10;
        public int maxHealth;
        public int currentHealth;

        public bool isDead;

        public int staminaLevel = 10;
        public int maxStamina;
        public int currentStamina;

        public HealthBar healthBar;
        public StaminaBar staminaBar;

        AnimatorHandler animatorHandler;

        private void Awake()
        {
            // FindFirstObjectByType вместо устаревшего FindObjectOfType(true):
            // тот же смысл (включая неактивные объекты), без obsolete-warning.
            // Ручное назначение в инспекторе имеет приоритет.
            if (healthBar == null)
                healthBar = FindFirstObjectByType<HealthBar>(FindObjectsInactive.Include);
            if (staminaBar == null)
                staminaBar = FindFirstObjectByType<StaminaBar>(FindObjectsInactive.Include);

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
                Debug.LogWarning("PlayerStats: HealthBar не найден — HP-бар обновляться не будет.");
            }

            maxStamina = SetMaxStaminaFromStaminaLevel();
            currentStamina = maxStamina;

            // Без SetMaxStamina слайдер остаётся с дефолтным maxValue = 1 и
            // визуально "опустошается" за один удар, хотя числа верные.
            if (staminaBar != null)
            {
                staminaBar.SetMaxStamina(maxStamina);
            }
            else
            {
                Debug.LogWarning("PlayerStats: StaminaBar не найден — бар выносливости обновляться не будет.");
            }
        }

        private int SetMaxHealthFromHealthLevel()
        {
            maxHealth = healthLevel * 10;
            return maxHealth;
        }

        private int SetMaxStaminaFromStaminaLevel()
        {
            maxStamina = staminaLevel * 10;
            return maxStamina;
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

        public void TakeStaminaDamage(int damage)
        {
            // Clamp снизу: без него выносливость уходила в минус, и после
            // "перерасхода" пришлось бы восстанавливать невидимый долг.
            currentStamina = Mathf.Max(0, currentStamina - damage);

            if (staminaBar != null)
            {
                staminaBar.SetCurrentStamina(currentStamina);
            }
        }
    }
}