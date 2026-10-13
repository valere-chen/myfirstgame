using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 鬼屋主菜单控制器：整体淡入、开始游戏、操作说明面板、退出游戏、环境音淡入。
/// </summary>
[DisallowMultipleComponent]
public class MainMenu : MonoBehaviour
{
    [Header("整体淡入淡出")]
    public CanvasGroup rootGroup;
    public float fadeInDuration = 1.2f;
    public float fadeOutDuration = 0.6f;

    [Header("操作说明面板")]
    public CanvasGroup helpPanel;
    public float helpFadeDuration = 0.25f;

    [Header("音频")]
    public AudioSource ambientSource;
    public float ambientTargetVolume = 0.5f;
    public float ambientFadeDuration = 3f;
    public AudioSource sfxSource;
    public AudioClip buttonClickClip;

    bool m_HelpVisible;
    Coroutine m_HelpRoutine;

    void Awake()
    {
        EnsureInputModule();
        BindUnboundButtons();
    }

    /// <summary>
    /// 项目的 Active Input Handling 为 Both（新旧输入并存），游戏移动代码使用旧输入系统。
    /// 场景由构建脚本自动生成时，EventSystem 上若是未分配 actions 的 InputSystemUIInputModule，
    /// 按钮会完全不响应；这里统一替换为即插即用的 StandaloneInputModule。
    /// </summary>
    static void EnsureInputModule()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        // 仅启用新输入系统时不处理
        return;
#else
        var es = FindFirstObjectByType<EventSystem>();
        if (es == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            return;
        }

        bool hasStandalone = false;
        foreach (var module in es.GetComponents<BaseInputModule>())
        {
            if (module is StandaloneInputModule)
                hasStandalone = true;
            else
                Destroy(module); // 移除未配置 actions 的 InputSystemUIInputModule
        }
        if (!hasStandalone) es.gameObject.AddComponent<StandaloneInputModule>();
#endif
    }

    /// <summary>
    /// 兜底绑定：旧版菜单场景的按钮事件用 AddListener 注册、未序列化进场景文件，
    /// 这里在运行时按按钮文字补绑；已有持久化事件的按钮不重复绑定，避免触发两次。
    /// </summary>
    void BindUnboundButtons()
    {
        var buttons = FindObjectsByType<Button>(FindObjectsSortMode.None);
        foreach (var button in buttons)
        {
            if (button.onClick.GetPersistentEventCount() > 0) continue;
            var label = button.GetComponentInChildren<Text>();
            if (label == null) continue;
            string key = label.text.Replace("　", "").Replace(" ", "").Trim();
            switch (key)
            {
                case "开始游戏": button.onClick.AddListener(StartGame); break;
                case "操作说明": button.onClick.AddListener(ToggleHelp); break;
                case "退出游戏": button.onClick.AddListener(QuitGame); break;
                case "返回": button.onClick.AddListener(HideHelp); break;
            }
        }
    }
    void Start()
    {
        if (rootGroup != null)
        {
            rootGroup.alpha = 0f;
            StartCoroutine(FadeCanvas(rootGroup, 0f, 1f, fadeInDuration));
        }

        if (helpPanel != null) SetHelpInstant(false);

        if (ambientSource != null)
        {
            ambientSource.volume = 0f;
            ambientSource.loop = true;
            if (!ambientSource.isPlaying) ambientSource.Play();
            StartCoroutine(FadeAmbient());
        }
    }

    void Update()
    {
        // Esc 关闭说明面板
        if (m_HelpVisible && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1)))
        {
            HideHelp();
        }
    }

    /// <summary>开始游戏：淡出后加载游戏场景。</summary>
    public void StartGame()
    {
        PlayClick();
        StartCoroutine(StartGameRoutine());
    }

    IEnumerator StartGameRoutine()
    {
        if (rootGroup != null)
            yield return StartCoroutine(FadeCanvas(rootGroup, rootGroup.alpha, 0f, fadeOutDuration));
        SceneManager.LoadScene("SampleScene");
    }

    /// <summary>打开/关闭操作说明面板。</summary>
    public void ToggleHelp()
    {
        PlayClick();
        if (m_HelpVisible) HideHelp();
        else ShowHelp();
    }

    public void ShowHelp()
    {
        m_HelpVisible = true;
        if (m_HelpRoutine != null) StopCoroutine(m_HelpRoutine);
        m_HelpRoutine = StartCoroutine(HelpRoutine(true));
    }

    public void HideHelp()
    {
        m_HelpVisible = false;
        if (m_HelpRoutine != null) StopCoroutine(m_HelpRoutine);
        m_HelpRoutine = StartCoroutine(HelpRoutine(false));
    }

    IEnumerator HelpRoutine(bool show)
    {
        if (helpPanel == null) yield break;
        helpPanel.blocksRaycasts = show;
        helpPanel.interactable = show;
        yield return StartCoroutine(FadeCanvas(helpPanel, helpPanel.alpha, show ? 1f : 0f, helpFadeDuration));
    }

    void SetHelpInstant(bool show)
    {
        helpPanel.alpha = show ? 1f : 0f;
        helpPanel.blocksRaycasts = show;
        helpPanel.interactable = show;
    }

    /// <summary>退出游戏。</summary>
    public void QuitGame()
    {
        PlayClick();
        StartCoroutine(QuitRoutine());
    }

    IEnumerator QuitRoutine()
    {
        if (rootGroup != null)
            yield return StartCoroutine(FadeCanvas(rootGroup, rootGroup.alpha, 0f, fadeOutDuration));

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void PlayClick()
    {
        if (sfxSource != null && buttonClickClip != null)
            sfxSource.PlayOneShot(buttonClickClip);
    }

    IEnumerator FadeCanvas(CanvasGroup group, float from, float to, float duration)
    {
        float elapsed = 0f;
        group.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        group.alpha = to;
    }

    IEnumerator FadeAmbient()
    {
        float elapsed = 0f;
        while (elapsed < ambientFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            ambientSource.volume = Mathf.Lerp(0f, ambientTargetVolume, Mathf.Clamp01(elapsed / ambientFadeDuration));
            yield return null;
        }
        ambientSource.volume = ambientTargetVolume;
    }
}
