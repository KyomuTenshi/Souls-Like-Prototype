using UnityEngine;

namespace SG {
    public class Interactable : MonoBehaviour
    {
        public float radius = 0.6f;
        public string interactbleText;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, radius);
        }

        public virtual void Interact(PlayerManager playerManager)
        {
            Debug.Log("You interacted with " + transform.name);
        }
    }
}