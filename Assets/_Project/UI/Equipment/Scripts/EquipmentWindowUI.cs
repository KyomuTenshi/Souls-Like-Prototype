using UnityEngine;

namespace SG {
    public class EquipmentWindowUI : MonoBehaviour
    {
        public bool rightHandSlot01Selected;
        public bool rightHandSlot02Selected;
        public bool leftHandSlot01Selected;
        public bool leftHandSlot02Selected;

        HandEquipmentSlotUI[] handEquipmentSlots;
        UIManager uiManager;

        public void LoadWeaponsOnEquipmentScreen(PlayerInventory playerInventory)
        {
            // Слоты ищутся здесь, а не в Awake/Start (в туториале — в Start): если окно выключено в сцене,
            // его Awake не вызовется до UIManager.Start, массив останется null и будет NullReferenceException.
            handEquipmentSlots ??= GetComponentsInChildren<HandEquipmentSlotUI>(true);

            for (int i = 0; i < handEquipmentSlots.Length; i++)
            {
                if (handEquipmentSlots[i].rightHandSlot01)
                {
                    handEquipmentSlots[i].AddItem(playerInventory.weaponsInRightHandSlots[0]);
                }
                else if (handEquipmentSlots[i].rightHandSlot02)
                {
                    handEquipmentSlots[i].AddItem(playerInventory.weaponsInRightHandSlots[1]);
                }
                else if (handEquipmentSlots[i].leftHandSlot01)
                {
                    handEquipmentSlots[i].AddItem(playerInventory.weaponsInLeftHandSlots[0]);
                }
                else if (handEquipmentSlots[i].leftHandSlot02)
                {
                    handEquipmentSlots[i].AddItem(playerInventory.weaponsInLeftHandSlots[1]);
                }
            }
        }

        public void SelectRightHandSlot01()
        {
            ResetAllSelectedSlots();
            rightHandSlot01Selected = true;
            OpenWeaponInventoryForSelectedSlot();
        }

        public void SelectRightHandSlot02()
        {
            ResetAllSelectedSlots();
            rightHandSlot02Selected = true;
            OpenWeaponInventoryForSelectedSlot();
        }

        public void SelectLeftHandSlot01()
        {
            ResetAllSelectedSlots();
            leftHandSlot01Selected = true;
            OpenWeaponInventoryForSelectedSlot();
        }

        public void SelectLeftHandSlot02()
        {
            ResetAllSelectedSlots();
            leftHandSlot02Selected = true;
            OpenWeaponInventoryForSelectedSlot();
        }

        // Сбрасываем перед выбором нового слота: иначе после клика по двум слотам подряд
        // оба флага остались бы true, и оружие ушло бы не в тот слот.
        public void ResetAllSelectedSlots()
        {
            rightHandSlot01Selected = false;
            rightHandSlot02Selected = false;
            leftHandSlot01Selected = false;
            leftHandSlot02Selected = false;
        }

        public bool TryGetSelectedSlot(out bool isLeft, out int slotIndex)
        {
            isLeft = leftHandSlot01Selected || leftHandSlot02Selected;
            slotIndex = (rightHandSlot02Selected || leftHandSlot02Selected) ? 1 : 0;
            return rightHandSlot01Selected || rightHandSlot02Selected || leftHandSlot01Selected || leftHandSlot02Selected;
        }

        // В туториале окна переключаются через SetActive в OnClick кнопок слотов в инспекторе.
        // Здесь это сделано в коде, чтобы не перенастраивать кнопки в сцене.
        void OpenWeaponInventoryForSelectedSlot()
        {
            uiManager ??= GetComponentInParent<UIManager>(true);
            uiManager.OpenWeaponInventoryWindow();
        }
    }
}
