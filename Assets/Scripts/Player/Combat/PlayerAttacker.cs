using UnityEngine;

namespace SG {
    public class PlayerAttacker : MonoBehaviour
    {
        AnimatorHandler animatorHandler;
        PlayerManager playerManager;
        InputHandler inputHandler;
        WeaponSlotManager weaponSlotManager;
        PlayerStats playerStats;
        public string lastAttack;

        private void Awake()
        {
            animatorHandler = GetComponentInChildren<AnimatorHandler>();
            weaponSlotManager = GetComponentInChildren<WeaponSlotManager>();
            playerManager = GetComponent<PlayerManager>();
            inputHandler = GetComponent<InputHandler>();
            playerStats = GetComponent<PlayerStats>();
        }

        // Souls-правило: замахнуться можно, пока стамина > 0 (стоимость спишет
        // Animation Event через WeaponSlotManager, и она может увести в ноль).
        // При playerStats == null гейт тихо отключается — ничего не ломаем.
        private bool HasStaminaForAttack()
        {
            return playerStats == null || playerStats.HasStamina();
        }

        public void HandleWeaponCombo(WeaponItem weapon)
        {
            if (weapon == null)
                return;

            if (!HasStaminaForAttack())
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

            if (!HasStaminaForAttack())
                return;

            if (string.IsNullOrEmpty(weapon.OH_Light_Attack_1))
            {
                Debug.LogWarning("OH_Light_Attack_1 не задан на оружии " + weapon.itemName);
                return;
            }

            weaponSlotManager.attackingWeapon = weapon;
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

            if (!HasStaminaForAttack())
                return;

            if (string.IsNullOrEmpty(weapon.OH_Heavy_Attack_1))
            {
                Debug.LogWarning("OH_Heavy_Attack_1 не задан на оружии " + weapon.itemName);
                return;
            }

            weaponSlotManager.attackingWeapon = weapon;
            animatorHandler.PlayeTargetAnimation(weapon.OH_Heavy_Attack_1, true);
            lastAttack = weapon.OH_Heavy_Attack_1;
        }
    }
}