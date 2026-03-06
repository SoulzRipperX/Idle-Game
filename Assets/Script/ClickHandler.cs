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

        if (ripeCount > 0d)
            targetPool.Spawn("Ready x" + ripeCount.ToString("F0"));
        else
            targetPool.Spawn("Water");
    }
}
