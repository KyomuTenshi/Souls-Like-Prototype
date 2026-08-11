using UnityEngine;

namespace SG {
    public class PlayerAttacker : MonoBehaviour
    {
        AnimatorHandler animatorHandler;
        PlayerManager playerManager;
        InputHandler inputHandler;
        WeaponSlotManager weaponSlotManager;
        PlayerStats playerStats;
        public string lastAttack;

        [Header("Attack Orientation (NieR-style)")]
        // Перед атакой (и каждым шагом комбо) персонаж доворачивается в
        // сторону текущего ввода движения: удар идёт туда, куда игрок держит
        // стик в момент нажатия, а не куда персонаж смотрел до него.
        [SerializeField] bool orientAttacksToInput = true;

        private void Awake()
        {
            animatorHandler = GetComponentInChildren<AnimatorHandler>();
            weaponSlotManager = GetComponentInChildren<WeaponSlotManager>();
            playerManager = GetComponent<PlayerManager>();
            inputHandler = GetComponent<InputHandler>();
            playerStats = GetComponent<PlayerStats>();
        }

        #region Stamina Gates
        // Action-режим: лёгкие атаки и комбо не гейтятся вообще — комбо-флоу
        // нельзя оборвать пустой стаминой. Souls-режим: правило туториала.
        // playerStats == null — гейт тихо отключается.
        private bool HasStaminaForLightAttack()
        {
            if (playerStats == null)
                return true;

            if (playerStats.IsActionMode)
                return true;

            return playerStats.HasStamina();
        }

        // Тяжёлые гейтятся в обоих режимах — в Action это единственный
        // потребитель стамины, ради которого бар и живёт.
        private bool HasStaminaForHeavyAttack()
        {
            return playerStats == null || playerStats.HasStamina();
        }
        #endregion

        // Корень CameraHandler'а, а не Camera.main: у камеры forward наклонён
        // по pitch и загрязнял бы направление доворота.
        private void OrientTowardsInputDirection()
        {
            if (!orientAttacksToInput)
                return;

            if (inputHandler == null || inputHandler.moveAmount <= 0.1f)
                return;

            CameraHandler cameraHandler = CameraHandler.singleton;
            if (cameraHandler == null)
                return;

            Vector3 direction = cameraHandler.transform.forward * inputHandler.vertical;
            direction += cameraHandler.transform.right * inputHandler.horizontal;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.0001f)
                return;

            transform.rotation = Quaternion.LookRotation(direction.normalized);
        }

        #region Combo
        public void HandleWeaponCombo(WeaponItem weapon)
        {
            if (weapon == null)
                return;

            if (!HasStaminaForLightAttack())
                return;

            if (inputHandler.comboFlag)
            {
                animatorHandler.DisableConbo();

                // Шаги комбо держат attackingWeapon актуальным: stamina-события
                // (DrainStamina*) на клипах читают именно его.
                weaponSlotManager.attackingWeapon = weapon;

                if (lastAttack == weapon.OH_Light_Attack_1)
                {
                    OrientTowardsInputDirection();
                    animatorHandler.PlayeTargetAnimation(weapon.OH_Light_Attack_2, true);
                    lastAttack = weapon.OH_Light_Attack_2;
                }
                else if (lastAttack == weapon.OH_Light_Attack_2)
                {
                    OrientTowardsInputDirection();
                    animatorHandler.PlayeTargetAnimation(weapon.OH_Light_Attack_3, true);
                    lastAttack = weapon.OH_Light_Attack_3;
                }
                else if (lastAttack == weapon.OH_Light_Attack_3)
                {
                    OrientTowardsInputDirection();
                    animatorHandler.PlayeTargetAnimation(weapon.OH_Light_Attack_4, true);
                    lastAttack = weapon.OH_Light_Attack_4;
                }
                else if (!string.IsNullOrEmpty(weapon.OH_Heavy_Attack_1) && lastAttack == weapon.OH_Heavy_Attack_1)
                {
                    // Возврат из тяжёлого финишера в лёгкую цепочку
                    // (light-heavy-light, как в NieR). Ветка спит, пока на
                    // клипе тяжёлой атаки нет событий EnableCombo/
                    // ComboWindowClosed; расставишь — заработает без правок.
                    OrientTowardsInputDirection();
                    animatorHandler.PlayeTargetAnimation(weapon.OH_Light_Attack_1, true);
                    lastAttack = weapon.OH_Light_Attack_1;
                }
            }
        }

        // Тяжёлый финишер из комбо-окна: heavy посреди лёгкой цепочки ветвит
        // её в OH_Heavy_Attack_1, не дожидаясь конца лёгких ударов.
        public void HandleHeavyComboFinisher(WeaponItem weapon)
        {
            if (weapon == null)
                return;

            if (!HasStaminaForHeavyAttack())
                return;

            if (string.IsNullOrEmpty(weapon.OH_Heavy_Attack_1))
            {
                Debug.LogWarning("OH_Heavy_Attack_1 не задан на оружии " + weapon.itemName);
                return;
            }

            if (inputHandler.comboFlag)
            {
                animatorHandler.DisableConbo();

                OrientTowardsInputDirection();

                weaponSlotManager.attackingWeapon = weapon;
                animatorHandler.PlayeTargetAnimation(weapon.OH_Heavy_Attack_1, true);
                lastAttack = weapon.OH_Heavy_Attack_1;
            }
        }
        #endregion

        #region Basic Attacks
        public void HandleLightAttack(WeaponItem weapon)
        {
            if (weapon == null)
            {
                Debug.LogWarning("PlayerAttacker: атака без оружия — проверь, что unarmedWeapon назначен в PlayerInventory.");
                return;
            }

            if (playerManager.isIntetacting)
                return;

            if (!HasStaminaForLightAttack())
                return;

            if (string.IsNullOrEmpty(weapon.OH_Light_Attack_1))
            {
                Debug.LogWarning("OH_Light_Attack_1 не задан на оружии " + weapon.itemName);
                return;
            }

            OrientTowardsInputDirection();

            weaponSlotManager.attackingWeapon = weapon;
            animatorHandler.PlayeTargetAnimation(weapon.OH_Light_Attack_1, true);
            lastAttack = weapon.OH_Light_Attack_1;
        }

        public void HandleHeavytAttack(WeaponItem weapon)
        {
            if (weapon == null)
            {
                Debug.LogWarning("PlayerAttacker: атака без оружия — проверь, что unarmedWeapon назначен в PlayerInventory.");
                return;
            }

            if (playerManager.isIntetacting)
                return;

            if (!HasStaminaForHeavyAttack())
                return;

            if (string.IsNullOrEmpty(weapon.OH_Heavy_Attack_1))
            {
                Debug.LogWarning("OH_Heavy_Attack_1 не задан на оружии " + weapon.itemName);
                return;
            }

            OrientTowardsInputDirection();

            weaponSlotManager.attackingWeapon = weapon;
            animatorHandler.PlayeTargetAnimation(weapon.OH_Heavy_Attack_1, true);
            lastAttack = weapon.OH_Heavy_Attack_1;
        }
        #endregion
    }
}