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
            // Guard на сам icon: раньше при null-ссылке в инспекторе исключение
            // прерывало ВЕСЬ цикл в EquipmentWindowUI.LoadWeaponOnEquipmentScreen —
            // не обновлялась не только эта ячейка, но и все остальные после неё.
            // Теперь одна незаполненная ссылка не валит соседние ячейки, а
            // указывает точный объект — кликни по warning в консоли, чтобы
            // подсветить его в Hierarchy, и перетащи дочерний Image в поле Icon.
            if (icon == null)
            {
                Debug.LogWarning("HandEquipmentSlotUI: поле Icon не назначено на " + gameObject.name + ".", this);
                return;
            }

            // БЫЛО: newWeapon.itemIcon без проверки. Пустой слот руки — это
            // НОРМА (второй слот почти всегда пуст в начале игры), и первое же
            // открытие окна экипировки роняло NRE. Пустой слот показываем как
            // активную ячейку без иконки — НЕ SetActive(false), иначе по ней
            // нельзя будет кликнуть, чтобы экипировать оружие В пустой слот
            // (это тема ближайших уроков туториала).
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