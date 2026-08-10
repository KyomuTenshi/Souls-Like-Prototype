using UnityEngine;

namespace SG {
    public class UIManager : MonoBehaviour
    {
        // Можно назначить в инспекторе; если пусто — найдём сами в Start.
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

        // БЫЛО: окно открывалось, но НИЧЕМ не заполнялось — ни экран
        // экипировки (LoadWeaponOnEquipmentScreen никто не вызывал), ни
        // список инвентаря. Теперь при каждом открытии данные обновляются —
        // так подобранное между открытиями оружие сразу видно в списке.
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

                // Слоты — заранее расставленные ячейки в Canvas. Если оружия
                // в списке больше, чем ячеек, лишнее просто не показывается.
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

            // Пока открыто меню, курсор нужен для кликов по кнопкам —
            // PlayerManager.Awake() держит его залоченным и невидимым для
            // 3rd-person камеры, без этого мышь физически не дотянется
            // до UI-элементов.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void CloseSelectWindow()
        {
            if (selectWindow == null)
                return;

            selectWindow.SetActive(false);

            // Возвращаем курсор в состояние геймплея (см. PlayerManager.Awake).
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}