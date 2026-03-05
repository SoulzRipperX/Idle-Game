using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI incomeText;
    [SerializeField] private TextMeshProUGUI autoText;

    private void OnEnable()
    {
        GameManager.OnMoneyChanged += UpdateMoney;
        GameManager.OnIncomeChanged += UpdateIncome;
        GameManager.OnAutoStateChanged += UpdateAuto;

        if (GameManager.Instance != null)
        {
            UpdateMoney(GameManager.Instance.Money);
            UpdateIncome(GameManager.Instance.IncomePerSecond);
            UpdateAuto(GameManager.Instance.AutoEnabled);
        }
    }

    private void OnDisable()
    {
        GameManager.OnMoneyChanged -= UpdateMoney;
        GameManager.OnIncomeChanged -= UpdateIncome;
        GameManager.OnAutoStateChanged -= UpdateAuto;
    }

    private void UpdateMoney(double value)
    {
        if (moneyText != null)
            moneyText.text = Format(value);
    }

    private void UpdateIncome(double value)
    {
        if (incomeText != null)
            incomeText.text = Format(value) + " /s";
    }

    private void UpdateAuto(bool state)
    {
        if (autoText != null)
            autoText.text = state ? "AUTO: ON" : "AUTO: OFF";
    }

    private string Format(double num)
    {
        if (num >= 1_000_000_000d)
            return (num / 1_000_000_000d).ToString("F1") + "B";
        if (num >= 1_000_000d)
            return (num / 1_000_000d).ToString("F1") + "M";
        if (num >= 1_000d)
            return (num / 1_000d).ToString("F1") + "K";

        return num.ToString("F1");
    }
}
