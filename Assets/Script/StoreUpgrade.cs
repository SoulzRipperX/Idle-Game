using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoreUpgrade : MonoBehaviour
{
    [Header("Config")]
    public string upgradeName;
    [SerializeField] private string upgradeId;
    [SerializeField] private double startPrice = 15;
    [SerializeField] private double priceMultiplier = 1.15;
    [SerializeField] private double incomePerLevel = 0.1;
    [SerializeField] private int level;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Button button;

    public int Level => level;

    public string UpgradeId
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(upgradeId))
                return upgradeId;

            return string.IsNullOrWhiteSpace(upgradeName) ? gameObject.name : upgradeName;
        }
    }

    private void OnEnable()
    {
        GameManager.OnMoneyChanged += UpdateUI;

        if (GameManager.Instance != null)
            UpdateUI(GameManager.Instance.Money);
    }

    private void OnDisable()
    {
        GameManager.OnMoneyChanged -= UpdateUI;
    }

    public void SetLevel(int value)
    {
        level = Mathf.Max(0, value);
    }

    public double CalculatePrice()
    {
        return startPrice * System.Math.Pow(priceMultiplier, level);
    }

    public double CalculateIncomePerSecond()
    {
        return incomePerLevel * level;
    }

    public void Purchase()
    {
        if (GameManager.Instance == null)
            return;

        double price = CalculatePrice();

        if (!GameManager.Instance.SpendMoney(price))
            return;

        level++;
        GameManager.Instance.HandleUpgradeStateChanged();
        UpdateUI(GameManager.Instance.Money);
    }

    private void UpdateUI(double _)
    {
        if (GameManager.Instance == null) return;
        string displayName = !string.IsNullOrWhiteSpace(upgradeName)
            ? upgradeName
            : (!string.IsNullOrWhiteSpace(upgradeId) ? upgradeId : gameObject.name);

        if (priceText != null)
            priceText.text = $"{displayName}\n{GameManager.Instance.FormatMoney(CalculatePrice())}";

        if (button != null)
            button.interactable = GameManager.Instance.Money >= CalculatePrice();
    }
}
