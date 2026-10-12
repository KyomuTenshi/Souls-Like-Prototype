using System.Collections.Generic;
using UnityEngine;

namespace SG {
    public class CameraHandler : MonoBehaviour
    {
        InputHandler inputHandler;
        public Transform targetTransform;
        public Transform cameraTransform;
        public Transform cameraPivotTransform;
        private Transform myTransform;
        private Vector3 cameraTransformPosition;
        public LayerMask ignoreLayers;
        private Vector3 cameraFollowVelocity = Vector3.zero;

        public static CameraHandler singleton;

        // Чувствительность задаётся в градусах на единицу смещения мыши.
        // В туториале ввод делился на delta (0.02 в FixedUpdate), поэтому значения здесь в 50 раз больше.
        public float lookSpeed = 5f;
        public float pivotSpeed = 1.5f;

        // В туториале было followSpeed со временем сглаживания delta / followSpeed (больше — быстрее).
        // Поле переименовано, чтобы старое значение из инспектора не переносилось с обратным смыслом.
        [Tooltip("Время сглаживания следования за персонажем, с. Меньше — камера отстаёт меньше. 0 — без задержки.")]
        public float followSmoothTime = 0.05f;

        private float targetPosition;
        private float defaultPosition;
        private float lookAngle;
        private float pivotAngle;
        private float minimumPivot = -35;
        private float maximumPivot = 35;

        public float cameraSphereRadius = 0.2f;
        public float cameraCollisionOffset = 0.2f;
        public float minimumCollisionOffset = 0.2f;

        public Transform currentLockOnTarget;

        List<CharacterManager> aviableTargets = new List<CharacterManager>();
        public Transform nearestLockOnTarget;
        public Transform leftLockOnTarget;
        public Transform rightLockOnTarget;
        public float maximumLockOnDistance = 30;

        private void Awake()
        {
            singleton = this;
            myTransform = transform;
            defaultPosition = cameraTransform.localPosition.z;
            ignoreLayers = ~(1 << 8 | 1 << 9 | 1 << 10);
            PlayerManager playerManager = FindFirstObjectByType<PlayerManager>();
            if (playerManager != null)
            {
                targetTransform = playerManager.transform;
            }
            inputHandler = FindFirstObjectByType<InputHandler>();
        }

        public void FollowTarget(float delta)
        {
            Vector3 targetPosition = Vector3.SmoothDamp(myTransform.position, targetTransform.position, ref cameraFollowVelocity, followSmoothTime);
            myTransform.position = targetPosition;

            HandleCameraCollisions(delta);
        }

        public void HandleCameraRotation(float delta, float mouseXInput, float mouseYInput)
        {
            if (inputHandler.lockOnFlag == false && currentLockOnTarget == null)
            {
                // Дельта мыши — это уже смещение за кадр, поэтому на delta не делится (в туториале делилось).
                lookAngle += mouseXInput * lookSpeed;
                pivotAngle -= mouseYInput * pivotSpeed;
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
                float velocity = 0;

                Vector3 dir = currentLockOnTarget.position - transform.position;
                dir.Normalize();
                dir.y = 0;

                Quaternion targetRotation = Quaternion.LookRotation(dir);
                transform.rotation = targetRotation;

                dir = currentLockOnTarget.position - cameraPivotTransform.position;
                dir.Normalize();

                targetRotation = Quaternion.LookRotation(dir);
                Vector3 eulerAngle = targetRotation.eulerAngles;
                // Поворот по Y уже сделан выше на самом CameraHandler, pivot — его дочерний объект,
                // поэтому в localEulerAngles пивота Y обнуляется, иначе поворот по Y применился бы дважды.
                eulerAngle.y = 0;
                cameraPivotTransform.localEulerAngles = eulerAngle;
            }
        }

