using UnityEngine;

namespace SG {
    public class PlayerAttacker : MonoBehaviour
    {
        AnimatorHandler animatorHandler;
        PlayerManager playerManager;
        InputHandler inputHandler;
        public string lastAttack;

        private void Awake()
        {
            animatorHandler = GetComponentInChildren<AnimatorHandler>();
            playerManager = GetComponent<PlayerManager>();
            inputHandler = GetComponent<InputHandler>();
        }

        public void HandleWeaponCombo(WeaponItem weapon)
        {
            if(inputHandler.comboFlag)
            { 
                animatorHandler.anim.SetBool("canDoCombo", false);
                
                if (lastAttack == weapon.OH_Light_Attack_1)
                {
                    animatorHandler.PlayeTargetAnimation(weapon.OH_Light_Attack_2, true);
                    lastAttack = weapon.OH_Light_Attack_2;
                }
                else if (lastAttack == weapon.OH_Light_Attack_2)
                {
                    animatorHandler.PlayeTargetAnimation(weapon.OH_Light_Attack_3, true);
                    lastAttack = weapon.OH_Light_Attack_3;
                }
                else if (lastAttack == weapon.OH_Light_Attack_3)
                {
                    animatorHandler.PlayeTargetAnimation(weapon.OH_Light_Attack_4, true);
                    lastAttack = weapon.OH_Light_Attack_4;
                }
            }
        }
        public void HandleLightAttack(WeaponItem weapon)
        {
            // Не начинаем новую атаку, пока уже играет анимация-интеракция
            // (атака/ролл/падение). Без этой проверки повторяющийся ввод (см.
            // фикс дублирующихся подписок в InputHandler) вызывал бы CrossFade
            // поверх уже играющей анимации атаки каждый кадр, обрывая её на
            // середине.
            if (playerManager.isIntetacting)
                return;

            if (string.IsNullOrEmpty(weapon.OH_Light_Attack_1))
            {
                // Имя состояния анимации не задано в инспекторе на ассете
                // оружия — CrossFade с пустой строкой валит в консоль
                // "State could not be found" / "Invalid Layer Index -1".
                // Выходим тихо с понятным предупреждением вместо спама ошибок.
                Debug.LogWarning("OH_Light_Attack_1 не задан на оружии " + weapon.itemName);
                return;
            }

            animatorHandler.PlayeTargetAnimation(weapon.OH_Light_Attack_1, true);
            lastAttack = weapon.OH_Light_Attack_1;
        }

        public void HandleHeavytAttack(WeaponItem weapon)
        {
            if (playerManager.isIntetacting)
                return;

            if (string.IsNullOrEmpty(weapon.OH_Heavy_Attack_1))
            {
                Debug.LogWarning("OH_Heavy_Attack_1 не задан на оружии " + weapon.itemName);
                return;
            }

            animatorHandler.PlayeTargetAnimation(weapon.OH_Heavy_Attack_1, true);
            lastAttack = weapon.OH_Heavy_Attack_1;
        }
    }
}