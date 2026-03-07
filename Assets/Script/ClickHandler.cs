using UnityEngine;
using UnityEngine.Serialization;

public class ClickHandler : MonoBehaviour
{
    [SerializeField] private FloatingTextPool pool;
    [FormerlySerializedAs("targetPlot")]
    [SerializeField] private PlantGarden targetGarden;
    [SerializeField] private AudioClip clickSfx;

    private void Awake()
    {
        if (targetGarden == null)
            targetGarden = GetComponent<PlantGarden>();
    }

    public void Click()
    {
        if (GameManager.Instance == null)
            return;

        bool becameRipe = GameManager.Instance.WaterGardenByClick(targetGarden);
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayGardenClick(clickSfx);

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