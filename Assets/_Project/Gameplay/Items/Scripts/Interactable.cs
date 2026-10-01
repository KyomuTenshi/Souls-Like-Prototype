using UnityEngine;
using UnityEngine.Serialization;

namespace SG {
    public class Interactable : MonoBehaviour
    {
        public float radius = 0.6f;
        [FormerlySerializedAs("interactbleText")]
        public string interactableText;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, radius);
        }

        public virtual void Interact(PlayerManager playerManager)
        {
            Debug.Log("You interacted with an object!");
        }
    }
}
