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
    }

    private void OnDisable()
    {
        GameManager.OnMoneyChanged -= UpdateMoney;
        GameManager.OnIncomeChanged -= UpdateIncome;
        GameManager.OnAutoStateChanged -= UpdateAuto;
    }

    void UpdateMoney(double value)
    {
        moneyText.text = Format(value);
    }

    void UpdateIncome(double value)
    {
        incomeText.text = Format(value) + " /s";
    }

    void UpdateAuto(bool state)
    {
        autoText.text = state ? "AUTO: ON" : "AUTO: OFF";
    }

    string Format(double num)
    {
        if (num >= 1_000_000_000)
            return (num / 1_000_000_000d).ToString("F1") + "B";
        if (num >= 1_000_000)
            return (num / 1_000_000d).ToString("F1") + "M";
        if (num >= 1_000)
            return (num / 1_000d).ToString("F1") + "K";

        return num.ToString("F1");
    }
}