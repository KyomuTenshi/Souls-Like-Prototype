using UnityEngine;

namespace SG {
    public class PlayerStats : MonoBehaviour
    {
        // Режим стамины. Souls — поведение туториала как было: гейт и
        // списания на лёгких/тяжёлых атаках, ролле, прыжке и спринте.
        // Action (NieR-style) — стамину тратят ТОЛЬКО тяжёлые атаки;
        // лёгкие атаки, ролл, прыжок и спринт бесплатны и не гейтятся.
        // Переключается в инспекторе одним полем — код туториала не трогаем.
        public enum StaminaMode { Souls, Action }

        [Header("Stamina Mode")]
        [SerializeField] StaminaMode staminaMode = StaminaMode.Action;

        // Читают PlayerAttacker / PlayerLocomotion / WeaponSlotManager,
        // чтобы решить, применять ли souls-гейт и списания.
        public bool IsActionMode { get { return staminaMode == StaminaMode.Action; } }

        public int healthLevel = 10;
        public int maxHealth;
        public int currentHealth;

        public bool isDead;

        [Header("I-Frames")]
        // Кадры неуязвимости (уклонение). Флаг ставят/снимают Animation
        // Event'ы EnableInvulnerability / DisableInvulnerability на клипе
        // Rolling (методы живут в AnimatorHandler — события должны быть на
        // объекте с Animator). Пока события не расставлены, флаг просто
        // всегда false — поведение как раньше, ничего не ломается.
        // Ролл без i-frames в action-игре не работает как защитный
        // инструмент: игрок физически не может "прододживать" удары.
        public bool isInvulnerable;

        public int staminaLevel = 10;
        public int maxStamina;
        public int currentStamina;

        [Header("Stamina Regen")]
        // Классика souls: стамина сама восстанавливается, но не сразу после
        // траты (задержка) и не во время спринта/действий.
        [SerializeField] float staminaRegenRate = 20f;      // единиц в секунду
        [SerializeField] float staminaRegenDelay = 1.2f;    // пауза после траты

        public HealthBar healthBar;
        public StaminaBar staminaBar;

        AnimatorHandler animatorHandler;
        PlayerManager playerManager;
        // Опциональный компонент возрождения (NieR-стиль смерти). Если его
        // на игроке нет — смерть работает как в туториале: труп лежит.
        PlayerRespawn playerRespawn;

        // Внутренний float-двойник currentStamina: позволяет плавно тратить
        // (спринт по delta) и плавно копить, а наружу (UI, туториал) отдавать
        // привычный int. currentStamina остаётся публичным int — совместимость
        // с уроками не трогаем.
        float staminaFloat;
        float staminaRegenTimer;

        private void Awake()
        {
            // FindFirstObjectByType вместо устаревшего FindObjectOfType(true):
            // тот же смысл (включая неактивные объекты), без obsolete-warning.
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

            // Без SetMaxStamina слайдер остаётся с дефолтным maxValue = 1 и
            // визуально "опустошается" за один удар, хотя числа верные.
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

            // Страховка от "вечной неуязвимости": если ролл был прерван чем-то
            // без Disable-события (например, срыв с обрыва в Falling посреди
            // клипа), флаг снимается, как только персонаж вышел из интеракции.
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

        // Действие доступно, пока стамина строго больше нуля (по умолчанию),
        // при этом стоимость может увести её в ноль — как в Dark Souls.
        public bool HasStamina(int cost = 1)
        {
            return currentStamina >= cost;
        }

        // БЫЛО: void. Теперь возвращает, ПРОШЁЛ ли урон: false — удар
        // проигнорирован (смерть или i-frames уклонения), true — урон
        // применён. Нужно хитстопу в DamageCollider: замирать должен только
        // реальный контакт, а не удар "сквозь" ролл. Смена void -> bool
        // обратно совместима по исходникам: все старые вызовы
        // playerStats.TakeDamage(x); (в т.ч. из будущих уроков туториала)
        // компилируются как раньше — возвращаемое значение просто игнорируется.
        public bool TakeDamage(int damage)
        {
            // После смерти урон игнорируется — иначе каждый последующий удар
            // заново запускал анимацию Dead.
            if (isDead)
                return false;

            // Активные i-frames уклонения: урон полностью игнорируется.
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

                // NieR-стиль: если на игроке есть PlayerRespawn — через
                // паузу возрождаемся на чекпоинте. Нет компонента — старое
                // поведение туториала (труп остаётся лежать).
                if (playerRespawn != null)
                {
                    playerRespawn.HandleDeath();
                }
            }
            else
            {
                // else, а не второй независимый if: на смертельном ударе раньше
                // в одном кадре игрались и BetaDamage, и Dead.
                animatorHandler.PlayeTargetAnimation("BetaDamage", true);
            }

            return true;
        }

        // Полное восстановление HP и стамины с обновлением баров. Зовут
        // Checkpoint (при активации) и PlayerRespawn (при возрождении).
        // isDead здесь НЕ трогаем — оживление это ответственность
        // PlayerRespawn, а чекпоинт лечит и так живого игрока.
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
        // Сигнатура не менялась — WeaponSlotManager и туториал зовут её как раньше.
        public void TakeStaminaDamage(int damage)
        {
            staminaFloat = Mathf.Max(0f, staminaFloat - damage);
            SyncStaminaToInt();
            staminaRegenTimer = staminaRegenDelay;
        }

        // Плавная трата по времени (спринт): дробные значения копятся во
        // float-двойнике, int в UI тикает вниз без рывков.
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

            // Не копим во время спринта и анимаций-интеракций (атака, ролл,
            // приземление) — стамина начинает возвращаться, когда игрок
            // "отдышался".
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