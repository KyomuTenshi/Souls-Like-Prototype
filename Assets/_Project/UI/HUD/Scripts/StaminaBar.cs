using UnityEngine;
using UnityEngine.UI;

namespace SG {
    public class StaminaBar : MonoBehaviour
    {
        public Slider slider;

        public void Start()
        {
            slider = GetComponent<Slider>();
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