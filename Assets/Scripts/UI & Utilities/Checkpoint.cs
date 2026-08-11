using UnityEngine;

namespace SG {
    // Чекпоинт NieR-стиля: подошёл, нажал взаимодействие — точка возрождения
    // обновлена, HP/стамина восстановлены. Наследование от Interactable
    // позволяет SphereCast'у в PlayerManager найти его без правок.
    public class Checkpoint : Interactable
    {
        [Header("Checkpoint Settings")]
        // Если не назначена — используется сам чекпоинт. Лучше пустышка чуть
        // в стороне, чтобы респаун не попал внутрь коллайдера чекпоинта.
        [SerializeField] Transform respawnPoint;
        [SerializeField] bool healOnActivate = true;

        public override void Interact(PlayerManager playerManager)
        {
            base.Interact(playerManager);

            PlayerRespawn playerRespawn = playerManager.GetComponent<PlayerRespawn>();

            if (playerRespawn == null)
            {
                Debug.LogWarning("Checkpoint: на игроке нет компонента PlayerRespawn — точка возрождения не сохранится.");
                return;
            }

            Transform point = respawnPoint != null ? respawnPoint : transform;
            playerRespawn.SetCheckpoint(point.position, point.rotation);

            if (healOnActivate)
            {
                PlayerStats playerStats = playerManager.GetComponent<PlayerStats>();
                if (playerStats != null)
                {
                    playerStats.RestoreHealthAndStamina();
                }
            }
        }
    }
}