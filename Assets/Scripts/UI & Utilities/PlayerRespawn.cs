using System.Collections;
using UnityEngine;

namespace SG {
    // NieR-стиль смерти: никакого штрафа — умер, подождал, возродился на
    // последнем чекпоинте с полным HP и стаминой. Компонент вешается на
    // игрока РЯДОМ с PlayerStats. Если его нет — PlayerStats работает
    // по-старому (труп лежит, как в туториале), ничего не ломается.
    public class PlayerRespawn : MonoBehaviour
    {
        [Header("Respawn Settings")]
        // Пауза между анимацией смерти и возрождением — время "принять" смерть.
        [SerializeField] float respawnDelay = 3f;
        // Состояние Animator, в которое выходим после возрождения. "Empty" —
        // нейтральное состояние из туториала, оно уже есть в контроллере.
        [SerializeField] string respawnAnimation = "Empty";

        Vector3 respawnPosition;
        Quaternion respawnRotation;

        PlayerStats playerStats;
        PlayerManager playerManager;
        PlayerLocomotion playerLocomotion;
        AnimatorHandler animatorHandler;

        Coroutine respawnCoroutine;

        private void Awake()
        {
            playerStats = GetComponent<PlayerStats>();
            playerManager = GetComponent<PlayerManager>();
            playerLocomotion = GetComponent<PlayerLocomotion>();
            animatorHandler = GetComponentInChildren<AnimatorHandler>();
        }

        private void Start()
        {
            // Пока не активирован ни один чекпоинт, точка возрождения —
            // место, где игрок начал сцену.
            respawnPosition = transform.position;
            respawnRotation = transform.rotation;
        }

        // Зовёт Checkpoint.Interact(). Точка задаётся позицией/поворотом, а
        // не Transform'ом — чекпоинт может быть уничтожен, а точка останется.
        public void SetCheckpoint(Vector3 position, Quaternion rotation)
        {
            respawnPosition = position;
            respawnRotation = rotation;
        }

        // Зовёт PlayerStats при смерти. Дубликаты (несколько смертельных
        // ударов в один кадр до isDead-guard'а) не плодят корутин.
        public void HandleDeath()
        {
            if (respawnCoroutine != null)
                return;

            respawnCoroutine = StartCoroutine(RespawnAfterDelay());
        }

        private IEnumerator RespawnAfterDelay()
        {
            yield return new WaitForSeconds(respawnDelay);
            Respawn();
            respawnCoroutine = null;
        }

        private void Respawn()
        {
            // Телепорт на чекпоинт. Скорость обнуляем ДО переноса, чтобы
            // остаточный импульс (например, от добившей атаки) не утащил
            // персонажа с точки в первый же кадр.
            if (playerLocomotion != null && playerLocomotion.rb != null)
            {
                playerLocomotion.rb.linearVelocity = Vector3.zero;
            }

            transform.position = respawnPosition;
            transform.rotation = respawnRotation;

            // Сбрасываем воздушные флаги: если смерть случилась в полёте,
            // без этого HandleFalling продолжил бы "падение" на чекпоинте.
            if (playerManager != null)
            {
                playerManager.isInAir = false;
                playerManager.isGrounded = true;
            }
            if (playerLocomotion != null)
            {
                playerLocomotion.inAirTimer = 0;
            }

            // Оживляем и восстанавливаем ресурсы (HP + стамина + бары).
            if (playerStats != null)
            {
                playerStats.isDead = false;
                // Страховка: если смерть застала персонажа с активными
                // i-frames (умер от чего-то, что их игнорирует, — например,
                // от будущей kill-zone), флаг не должен переехать в новую жизнь.
                playerStats.isInvulnerable = false;
                playerStats.RestoreHealthAndStamina();
            }

            // Выходим из "Dead" в нейтральное состояние. isInteracting = false
            // возвращает управление игроку.
            if (animatorHandler != null)
            {
                animatorHandler.PlayeTargetAnimation(respawnAnimation, false);
            }
        }
    }
}