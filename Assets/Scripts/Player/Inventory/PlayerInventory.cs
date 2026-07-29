using System.Collections;
using UnityEngine;

namespace SG {
    public class PlayerInventory : MonoBehaviour
    {
        WeaponSlotManager weaponSlotManager;

        public WeaponItem rightWeapon;
        public WeaponItem leftWeapon;

        public WeaponItem unarmedWeapon;

        public WeaponItem[] weaponInRightHandSlots = new WeaponItem[2];
        public WeaponItem[] weaponInLeftHandSlots = new WeaponItem[2];

        public int currentRightWeaponIndex = -1;
        public int currentLeftWeaponIndex = -1;

        private void Awake()
        {
            weaponSlotManager = GetComponentInChildren<WeaponSlotManager>();
        }

        private IEnumerator Start()
        {
            rightWeapon = unarmedWeapon;
            leftWeapon = unarmedWeapon;

            weaponSlotManager.LoadWeaponOnSlot(rightWeapon, false);
            weaponSlotManager.LoadWeaponOnSlot(leftWeapon, true);

            yield return null;

            weaponSlotManager.LoadWeaponOnSlot(rightWeapon, false);
            weaponSlotManager.LoadWeaponOnSlot(leftWeapon, true);
        }

        public void ChangeRightWeapon()
        {
            ChangeWeapon(false);
        }

        public void ChangeLeftWeapon()
        {
            ChangeWeapon(true);
        }

        // Единая логика цикла для обеих рук вместо двух копий лестницы if'ов.
        // Заодно исправлен баг туториала: пустые слоты пропускаются за ОДНО
        // нажатие (раньше при незанятых слотах требовалось два нажатия d-pad,
        // чтобы дойти до unarmed). Цикл: слот 0 -> слот 1 -> ... -> unarmed -> слот 0.
        private void ChangeWeapon(bool isLeft)
        {
            WeaponItem[] slots = isLeft ? weaponInLeftHandSlots : weaponInRightHandSlots;
            int currentIndex = isLeft ? currentLeftWeaponIndex : currentRightWeaponIndex;

            // Ищем следующий занятый слот после текущего.
            int nextIndex = -1;
            for (int i = currentIndex + 1; i < slots.Length; i++)
            {
                if (slots[i] != null)
                {
                    nextIndex = i;
                    break;
                }
            }

            // Занятых дальше нет — возвращаемся к безоружному (индекс -1),
            // следующее нажатие снова начнёт цикл со слота 0.
            WeaponItem newWeapon = nextIndex >= 0 ? slots[nextIndex] : unarmedWeapon;

            if (isLeft)
            {
                currentLeftWeaponIndex = nextIndex;
                leftWeapon = newWeapon;
            }
            else
            {
                currentRightWeaponIndex = nextIndex;
                rightWeapon = newWeapon;
            }

            weaponSlotManager.LoadWeaponOnSlot(newWeapon, isLeft);
        }
    }
}