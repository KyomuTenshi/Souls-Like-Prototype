using UnityEngine;
using UnityEngine.UI;

namespace SG {
    public class WeaponInventorySlot : MonoBehaviour
    {
        PlayerInventory playerInventory;
        WeaponSlotManager weaponSlotManager;
        UIManager uiManager;

        public Image icon;
        WeaponItem item;

        private void Awake()
        {
            playerInventory = FindFirstObjectByType<PlayerInventory>();
            // В туториале — FindObjectOfType<WeaponSlotManager>(). Берём менеджер именно игрока:
            // когда у врагов появится свой WeaponSlotManager, поиск по сцене может вернуть вражеский.
            weaponSlotManager = playerInventory.GetComponentInChildren<WeaponSlotManager>();
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
            if (uiManager.rightHandSlot01Selected)
            {
                SwapWithHandSlot(playerInventory.weaponsInRightHandSlots, 0);
            }
            else if (uiManager.rightHandSlot02Selected)
            {
                SwapWithHandSlot(playerInventory.weaponsInRightHandSlots, 1);
            }
            else if (uiManager.leftHandSlot01Selected)
            {
                SwapWithHandSlot(playerInventory.weaponsInLeftHandSlots, 0);
            }
            else if (uiManager.leftHandSlot02Selected)
            {
                SwapWithHandSlot(playerInventory.weaponsInLeftHandSlots, 1);
            }
            else
            {
                // Окно инвентаря открыто не из окна экипировки — слот не выбран, экипировать некуда.
                return;
            }

            // В туториале индекс берётся без проверки. При -1 в руке unarmedWeapon,
            // и weaponsInRightHandSlots[-1] дал бы IndexOutOfRangeException.
            if (playerInventory.currentRightWeaponIndex >= 0)
                playerInventory.rightWeapon = playerInventory.weaponsInRightHandSlots[playerInventory.currentRightWeaponIndex];
            if (playerInventory.currentLeftWeaponIndex >= 0)
                playerInventory.leftWeapon = playerInventory.weaponsInLeftHandSlots[playerInventory.currentLeftWeaponIndex];

            weaponSlotManager.LoadWeaponOnSlot(playerInventory.rightWeapon, false);
            weaponSlotManager.LoadWeaponOnSlot(playerInventory.leftWeapon, true);
            uiManager.equipmentWindowUI.LoadWeaponsOnEquipmentScreen(playerInventory);
            uiManager.ResetAllSelectedSlots();

            // В туториале этого нет: без UpdateUI в списке инвентаря остаётся надетое оружие и не появляется снятое.
            // Окна переключаются здесь, а не в OnClick кнопки: префаб слота не может ссылаться на окна в сцене.
            uiManager.UpdateUI();
            uiManager.OpenEquipmentWindow();
        }

        // Кладёт этот предмет в слот руки, а оружие из слота возвращает в инвентарь.
        void SwapWithHandSlot(WeaponItem[] handSlots, int slotIndex)
        {
            // В туториале старое оружие добавляется без проверки: из пустого слота в инвентарь попал бы null,
            // и UpdateUI упал бы на item.itemIcon.
            if (handSlots[slotIndex] != null)
                playerInventory.weaponInventory.Add(handSlots[slotIndex]);

            handSlots[slotIndex] = item;
            playerInventory.weaponInventory.Remove(item);
        }
    }
}
