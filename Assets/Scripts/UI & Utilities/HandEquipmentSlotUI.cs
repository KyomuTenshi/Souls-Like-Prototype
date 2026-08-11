using UnityEngine;
using UnityEngine.UI;

namespace SG {
    public class HandEquipmentSlotUI : MonoBehaviour
    {
        public Image icon;
        WeaponItem weapon;

        public bool rightHandSlot01;
        public bool rightHandSlot02;
        public bool leftHandSlot01;
        public bool leftHandSlot02;

        public void AddItem(WeaponItem newWeapon)
        {
            // Незаполненная ссылка Icon не должна ронять цикл обновления
            // остальных ячеек в EquipmentWindowUI.
            if (icon == null)
            {
                Debug.LogWarning("HandEquipmentSlotUI: поле Icon не назначено на " + gameObject.name + ".", this);
                return;
            }

            // Пустой слот руки — норма. Ячейку не выключаем (SetActive(false)),
            // иначе по ней нельзя будет кликнуть, чтобы экипировать оружие
            // в пустой слот.
            if (newWeapon == null)
            {
                weapon = null;
                icon.sprite = null;
                icon.enabled = false;
                gameObject.SetActive(true);
                return;
            }

            weapon = newWeapon;
            icon.sprite = weapon.itemIcon;
            icon.enabled = true;
            gameObject.SetActive(true);
        }

        public void ClearItem()
        {
            weapon = null;

            if (icon == null)
                return;

            icon.sprite = null;
            icon.enabled = false;
            gameObject.SetActive(false);
        }
    }
}