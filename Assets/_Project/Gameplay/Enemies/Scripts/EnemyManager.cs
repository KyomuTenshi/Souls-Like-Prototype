using UnityEngine;

namespace SG {
    public class EnemyManager : CharacterManager
    {
        EnemyLocomotionManager enemyLocomotionManager;
        bool isPerformingAction;

        [Header("A.I Settings")]
        public float detectionRadius = 20;
        public float minimumDetectionAngle = -50;
        public float maximumDetectionAngle = 50;

        private void Awake()
        {
            enemyLocomotionManager = GetComponent<EnemyLocomotionManager>();
        }

        private void Update()
        {
            HandleCurrentAction();
        }

        private void HandleCurrentAction()
        {
            if (enemyLocomotionManager.currentTarget == null)
            {
                enemyLocomotionManager.HandleDetection();
            }
        }
    }
}