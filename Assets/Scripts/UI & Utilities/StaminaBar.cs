using UnityEngine;
using UnityEngine.UI;

namespace SG {
    public class StaminaBar : MonoBehaviour
    {
        public Slider slider;

        private void Awake()
        {
            // Awake, а не Start: PlayerStats.Start() дёргает бар, а порядок
            // Start() между скриптами не гарантирован.
            if (slider == null)
            {
                slider = GetComponent<Slider>();
            }
        }

        public void SetMaxStamina(int maxStamina)
        {
            slider.maxValue = maxStamina;
            slider.value = maxStamina;
        }

        public void SetCurrentStamina(int currentStamina)
        {
            slider.value = currentStamina;
        }
    }
}