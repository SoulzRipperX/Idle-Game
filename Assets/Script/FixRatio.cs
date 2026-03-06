using UnityEngine;

public class FixRatio : MonoBehaviour
{
    [Header("Settings")]
    public Camera targetCamera;
    public float targetAspect = 16f / 9f;

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        UpdateCamera();
    }

    void UpdateCamera()
    {
        float screenAspect = (float)Screen.width / Screen.height;
        float scaleHeight = screenAspect / targetAspect;

        Rect rect = targetCamera.rect;

        if (scaleHeight < 1.0f)
        {
            rect.width = 1.0f;
            rect.height = scaleHeight;
            rect.x = 0;
            rect.y = (1.0f - scaleHeight) / 2.0f;
        }
        else
        {
            float scaleWidth = 1.0f / scaleHeight;
            rect.width = scaleWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleWidth) / 2.0f;
            rect.y = 0;
        }

        targetCamera.rect = rect;
    }
}