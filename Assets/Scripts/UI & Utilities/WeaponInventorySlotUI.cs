using UnityEngine;
using UnityEngine.UI;

namespace SG {
    // НОВЫЙ ФАЙЛ. UIManager уже ссылается на WeaponInventorySlotUI
    // (GetComponentsInChildren<WeaponInventorySlotUI>), но самого класса в
    // проекте не было — без него проект вообще не компилируется.
    //
    // Слот списка инвентаря (окно "все подобранные оружия"). Вешается на
    // каждый слот-ячейку внутри окна инвентаря; UIManager.UpdateUI()
    // заполняет ячейки из PlayerInventory.weaponsInventory.
    // Логика выбора/экипировки по клику — тема следующих уроков туториала,
    // сюда она добавится позже, ничего не сломав.
    public class WeaponInventorySlotUI : MonoBehaviour
    {
        public Image icon;
        WeaponItem item;

        public void AddItem(WeaponItem newItem)
        {
            // null-безопасно, по той же схеме, что HandEquipmentSlotUI:
            // пустая ячейка — валидный случай, а не повод для NRE.
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
            icon.sprite = null;
            icon.enabled = false;
            gameObject.SetActive(false);
        }
    }
}