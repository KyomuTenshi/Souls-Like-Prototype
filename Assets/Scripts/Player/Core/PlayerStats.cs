using UnityEngine;

namespace SG {
    public class PlayerStats : MonoBehaviour
    {
        public int healthLevel = 10;
        public int maxHealth;
        public int currentHealth;

        public bool isDead;

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

        public void TakeDamage(int damage)
        {
            // После смерти урон игнорируется — иначе каждый последующий удар
            // заново запускал анимацию Dead.
            if (isDead)
                return;

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
            }
            else
            {
                // else, а не второй независимый if: на смертельном ударе раньше
                // в одном кадре игрались и BetaDamage, и Dead.
                animatorHandler.PlayeTargetAnimation("BetaDamage", true);
            }
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