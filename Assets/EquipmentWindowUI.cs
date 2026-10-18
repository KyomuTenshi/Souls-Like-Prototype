using UnityEngine;

namespace SG {
    public class EquipmentWindowUI : MonoBehaviour
    {
        public bool rightHandSlot01Selected;
        public bool rightHandSlot02Selected;
        public bool leftHandSlot01Selected;
        public bool leftHandSlot02Selected;

        HandEquipmentSlotUI[] handEquipmentSlots;

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
            rightHandSlot01Selected = true;
        }

        public void SelectRightHandSlot02()
        {
            rightHandSlot02Selected = true;
        }
        
        public void SelectLeftHandSlot01()
        {
            leftHandSlot01Selected = true;
        }

        public void SelectLeftHandSlot02()
        {
            leftHandSlot02Selected = true;
        }
    }
}