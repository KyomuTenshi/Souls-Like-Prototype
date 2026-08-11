using UnityEngine;

namespace SG {
    public class DamagePlayer : MonoBehaviour
    {
        public int damage = 25;

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out PlayerStats playerStats))
            {
                playerStats.TakeDamage(damage);
            }
        }
    }
}