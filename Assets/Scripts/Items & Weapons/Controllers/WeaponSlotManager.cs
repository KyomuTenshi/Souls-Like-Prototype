using UnityEngine;

namespace SG {
    public class WeaponSlotManager : MonoBehaviour
    {
        [Header("Animator Layers")]
        // Имена СЛОЁВ в Animator Controller (вкладка Layers в окне Animator),
        // не имена состояний! Если слой не найден, используем -1 — CrossFade
        // тогда ищет состояние по всем слоям, и всё продолжает работать.
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

            // FindObjectsInactive.Include — чтобы выключенный на старте канвас
            // не оставил quickSlotsUI == null молча.
            quickSlotsUI = FindAnyObjectByType<QuickSlotsUI>(FindObjectsInactive.Include);

            leftArmLayerIndex = animator.GetLayerIndex(leftArmLayerName);
            rightArmLayerIndex = animator.GetLayerIndex(rightArmLayerName);

            playerStats = GetComponentInParent<PlayerStats>();

            // Warning, а не Error: со значением -1 CrossFade корректно найдёт
            // состояние по всем слоям, игра работает. Но лучше указать точное
            // имя слоя в инспекторе, чтобы CrossFade бил ровно в нужный слой.
            if (leftArmLayerIndex < 0)
                Debug.LogWarning($"WeaponSlotManager: слой '{leftArmLayerName}' не найден в Animator Controller — проверь вкладку Layers в окне Animator и укажи точное имя в инспекторе. Пока используется поиск по всем слоям.");
            if (rightArmLayerIndex < 0)
                Debug.LogWarning($"WeaponSlotManager: слой '{rightArmLayerName}' не найден в Animator Controller — проверь вкладку Layers в окне Animator и укажи точное имя в инспекторе. Пока используется поиск по всем слоям.");

            WeaponHolderSlot[] weaponHolderSlots = GetComponentsInChildren<WeaponHolderSlot>();
            foreach (WeaponHolderSlot weaponSlot in weaponHolderSlots)
            {
                if(weaponSlot.isLeftHandSlot)
                {
                    leftHandSlot = weaponSlot;
                } else if (weaponSlot.isRightHandSlot)
                {
                   rightHandSlot = weaponSlot; 
                }
            }
        }

        public void LoadWeaponOnSlot(WeaponItem weaponItem, bool isLeft)
        {
            if(isLeft)
            {
                leftHandSlot.LoadWeaponModel(weaponItem);
                LoadLeftHandDamageCollider();

                #region Handle Weapon Idle Animations
                if (weaponItem != null)
                {
                    animator.CrossFade(weaponItem.left_hand_idle, 0.2f, leftArmLayerIndex);
                } else
                {
                    animator.CrossFade("Left Arm Empty", 0.2f, leftArmLayerIndex);
                }
                #endregion
            } else
            {
                rightHandSlot.LoadWeaponModel(weaponItem);
                LoadRightHandDamageCollider();

                #region Handle Weapon Idle Animations
                if (weaponItem != null)
                {
                    animator.CrossFade(weaponItem.right_hand_idle, 0.2f, rightArmLayerIndex);
                } else
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
            // currentWeaponModel может быть null (безоружный без modelPrefab).
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

        // Guard'ы обязательны: методы вызываются Animation Event'ами из клипов
        // атак — без DamageCollider на текущем "оружии" событие роняло бы NRE.
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
        // Оба метода вызываются Animation Event'ами — guard'ы по той же
        // причине, что у Open/Close выше: событие может сработать, когда
        // attackingWeapon ещё не выставлен (клип запущен не через
        // PlayerAttacker) или playerStats не найден.
        public void DrainStaminaLightAttack()
        {
            if (playerStats == null || attackingWeapon == null)
                return;

            // Action-режим (NieR-style): лёгкие атаки бесплатны. Метод и
            // Animation Event'ы в клипах НЕ удаляем — при переключении
            // обратно в Souls всё снова списывается, как в туториале.
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