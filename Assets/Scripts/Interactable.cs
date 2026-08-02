using UnityEngine;

namespace SG {
    public class Interactable : MonoBehaviour
    {
        public float radius = 0.6f;
        public string interactbleText;

        // Было: OnDrawGiazmosSelescted — опечатка в имени. Unity ищет колбэк
        // строго по точному имени метода, никакого override/интерфейса нет,
        // поэтому метод с опечаткой компилировался, но никогда не вызывался,
        // и жёлтая сфера радиуса взаимодействия не рисовалась в редакторе.
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