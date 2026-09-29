using UnityEngine;

namespace SG {
    public class Item : ScriptableObject
    {
        [Header("Item Info")]
        public Sprite itemIcon;
        public string itemName;
    }
}