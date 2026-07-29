using UnityEngine;

namespace SG {
    public class DamagePlayer : MonoBehaviour
    {
        public int damage = 25;

        private void OnTriggerEnter(Collider other)
        {
            // TryGetComponent вместо GetComponent + проверки на null:
            // без editor-overhead на "null"-обёртку и в одну операцию.
            if (other.TryGetComponent(out PlayerStats playerStats))
            {
                playerStats.TakeDamage(damage);
            }
        }
    }
}