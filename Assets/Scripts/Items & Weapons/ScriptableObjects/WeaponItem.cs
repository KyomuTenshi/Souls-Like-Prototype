using UnityEngine;

namespace SG
{
    [CreateAssetMenu(fileName = "New Weapon", menuName = "Items/Weapon Item")]
    public class WeaponItem : Item
    {
        public GameObject modelPrefab;
        public bool isUnarmed;

        [Header("Idle Animations")]
        public string right_hand_idle;
        public string left_hand_idle;

        [Header("Attack Animations")]
        public string OH_Light_Attack_1;
        public string OH_Light_Attack_2;
        public string OH_Light_Attack_3;
        public string OH_Light_Attack_4;
        public string OH_Heavy_Attack_1;

        [Header("Damage")]
        // WeaponSlotManager прописывает это значение в хитбокс при
        // экипировке. Дефолт 25 = прежней константе DamageCollider.
        public int baseDamage = 25;

        [Header("Stamina Costs")]
        public int baseStamina;
        public float lightAttackMultiplier;
        public float heavyAttackMultiplier;
    }
}