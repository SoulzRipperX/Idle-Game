using UnityEngine;
using UnityEngine.UI;

public class PlantPlot : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private GameObject plotRoot;
    [SerializeField] private Image uiImage;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Button clickButton;
    [SerializeField] private Sprite stage0Sprite;
    [SerializeField] private Sprite stage1Sprite;
    [SerializeField] private Sprite stage2Sprite;
    [SerializeField] private Sprite stage3Sprite;
    [SerializeField] private Color unlockedColor = Color.white;
    [SerializeField] private Color lockedColor = new Color(1f, 1f, 1f, 0.55f);

    [Header("State")]
    [SerializeField] private bool unlockedAtStart;

    private float growthProgress;
    private bool isUnlocked;

    public bool IsUnlocked => isUnlocked;
    public float GrowthProgress => growthProgress;
    public bool IsRipe => isUnlocked && growthProgress >= 3f;

    private void Awake()
    {
        SetUnlocked(unlockedAtStart);
        RefreshVisual();
    }

    public void SetUnlocked(bool value)
    {
        isUnlocked = value;

        if (plotRoot != null)
            plotRoot.SetActive(true);
        else
            gameObject.SetActive(true);

        if (!isUnlocked)
            growthProgress = 0f;

        ApplyInteractableState();
        RefreshVisual();
    }

    public void SetGrowthProgress(float progress)
    {
        growthProgress = Mathf.Max(0f, progress);
        RefreshVisual();
    }

    public bool AddGrowth(float amount)
    {
        if (!isUnlocked || amount <= 0f || IsRipe)
            return false;

        bool wasRipe = IsRipe;
        growthProgress += amount;
        if (growthProgress > 3f)
            growthProgress = 3f;

        RefreshVisual();
        return !wasRipe && IsRipe;
    }

    public bool HarvestAndReset()
    {
        if (!IsRipe)
            return false;

        growthProgress = 0f;
        RefreshVisual();
        return true;
    }

    private void RefreshVisual()
    {
        if (!isUnlocked)
        {
            ApplySprite(stage0Sprite);
            return;
        }

        int stage = Mathf.Clamp(Mathf.FloorToInt(growthProgress), 0, 3);

        switch (stage)
        {
            case 0:
                ApplySprite(stage0Sprite);
                break;
            case 1:
                ApplySprite(stage1Sprite);
                break;
            case 2:
                ApplySprite(stage2Sprite);
                break;
            default:
                ApplySprite(stage3Sprite);
                break;
        }
    }

    private void ApplySprite(Sprite sprite)
    {
        if (uiImage != null)
        {
            uiImage.sprite = sprite;
            uiImage.color = isUnlocked ? unlockedColor : lockedColor;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = isUnlocked ? unlockedColor : lockedColor;
        }
    }

    private void ApplyInteractableState()
    {
        if (clickButton == null)
        {
            if (plotRoot != null)
                clickButton = plotRoot.GetComponent<Button>();
            else
                clickButton = GetComponent<Button>();
        }

        if (clickButton != null)
            clickButton.interactable = isUnlocked;
    }
}
