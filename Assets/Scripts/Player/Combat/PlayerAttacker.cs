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

        // Гейт разделён по типу атаки (см. PlayerStats.StaminaMode):
        // - Action-режим (NieR-style): лёгкие атаки и комбо бесплатны и не
        //   гейтятся вообще — комбо-флоу нельзя оборвать пустой стаминой.
        // - Souls-режим: старое правило туториала — замахнуться можно, пока
        //   стамина > 0 (стоимость спишет Animation Event через
        //   WeaponSlotManager, и она может увести в ноль).
        // При playerStats == null гейт тихо отключается — ничего не ломаем.
        private bool HasStaminaForLightAttack()
        {
            if (playerStats == null)
                return true;

            if (playerStats.IsActionMode)
                return true;

            return playerStats.HasStamina();
        }

        // Тяжёлые атаки гейтятся стаминой в ОБОИХ режимах — в Action-режиме
        // это единственный потребитель стамины, ради которого бар и живёт.
        private bool HasStaminaForHeavyAttack()
        {
            return playerStats == null || playerStats.HasStamina();
        }

        public void HandleWeaponCombo(WeaponItem weapon)
        {
            if (weapon == null)
                return;

            if (!HasStaminaForLightAttack())
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

            if (!HasStaminaForLightAttack())
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

            if (!HasStaminaForHeavyAttack())
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