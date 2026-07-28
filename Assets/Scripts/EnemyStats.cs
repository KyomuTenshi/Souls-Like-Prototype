using UnityEngine;

namespace SG {
    public class EnemyStats : MonoBehaviour
    {
        public int healthLevel = 10;
        public int maxHealth;
        public int currentHealth;

        Animator anim;

        private void Awake()
        {
            // Врагу не нужен player-специфичный AnimatorHandler (он завязан на
            // InputHandler/PlayerManager через GetComponentInParent) — работаем
            // напрямую со стандартным Animator.
            anim = GetComponent<Animator>();
        }

        void Start()
        {
            maxHealth = SetMaxHealthFromHealthLevel();
            currentHealth = maxHealth;
        }

        private int SetMaxHealthFromHealthLevel()
        {
            maxHealth = healthLevel * 10;
            return maxHealth;
        }

        public void TakeDamage(int damage)
        {
            currentHealth = currentHealth - damage;

            anim.SetBool("isInteracting", true);
            anim.CrossFade("BetaDamage", 0.2f);

            if(currentHealth <= 0)
            {
                currentHealth = 0;
                anim.SetBool("isInteracting", true);
                anim.CrossFade("Dead", 0.2f);
            }
        }
    }
}