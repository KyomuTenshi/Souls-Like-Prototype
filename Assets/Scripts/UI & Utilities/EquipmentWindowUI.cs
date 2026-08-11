using UnityEngine;

namespace SG {
    public class EquipmentWindowUI : MonoBehaviour
    {
        public bool rightHandSlot01Selected;
        public bool rightHandSlot02Selected;
        public bool leftHandSlot01Selected;
        public bool leftHandSlot02Selected;

        HandEquipmentSlotUI[] handEquipmentSlotUI;

        private void Awake()
        {
            CollectSlotsIfNeeded();
        }

        // Сбор ленивый и в Awake, а не в Start: окно на старте сцены
        // выключено, а UIManager после SetActive(true) обращается к слотам
        // в тот же кадр — Start к этому моменту ещё не выполнился бы.
        private void CollectSlotsIfNeeded()
        {
            if (handEquipmentSlotUI == null || handEquipmentSlotUI.Length == 0)
            {
                handEquipmentSlotUI = GetComponentsInChildren<HandEquipmentSlotUI>(true);
            }
        }

        public void LoadWeaponOnEquipmentScreen(PlayerInventory playerInventory)
        {
            if (playerInventory == null)
                return;

            CollectSlotsIfNeeded();

            for (int i = 0; i < handEquipmentSlotUI.Length; i++)
            {
                if (handEquipmentSlotUI[i].rightHandSlot01)
                {
                    handEquipmentSlotUI[i].AddItem(GetWeaponAt(playerInventory.weaponsInRightHandSlots, 0));
                }
                else if (handEquipmentSlotUI[i].rightHandSlot02)
                {
                    handEquipmentSlotUI[i].AddItem(GetWeaponAt(playerInventory.weaponsInRightHandSlots, 1));
                }
                else if (handEquipmentSlotUI[i].leftHandSlot01)
                {
                    handEquipmentSlotUI[i].AddItem(GetWeaponAt(playerInventory.weaponsInLeftHandSlots, 0));
                }
                else
                {
                    handEquipmentSlotUI[i].AddItem(GetWeaponAt(playerInventory.weaponsInLeftHandSlots, 1));
                }
            }
        }

        // Слота с этим индексом может не существовать — размер массива
        // задаётся в инспекторе. null означает пустую ячейку, AddItem(null)
        // корректно её отрисует.
        private WeaponItem GetWeaponAt(WeaponItem[] slots, int index)
        {
            if (slots == null || index < 0 || index >= slots.Length)
                return null;

            return slots[index];
        }

        public void SelectRightHandSlot01()
        {
            rightHandSlot01Selected = true;
            rightHandSlot02Selected = false;
            leftHandSlot01Selected = false;
            leftHandSlot02Selected = false;
        }

        public void SelectRightHandSlot02()
        {
            rightHandSlot01Selected = false;
            rightHandSlot02Selected = true;
            leftHandSlot01Selected = false;
            leftHandSlot02Selected = false;
        }

        public void SelectLeftHandSlot01()
        {
            rightHandSlot01Selected = false;
            rightHandSlot02Selected = false;
            leftHandSlot01Selected = true;
            leftHandSlot02Selected = false;
        }

        public void SelectLeftHandSlot02()
        {
            rightHandSlot01Selected = false;
            rightHandSlot02Selected = false;
            leftHandSlot01Selected = false;
            leftHandSlot02Selected = true;
        }
    }
}