using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace SG 
{
    public class CameraHandler : MonoBehaviour
    {
        InputHandler inputHandler;
        PlayerManager playerManager;
        public Transform targetTransform;
        public Transform cameraTransform;
        public Transform cameraPivotTransform;
        private Transform myTransform;
        private Vector3 cameraTransformPosition;
        public LayerMask ignoreLayers;
        public LayerMask environmentLayer;
        private Vector3 cameraFollowVelocity = Vector3.zero;
        private float cameraHeightVelocityY;

        public static CameraHandler singleton;

        [Header("Camera Speeds")]
        public float lookSpeed = 100f;
        public float pivotSpeed = 100f;
        public float followSpeed = 0.1f;

        [Header("Mouse Sensitivity")]
        [SerializeField] private float mouseLookSensitivity = 0.2f;
        [SerializeField] private float mousePivotSensitivity = 0.15f;

        private float targetPosition;
        private float defaultPosition;
        private float lookAngle;
        private float pivotAngle;

        [Header("Camera Limits")]
        [SerializeField] private float minimumPivot = -35f;
        [SerializeField] private float maximumPivot = 35f;

        [Header("Collision Settings")]
        public float cameraSphereRadius = 0.2f;
        public float cameraCollisionOffSet = 0.2f;
        public float minimunCollisionOffSet = 0.2f;
        [FormerlySerializedAs("lockefPivotPosition")]
        public float lockedPivotPosition = 2.25f;
        public float unlockedPivotPosition = 1.65f;

        [Header("Pivot Height Smoothing")]
        // SmoothDamp-время подъёма/спуска пивота при входе/выходе из lock-on.
        [SerializeField] private float pivotHeightSmoothTime = 0.15f;

        [Header("Lock On")]
        public Transform currentLockOnTarget;

        List<CharacterManager> availableTargets = new List<CharacterManager>();
        public Transform leftLockOnTarget;
        public Transform rightLockOnTarget;
        public Transform nearestLockOnTarget;
        public float maximumLockOnDistance = 30;

        private void Awake()
        {
            singleton = this;
            myTransform = transform;
            defaultPosition = cameraTransform.localPosition.z;
            ignoreLayers = ~(1 << 8 | 1 << 9 | 1 << 10);

            PlayerManager player = FindFirstObjectByType<PlayerManager>();
            if (player != null)
            {
                targetTransform = player.transform;
                playerManager = player;
            }
            else
            {
                Debug.LogError("CameraHandler: PlayerManager не найден в сцене — камере не за кем следовать.");
            }
            inputHandler = FindFirstObjectByType<InputHandler>(FindObjectsInactive.Include);
        }

        public void Start()
        {
            environmentLayer = LayerMask.NameToLayer("Environment");
        }

        public void FollowTarget(float delta)
        {
            if (targetTransform == null)
                return;

            Vector3 targetPos = Vector3.SmoothDamp(
                myTransform.position, 
                targetTransform.position, 
                ref cameraFollowVelocity, 
                followSpeed
            );
            myTransform.position = targetPos;

            HandleCameraCollisions(delta);
        }

        public void HandleCameraRotation(float delta, float mouseXInput, float mouseYInput)
        {
            if (inputHandler.lockOnFlag == false && currentLockOnTarget == null)
            {
                lookAngle += mouseXInput * mouseLookSensitivity;
                pivotAngle -= mouseYInput * mousePivotSensitivity;

                pivotAngle = Mathf.Clamp(pivotAngle, minimumPivot, maximumPivot);

                Vector3 rotation = Vector3.zero;
                rotation.y = lookAngle;
                Quaternion targetRotation = Quaternion.Euler(rotation);
                myTransform.rotation = targetRotation;

                rotation = Vector3.zero;
                rotation.x = pivotAngle;

                targetRotation = Quaternion.Euler(rotation);
                cameraPivotTransform.localRotation = targetRotation;
            }
            else
            {
                // Переходный кадр: lockOnFlag уже поднят, а цель ещё/уже null
                // (уничтожена, рассинхрон флага и цели) — кадр без поворота
                // невиден глазу, NRE каждый кадр — виден. Та же схема, что в
                // PlayerLocomotion.HandleRotation.
                if (currentLockOnTarget == null)
                    return;

                Vector3 dir = currentLockOnTarget.position - transform.position;
                dir.Normalize();
                dir.y = 0;

                Quaternion targetRotation = Quaternion.LookRotation(dir);
                transform.rotation = targetRotation;

                dir = currentLockOnTarget.position - cameraPivotTransform.position;
                dir.Normalize();

                targetRotation = Quaternion.LookRotation(dir);
                Vector3 eulerAngle = targetRotation.eulerAngles;
                eulerAngle.y = 0;
                cameraPivotTransform.localEulerAngles = eulerAngle;
            }
        }

        public void HandleCameraCollisions(float delta)
        {
            targetPosition = defaultPosition;
            RaycastHit hit;

            Vector3 direction = cameraTransform.position - cameraPivotTransform.position;
            direction.Normalize();

            if (Physics.SphereCast(cameraPivotTransform.position, cameraSphereRadius, direction, out hit, Mathf.Abs(targetPosition), ignoreLayers))
            {
                float dis = Vector3.Distance(cameraPivotTransform.position, hit.point);
                targetPosition = -(dis - cameraCollisionOffSet);
            }

            if (Mathf.Abs(targetPosition) < minimunCollisionOffSet)
            {
                targetPosition = -minimunCollisionOffSet;
            }

            cameraTransformPosition = cameraTransform.localPosition;
            float t = Mathf.Clamp01(delta / 0.2f);
            cameraTransformPosition.z = Mathf.Lerp(cameraTransform.localPosition.z, targetPosition, t);
            cameraTransform.localPosition = cameraTransformPosition;
        }

        public void HandleLockOn()
        {
            availableTargets.Clear();
            nearestLockOnTarget = null;
            leftLockOnTarget = null;
            rightLockOnTarget = null;

            float shortestDistance = Mathf.Infinity;
            float shortestDistanceLeftTarget = Mathf.Infinity;
            float shortestDistanceRightTarget = Mathf.Infinity;

            Collider[] colliders = Physics.OverlapSphere(targetTransform.position, maximumLockOnDistance);

            for (int i = 0; i < colliders.Length; i++)
            {
                CharacterManager characterManager = colliders[i].GetComponent<CharacterManager>();

                if (characterManager != null)
                {
                    Vector3 lockTargetDirection = characterManager.transform.position - targetTransform.position;
                    float distanceFromTarget = Vector3.Distance(targetTransform.position, characterManager.transform.position);
                    float viewableAngle = Vector3.Angle(lockTargetDirection, targetTransform.forward);

                    if (characterManager.transform.root != targetTransform.transform.root && viewableAngle > -50 && viewableAngle < 50 && distanceFromTarget <= maximumLockOnDistance)
                    {
                        if (playerManager != null)
                        {
                            RaycastHit hit;
                            bool blockedByEnvironment =
                                Physics.Linecast(playerManager.lockOnTransform.position, characterManager.lockOnTransform.position, out hit)
                                && hit.transform.gameObject.layer == environmentLayer;

                            if (blockedByEnvironment)
                            {
                                Debug.DrawLine(playerManager.lockOnTransform.position, characterManager.lockOnTransform.position, Color.red);
                            }
                            else
                            {
                                availableTargets.Add(characterManager);
                            }
                        }
                    }
                }
            }

            for (int k = 0; k < availableTargets.Count; k++)
            {
                float distanceFromTarget = Vector3.Distance(targetTransform.position, availableTargets[k].transform.position);

                if (distanceFromTarget < shortestDistance)
                {
                    shortestDistance = distanceFromTarget;
                    nearestLockOnTarget = availableTargets[k].lockOnTransform;
                }

                if (inputHandler.lockOnFlag && currentLockOnTarget != null)
                {
                    Vector3 relativeEnemyPosition = currentLockOnTarget.InverseTransformPoint(availableTargets[k].transform.position);
                    float distanceFromCurrentTarget = Mathf.Abs(relativeEnemyPosition.x);

                    if (relativeEnemyPosition.x > 0.00f && distanceFromCurrentTarget < shortestDistanceLeftTarget)
                    {
                        shortestDistanceLeftTarget = distanceFromCurrentTarget;
                        leftLockOnTarget = availableTargets[k].lockOnTransform;
                    }

                    if (relativeEnemyPosition.x < 0.00f && distanceFromCurrentTarget < shortestDistanceRightTarget)
                    {
                        shortestDistanceRightTarget = distanceFromCurrentTarget;
                        rightLockOnTarget = availableTargets[k].lockOnTransform;
                    }
                }
            }
        }

        public void ClearLockOnTargets()
        {
            nearestLockOnTarget = null;
            leftLockOnTarget = null;
            rightLockOnTarget = null;
            availableTargets.Clear();
            currentLockOnTarget = null;
        }

        public void SetCameraHeight()
        {
            // Трогаем только Y: полное присваивание localPosition стирало бы
            // горизонтальный офсет пивота, заданный в сцене. Высота 0 в
            // инспекторе = "на уровне пивота родителя" — это не баг
            // сглаживания; держи Locked/Unlocked под рост модели
            // (~1.6-1.8 / 2.0-2.3).
            // Третий параметр SmoothDamp — smoothTime (сколько секунд занимает
            // сглаживание), а не delta: Time.deltaTime здесь делал переход
            // почти мгновенным и fps-зависимым.
            Vector3 pivotLocalPosition = cameraPivotTransform.localPosition;
            float targetHeight = currentLockOnTarget != null ? lockedPivotPosition : unlockedPivotPosition;
            pivotLocalPosition.y = Mathf.SmoothDamp(pivotLocalPosition.y, targetHeight, ref cameraHeightVelocityY, pivotHeightSmoothTime);
            cameraPivotTransform.localPosition = pivotLocalPosition;
        }
    }
}