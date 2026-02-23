using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI incomeText;

    private void OnEnable()
    {
        GameManager.OnMoneyChanged += UpdateMoney;
        GameManager.OnIncomeChanged += UpdateIncome;
    }

    private void OnDisable()
    {
        GameManager.OnMoneyChanged -= UpdateMoney;
        GameManager.OnIncomeChanged -= UpdateIncome;
    }

    void UpdateMoney()
    {
        moneyText.text = Format(GameManager.Instance.Money);
    }

    void UpdateIncome()
    {
        incomeText.text = Format(GameManager.Instance.IncomePerSecond) + " /s";
    }

    string Format(double num)
    {
        if (num >= 1_000_000)
            return (num / 1_000_000d).ToString("F1") + "M";
        if (num >= 1_000)
            return (num / 1_000d).ToString("F1") + "K";

        return num.ToString("F1");
    }
}
