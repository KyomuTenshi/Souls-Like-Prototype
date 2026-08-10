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

        // БЫЛО: GetComponentsInChildren в Start(). Две проблемы:
        // 1) Awake/Start НЕ выполняются у выключенных объектов, а окно
        //    экипировки на старте сцены обычно выключено. UIManager включает
        //    его через SetActive(true) и сразу зовёт LoadWeaponOnEquipmentScreen
        //    — Awake при SetActive выполнится синхронно, а Start только в конце
        //    кадра, т.е. со Start массив был бы ещё null -> NRE.
        // 2) Без (true) выключенные слоты-ячейки не попадали бы в массив.
        // Ленивая инициализация — страховка на случай вызова до Awake.
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

        // БЫЛО: playerInventory.weaponsInRightHandSlots[1] напрямую. Код жёстко
        // предполагал размер массива РОВНО 2 на каждую руку — если в инспекторе
        // задан массив меньше (например, размер 1, как в текущем проекте),
        // обращение по индексу 1 вылетало в IndexOutOfRangeException прямо при
        // открытии окна экипировки. Теперь индекс всегда проверяется против
        // реальной длины массива: слота нет — вернём null, а
        // HandEquipmentSlotUI.AddItem(null) уже умеет показывать пустую ячейку
        // без иконки, ничего не крашится.
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