using System.Collections;
using System.Collections.Generic;
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

        // Инициализация прямо тут (а не через Awake): список должен быть
        // готов к первому Add() даже если что-то подберёт оружие раньше,
        // чем этот компонент пройдёт Awake — например, если подбор случится
        // в тот же кадр, что и Instantiate игрока.
        public List<WeaponItem> weaponsInventory = new List<WeaponItem>();

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
        // Пустые слоты пропускаются за ОДНО нажатие. Цикл: слот 0 -> слот 1
        // -> ... -> unarmed -> слот 0.
        private void ChangeWeapon(bool isLeft)
        {
            WeaponItem[] slots = isLeft ? weaponInLeftHandSlots : weaponInRightHandSlots;
            int currentIndex = isLeft ? currentLeftWeaponIndex : currentRightWeaponIndex;

            int nextIndex = -1;
            for (int i = currentIndex + 1; i < slots.Length; i++)
            {
                if (slots[i] != null)
                {
                    nextIndex = i;
                    break;
                }
            }

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