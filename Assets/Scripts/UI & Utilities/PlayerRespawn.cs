using System.Collections;
using UnityEngine;

namespace SG {
    // NieR-стиль смерти: умер — пауза — возрождение на последнем чекпоинте
    // с полным HP/стаминой. Компонент опционален: без него PlayerStats
    // работает как в туториале (труп остаётся лежать).
    public class PlayerRespawn : MonoBehaviour
    {
        [Header("Respawn Settings")]
        [SerializeField] float respawnDelay = 3f;
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
            // До первого чекпоинта возрождаемся там, где началась сцена.
            respawnPosition = transform.position;
            respawnRotation = transform.rotation;
        }

        // Позиция/поворот вместо Transform: чекпоинт может быть уничтожен,
        // а точка возрождения должна пережить его.
        public void SetCheckpoint(Vector3 position, Quaternion rotation)
        {
            respawnPosition = position;
            respawnRotation = rotation;
        }

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
            // Скорость обнуляем до телепорта, чтобы остаточный импульс не
            // утащил персонажа с точки в первый же кадр.
            if (playerLocomotion != null && playerLocomotion.rb != null)
            {
                playerLocomotion.rb.linearVelocity = Vector3.zero;
            }

            transform.position = respawnPosition;
            transform.rotation = respawnRotation;

            // Смерть могла случиться в полёте — без сброса флагов
            // HandleFalling продолжил бы "падение" на чекпоинте.
            if (playerManager != null)
            {
                playerManager.isInAir = false;
                playerManager.isGrounded = true;
            }
            if (playerLocomotion != null)
            {
                playerLocomotion.inAirTimer = 0;
            }

            if (playerStats != null)
            {
                playerStats.isDead = false;
                playerStats.isInvulnerable = false;
                playerStats.RestoreHealthAndStamina();
            }

            // Выход из "Dead"; isInteracting = false возвращает управление.
            if (animatorHandler != null)
            {
                animatorHandler.PlayeTargetAnimation(respawnAnimation, false);
            }
        }
    }
}