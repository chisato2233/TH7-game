using UnityEngine;
using UnityEngine.InputSystem;

namespace TH7
{
    /// <summary>
    /// 世界地图输入控制器
    /// 连接 Input System 和 PlayerActionProvider
    /// 处理 WASD 摄像头移动（通过移动 Cinemachine Follow Target）
    /// </summary>
    public class WorldInputController : MonoBehaviour
    {
        [Header("Input Actions")]
        [SerializeField] InputActionAsset inputActions;

        [Header("Action Names")]
        [SerializeField] string clickActionName = "World/Click";
        [SerializeField] string rightClickActionName = "World/RightClick";
        [SerializeField] string endTurnActionName = "World/EndTurn";
        [SerializeField] string cameraMoveActionName = "World/CameraMove";

        [Header("Camera Control")]
        [SerializeField] Transform cameraFollowTarget;
        [SerializeField] float cameraMoveSpeed = 10f;

        // Input Actions
        InputAction clickAction;
        InputAction rightClickAction;
        InputAction endTurnAction;
        InputAction cameraMoveAction;

        PlayerActionProvider actionProvider;
        bool isEnabled;

        void Awake()
        {
            SetupInputActions();
        }

        void SetupInputActions()
        {
            if (inputActions == null)
            {
                Debug.LogWarning("[WorldInput] InputActionAsset 未配置，使用默认输入");
                CreateDefaultInputActions();
                return;
            }

            clickAction = inputActions.FindAction(clickActionName);
            rightClickAction = inputActions.FindAction(rightClickActionName);
            endTurnAction = inputActions.FindAction(endTurnActionName);
            cameraMoveAction = inputActions.FindAction(cameraMoveActionName);
        }

        void CreateDefaultInputActions()
        {
            // 创建默认输入（不依赖 InputActionAsset）
            clickAction = new InputAction("Click", InputActionType.Button, "<Mouse>/leftButton");
            rightClickAction = new InputAction("RightClick", InputActionType.Button, "<Mouse>/rightButton");
            endTurnAction = new InputAction("EndTurn", InputActionType.Button, "<Keyboard>/e");

            // WASD 摄像头移动
            cameraMoveAction = new InputAction("CameraMove", InputActionType.Value);
            cameraMoveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
        }

        /// <summary>
        /// 绑定 PlayerActionProvider
        /// </summary>
        public void BindActionProvider(PlayerActionProvider provider)
        {
            actionProvider = provider;

            if (provider != null)
            {
                provider.BindInputActions(clickAction, rightClickAction, endTurnAction);
            }
        }

        /// <summary>
        /// 启用输入
        /// </summary>
        public void EnableInput()
        {
            isEnabled = true;

            clickAction?.Enable();
            rightClickAction?.Enable();
            endTurnAction?.Enable();
            cameraMoveAction?.Enable();

            Debug.Log("[WorldInput] 输入已启用");
        }

        /// <summary>
        /// 禁用输入
        /// </summary>
        public void DisableInput()
        {
            isEnabled = false;

            clickAction?.Disable();
            rightClickAction?.Disable();
            endTurnAction?.Disable();
            cameraMoveAction?.Disable();

            actionProvider?.SetEnabled(false);

            Debug.Log("[WorldInput] 输入已禁用");
        }

        void Update()
        {
            if (!isEnabled) return;

            // 更新鼠标悬停预览
            if (actionProvider != null)
            {
                actionProvider.UpdateHover();
            }

            // WASD 摄像头移动
            UpdateCameraMovement();
        }

        void UpdateCameraMovement()
        {
            if (cameraFollowTarget == null || cameraMoveAction == null) return;

            Vector2 input = cameraMoveAction.ReadValue<Vector2>();
            if (input.sqrMagnitude < 0.01f) return;

            Vector3 move = new Vector3(input.x, input.y, 0) * cameraMoveSpeed * Time.deltaTime;
            cameraFollowTarget.position += move;
        }

        void OnDestroy()
        {
            // 如果是手动创建的 InputAction，需要清理
            if (inputActions == null)
            {
                clickAction?.Dispose();
                rightClickAction?.Dispose();
                endTurnAction?.Dispose();
                cameraMoveAction?.Dispose();
            }
        }
    }
}
