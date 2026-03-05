using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class RGBTextColor : MonoBehaviour
{
    public enum ColorMode
    {
        StaticRgb,
        RainbowCycle
    }

    [Header("Mode")]
    [SerializeField] private ColorMode mode = ColorMode.StaticRgb;

    [Header("Static RGB (0-255)")]
    [Range(0, 255)] [SerializeField] private int red = 255;
    [Range(0, 255)] [SerializeField] private int green = 255;
    [Range(0, 255)] [SerializeField] private int blue = 255;
    [Range(0f, 1f)] [SerializeField] private float alpha = 1f;

    [Header("Rainbow")]
    [SerializeField] private float rainbowSpeed = 0.5f;
    [SerializeField] private float saturation = 1f;
    [SerializeField] private float value = 1f;

    private TextMeshProUGUI textMesh;

    private void Awake()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        if (textMesh == null) return;

        if (mode == ColorMode.StaticRgb)
        {
            textMesh.color = new Color32((byte)red, (byte)green, (byte)blue, (byte)Mathf.RoundToInt(alpha * 255f));
            return;
        }

        float hue = Mathf.Repeat(Time.time * rainbowSpeed, 1f);
        Color rainbow = Color.HSVToRGB(hue, Mathf.Clamp01(saturation), Mathf.Clamp01(value));
        rainbow.a = alpha;
        textMesh.color = rainbow;
    }
}
