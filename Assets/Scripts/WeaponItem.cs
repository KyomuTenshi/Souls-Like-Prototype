using UnityEngine;

namespace SG
{
    [CreateAssetMenu(fileName = "New Weapon", menuName = "Items/Weapon Item")]
    public class WeaponItem : Item
    {
        [Header("Weapon Settings")]
        public GameObject modelPrefab;
        public bool isUnarmed;
    }
}