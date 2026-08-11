using UnityEngine;

namespace SG {
    public class DamageCollider : MonoBehaviour
    {
        Collider damageCollider;

        public int currentWeaponDamage = 25;

        [Header("Hit Stop (game feel)")]
        // Настраивается на префабе оружия: тяжёлый молот замирает дольше
        // кинжала. 0 = хитстоп выключен.
        [SerializeField] float hitStopDuration = 0.07f;
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

        // Опечатка (Disale) оставлена намеренно: имя используют
        // WeaponSlotManager и будущие уроки туториала.
        public void DisaleDamageCollider()
        {
             damageCollider.enabled = false;
        }

        public void OnTriggerEnter(Collider other)
        {
            // Хитбокс оружия — ребёнок владельца и при замахе пересекает его
            // капсулу; без guard'а оружие било бы собственного хозяина.
            if (other.transform.root == transform.root)
                return;

            // Хитстоп играем только если урон реально прошёл (TakeDamage
            // вернул true): удар в i-frames или по трупу замирать не должен —
            // так игрок телом чувствует разницу между "попал" и "сквозь".
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
                        // Добивающий удар замирает дольше — акцент на убийстве,
                        // как в NieR/DMC.
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