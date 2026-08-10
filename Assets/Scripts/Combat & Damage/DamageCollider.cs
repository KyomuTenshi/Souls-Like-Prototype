using UnityEngine;

namespace SG {
    public class DamageCollider : MonoBehaviour
    {
        Collider damageCollider;

        public int currentWeaponDamage = 25;

        [Header("Hit Stop (game feel)")]
        // Микро-заморозка при попадании ЭТОГО хитбокса (сек реального
        // времени). Поле лежит на префабе оружия — тяжёлый молот может
        // замирать дольше лёгкого кинжала. 0 = хитстоп выключен.
        [SerializeField] float hitStopDuration = 0.07f;
        // Добивающий удар (цель умерла) замирает дольше — акцент на
        // убийстве, как в NieR/DMC.
        [SerializeField] float killHitStopMultiplier = 2f;

        private void Awake()
        {
            damageCollider = GetComponent<Collider>();
            damageCollider.gameObject.SetActive(true);
            damageCollider.isTrigger = true;
            damageCollider.enabled = false;
        }

        public void EnableDamageCollider()
        {
            damageCollider.enabled = true;
        }

        // Имя с опечаткой (Disale) оставлено намеренно: на него завязан
        // WeaponSlotManager, и будущие уроки туториала используют это же имя.
        public void DisaleDamageCollider()
        {
             damageCollider.enabled = false;
        }

        public void OnTriggerEnter(Collider other)
        {
            // Свой корень не бьём: хитбокс оружия — ребёнок владельца, и во
            // время замаха он свободно пересекает капсулу самого владельца.
            // Без guard'а меч игрока с тегом Player на корне бил бы своего же
            // хозяина, а хитбокс врага — самого врага. Слои это обычно
            // маскируют, но правило не должно зависеть от настройки матрицы
            // коллизий.
            if (other.transform.root == transform.root)
                return;

            // CompareTag вместо сравнения строк через ==: быстрее и кидает
            // явную ошибку при опечатке в имени тега вместо тихого промаха.
            // else if — объект не может быть Player и Enemy одновременно.
            //
            // Хитстоп играем только если TakeDamage ВЕРНУЛ true, то есть урон
            // реально прошёл. Удар в i-frames уклонения или по трупу — это
            // промах, и замирать он не должен: так игрок телом чувствует
            // разницу между "попал" и "прошёл сквозь".
            if (other.CompareTag("Player"))
            {
                if (other.TryGetComponent(out PlayerStats playerStats))
                {
                    if (playerStats.TakeDamage(currentWeaponDamage))
                    {
                        HitStop.Play(hitStopDuration);
                    }
                }
            }
            else if (other.CompareTag("Enemy"))
            {
                if (other.TryGetComponent(out EnemyStats enemyStats))
                {
                    if (enemyStats.TakeDamage(currentWeaponDamage))
                    {
                        float duration = enemyStats.isDead
                            ? hitStopDuration * killHitStopMultiplier
                            : hitStopDuration;

                        HitStop.Play(duration);
                    }
                }
            }
        }
    }
}