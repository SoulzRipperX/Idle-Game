using UnityEngine;

public class ClickHandler : MonoBehaviour
{
    [SerializeField] private FloatingTextPool pool;
    [SerializeField] private PlantPlot targetPlot;
    [SerializeField] private AudioClip clickSfx;

    private void Awake()
    {
        if (targetPlot == null)
            targetPlot = GetComponent<PlantPlot>();
    }

    public void Click()
    {
        if (GameManager.Instance == null)
            return;

        bool becameRipe = GameManager.Instance.WaterPlotByClick(targetPlot);
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayPlotClick(clickSfx);

        FloatingTextPool targetPool = pool != null ? pool : FloatingTextPool.Instance;
        if (targetPool == null)
            return;

        Vector2 pointerPos = Input.mousePosition;
        if (Input.touchCount > 0)
            pointerPos = Input.GetTouch(0).position;

        if (becameRipe)
            targetPool.SpawnAtScreenPosition("Ready", pointerPos);
        else
            targetPool.SpawnAtScreenPosition("Water", pointerPos);
    }
}
