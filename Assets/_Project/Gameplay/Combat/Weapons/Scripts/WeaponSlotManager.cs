using UnityEngine;

namespace SG {
    public class WeaponSlotManager : MonoBehaviour
    {
        WeaponHolderSlot leftHandSlot;
        WeaponHolderSlot rightHandSlot;

        DamageCollider leftHandDamageCollider;
        DamageCollider rightHandDamageCollider;

        public WeaponItem attackingWeapon;

        Animator animator;
        QuickSlotUI quickSlotUI;
        PlayerStats playerStats;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            quickSlotUI = FindFirstObjectByType<QuickSlotUI>();
            playerStats = GetComponentInParent<PlayerStats>();

            WeaponHolderSlot[] weaponHolderSlots = GetComponentsInChildren<WeaponHolderSlot>();
            foreach (WeaponHolderSlot weaponSlot in weaponHolderSlots)
            {
                if (weaponSlot.isLeftHandSlot)
                {
                    leftHandSlot = weaponSlot;
                }
                else if (weaponSlot.isRightHandSlot)
                {
                    rightHandSlot = weaponSlot;
                }
            }
        }

        public void LoadWeaponOnSlot(WeaponItem weaponItem, bool isLeft)
        {
            if (isLeft)
            {
                leftHandSlot.LoadWeaponModel(weaponItem);
                LoadLeftWeaponDamageCollider();
                
                if (quickSlotUI != null)
                    quickSlotUI.UpdateWeaponQuickSlotUI(true, weaponItem);

                #region Handle Left Weapon Idle Animations
                if (weaponItem != null)
                {
                    animator.CrossFade(weaponItem.left_hand_idle, 0.2f);
                }
                else
                {
                    animator.CrossFade("Left Arm Empty", 0.2f);
                }
                #endregion
            }
            else
            {
                rightHandSlot.LoadWeaponModel(weaponItem);
                LoadRightWeaponDamageCollider();
                
                if (quickSlotUI != null)
                    quickSlotUI.UpdateWeaponQuickSlotUI(false, weaponItem);

                #region Handle Right Weapon Idle Animations
                if (weaponItem != null)
                {
                    animator.CrossFade(weaponItem.right_hand_idle, 0.2f);
                }
                else
                {
                    animator.CrossFade("Right Arm Empty", 0.2f);
                }
                #endregion
            }
        }

        #region Handle Weapon's Damage Collider

        private void LoadLeftWeaponDamageCollider()
        {
            if (leftHandSlot.currentWeaponModel != null)
                leftHandDamageCollider = leftHandSlot.currentWeaponModel.GetComponentInChildren<DamageCollider>();
            else
                leftHandDamageCollider = null;
        }

        private void LoadRightWeaponDamageCollider()
        {
            if (rightHandSlot.currentWeaponModel != null)
                rightHandDamageCollider = rightHandSlot.currentWeaponModel.GetComponentInChildren<DamageCollider>();
            else
                rightHandDamageCollider = null;
        }

        public void OpenRightDamageCollider()
        {
            if (rightHandDamageCollider != null)
                rightHandDamageCollider.EnableDamageCollider();
        }

        public void OpenLeftDamageCollider()
        {
            if (leftHandDamageCollider != null)
                leftHandDamageCollider.EnableDamageCollider();
        }

        public void CloseRightHandDamageCollider()
        {
            if (rightHandDamageCollider != null)
                rightHandDamageCollider.DisableDamageCollider();
        }

        public void CloseLeftHandDamageCollider()
        {
            if (leftHandDamageCollider != null)
                leftHandDamageCollider.DisableDamageCollider();
        }

        #endregion

        #region Handle Weapon's Stamina Drain
        public void DrainStaminaLightAttack()
        {
            if (attackingWeapon != null)
                playerStats.TakeStaminaDamage(Mathf.RoundToInt(attackingWeapon.baseStamina * attackingWeapon.lightAttackMultiplier));
        }

        public void DrainStaminaHeavyAttack()
        {
            if (attackingWeapon != null)
                playerStats.TakeStaminaDamage(Mathf.RoundToInt(attackingWeapon.baseStamina * attackingWeapon.heavyAttackMultiplier));
        }
        #endregion
    }
}