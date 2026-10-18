using UnityEngine;
using UnityEngine.UI;

namespace SG {
    public class HandEquipmentSlotUI : MonoBehaviour
    {
        public Image icon;
        WeaponItem weaponItem;

        public bool rightHandSlot01;
        public bool rightHandSlot02;
        public bool leftHandSlot01;
        public bool leftHandSlot02;

        public void AddItem(WeaponItem newItem)
        {
            weaponItem = newItem;

            if (weaponItem == null)
            {
                icon.sprite = null;
                icon.enabled = false;
                return;
            }

            icon.sprite = weaponItem.itemIcon;
            icon.enabled = true;
            gameObject.SetActive(true);
        }

        public void ClearItem()
        {
            weaponItem = null;
            icon.sprite = null;
            icon.enabled = false;
            gameObject.SetActive(false);
        }

    }
}