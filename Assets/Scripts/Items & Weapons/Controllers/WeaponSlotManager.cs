using UnityEngine;

namespace SG {
    public class WeaponSlotManager : MonoBehaviour
    {
        [Header("Animator Layers")]
        // Имена СЛОЁВ в Animator Controller, не имена состояний. Слой не
        // найден — index = -1, CrossFade ищет состояние по всем слоям.
        [SerializeField] private string leftArmLayerName = "Left Arm";
        [SerializeField] private string rightArmLayerName = "Right Arm";

        public WeaponItem attackingWeapon;

        WeaponHolderSlot leftHandSlot;
        WeaponHolderSlot rightHandSlot;

        DamageCollider leftHandDamageCollider;
        DamageCollider rightHandDamageCollider;

        Animator animator;
        QuickSlotsUI quickSlotsUI;

        PlayerStats playerStats;

        int leftArmLayerIndex;
        int rightArmLayerIndex;

        private void Awake()
        {
            animator = GetComponent<Animator>();

            // Include — выключенный на старте канвас не должен оставить
            // quickSlotsUI == null молча.
            quickSlotsUI = FindAnyObjectByType<QuickSlotsUI>(FindObjectsInactive.Include);

            leftArmLayerIndex = animator.GetLayerIndex(leftArmLayerName);
            rightArmLayerIndex = animator.GetLayerIndex(rightArmLayerName);

            playerStats = GetComponentInParent<PlayerStats>();

            if (leftArmLayerIndex < 0)
                Debug.LogWarning($"WeaponSlotManager: слой '{leftArmLayerName}' не найден в Animator Controller — укажи точное имя в инспекторе. Пока идёт поиск по всем слоям.");
            if (rightArmLayerIndex < 0)
                Debug.LogWarning($"WeaponSlotManager: слой '{rightArmLayerName}' не найден в Animator Controller — укажи точное имя в инспекторе. Пока идёт поиск по всем слоям.");

            WeaponHolderSlot[] weaponHolderSlots = GetComponentsInChildren<WeaponHolderSlot>();
            foreach (WeaponHolderSlot weaponSlot in weaponHolderSlots)
            {
                if (weaponSlot.isLeftHandSlot)
                {
                    leftHandSlot = weaponSlot;
                }
                else if (weaponSlot.isRightHandSlot)
                {
                    rightHandSlot = weaponSlot;
                }
            }
        }

        public void LoadWeaponOnSlot(WeaponItem weaponItem, bool isLeft)
        {
            if (isLeft)
            {
                leftHandSlot.LoadWeaponModel(weaponItem);
                LoadLeftHandDamageCollider();

                // Урон оружия -> его хитбокс; без этого DamageCollider бил бы
                // своей константой и baseDamage был бы мёртвым полем.
                if (leftHandDamageCollider != null && weaponItem != null)
                {
                    leftHandDamageCollider.currentWeaponDamage = weaponItem.baseDamage;
                }

                #region Handle Weapon Idle Animations
                if (weaponItem != null)
                {
                    animator.CrossFade(weaponItem.left_hand_idle, 0.2f, leftArmLayerIndex);
                }
                else
                {
                    animator.CrossFade("Left Arm Empty", 0.2f, leftArmLayerIndex);
                }
                #endregion
            }
            else
            {
                rightHandSlot.LoadWeaponModel(weaponItem);
                LoadRightHandDamageCollider();

                if (rightHandDamageCollider != null && weaponItem != null)
                {
                    rightHandDamageCollider.currentWeaponDamage = weaponItem.baseDamage;
                }

                #region Handle Weapon Idle Animations
                if (weaponItem != null)
                {
                    animator.CrossFade(weaponItem.right_hand_idle, 0.2f, rightArmLayerIndex);
                }
                else
                {
                    animator.CrossFade("Right Arm Empty", 0.2f, rightArmLayerIndex);
                }
                #endregion
            }

            if (quickSlotsUI != null)
            {
                quickSlotsUI.UpdateWeaponQuickSlotsUI(isLeft, weaponItem);
            }
        }

        #region Handle Weapon's Damage Collider
        public void LoadLeftHandDamageCollider()
        {
            GameObject weaponModel = leftHandSlot.currentWeaponModel;
            leftHandDamageCollider = weaponModel != null
                ? weaponModel.GetComponentInChildren<DamageCollider>()
                : null;
        }

        public void LoadRightHandDamageCollider()
        {
            GameObject weaponModel = rightHandSlot.currentWeaponModel;
            rightHandDamageCollider = weaponModel != null
                ? weaponModel.GetComponentInChildren<DamageCollider>()
                : null;
        }

        // Методы региона зовутся Animation Event'ами из клипов — guard'ы
        // обязательны: события могут прийти без коллайдера/оружия/статов.
        public void OpenLeftHandDamageCollider()
        {
            if (leftHandDamageCollider != null)
                leftHandDamageCollider.EnableDamageCollider();
        }

        public void OpenRightHandDamageCollider()
        {
            if (rightHandDamageCollider != null)
                rightHandDamageCollider.EnableDamageCollider();
        }

        public void CloseLeftHandDamageCollider()
        {
            if (leftHandDamageCollider != null)
                leftHandDamageCollider.DisaleDamageCollider();
        }

        public void CloseRightHandDamageCollider()
        {
            if (rightHandDamageCollider != null)
                rightHandDamageCollider.DisaleDamageCollider();
        }

        #endregion

        #region Handle Weapon Stamina Drain
        public void DrainStaminaLightAttack()
        {
            if (playerStats == null || attackingWeapon == null)
                return;

            // Action-режим (NieR): лёгкие атаки бесплатны. Метод и события в
            // клипах не удаляем — при переключении в Souls всё снова
            // списывается, как в туториале.
            if (playerStats.IsActionMode)
                return;

            playerStats.TakeStaminaDamage(Mathf.RoundToInt(attackingWeapon.baseStamina * attackingWeapon.lightAttackMultiplier));
        }

        public void DrainStaminaHeavyAttack()
        {
            if (playerStats == null || attackingWeapon == null)
                return;

            playerStats.TakeStaminaDamage(Mathf.RoundToInt(attackingWeapon.baseStamina * attackingWeapon.heavyAttackMultiplier));
        }
        #endregion
    }
}