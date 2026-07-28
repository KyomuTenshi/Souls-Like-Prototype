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

        public void DisaleDamageCollider()
        {
             damageCollider.enabled = false;
        }

        public void OnTriggerEnter(Collider other)
        {
            if (other.tag == "Player")
            {
                PlayerStats playerStats = other.GetComponent<PlayerStats>();

                if (playerStats != null)
                {
                    playerStats.TakeDamage(currentWeaponDamage);
                }
            }

            if (other.tag == "Enemy")
            {
                EnemyStats enemyStats = other.GetComponent<EnemyStats>();

                if (enemyStats != null)
                {
                    enemyStats.TakeDamage(currentWeaponDamage);
                }
            }
        }
    }
}