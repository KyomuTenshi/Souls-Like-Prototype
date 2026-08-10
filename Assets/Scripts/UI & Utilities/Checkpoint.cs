using UnityEngine;

namespace SG {
    // Чекпоинт NieR-стиля (аналог точек сохранения): подошёл, нажал
    // "взаимодействовать" — точка возрождения обновлена, HP/стамина
    // восстановлены. Наследуемся от Interactable, чтобы существующий
    // SphereCast в PlayerManager.CheckForInteractableObject нашёл чекпоинт
    // без единой правки — та же схема, что у WeaponPickUp.
    // Нужен объект с коллайдером, этим компонентом и текстом в
    // interactbleText (например, "Активировать чекпоинт").
    public class Checkpoint : Interactable
    {
        [Header("Checkpoint Settings")]
        // Точка возрождения. Если не назначена — используется сам чекпоинт.
        // Лучше назначить пустышку-ребёнка чуть В СТОРОНЕ от чекпоинта,
        // чтобы после респауна игрок не оказался внутри его коллайдера.
        [SerializeField] Transform respawnPoint;
        // Полное восстановление HP/стамины при активации — как у точек
        // сохранения в NieR.
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