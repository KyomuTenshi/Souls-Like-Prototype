using UnityEngine;

namespace SG {
    public class UIManager : MonoBehaviour
    {
        public PlayerInventory playerInventory;
        public GameObject selectWindow;

        EquipmentWindowUI equipmentWindowUI;
        WeaponInventorySlotUI[] weaponInventorySlots;

        private void Start()
        {
            if (playerInventory == null)
            {
                playerInventory = FindFirstObjectByType<PlayerInventory>();
            }

            weaponInventorySlots = GetComponentsInChildren<WeaponInventorySlotUI>(true);
            equipmentWindowUI = GetComponentInChildren<EquipmentWindowUI>(true);
        }

        // Данные обновляются при каждом открытии окна — оружие, подобранное
        // между открытиями, сразу видно в списке.
        public void UpdateUI()
        {
            if (playerInventory == null)
            {
                Debug.LogWarning("UIManager: PlayerInventory не найден — окна инвентаря/экипировки заполнить нечем.");
                return;
            }

            if (equipmentWindowUI != null)
            {
                equipmentWindowUI.LoadWeaponOnEquipmentScreen(playerInventory);
            }

            if (weaponInventorySlots != null)
            {
                for (int i = 0; i < weaponInventorySlots.Length; i++)
                {
                    if (i < playerInventory.weaponsInventory.Count)
                    {
                        weaponInventorySlots[i].AddItem(playerInventory.weaponsInventory[i]);
                    }
                    else
                    {
                        weaponInventorySlots[i].ClearItem();
                    }
                }

                if (playerInventory.weaponsInventory.Count > weaponInventorySlots.Length)
                {
                    Debug.LogWarning("UIManager: слотов инвентаря меньше, чем оружия в списке — добавь ячеек WeaponInventorySlotUI в Canvas.");
                }
            }
        }

        public void OpenSelectWindow()
        {
            if (selectWindow == null)
            {
                Debug.LogWarning("UIManager: selectWindow не назначен в инспекторе.");
                return;
            }

            selectWindow.SetActive(true);
            UpdateUI();

            // Для кликов по UI курсор нужно разлочить — в геймплее его
            // держит залоченным PlayerManager.Awake().
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void CloseSelectWindow()
        {
            if (selectWindow == null)
                return;

            selectWindow.SetActive(false);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}