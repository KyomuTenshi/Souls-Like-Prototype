using UnityEngine;

namespace SG {
    public class WeaponPickUp : Interactable
    {
        public WeaponItem weapon;

        public override void Interact(PlayerManager playerManager)
        {
            base.Interact(playerManager);
            PickUpItem(playerManager);
        }

        private void PickUpItem(PlayerManager playerManager)
        {
            if (weapon == null)
            {
                Debug.LogWarning("WeaponPickUp: поле 'weapon' не назначено на " + gameObject.name);
                return;
            }

            PlayerInventory playerInventory = playerManager.GetComponent<PlayerInventory>();
            PlayerLocomotion playerLocomotion = playerManager.GetComponent<PlayerLocomotion>();
            AnimatorHandler animatorHandler = playerManager.GetComponentInChildren<AnimatorHandler>();

            playerLocomotion.rb.linearVelocity = Vector3.zero;
            animatorHandler.PlayeTargetAnimation("Pick Up Item", true);

            playerInventory.weaponsInventory.Add(weapon);

            playerManager.ShowItemPickupNotification(weapon.itemName, weapon.itemIcon);

            Destroy(gameObject);
        }
    }
}