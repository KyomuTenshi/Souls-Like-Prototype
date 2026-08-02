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

            // Анимация подбора играет на ИГРОКЕ, а не на объекте оружия —
            // откладывать уничтожение ради анимации не нужно. SetActive(false)
            // + отложенный Destroy были избыточной комбинацией: Destroy
            // прекрасно работает и без предварительной деактивации.
            Destroy(gameObject);
        }
    }
}