        private void HandleCameraCollisions(float delta)
        {
            targetPosition = defaultPosition;
            RaycastHit hit;
            Vector3 direction = cameraTransform.position - cameraPivotTransform.position;
            direction.Normalize();

            // Триггеры игнорируются, иначе камера прижималась к персонажу у триггер-зон (огонь, предметы).
            if (Physics.SphereCast(cameraPivotTransform.position, cameraSphereRadius, direction, out hit, Mathf.Abs(targetPosition), ignoreLayers, QueryTriggerInteraction.Ignore))
            {
                float distance = Vector3.Distance(cameraPivotTransform.position, hit.point);
                targetPosition = -(distance - cameraCollisionOffset);
            }

            if (Mathf.Abs(targetPosition) < minimumCollisionOffset)
            {
                targetPosition = -minimumCollisionOffset;
            }

            cameraTransformPosition.z = Mathf.Lerp(cameraTransform.localPosition.z, targetPosition, delta / 0.2f);
            cameraTransform.localPosition = cameraTransformPosition;
        }

        public void HandleLockOn()
        {
            float shortestDistance = Mathf.Infinity;
            float shortestDistanceOfLeftTarget = Mathf.Infinity;
            float shortestDistanceOfRightTarget = Mathf.Infinity;

            // Список и цели пересчитываются заново при каждом вызове (в туториале не очищались):
            // при переключении влево/вправо HandleLockOn вызывается повторно, и без очистки в списке
            // копились дубликаты и враги, которые уже ушли из радиуса или из поля зрения.
            aviableTargets.Clear();
            leftLockOnTarget = null;
            rightLockOnTarget = null;

            Collider[] colliders = Physics.OverlapSphere(targetTransform.position, 26);

            for (int i = 0; i < colliders.Length; i++)
            {
                CharacterManager character = colliders[i].GetComponent<CharacterManager>();

                if (character != null)
                {
                    Vector3 lockTargetDirection = character.transform.position - targetTransform.position;
                    float distanceFromTarget = Vector3.Distance(targetTransform.position, character.lockOnTransform.position);
                    float viewableAngle = Vector3.Angle(lockTargetDirection, cameraTransform.forward);

                    if (character.transform.root != targetTransform.transform.root 
                        && viewableAngle > -50 && viewableAngle < 50 && distanceFromTarget <= maximumLockOnDistance)
                    {
                        aviableTargets.Add(character);
                    }
                }
            }

            for (int k = 0; k < aviableTargets.Count; k++)
            {
                float distanceFromTarget = Vector3.Distance(targetTransform.position, aviableTargets[k].lockOnTransform.position);

                if (distanceFromTarget < shortestDistance)
                {
                    shortestDistance = distanceFromTarget;
                    nearestLockOnTarget = aviableTargets[k].lockOnTransform;
                }

                if (inputHandler.lockOnFlag && currentLockOnTarget != null
                    && aviableTargets[k].lockOnTransform != currentLockOnTarget)
                {
                    // «Лево/право» считается относительно камеры и текущей цели (в туториале — от игрока
                    // и по мировой оси X): мировой X не зависит от того, куда смотрит камера, поэтому при
                    // повороте в другую сторону выбор путался, а знаки left/right были перепутаны.
                    // myTransform камеры вращается только по Y, поэтому его локальный X — горизонталь экрана.
                    Vector3 relativeEnemyPosition = myTransform.InverseTransformPoint(aviableTargets[k].lockOnTransform.position);
                    Vector3 relativeCurrentTargetPosition = myTransform.InverseTransformPoint(currentLockOnTarget.position);
                    var distanceFromLeftTarget = relativeCurrentTargetPosition.x - relativeEnemyPosition.x;
                    var distanceFromRightTarget = relativeEnemyPosition.x - relativeCurrentTargetPosition.x;

                    if (distanceFromLeftTarget > 0 && distanceFromLeftTarget < shortestDistanceOfLeftTarget)
                    {
                        shortestDistanceOfLeftTarget = distanceFromLeftTarget;
                        leftLockOnTarget = aviableTargets[k].lockOnTransform;
                    }

                    if (distanceFromRightTarget > 0 && distanceFromRightTarget < shortestDistanceOfRightTarget)
                    {
                        shortestDistanceOfRightTarget = distanceFromRightTarget;
                        rightLockOnTarget = aviableTargets[k].lockOnTransform;
                    }
                }
            }
        }

        public void ClearLockOnTargets()
        {
            aviableTargets.Clear();
            nearestLockOnTarget = null;
            leftLockOnTarget = null;
            rightLockOnTarget = null;
            currentLockOnTarget = null;
        }
    }
}
