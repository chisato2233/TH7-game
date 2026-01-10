using UnityEngine;
using UnityEngine.InputSystem;

namespace TH7
{
    /// <summary>
    /// 世界地图输入控制器
    /// 连接 Input System 和 PlayerActionProvider
    /// 注意：摄像头控制由 Cinemachine 处理
    /// </summary>
    public class WorldInputController : MonoBehaviour
    {
        [Header("Input Actions")]
        [SerializeField] InputActionAsset inputActions;

        [Header("Action Names")]
        [SerializeField] string clickActionName = "World/Click";
        [SerializeField] string rightClickActionName = "World/RightClick";
        [SerializeField] string endTurnActionName = "World/EndTurn";

        // Input Actions
        InputAction clickAction;
        InputAction rightClickAction;
        InputAction endTurnAction;

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
        }

        void CreateDefaultInputActions()
        {
            // 创建默认输入（不依赖 InputActionAsset）
            clickAction = new InputAction("Click", InputActionType.Button, "<Mouse>/leftButton");
            rightClickAction = new InputAction("RightClick", InputActionType.Button, "<Mouse>/rightButton");
            endTurnAction = new InputAction("EndTurn", InputActionType.Button, "<Keyboard>/e");
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

            actionProvider?.SetEnabled(false);

            Debug.Log("[WorldInput] 输入已禁用");
        }

        void Update()
        {
            // 更新鼠标悬停预览
            if (isEnabled && actionProvider != null)
            {
                actionProvider.UpdateHover();
            }
        }

        void OnDestroy()
        {
            // 如果是手动创建的 InputAction，需要清理
            if (inputActions == null)
            {
                clickAction?.Dispose();
                rightClickAction?.Dispose();
                endTurnAction?.Dispose();
            }
        }
    }
}
