using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 主菜单按钮反馈：鼠标悬停 / 键盘选中时放大、文字变亮、右侧光点浮现；点击可播放音效。
/// </summary>
[RequireComponent(typeof(Button))]
public class MenuButtonFX : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler,
    ISelectHandler, IDeselectHandler
{
    [Tooltip("悬停时的缩放倍数")]
    public float hoverScale = 1.06f;
    [Tooltip("动画速度")]
    public float lerpSpeed = 14f;
    [Tooltip("悬停时文字颜色")]
    public Color hoverTextColor = new Color(1f, 0.86f, 0.45f, 1f);
    [Tooltip("悬停时按钮底色乘数（变亮）")]
    public Color hoverImageColor = new Color(1.18f, 1.12f, 0.98f, 1f);
    [Tooltip("悬停光点（可选，按钮两侧的装饰光点）")]
    public Image[] glowDots;
    [Tooltip("音效源（可选）")]
    public AudioSource sfxSource;
    public AudioClip hoverClip;
    public AudioClip clickClip;

    Button m_Button;
    Text m_Text;
    Image m_Image;
    Color m_NormalText;
    Color m_NormalImage = Color.white;
    Vector3 m_NormalScale;
    bool m_Hover;

    void Awake()
    {
        m_Button = GetComponent<Button>();
        m_Text = GetComponentInChildren<Text>();
        m_Image = m_Button.image;
        if (m_Text != null) m_NormalText = m_Text.color;
        if (m_Image != null) m_NormalImage = m_Image.color;
        m_NormalScale = transform.localScale;
        m_Button.onClick.AddListener(PlayClick);
    }

    void OnEnable()
    {
        m_Hover = false;
        transform.localScale = m_NormalScale;
        if (m_Text != null) m_Text.color = m_NormalText;
        if (m_Image != null) m_Image.color = m_NormalImage;
        SetGlowAlpha(0f);
    }

    void Update()
    {
        bool active = m_Hover && m_Button.IsInteractable();
        float t = Time.unscaledDeltaTime * lerpSpeed;

        float scale = active ? hoverScale : 1f;
        transform.localScale = Vector3.Lerp(transform.localScale, m_NormalScale * scale, t);

        if (m_Text != null)
            m_Text.color = Color.Lerp(m_Text.color, active ? hoverTextColor : m_NormalText, t);

        if (m_Image != null)
            m_Image.color = Color.Lerp(m_Image.color, active ? hoverImageColor : m_NormalImage, t);

        // 光点呼吸闪烁
        float glow = active ? 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 5f) : 0f;
        SetGlowAlpha(Mathf.MoveTowards(CurrentGlowAlpha(), glow, t));
    }

    public void OnPointerEnter(PointerEventData eventData) => Hover();
    public void OnPointerExit(PointerEventData eventData) => m_Hover = false;
    public void OnSelect(BaseEventData eventData) => Hover();
    public void OnDeselect(BaseEventData eventData) => m_Hover = false;

    public void OnPointerDown(PointerEventData eventData) => PlayClick();

    void Hover()
    {
        if (m_Hover) return;
        m_Hover = true;
        if (sfxSource != null && hoverClip != null)
            sfxSource.PlayOneShot(hoverClip);
    }

    void PlayClick()
    {
        if (sfxSource != null && clickClip != null)
            sfxSource.PlayOneShot(clickClip);
    }

    float CurrentGlowAlpha()
    {
        if (glowDots == null || glowDots.Length == 0 || glowDots[0] == null) return 0f;
        return glowDots[0].color.a;
    }

    void SetGlowAlpha(float alpha)
    {
        if (glowDots == null) return;
        foreach (var dot in glowDots)
        {
            if (dot == null) continue;
            var c = dot.color;
            c.a = alpha;
            dot.color = c;
            dot.enabled = alpha > 0.01f;
        }
    }
}
