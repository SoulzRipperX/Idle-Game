using UnityEngine;

public class ClickHandler : MonoBehaviour
{
    [SerializeField] private FloatingTextPool pool;

    public void Click()
    {
        if (GameManager.Instance == null)
            return;

        double ripeCount = GameManager.Instance.WaterByClick();

        FloatingTextPool targetPool = pool != null ? pool : FloatingTextPool.Instance;
        if (targetPool == null)
            return;

        Vector2 pointerPos = Input.mousePosition;
        if (Input.touchCount > 0)
            pointerPos = Input.GetTouch(0).position;

        if (ripeCount > 0d)
            targetPool.SpawnAtScreenPosition("Ready x" + ripeCount.ToString("F0"), pointerPos);
        else
            targetPool.SpawnAtScreenPosition("Water", pointerPos);
    }
}
