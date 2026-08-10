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
        public LayerMask ignoreLayers;
        private Vector3 cameraFollowVelocity = Vector3.zero;

        public static CameraHandler singleton;

        [Header("Camera Speeds")]
        // lookSpeed/pivotSpeed — скорости для АНАЛОГОВОГО ввода (стик
        // геймпада): значение [-1..1] * скорость * delta = градусы в секунду.
        // Сейчас в PlayerControls.inputactions геймпад не привязан вообще —
        // поля лежат готовыми на будущее, мышь их больше не использует.
        public float lookSpeed = 100f;
        public float pivotSpeed = 100f;
        public float followSpeed = 0.1f;

        [Header("Mouse Sensitivity")]
        // БЫЛО: mouseX * lookSpeed * delta. Камера привязана к <Mouse>/delta —
        // это уже "пиксели С ПРОШЛОГО КАДРА", величина сама по себе per-frame.
        // Домножение на deltaTime делало чувствительность зависимой от FPS:
        // одно и то же движение мыши на 120 FPS поворачивало камеру вдвое
        // слабее, чем на 60, а при просадках кадра сенса "плавала".
        // Для мыши правильный масштаб: пиксели * сенса, БЕЗ delta.
        // Подкрути значения под свою мышь/DPI.
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

        private void Awake()
        {
            singleton = this;
            myTransform = transform;
            defaultPosition = cameraTransform.localPosition.z;
            // Игнорируем слои 8, 9, 10 (обычно Player, Controller, NPC)
            ignoreLayers = ~(1 << 8 | 1 << 9 | 1 << 10);

            // Явная проверка результата поиска: в сцене без игрока (меню,
            // тестовая сцена) прямое .transform кидало NRE прямо в Awake и
            // ломало singleton для всех остальных. FollowTarget уже умеет
            // жить с targetTransform == null — камера просто не следит.
            PlayerManager player = FindFirstObjectByType<PlayerManager>();
            if (player != null)
            {
                targetTransform = player.transform;
            }
            else
            {
                Debug.LogError("CameraHandler: PlayerManager не найден в сцене — камере не за кем следовать.");
            }
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

        // delta оставлен в сигнатуре для совместимости вызова из PlayerManager
        // и на будущее: стик геймпада (когда появится в биндингах)
        // масштабируется именно на delta, в отличие от мыши.
        public void HandleCameraRotation(float delta, float mouseXInput, float mouseYInput)
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
            // Clamp01 защищает от рывка камеры при просадках FPS (большой delta).
            float t = Mathf.Clamp01(delta / 0.2f);
            cameraTransformPosition.z = Mathf.Lerp(cameraTransform.localPosition.z, targetPosition, t);
            cameraTransform.localPosition = cameraTransformPosition;
        }
    }
}