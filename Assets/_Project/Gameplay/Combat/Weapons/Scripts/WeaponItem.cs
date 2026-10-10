using UnityEngine;

namespace SG {
    [CreateAssetMenu(fileName = "New Weapon", menuName = "Items/Weapon")]
    public class WeaponItem : Item
    {
        public GameObject weaponPrefab;
        public bool isUnarmed;

        [Header("Idle Animations")]
        public string rigth_hand_idle;
        public string left_hand_idle;
        
        [Header("Attack Animations")]
        public string OH_Light_Attack_1;
        public string OH_Heavy_Attack_1;
        public string OH_Light_Attack_2;
        public string OH_Heavy_Attack_2;
        public string OH_Light_Attack_3;
        public string OH_Heavy_Attack_3;
        public string OH_Light_Attack_4;
        public string OH_Heavy_Attack_4;

        [Header("Stamina Costs")]
        public int baseStamina;
        public float lightAttackMultiplier;
        public float heavyAttackMultiplier;
    }
}