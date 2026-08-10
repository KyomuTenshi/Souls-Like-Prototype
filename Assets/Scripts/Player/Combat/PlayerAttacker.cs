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
        // Перед атакой персонаж мгновенно доворачивается в сторону текущего
        // ввода движения (камера-относительно). Без этого атака уходила туда,
        // куда персонаж СМОТРЕЛ до нажатия — в NieR/Souls удар всегда идёт
        // туда, куда игрок держит стик в момент нажатия. Работает и для
        // каждого шага комбо (redirect цепочки). Ввода нет — бьём по
        // текущему направлению взгляда, как раньше.
        [SerializeField] bool orientAttacksToInput = true;

        private void Awake()
        {
            animatorHandler = GetComponentInChildren<AnimatorHandler>();
            weaponSlotManager = GetComponentInChildren<WeaponSlotManager>();
            playerManager = GetComponent<PlayerManager>();
            inputHandler = GetComponent<InputHandler>();
            playerStats = GetComponent<PlayerStats>();
        }

        // Гейт разделён по типу атаки (см. PlayerStats.StaminaMode):
        // - Action-режим (NieR-style): лёгкие атаки и комбо бесплатны и не
        //   гейтятся вообще — комбо-флоу нельзя оборвать пустой стаминой.
        // - Souls-режим: старое правило туториала — замахнуться можно, пока
        //   стамина > 0 (стоимость спишет Animation Event через
        //   WeaponSlotManager, и она может увести в ноль).
        // При playerStats == null гейт тихо отключается — ничего не ломаем.
        private bool HasStaminaForLightAttack()
        {
            if (playerStats == null)
                return true;

            if (playerStats.IsActionMode)
                return true;

            return playerStats.HasStamina();
        }

        // Тяжёлые атаки гейтятся стаминой в ОБОИХ режимах — в Action-режиме
        // это единственный потребитель стамины, ради которого бар и живёт.
        private bool HasStaminaForHeavyAttack()
        {
            return playerStats == null || playerStats.HasStamina();
        }

        // Доворот в сторону ввода. Используем корень CameraHandler'а
        // (он вращается только по yaw), а не Camera.main — у самой камеры
        // forward наклонён по pitch и загрязнял бы направление.
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

        public void HandleWeaponCombo(WeaponItem weapon)
        {
            if (weapon == null)
                return;

            if (!HasStaminaForLightAttack())
                return;

            if(inputHandler.comboFlag)
            { 
                animatorHandler.anim.SetBool("canDoCombo", false);
                
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
                    // НОВОЕ (Фаза 3): возврат из тяжёлого финишера обратно в
                    // лёгкую цепочку — строки вида light-heavy-light, как в
                    // NieR. Ветка СПИТ, пока на клипе тяжёлой атаки нет
                    // событий EnableCombo/ComboWindowClosed (комбо-окно из
                    // тяжёлой просто не открывается). Расставишь события —
                    // строка заработает без единой правки кода.
                    OrientTowardsInputDirection();
                    animatorHandler.PlayeTargetAnimation(weapon.OH_Light_Attack_1, true);
                    lastAttack = weapon.OH_Light_Attack_1;
                }
            }
        }

        // НОВОЕ (Фаза 3). Тяжёлый ФИНИШЕР из комбо-окна: RT посреди лёгкой
        // цепочки ветвит её в OH_Heavy_Attack_1 сразу, не дожидаясь конца
        // всех лёгких ударов. Зовёт InputHandler.HandleAttackInput по той же
        // схеме, что HandleWeaponCombo (comboFlag поднят на время вызова).
        // Стамина: как у обычной тяжёлой — гейт в обоих режимах, списание
        // сделает Animation Event DrainStaminaHeavyAttack на клипе.
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

        public void HandleLightAttack(WeaponItem weapon)
        {
            // Оружие может быть null, если unarmedWeapon не назначен в
            // PlayerInventory — без guard'а нажатие атаки роняло NRE.
            if (weapon == null)
            {
                Debug.LogWarning("PlayerAttacker: атака без оружия — проверь, что unarmedWeapon назначен в PlayerInventory.");
                return;
            }

            // Не начинаем новую атаку, пока играет анимация-интеракция.
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
    }
}