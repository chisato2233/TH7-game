using UnityEngine;
using GameFramework;

namespace TH7
{
    /// <summary>
    /// 战斗输入控制器
    /// 处理玩家输入并转发给 BattleActionProvider
    /// </summary>
    public class BattleInputController : GameBehaviour
    {
        [Header("References")]
        [SerializeField] Camera battleCamera;

        BattleActionProvider actionProvider;
        bool isEnabled;

        /// <summary>
        /// 获取绑定的行动提供者
        /// </summary>
        public BattleActionProvider ActionProvider => actionProvider;

        /// <summary>
        /// 绑定行动提供者
        /// </summary>
        public void BindActionProvider(BattleActionProvider provider)
        {
            actionProvider = provider;
        }

        /// <summary>
        /// 启用输入
        /// </summary>
        public void EnableInput()
        {
            isEnabled = true;
            Debug.Log("[BattleInputController] Input enabled");
        }

        /// <summary>
        /// 禁用输入
        /// </summary>
        public void DisableInput()
        {
            isEnabled = false;
            Debug.Log("[BattleInputController] Input disabled");
        }

        void Update()
        {
            if (!isEnabled || actionProvider == null)
                return;

            HandleMouseInput();
            HandleKeyboardInput();
        }

        void HandleMouseInput()
        {
            // 左键点击
            if (Input.GetMouseButtonDown(0))
            {
                HandleClick(false);
            }
            // 右键点击
            else if (Input.GetMouseButtonDown(1))
            {
                HandleClick(true);
            }
        }

        void HandleClick(bool isRightClick)
        {
            if (battleCamera == null)
                return;

            // 屏幕坐标转世界坐标
            Vector3 worldPos = battleCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int cellPos = new Vector2Int(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.y));

            // 检查是否点击了单位
            var unit = FindUnitAtPosition(worldPos);
            if (unit != null)
            {
                actionProvider.OnUnitClicked(unit, isRightClick);
            }
            else
            {
                actionProvider.OnTileClicked(cellPos, isRightClick);
            }
        }

        BattleUnit FindUnitAtPosition(Vector3 worldPos)
        {
            // 使用 2D 射线检测
            RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
            if (hit.collider != null)
            {
                return hit.collider.GetComponent<BattleUnit>();
            }
            return null;
        }

        void HandleKeyboardInput()
        {
            // W 键等待
            if (Input.GetKeyDown(KeyCode.W))
            {
                actionProvider.OnWaitRequested();
            }
            // D 键防御
            else if (Input.GetKeyDown(KeyCode.D))
            {
                actionProvider.OnDefendRequested();
            }
            // Escape 键取消
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                actionProvider.CancelRequest();
            }
        }
    }
}
