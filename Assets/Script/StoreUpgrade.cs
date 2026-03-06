using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoreUpgrade : MonoBehaviour
{
    [Header("Upgrade")]
    [SerializeField] private UpgradeType upgradeType;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private Button button;
    [SerializeField] private AudioClip upgradeSfx;

    private void OnEnable()
    {
        GameManager.OnMoneyChanged += HandleUpdate;
        GameManager.OnGameStateChanged += UpdateUI;

        UpdateUI();
    }

    private void OnDisable()
    {
        GameManager.OnMoneyChanged -= HandleUpdate;
        GameManager.OnGameStateChanged -= UpdateUI;
    }

    public void Purchase()
    {
        if (GameManager.Instance == null)
            return;

        bool success = GameManager.Instance.PurchaseUpgrade(upgradeType);
        if (success && AudioManager.Instance != null)
            AudioManager.Instance.PlayUpgrade(upgradeSfx);

        UpdateUI();
    }

    private void HandleUpdate(double _)
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (GameManager.Instance == null)
            return;

        int level = GameManager.Instance.GetUpgradeLevel(upgradeType);
        int maxLevel = GameManager.Instance.GetUpgradeMaxLevel(upgradeType);
        bool isMax = GameManager.Instance.IsUpgradeMaxed(upgradeType);

        if (labelText != null)
        {
            string name = GameManager.Instance.GetUpgradeName(upgradeType);
            string levelText = $"Lv.{level}/{maxLevel}";
            string costText = isMax
                ? "MAX"
                : GameManager.Instance.FormatMoney(GameManager.Instance.GetUpgradePrice(upgradeType));

            labelText.text = $"{name}\n{levelText}  Cost: {costText}";
        }

        if (button != null)
        {
            button.interactable = !isMax && GameManager.Instance.Money >= GameManager.Instance.GetUpgradePrice(upgradeType);
        }
    }
}
