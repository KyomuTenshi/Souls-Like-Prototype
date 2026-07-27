using UnityEngine;

namespace SG 
{
    public class CameraHandler : MonoBehaviour
    {
        public Transform targetTransform;
        public Transform cameraTransform;
        public Transform cameraPivotTransform;
        private Transform myTransform;
        private Vector3 cameraTransformPosition;
        private LayerMask ignoreLayers;
        private Vector3 cameraFollowVelocity = Vector3.zero;

        public static CameraHandler singleton;

        [Header("Camera Speeds")]
        public float lookSpeed = 100f;
        public float pivotSpeed = 100f;
        public float followSpeed = 0.1f;

        private float targetPosition;
        private float defaultPosition;
        private float lookAngle;
        private float pivotAngle;

        [Header("Camera Limits")]
        // БЫЛО: minimumPivot/maximumPivot были private БЕЗ [SerializeField].
        // Unity сериализует и показывает в инспекторе только public поля или
        // private с атрибутом [SerializeField]. Без него [Header] выше рисовал
        // заголовок, а сами поля в инспекторе не появлялись и их нельзя было
        // подкрутить не залезая в код. Добавлен [SerializeField].
        [SerializeField] private float minimumPivot = -35f;
        [SerializeField] private float maximumPivot = 35f;

        [Header("Collision Settings")]
        public float cameraSphereRadius = 0.2f;
        public float cameraCollisionOffSet = 0.2f;
        public float minimunCollisionOffSet = 0.2f;

        private void Awake()
        {
            singleton = this;
            myTransform = transform;
            defaultPosition = cameraTransform.localPosition.z;
            // Игнорируем слои 8, 9, 10 (обычно Player, Controller, NPC)
            ignoreLayers = ~(1 << 8 | 1 << 9 | 1 << 10);
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
            lookAngle += mouseXInput * lookSpeed * delta;
            pivotAngle -= mouseYInput * pivotSpeed * delta;

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

        public void HandleCameraCollisions(float delta)
        {
            targetPosition = defaultPosition;
            RaycastHit hit;

            // Вектор строго от Pivot к Камере
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
            // БЫЛО: delta / 0.2f — при просадках FPS (большой delta) множитель
            // мог превысить 1 и Lerp давал резкий "перескок" камеры сквозь стену
            // вместо плавного подтягивания. Clamp01 убирает этот рывок, оставляя
            // ту же скорость сглаживания при нормальном FPS.
            float t = Mathf.Clamp01(delta / 0.2f);
            cameraTransformPosition.z = Mathf.Lerp(cameraTransform.localPosition.z, targetPosition, t);
            cameraTransform.localPosition = cameraTransformPosition;
        }
    }
}