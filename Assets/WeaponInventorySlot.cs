using UnityEngine;
using UnityEngine.UI;

namespace SG {
    public class WeaponInventorySlot : MonoBehaviour
    {
        public Image icon;
        WeaponItem item;

        public void AddItem(WeaponItem newItem)
        {
            item = newItem;
            icon.sprite = item.itemIcon;
            icon.enabled = true;
            gameObject.SetActive(true);
        }

        public void ClearInventorySlot()
        {
            item = null;
            icon.sprite = null;
            icon.enabled = false;
            gameObject.SetActive(false);
        }
    }
}