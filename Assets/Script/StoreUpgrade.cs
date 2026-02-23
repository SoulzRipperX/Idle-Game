using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StoreUpgrade : MonoBehaviour
{
    public double startPrice = 15;
    public double priceMultiplier = 1.15;
    public double incomePerLevel = 0.1;
    public int level = 0;
    public string upgradeName;

    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Button button;

    private void OnEnable()
    {
        GameManager.OnMoneyChanged += UpdateUI;
    }

    private void OnDisable()
    {
        GameManager.OnMoneyChanged -= UpdateUI;
    }

    public double CalculatePrice()
    {
        return startPrice * System.Math.Pow(priceMultiplier, level);
    }

    public double CalculateIncomePerSecond()
    {
        return incomePerLevel * level;
    }
}
