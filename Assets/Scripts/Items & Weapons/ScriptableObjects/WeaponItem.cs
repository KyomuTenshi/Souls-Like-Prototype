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
    }
}