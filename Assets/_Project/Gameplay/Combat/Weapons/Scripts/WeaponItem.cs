using UnityEngine;

namespace SG {
    [CreateAssetMenu(fileName = "New Weapon", menuName = "Items/Weapon")]
    public class WeaponItem : Item
    {
        public GameObject weaponPrefab;
        public bool isUnarmed;
    }
}