using UnityEngine;

namespace SG {
    public class WeaponSlotManager : MonoBehaviour
    {
        WeaponHolderSlot leftHandSlot;
        WeaponHolderSlot rightHandSlot;

        DamageCollider leftHandDamageCollider;
        DamageCollider rightHandDamageCollider;

        private void Awake()
        {
            WeaponHolderSlot[] weaponHolderSlots = GetComponentsInChildren<WeaponHolderSlot>();
            foreach (WeaponHolderSlot weaponSlot in weaponHolderSlots)
            {
                if(weaponSlot.isLeftHandSlot)
                {
                    leftHandSlot = weaponSlot;
                } else if (weaponSlot.isRightHandSlot)
                {
                   rightHandSlot = weaponSlot; 
                }
            }
        }

        public void LoadWeaponOnSlot(WeaponItem weaponItem, bool isLeft)
        {
            if(isLeft)
            {
                leftHandSlot.LoadWeaponModel(weaponItem);
                LoadLeftHandDamageCollider();
            } else
            {
                rightHandSlot.LoadWeaponModel(weaponItem);
                LoadRightHandDamageCollider();
            }
        }

        #region Handle Weapon's Damage Collider
        public void LoadLeftHandDamageCollider()
        {
            // GetComponentInChildren, а не GetComponent — DamageCollider может
            // висеть не на корне модели оружия, а на вложенном объекте
            // (например, на самом лезвии). GetComponent такое не найдёт и молча
            // вернёт null.
            leftHandDamageCollider = leftHandSlot.currentWeaponModel.GetComponentInChildren<DamageCollider>();
        }

        public void LoadRightHandDamageCollider()
        {
            // БЫЛО: rightHandDamageCollider = leftHandSlot.currentWeaponModel...
            // Копипаст-опечатка — брали модель оружия из ЛЕВОЙ руки, хотя грузили
            // оружие в правую. У leftHandSlot.currentWeaponModel в этот момент
            // ничего не было (null), отсюда UnassignedReferenceException, а следом
            // NullReferenceException в OpenRightHandDamageCollider, потому что
            // rightHandDamageCollider так и не успевал получить значение.
            rightHandDamageCollider = rightHandSlot.currentWeaponModel.GetComponentInChildren<DamageCollider>();
        }

        public void OpenLeftHandDamageCollider()
        {
            leftHandDamageCollider.EnableDamageCollider();
        }

        public void OpenRightHandDamageCollider()
        {
            rightHandDamageCollider.EnableDamageCollider(); 
        }

        public void CloseLeftHandDamageCollider()
        {
            // БЫЛО: вызывался EnableDamageCollider() — "закрытие" на самом деле
            // включало коллайдер, а не выключало.
            leftHandDamageCollider.DisaleDamageCollider();
        }

        public void CloseRightHandDamageCollider()
        {
            // Та же опечатка, что и выше.
            rightHandDamageCollider.DisaleDamageCollider();
        }

        #endregion
    }
}