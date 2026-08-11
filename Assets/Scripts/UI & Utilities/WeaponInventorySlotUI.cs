using UnityEngine;
using UnityEngine.UI;

namespace SG {
    // Слот списка инвентаря (окно "все подобранные оружия"). Вешается на
    // ячейку в Canvas; UIManager.UpdateUI() заполняет ячейки из
    // PlayerInventory.weaponsInventory. Выбор/экипировка по клику — тема
    // следующих уроков туториала.
    public class WeaponInventorySlotUI : MonoBehaviour
    {
        public Image icon;
        WeaponItem item;

        public void AddItem(WeaponItem newItem)
        {
            // Тот же guard, что в HandEquipmentSlotUI: незаполненный Icon на
            // одной ячейке не должен NRE ронять весь цикл UIManager.UpdateUI()
            // и оставлять остальные слоты необновлёнными.
            if (icon == null)
            {
                Debug.LogWarning("WeaponInventorySlotUI: поле Icon не назначено на " + gameObject.name + ".", this);
                return;
            }

            if (newItem == null)
            {
                ClearItem();
                return;
            }

            item = newItem;
            icon.sprite = item.itemIcon;
            icon.enabled = true;
            gameObject.SetActive(true);
        }

        public void ClearItem()
        {
            item = null;

            if (icon == null)
                return;

            icon.sprite = null;
            icon.enabled = false;
            gameObject.SetActive(false);
        }
    }
}