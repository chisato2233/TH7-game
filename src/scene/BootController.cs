using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;
using DG.Tweening;
using GameFramework;

namespace TH7
{
    // 启动场景控制器，播放启动动画后跳转主菜单
    public class BootController : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] string mainMenuScene = "MainMenuScene";

        [Header("UI References")]
        [SerializeField] CanvasGroup fadeCanvas;
        [SerializeField] Image logoImage;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text subtitleText;

        [Header("Animation Settings")]
        [SerializeField] float logoFadeInDuration = 0.8f;
        [SerializeField] float logoStayDuration = 1.5f;
        [SerializeField] float titleFadeInDuration = 0.6f;
        [SerializeField] float finalFadeOutDuration = 0.5f;

        void Awake()
        {
            DOTween.Init();
            InitializeSystems();
        }

        void Start()
        {
            SetupInitialState();
            StartCoroutine(PlayBootSequence());
        }

        void InitializeSystems()
        {
            // GameEntry.Instance 会自动初始化核心系统
            _ = GameEntry.Instance;
            Debug.Log("[Boot] 系统初始化完成");
        }

        void SetupInitialState()
        {
            if (fadeCanvas) fadeCanvas.alpha = 1f;
            if (logoImage) logoImage.color = new Color(1, 1, 1, 0);
            if (titleText) titleText.color = new Color(1, 1, 1, 0);
            if (subtitleText) subtitleText.color = new Color(1, 1, 1, 0);
        }

        IEnumerator PlayBootSequence()
        {
            // 等待一帧确保初始化完成
            yield return null;

            // Logo 淡入 + 缩放弹出
            if (logoImage)
            {
                logoImage.transform.localScale = Vector3.one * 0.5f;
                var logoSequence = DOTween.Sequence()
                    .Append(logoImage.DOFade(1f, logoFadeInDuration))
                    .Join(logoImage.transform.DOScale(1f, logoFadeInDuration).SetEase(Ease.OutBack));
                yield return logoSequence.WaitForCompletion();
            }

            // Logo 停留
            yield return new WaitForSeconds(logoStayDuration * 0.5f);

            // 标题文字逐个淡入 + 上移
            if (titleText)
            {
                titleText.transform.localPosition += Vector3.down * 30f;
                var titleSequence = DOTween.Sequence()
                    .Append(titleText.DOFade(1f, titleFadeInDuration))
                    .Join(titleText.transform.DOLocalMoveY(titleText.transform.localPosition.y + 30f, titleFadeInDuration).SetEase(Ease.OutCubic));
                yield return titleSequence.WaitForCompletion();
            }

            // 副标题淡入
            if (subtitleText)
            {
                yield return subtitleText.DOFade(1f, titleFadeInDuration * 0.6f).WaitForCompletion();
            }

            // 停留展示
            yield return new WaitForSeconds(logoStayDuration * 0.5f);

            // 整体淡出
            if (fadeCanvas)
            {
                fadeCanvas.alpha = 0f;
                var fadePanel = fadeCanvas.GetComponentInChildren<Image>();
                if (fadePanel)
                {
                    fadePanel.color = Color.black;
                    yield return fadeCanvas.DOFade(1f, finalFadeOutDuration).WaitForCompletion();
                }
            }

            // 异步加载场景
            var asyncOp = SceneManager.LoadSceneAsync(mainMenuScene);
            asyncOp.allowSceneActivation = true;
        }

        void OnDestroy()
        {
            DOTween.Kill(transform);
        }
    }
}
