using UnityEngine;

namespace SG {
    public class PlayerStats : CharacterStats
    {
        HealthBar healthBar;
        StaminaBar staminaBar;
        AnimatorHandler animatorHandler;

        private void Awake()
        {
            healthBar = FindFirstObjectByType<HealthBar>();
            staminaBar = FindFirstObjectByType<StaminaBar>();
            animatorHandler = GetComponentInChildren<AnimatorHandler>();
        }

        void Start()
        {
            maxHealth = SetMaxHealthFromHealthLevel();
            currentHealth = maxHealth;
            healthBar.SetMaxHealth(maxHealth);

            maxStamina = SetMaxStaminaFromStaminaLevel();
            currentStamina = maxStamina;
            staminaBar.SetMaxStamina(maxStamina);
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

        // Мёртвый персонаж урон не получает: иначе Damage_01 прерывал анимацию смерти.
        public void TakeDamage(int damage)
        {
            if (currentHealth <= 0)
                return;

            currentHealth = currentHealth - damage;

            if (currentHealth <= 0)
            {
                currentHealth = 0;
                animatorHandler.PlayTargetAnimation("Death_02", true);
            }
            else
            {
                animatorHandler.PlayTargetAnimation("Damage_01", true);
            }

            healthBar.SetCurrentHealth(currentHealth);
        }

        public void TakeStaminaDamage(int damage)
        {
            currentStamina = currentStamina - damage;
            staminaBar.SetCurrentStamina(currentStamina);
        }
    }
}
