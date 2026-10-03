using UnityEngine;

namespace SG {
    public class UIManager : MonoBehaviour
    {
        public PlayerInventory playerInventory;

        [Header("UI Windows")]
        public GameObject hudWindow;
        public GameObject selectWindow;
        public GameObject weaponInventoryWindow;

        [Header("Weapon Inventory Slots")]
        public GameObject weaponInventorySlotPrefab;
        public Transform weaponInventorySlotParent;
        WeaponInventorySlot[] weaponInventorySlots;

        public void Start()
        {
            weaponInventorySlots = weaponInventorySlotParent.GetComponentsInChildren<WeaponInventorySlot>();
        }
        public void UpdateUI()
        {
            #region Weapon Inventory Slots

            for (int i = 0; i < weaponInventorySlots.Length; i++)
            {
                if (i < playerInventory.weaponInventory.Count)
                {
                    if (weaponInventorySlots.Length > playerInventory.weaponInventory.Count)
                    {
                        Instantiate(weaponInventorySlotPrefab, weaponInventorySlotParent);
                        weaponInventorySlots = weaponInventorySlotParent.GetComponentsInChildren<WeaponInventorySlot>();
                    }
                    weaponInventorySlots[i].AddItem(playerInventory.weaponInventory[i]);
                }
                else
                {
                    weaponInventorySlots[i].ClearInventorySlot();
                    
                }
            }
            #endregion
        }

        public void OpenSelectWindow()
        {
            selectWindow.SetActive(true);
            // Показываем курсор
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void CloseSelectWindow()
        {
            selectWindow.SetActive(false);
            // Прячем курсор обратно в игру
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void CloseAllInventoryWindows()
        {
            weaponInventoryWindow.SetActive(false);
        }
    }
}