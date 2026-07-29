using UnityEngine;
using UnityEngine.UI;

namespace SG {
    public class QuickSlotsUI : MonoBehaviour
    {
        public Image leftWeaponIcon;
        public Image rightWeaponIcon;

        public void UpdateWeaponQuickSlotsUI(bool isLeft, WeaponItem weapon)
        {
            Image targetIcon = isLeft ? leftWeaponIcon : rightWeaponIcon;

            if (targetIcon == null)
            {
                return;
            }

            if (weapon != null && weapon.itemIcon != null)
            {
                targetIcon.sprite = weapon.itemIcon;
                targetIcon.enabled = true;
            }
            else
            {
                targetIcon.sprite = null;
                targetIcon.enabled = false;
            }
        }
    }
}