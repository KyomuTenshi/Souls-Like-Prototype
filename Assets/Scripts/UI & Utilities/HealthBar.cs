using UnityEngine;
using UnityEngine.UI;

namespace SG {
    public class HealthBar : MonoBehaviour
    {
        public Slider slider;

        private void Awake()
        {
            // Awake, а не Start: PlayerStats.Start() дёргает SetMaxHealth(),
            // а порядок Start() между скриптами Unity не гарантирует.
            // Все Awake() выполняются раньше любого Start() — здесь slider
            // гарантированно готов к первому обращению.
            if (slider == null)
            {
                slider = GetComponent<Slider>();
            }
        }

        public void SetMaxHealth(int maxHealth)
        {
            slider.maxValue = maxHealth;
            slider.value = maxHealth;
        }

        public void SetCurrentHealth(int currentHealth)
        {
            slider.value = currentHealth;
        }
    }
}