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
            if (weapon == null)
                return;

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
            // Оружие может быть null, если unarmedWeapon не назначен в
            // PlayerInventory — без guard'а нажатие атаки роняло NRE.
            if (weapon == null)
            {
                Debug.LogWarning("PlayerAttacker: атака без оружия — проверь, что unarmedWeapon назначен в PlayerInventory.");
                return;
            }

            // Не начинаем новую атаку, пока играет анимация-интеракция.
            if (playerManager.isIntetacting)
                return;

            if (string.IsNullOrEmpty(weapon.OH_Light_Attack_1))
            {
                Debug.LogWarning("OH_Light_Attack_1 не задан на оружии " + weapon.itemName);
                return;
            }

            animatorHandler.PlayeTargetAnimation(weapon.OH_Light_Attack_1, true);
            lastAttack = weapon.OH_Light_Attack_1;
        }

        public void HandleHeavytAttack(WeaponItem weapon)
        {
            if (weapon == null)
            {
                Debug.LogWarning("PlayerAttacker: атака без оружия — проверь, что unarmedWeapon назначен в PlayerInventory.");
                return;
            }

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