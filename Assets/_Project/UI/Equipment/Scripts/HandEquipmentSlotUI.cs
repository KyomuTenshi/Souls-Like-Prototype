using UnityEngine;
using UnityEngine.UI;

namespace SG {
    public class HandEquipmentSlotUI : MonoBehaviour
    {
        UIManager uiManager;

        public Image icon;
        WeaponItem weaponItem;

        public bool rightHandSlot01;
        public bool rightHandSlot02;
        public bool leftHandSlot01;
        public bool leftHandSlot02;

        private void Awake()
        {
            uiManager = GetComponentInParent<UIManager>(true);
        }

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

        public void SelectThisSlot()
        {
            // В туториале флаг только ставится. Если раньше был выбран другой слот, флагов true стало бы два,
            // и EquipThisItem положил бы оружие в первый по порядку проверок, а не в нажатый.
            uiManager.ResetAllSelectedSlots();

            if (rightHandSlot01)
            {
                uiManager.rightHandSlot01Selected = true;
            }
            else if (rightHandSlot02)
            {
                uiManager.rightHandSlot02Selected = true;
            }
            else if (leftHandSlot01)
            {
                uiManager.leftHandSlot01Selected = true;
            }
            else 
            {
                uiManager.leftHandSlot02Selected = true;
            }
        }
    }
}