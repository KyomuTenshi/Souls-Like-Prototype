using UnityEngine;

namespace SG {
    public class EquipmentWindowUI : MonoBehaviour
    {
        public HandEquipmentSlotUI[] handEquipmentSlots;

        public void LoadWeaponsOnEquipmentScreen(PlayerInventory playerInventory)
        {
            // Слоты назначены в инспекторе. Если массив забыли заполнить, ищем их сами (в туториале — в Start).
            // Не в Start: окно выключено в сцене, и его Start не вызовется раньше UIManager.Start.
            if (handEquipmentSlots == null || handEquipmentSlots.Length == 0)
                handEquipmentSlots = GetComponentsInChildren<HandEquipmentSlotUI>(true);

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
    }
}
