using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

namespace TH7
{
    /// <summary>
    /// Cinemachine 摄像头控制器
    /// 处理 WASD 移动和滚轮缩放
    /// </summary>
    public class CinemachineCameraController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] CinemachineCamera virtualCamera;

        [Header("Movement")]
        [SerializeField] float moveSpeed = 10f;
        [SerializeField] float edgePanSpeed = 5f;
        [SerializeField] float edgePanBorder = 20f;
        [SerializeField] bool enableEdgePan = false;

        [Header("Zoom")]
        [SerializeField] float zoomSpeed = 2f;
        [SerializeField] float minZoom = 3f;
        [SerializeField] float maxZoom = 15f;

        [Header("Bounds (Optional)")]
        [SerializeField] bool useBounds;
        [SerializeField] Vector2 boundsMin = new(-50, -50);
        [SerializeField] Vector2 boundsMax = new(50, 50);

        [Header("Input")]
        [SerializeField] InputActionAsset inputActions;
        [SerializeField] string moveActionName = "World/CameraMove";
        [SerializeField] string zoomActionName = "World/CameraZoom";

        InputAction moveAction;
        InputAction zoomAction;
        bool inputEnabled = true;

        void Awake()
        {
            if (virtualCamera == null)
                virtualCamera = GetComponent<CinemachineCamera>();

            SetupInputActions();
        }

        void SetupInputActions()
        {
            if (inputActions != null)
            {
                moveAction = inputActions.FindAction(moveActionName);
                zoomAction = inputActions.FindAction(zoomActionName);
            }

            // 如果没有配置或找不到，创建默认输入
            if (moveAction == null)
            {
                moveAction = new InputAction("CameraMove", InputActionType.Value);
                moveAction.AddCompositeBinding("2DVector")
                    .With("Up", "<Keyboard>/w")
                    .With("Down", "<Keyboard>/s")
                    .With("Left", "<Keyboard>/a")
                    .With("Right", "<Keyboard>/d");

                // 添加方向键支持
                moveAction.AddCompositeBinding("2DVector")
                    .With("Up", "<Keyboard>/upArrow")
                    .With("Down", "<Keyboard>/downArrow")
                    .With("Left", "<Keyboard>/leftArrow")
                    .With("Right", "<Keyboard>/rightArrow");
            }

            if (zoomAction == null)
            {
                zoomAction = new InputAction("CameraZoom", InputActionType.Value, "<Mouse>/scroll/y");
            }
        }

        void OnEnable()
        {
            moveAction?.Enable();
            zoomAction?.Enable();
        }

        void OnDisable()
        {
            moveAction?.Disable();
            zoomAction?.Disable();
        }

        void Update()
        {
            if (!inputEnabled) return;

            HandleMovement();
            HandleEdgePan();
            HandleZoom();
        }

        void HandleMovement()
        {
            if (moveAction == null) return;

            var move = moveAction.ReadValue<Vector2>();
            if (move.sqrMagnitude > 0.01f)
            {
                var delta = new Vector3(move.x, move.y, 0) * moveSpeed * Time.deltaTime;
                MoveCamera(delta);
            }
        }

        void HandleEdgePan()
        {
            if (!enableEdgePan) return;

            var mousePos = Mouse.current?.position.ReadValue() ?? Vector2.zero;
            var move = Vector2.zero;

            if (mousePos.x < edgePanBorder)
                move.x = -1;
            else if (mousePos.x > Screen.width - edgePanBorder)
                move.x = 1;

            if (mousePos.y < edgePanBorder)
                move.y = -1;
            else if (mousePos.y > Screen.height - edgePanBorder)
                move.y = 1;

            if (move.sqrMagnitude > 0.01f)
            {
                var delta = new Vector3(move.x, move.y, 0) * edgePanSpeed * Time.deltaTime;
                MoveCamera(delta);
            }
        }

        void MoveCamera(Vector3 delta)
        {
            var newPos = transform.position + delta;

            if (useBounds)
            {
                newPos.x = Mathf.Clamp(newPos.x, boundsMin.x, boundsMax.x);
                newPos.y = Mathf.Clamp(newPos.y, boundsMin.y, boundsMax.y);
            }

            transform.position = newPos;
        }

        void HandleZoom()
        {
            if (zoomAction == null || virtualCamera == null) return;

            var scroll = zoomAction.ReadValue<float>();
            if (Mathf.Abs(scroll) > 0.01f)
            {
                var lens = virtualCamera.Lens;
                // 滚轮向上(正值)应该放大(减小 orthographic size)
                lens.OrthographicSize -= scroll * zoomSpeed * 0.1f;
                lens.OrthographicSize = Mathf.Clamp(lens.OrthographicSize, minZoom, maxZoom);
                virtualCamera.Lens = lens;
            }
        }

        /// <summary>
        /// 启用输入
        /// </summary>
        public void EnableInput()
        {
            inputEnabled = true;
            moveAction?.Enable();
            zoomAction?.Enable();
        }

        /// <summary>
        /// 禁用输入
        /// </summary>
        public void DisableInput()
        {
            inputEnabled = false;
            moveAction?.Disable();
            zoomAction?.Disable();
        }

        /// <summary>
        /// 移动摄像头到指定位置
        /// </summary>
        public void MoveTo(Vector3 position)
        {
            position.z = transform.position.z;

            if (useBounds)
            {
                position.x = Mathf.Clamp(position.x, boundsMin.x, boundsMax.x);
                position.y = Mathf.Clamp(position.y, boundsMin.y, boundsMax.y);
            }

            transform.position = position;
        }

        /// <summary>
        /// 平滑移动到指定位置
        /// </summary>
        public void SmoothMoveTo(Vector3 position, float duration = 0.5f)
        {
            StartCoroutine(SmoothMoveCoroutine(position, duration));
        }

        System.Collections.IEnumerator SmoothMoveCoroutine(Vector3 target, float duration)
        {
            target.z = transform.position.z;

            if (useBounds)
            {
                target.x = Mathf.Clamp(target.x, boundsMin.x, boundsMax.x);
                target.y = Mathf.Clamp(target.y, boundsMin.y, boundsMax.y);
            }

            var start = transform.position;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / duration);
                transform.position = Vector3.Lerp(start, target, t);
                yield return null;
            }

            transform.position = target;
        }

        /// <summary>
        /// 设置缩放
        /// </summary>
        public void SetZoom(float size)
        {
            if (virtualCamera == null) return;

            var lens = virtualCamera.Lens;
            lens.OrthographicSize = Mathf.Clamp(size, minZoom, maxZoom);
            virtualCamera.Lens = lens;
        }

        /// <summary>
        /// 设置边界
        /// </summary>
        public void SetBounds(Vector2 min, Vector2 max)
        {
            boundsMin = min;
            boundsMax = max;
            useBounds = true;
        }

        void OnDestroy()
        {
            // 清理手动创建的 InputAction
            if (inputActions == null)
            {
                moveAction?.Dispose();
                zoomAction?.Dispose();
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (!useBounds) return;

            Gizmos.color = Color.yellow;
            var center = new Vector3((boundsMin.x + boundsMax.x) / 2, (boundsMin.y + boundsMax.y) / 2, 0);
            var size = new Vector3(boundsMax.x - boundsMin.x, boundsMax.y - boundsMin.y, 0);
            Gizmos.DrawWireCube(center, size);
        }
#endif
    }
}
