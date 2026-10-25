using UnityEngine;

namespace SG {
    public class UIManager : MonoBehaviour
    {
        public PlayerInventory playerInventory;
        public EquipmentWindowUI equipmentWindowUI;

        [Header("UI Windows")]
        public GameObject hudWindow;
        public GameObject selectWindow;
        public GameObject equipmentScreenWindow;
        public GameObject weaponInventoryWindow;

        [Header("Equipment Window Slot Selected")]
        public bool rightHandSlot01Selected;
        public bool rightHandSlot02Selected;
        public bool leftHandSlot01Selected;
        public bool leftHandSlot02Selected;

        [Header("Weapon Inventory Slots")]
        public GameObject weaponInventorySlotPrefab;
        public Transform weaponInventorySlotParent;
        WeaponInventorySlot[] weaponInventorySlots;

        public void Start()
        {
            weaponInventorySlots = weaponInventorySlotParent.GetComponentsInChildren<WeaponInventorySlot>(true);
            equipmentWindowUI.LoadWeaponsOnEquipmentScreen(playerInventory);

            // Курсор прячется сразу при старте (в туториале — только после первого закрытия меню):
            // иначе при повороте камеры он уходил за край окна, а клик мог попасть в другое окно.
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        public void UpdateUI()
        {
            #region Weapon Inventory Slots

            for (int i = 0; i < weaponInventorySlots.Length; i++)
            {
                if (i < playerInventory.weaponInventory.Count)
                {
                    if (weaponInventorySlots.Length < playerInventory.weaponInventory.Count)
                    {
                        Instantiate(weaponInventorySlotPrefab, weaponInventorySlotParent);
                        weaponInventorySlots = weaponInventorySlotParent.GetComponentsInChildren<WeaponInventorySlot>(true);
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
            equipmentScreenWindow.SetActive(false);
            ResetAllSelectedSlots();
        }

        public void OpenWeaponInventoryWindow()
        {
            equipmentScreenWindow.SetActive(false);
            weaponInventoryWindow.SetActive(true);
        }

        public void OpenEquipmentWindow()
        {
            ResetAllSelectedSlots();
            weaponInventoryWindow.SetActive(false);
            equipmentScreenWindow.SetActive(true);
        }

        public void ResetAllSelectedSlots()
        {
            rightHandSlot01Selected = false;
            rightHandSlot02Selected = false;
            leftHandSlot01Selected = false;
            leftHandSlot02Selected = false;
        }
    }
}