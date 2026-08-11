// CameraHandler.cs
using System.Collections.Generic;
using UnityEngine;

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
        // ИСПРАВЛЕНО: было Vector3 cameraHeightVelocity, а сглаживалась им
        // только Y-компонента (см. SetCameraHeight ниже) — лишние X/Z поля
        // ref-параметра просто не использовались. Float честнее отражает,
        // что тут реально сглаживается.
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
        public float lockefPivotPosition = 2.25f;
        public float unlockedPivotPosition = 1.65f;

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
                if (inputHandler)
                {
                    lookAngle += mouseXInput * mouseLookSensitivity;
                    pivotAngle -= mouseYInput * mousePivotSensitivity;
                }

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
            // ИСПРАВЛЕНО: раньше пивоту целиком присваивался
            // new Vector3(0, height) — обнулялись X и Z, если у Camera Pivot
            // был осмысленный горизонтальный офсет, он стирался каждый кадр.
            // Теперь трогаем только Y, X/Z остаются как заданы в сцене.
            // Плюс: если height == 0 (как сейчас в инспекторе) — камера
            // ожидаемо едет к локальному нулю относительно Camera Holder,
            // это не баг сглаживания, а то, что 0 в принципе означает
            // "на уровне пивота родителя". Верни Locked/Unlocked в разумные
            // значения под рост своей модели (например, 1.6-1.8 / 2.0-2.3).
            Vector3 pivotLocalPosition = cameraPivotTransform.localPosition;
            float targetHeight = currentLockOnTarget != null ? lockefPivotPosition : unlockedPivotPosition;
            pivotLocalPosition.y = Mathf.SmoothDamp(pivotLocalPosition.y, targetHeight, ref cameraHeightVelocityY, Time.deltaTime);
            cameraPivotTransform.localPosition = pivotLocalPosition;
        }
    }
}