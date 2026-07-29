using UnityEngine;

namespace SG {
    public class DamageCollider : MonoBehaviour
    {
        Collider damageCollider;

        public int currentWeaponDamage = 25;

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
            // CompareTag вместо сравнения строк через ==: быстрее и кидает
            // явную ошибку при опечатке в имени тега вместо тихого промаха.
            // else if — объект не может быть Player и Enemy одновременно.
            if (other.CompareTag("Player"))
            {
                if (other.TryGetComponent(out PlayerStats playerStats))
                {
                    playerStats.TakeDamage(currentWeaponDamage);
                }
            }
            else if (other.CompareTag("Enemy"))
            {
                if (other.TryGetComponent(out EnemyStats enemyStats))
                {
                    enemyStats.TakeDamage(currentWeaponDamage);
                }
            }
        }
    }
}