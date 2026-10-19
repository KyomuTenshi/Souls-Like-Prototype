using UnityEngine;
using UnityEngine.UI;

namespace SG {
    public class WeaponInventorySlot : MonoBehaviour
    {
        PlayerInventory playerInventory;
        UIManager uiManager;

        public Image icon;
        WeaponItem item;

        private void Awake()
        {
            playerInventory = FindFirstObjectByType<PlayerInventory>();
            uiManager = GetComponentInParent<UIManager>(true);
        }

        public void AddItem(WeaponItem newItem)
        {
            item = newItem;
            icon.sprite = item.itemIcon;
            icon.enabled = true;
            gameObject.SetActive(true);
        }

        public void ClearInventorySlot()
        {
            item = null;
            icon.sprite = null;
            icon.enabled = false;
            gameObject.SetActive(false);
        }

        public void EquipThisItem()
        {
            EquipmentWindowUI equipmentWindowUI = uiManager.equipmentWindowUI;

            // Окно инвентаря открыто не из окна экипировки — выбранного слота нет, экипировать некуда.
            if (item == null || !equipmentWindowUI.TryGetSelectedSlot(out bool isLeft, out int slotIndex))
                return;

            playerInventory.EquipWeaponInHandSlot(item, isLeft, slotIndex);

            equipmentWindowUI.ResetAllSelectedSlots();
            equipmentWindowUI.LoadWeaponsOnEquipmentScreen(playerInventory);
            uiManager.UpdateUI();
            uiManager.OpenEquipmentWindow();
        }
    }
}
