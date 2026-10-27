using UnityEngine;

namespace SG {
    public class PlayerAttacker : MonoBehaviour
    {
        AnimatorHandler animatorHandler;
        InputHandler inputHandler;
        WeaponSlotManager weaponSlotManager;
        public string lastAttack;

        private void Awake()
        {
            animatorHandler = GetComponentInChildren<AnimatorHandler>();
            weaponSlotManager = GetComponentInChildren<WeaponSlotManager>();
            inputHandler = GetComponentInChildren<InputHandler>();
        }

        public void HandleWeaponCombo(WeaponItem weapon)
        {
            if (inputHandler.comboFlag)
            {
                animatorHandler.anim.SetBool("canDoCombo", false);

                if (lastAttack == weapon.OH_Light_Attack_1)
                {
                    animatorHandler.PlayTargetAnimation(weapon.OH_Light_Attack_2, true);
                    lastAttack = weapon.OH_Light_Attack_2;
                }
                else if (lastAttack == weapon.OH_Light_Attack_2)
                {
                    animatorHandler.PlayTargetAnimation(weapon.OH_Light_Attack_3, true);
                    lastAttack = weapon.OH_Light_Attack_3;
                }
                else if (lastAttack == weapon.OH_Light_Attack_3)
                {
                    animatorHandler.PlayTargetAnimation(weapon.OH_Light_Attack_4, true);
                    lastAttack = weapon.OH_Light_Attack_4;
                }
                else if (lastAttack == weapon.OH_Heavy_Attack_1)
                {
                    animatorHandler.PlayTargetAnimation(weapon.OH_Heavy_Attack_2, true);
                    lastAttack = weapon.OH_Heavy_Attack_2;
                }
                else if (lastAttack == weapon.OH_Heavy_Attack_2)
                {
                    animatorHandler.PlayTargetAnimation(weapon.OH_Heavy_Attack_3, true);
                    lastAttack = weapon.OH_Heavy_Attack_3;
                }
                else if (lastAttack == weapon.OH_Heavy_Attack_3)
                {
                    animatorHandler.PlayTargetAnimation(weapon.OH_Heavy_Attack_4, true);
                    lastAttack = weapon.OH_Heavy_Attack_4;
                }
                else if (lastAttack == weapon.TH_Regular_Attack_1)
                {
                    animatorHandler.PlayTargetAnimation(weapon.TH_Regular_Attack_2, true);
                    lastAttack = weapon.TH_Regular_Attack_2;
                }
                else if (lastAttack == weapon.TH_Regular_Attack_2)
                {
                    animatorHandler.PlayTargetAnimation(weapon.TH_Regular_Attack_3, true);
                    lastAttack = weapon.TH_Regular_Attack_3;
                }
            }
        }

        // Атака пропускается, если у оружия не задана анимация (например, Unarmed): иначе PlayTargetAnimation
        // ставил isInteracting = true, переход в пустое состояние не выполнялся, и персонаж замирал.
        public void HandleLightAttack(WeaponItem weapon)
        {
            if (weapon == null)
                return;

            // В двуручном хвате проверяем двуручную анимацию: её может не быть, даже если одноручная задана.
            string firstAttack = inputHandler.twoHandFlag ? weapon.TH_Regular_Attack_1 : weapon.OH_Light_Attack_1;
            if (string.IsNullOrEmpty(firstAttack))
                return;

            weaponSlotManager.attackingWeapon = weapon;

            if (inputHandler.twoHandFlag)
            {
                animatorHandler.PlayTargetAnimation(weapon.TH_Regular_Attack_1, true);
                lastAttack = weapon.TH_Regular_Attack_1;
            }
            else
            {
                weaponSlotManager.attackingWeapon = weapon;
                animatorHandler.PlayTargetAnimation(weapon.OH_Light_Attack_1, true);
                lastAttack = weapon.OH_Light_Attack_1;
            }
        }

        public void HandleHeavyAttack(WeaponItem weapon)
        {
            if (weapon == null || string.IsNullOrEmpty(weapon.OH_Heavy_Attack_1))
                return;

            weaponSlotManager.attackingWeapon = weapon;

            if (inputHandler.twoHandFlag)
            {
                
            }
            else
            {
                weaponSlotManager.attackingWeapon = weapon;
                animatorHandler.PlayTargetAnimation(weapon.OH_Heavy_Attack_1, true);
                lastAttack = weapon.OH_Heavy_Attack_1;
            }
        }
    }
}
