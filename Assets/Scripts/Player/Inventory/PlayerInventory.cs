using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace SG {
    public class PlayerInventory : MonoBehaviour
    {
        WeaponSlotManager weaponSlotManager;

        public WeaponItem rightWeapon;
        public WeaponItem leftWeapon;

        public WeaponItem unarmedWeapon;

        // Имена — под туториал (EquipmentWindowUI и будущие уроки обращаются
        // именно к ним). FormerlySerializedAs сохраняет оружия, назначенные
        // в инспекторе под старым именем.
        [FormerlySerializedAs("weaponInRightHandSlots")]
        public WeaponItem[] weaponsInRightHandSlots = new WeaponItem[2];
        [FormerlySerializedAs("weaponInLeftHandSlots")]
        public WeaponItem[] weaponsInLeftHandSlots = new WeaponItem[2];

        public int currentRightWeaponIndex = -1;
        public int currentLeftWeaponIndex = -1;

        // Инициализация на месте, не в Awake: список должен быть готов к
        // первому Add(), даже если подбор случится раньше Awake компонента.
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

            // Повтор через кадр: первый проход может пройти до готовности
            // Animator/UI (порядок инициализации сцены), второй гарантирует
            // корректные idle-слои и иконки квик-слотов.
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

        // Единая логика цикла для обеих рук. Пустые слоты пропускаются за
        // одно нажатие. Цикл: слот 0 -> слот 1 -> ... -> unarmed -> слот 0.
        private void ChangeWeapon(bool isLeft)
        {
            WeaponItem[] slots = isLeft ? weaponsInLeftHandSlots : weaponsInRightHandSlots;
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