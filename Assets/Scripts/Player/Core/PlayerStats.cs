using UnityEngine;

namespace SG {
    public class PlayerStats : MonoBehaviour
    {
        // Souls — поведение туториала: гейт и списания на атаках, ролле,
        // прыжке, спринте. Action (NieR) — стамину тратят только тяжёлые
        // атаки, остальное бесплатно. Код туториала не трогается.
        public enum StaminaMode { Souls, Action }

        [Header("Stamina Mode")]
        [SerializeField] StaminaMode staminaMode = StaminaMode.Action;

        public bool IsActionMode { get { return staminaMode == StaminaMode.Action; } }

        public int healthLevel = 10;
        public int maxHealth;
        public int currentHealth;

        public bool isDead;

        [Header("I-Frames")]
        // Ставят/снимают Animation Event'ы EnableInvulnerability /
        // DisableInvulnerability на клипе Rolling (методы в AnimatorHandler).
        // События не расставлены — флаг всегда false, поведение как раньше.
        public bool isInvulnerable;

        public int staminaLevel = 10;
        public int maxStamina;
        public int currentStamina;

        [Header("Stamina Regen")]
        [SerializeField] float staminaRegenRate = 20f;      // единиц в секунду
        [SerializeField] float staminaRegenDelay = 1.2f;    // пауза после траты

        public HealthBar healthBar;
        public StaminaBar staminaBar;

        AnimatorHandler animatorHandler;
        PlayerManager playerManager;
        // Опциональный компонент (NieR-смерть). Нет — труп лежит, как в туториале.
        PlayerRespawn playerRespawn;

        // Float-двойник currentStamina: плавная трата/накопление, наружу
        // (UI, туториал) отдаётся привычный int.
        float staminaFloat;
        float staminaRegenTimer;

        private void Awake()
        {
            // Ручное назначение в инспекторе имеет приоритет.
            if (healthBar == null)
                healthBar = FindFirstObjectByType<HealthBar>(FindObjectsInactive.Include);
            if (staminaBar == null)
                staminaBar = FindFirstObjectByType<StaminaBar>(FindObjectsInactive.Include);

            animatorHandler = GetComponentInChildren<AnimatorHandler>();
            playerManager = GetComponent<PlayerManager>();
            playerRespawn = GetComponent<PlayerRespawn>();
        }

        void Start()
        {
            maxHealth = SetMaxHealthFromHealthLevel();
            currentHealth = maxHealth;

            if (healthBar != null)
            {
                healthBar.SetMaxHealth(maxHealth);
            }
            else
            {
                Debug.LogWarning("PlayerStats: HealthBar не найден — HP-бар обновляться не будет.");
            }

            maxStamina = SetMaxStaminaFromStaminaLevel();
            currentStamina = maxStamina;
            staminaFloat = maxStamina;

            // Без SetMaxStamina слайдер остался бы с maxValue = 1 и визуально
            // опустошался за один удар.
            if (staminaBar != null)
            {
                staminaBar.SetMaxStamina(maxStamina);
            }
            else
            {
                Debug.LogWarning("PlayerStats: StaminaBar не найден — бар выносливости обновляться не будет.");
            }
        }

        private void Update()
        {
            RegenerateStamina();

            // Страховка от вечной неуязвимости: ролл прерван без
            // Disable-события (срыв с обрыва) — флаг снимается с выходом из
            // интеракции.
            if (isInvulnerable && playerManager != null && !playerManager.isIntetacting)
            {
                isInvulnerable = false;
            }
        }

        private int SetMaxHealthFromHealthLevel()
        {
            maxHealth = healthLevel * 10;
            return maxHealth;
        }

        private int SetMaxStaminaFromStaminaLevel()
        {
            maxStamina = staminaLevel * 10;
            return maxStamina;
        }

        // Доступно, пока стамина строго больше нуля; стоимость может увести
        // её в ноль — как в Dark Souls.
        public bool HasStamina(int cost = 1)
        {
            return currentStamina >= cost;
        }

        // Возвращает, прошёл ли урон (false — смерть или i-frames). Нужно
        // хитстопу: замирает только реальный контакт. Старые вызовы
        // TakeDamage(x); компилируются как раньше.
        public bool TakeDamage(int damage)
        {
            if (isDead)
                return false;

            if (isInvulnerable)
                return false;

            currentHealth = currentHealth - damage;

            if (healthBar != null)
            {
                healthBar.SetCurrentHealth(currentHealth);
            }

            if (currentHealth <= 0)
            {
                currentHealth = 0;
                isDead = true;
                animatorHandler.PlayeTargetAnimation("Dead", true);

                if (playerRespawn != null)
                {
                    playerRespawn.HandleDeath();
                }
            }
            else
            {
                animatorHandler.PlayeTargetAnimation("BetaDamage", true);
            }

            return true;
        }

        // Полное восстановление с обновлением баров. Зовут Checkpoint и
        // PlayerRespawn. isDead не трогаем: оживление — ответственность
        // PlayerRespawn, а чекпоинт лечит живого.
        public void RestoreHealthAndStamina()
        {
            currentHealth = maxHealth;

            if (healthBar != null)
            {
                healthBar.SetCurrentHealth(currentHealth);
            }

            staminaFloat = maxStamina;
            staminaRegenTimer = 0f;
            SyncStaminaToInt();
        }

        // Разовая трата (атаки через Animation Event, ролл, прыжок).
        public void TakeStaminaDamage(int damage)
        {
            staminaFloat = Mathf.Max(0f, staminaFloat - damage);
            SyncStaminaToInt();
            staminaRegenTimer = staminaRegenDelay;
        }

        // Плавная трата по времени (спринт).
        public void DrainStamina(float amount)
        {
            if (amount <= 0f)
                return;

            staminaFloat = Mathf.Max(0f, staminaFloat - amount);
            SyncStaminaToInt();
            staminaRegenTimer = staminaRegenDelay;
        }

        private void RegenerateStamina()
        {
            if (isDead)
                return;

            // Не копим во время спринта и интеракций — стамина возвращается,
            // когда игрок "отдышался".
            if (playerManager != null && (playerManager.isSprinting || playerManager.isIntetacting))
                return;

            if (staminaRegenTimer > 0f)
            {
                staminaRegenTimer -= Time.deltaTime;
                return;
            }

            if (staminaFloat >= maxStamina)
                return;

            staminaFloat = Mathf.Min(maxStamina, staminaFloat + staminaRegenRate * Time.deltaTime);
            SyncStaminaToInt();
        }

        private void SyncStaminaToInt()
        {
            int newValue = Mathf.FloorToInt(staminaFloat);

            if (newValue == currentStamina)
                return;

            currentStamina = newValue;

            if (staminaBar != null)
            {
                staminaBar.SetCurrentStamina(currentStamina);
            }
        }
    }
}