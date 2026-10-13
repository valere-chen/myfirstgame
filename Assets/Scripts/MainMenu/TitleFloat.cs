using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 标题漂浮与呼吸发光动画。
/// </summary>
public class TitleFloat : MonoBehaviour
{
    [Tooltip("上下漂浮幅度（像素）")]
    public float amplitudeY = 10f;
    [Tooltip("漂浮速度")]
    public float floatSpeed = 1.1f;
    [Tooltip("呼吸缩放幅度")]
    public float breatheAmount = 0.015f;
    [Tooltip("需要呼吸变色的标题文字（可选）")]
    public Text glowText;
    public Color colorA = new Color(1f, 0.82f, 0.45f, 1f);
    public Color colorB = new Color(1f, 0.93f, 0.66f, 1f);

    Vector3 m_StartPos;
    Vector3 m_StartScale;

    void Start()
    {
        m_StartPos = transform.localPosition;
        m_StartScale = transform.localScale;
    }

    void Update()
    {
        float t = Time.unscaledTime * floatSpeed;
        transform.localPosition = m_StartPos + new Vector3(0f, Mathf.Sin(t) * amplitudeY, 0f);

        float s = 1f + Mathf.Sin(t * 0.8f) * breatheAmount;
        transform.localScale = m_StartScale * s;

        if (glowText != null)
        {
            float k = Mathf.InverseLerp(-1f, 1f, Mathf.Sin(t * 0.9f));
            glowText.color = Color.Lerp(colorA, colorB, k);
        }
    }
}
