using System.Collections.Generic;
using UnityEngine;

namespace TH7
{
    /// <summary>
    /// 路径预览显示
    /// </summary>
    public class PathPreview : MonoBehaviour
    {
        [Header("Line Settings")]
        [SerializeField] LineRenderer lineRenderer;
        [SerializeField] Color pathColor = new(0.2f, 0.8f, 0.2f, 0.8f);
        [SerializeField] float lineWidth = 0.1f;

        [Header("Destination Marker")]
        [SerializeField] SpriteRenderer destinationMarker;
        [SerializeField] Color reachableColor = Color.green;
        [SerializeField] Color unreachableColor = Color.red;

        MapManager mapManager;

        void Awake()
        {
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
                lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                lineRenderer.startColor = pathColor;
                lineRenderer.endColor = pathColor;
                lineRenderer.startWidth = lineWidth;
                lineRenderer.endWidth = lineWidth;
                lineRenderer.sortingOrder = 10;
            }

            if (destinationMarker == null)
            {
                var markerGo = new GameObject("DestinationMarker");
                markerGo.transform.SetParent(transform);
                destinationMarker = markerGo.AddComponent<SpriteRenderer>();
                destinationMarker.sortingOrder = 11;
            }

            Hide();
        }

        public void Initialize(MapManager map)
        {
            mapManager = map;
        }

        /// <summary>
        /// 显示路径预览
        /// </summary>
        public void ShowPath(List<Vector3Int> path, bool canReach)
        {
            if (path == null || path.Count == 0 || mapManager == null)
            {
                Hide();
                return;
            }

            // 设置线条颜色
            Color color = canReach ? reachableColor : unreachableColor;
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;

            // 设置路径点
            lineRenderer.positionCount = path.Count;
            for (int i = 0; i < path.Count; i++)
            {
                Vector3 worldPos = mapManager.CellToWorld(path[i]);
                lineRenderer.SetPosition(i, worldPos);
            }

            lineRenderer.enabled = true;

            // 显示目标点标记
            if (destinationMarker != null)
            {
                Vector3 destPos = mapManager.CellToWorld(path[^1]);
                destinationMarker.transform.position = destPos;
                destinationMarker.color = color;
                destinationMarker.enabled = true;
            }
        }

        /// <summary>
        /// 隐藏路径预览
        /// </summary>
        public void Hide()
        {
            if (lineRenderer != null)
                lineRenderer.enabled = false;

            if (destinationMarker != null)
                destinationMarker.enabled = false;
        }
    }
}
