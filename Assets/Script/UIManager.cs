using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI incomeText;
    [SerializeField] private TextMeshProUGUI autoText;
    [SerializeField] private TextMeshProUGUI plotText;
    [SerializeField] private TextMeshProUGUI saveStatusText;
    [SerializeField] private TextMeshProUGUI saveSlotText;
    [SerializeField] private Toggle autoToggle;

    private float saveMessageTimer;

    private void OnEnable()
    {
        EnsureAutoToggleReference();

        GameManager.OnMoneyChanged += UpdateMoney;
        GameManager.OnIncomeChanged += UpdateIncome;
        GameManager.OnAutoStateChanged += UpdateAuto;
        GameManager.OnGameStateChanged += RefreshStats;
        GameManager.OnSaveMessage += ShowSaveMessage;

        if (GameManager.Instance != null)
        {
            UpdateMoney(GameManager.Instance.Money);
            UpdateIncome(GameManager.Instance.IncomePerSecond);
            UpdateAuto(GameManager.Instance.AutoEnabled);
            RefreshStats();
        }

        if (saveStatusText != null)
            saveStatusText.text = string.Empty;
    }

    private void OnDisable()
    {
        GameManager.OnMoneyChanged -= UpdateMoney;
        GameManager.OnIncomeChanged -= UpdateIncome;
        GameManager.OnAutoStateChanged -= UpdateAuto;
        GameManager.OnGameStateChanged -= RefreshStats;
        GameManager.OnSaveMessage -= ShowSaveMessage;
    }

    private void Update()
    {
        if (saveStatusText == null || string.IsNullOrEmpty(saveStatusText.text))
            return;

        saveMessageTimer -= Time.deltaTime;
        if (saveMessageTimer <= 0f)
            saveStatusText.text = string.Empty;
    }

    public void OnTapSave()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ManualSave();
    }

    public void OnTapLoad()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ManualLoad();
    }

    public void OnTapDeleteSave()
    {
        if (GameManager.Instance == null)
            return;

        GameManager.Instance.DeleteActiveSaveSlot();
        RefreshSaveSlotLabel();
    }

    public void OnTapOpenSaveFolder()
    {
        if (GameManager.Instance == null)
            return;

        GameManager.Instance.OpenSaveFolder();
        ShowSaveMessage("Open: " + GameManager.Instance.GetSaveFolderPath());
    }

    public void OnTapNextSaveSlot()
    {
        if (GameManager.Instance == null)
            return;

        int next = GameManager.Instance.GetActiveSaveSlot() + 1;
        if (next > GameManager.Instance.GetSaveSlotCount())
            next = 1;

        GameManager.Instance.SetActiveSaveSlot(next);
        RefreshSaveSlotLabel();
    }

    public void OnTapSaveSlot1() => SetSaveSlot(1);
    public void OnTapSaveSlot2() => SetSaveSlot(2);
    public void OnTapSaveSlot3() => SetSaveSlot(3);

    public void OnTapAuto()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SetAutoEnabled(!GameManager.Instance.AutoEnabled);
    }

    public void OnAutoToggleChanged(bool isOn)
    {
        if (GameManager.Instance == null)
            return;

        GameManager.Instance.SetAutoEnabled(isOn);

        EnsureAutoToggleReference();
        if (autoToggle != null)
            autoToggle.SetIsOnWithoutNotify(GameManager.Instance.AutoEnabled);
    }

    public void OnTapSell()
    {
        if (GameManager.Instance == null)
            return;

        double earned = GameManager.Instance.SellAllRipe();
        if (earned > 0d)
            ShowSaveMessage("Sold +" + GameManager.Instance.FormatMoney(earned));
        else
            ShowSaveMessage("No ripe plant");
    }

    private void UpdateMoney(double value)
    {
        if (moneyText == null) return;
        moneyText.text = "Coins: " + Format(value);
    }

    private void UpdateIncome(double value)
    {
        if (incomeText == null) return;
        incomeText.text = "Auto Sell: " + Format(value) + " /s";
    }

    private void UpdateAuto(bool state)
    {
        if (autoText == null) return;
        autoText.text = state ? "AUTO: ON" : "AUTO: OFF";

        EnsureAutoToggleReference();
        if (autoToggle != null)
            autoToggle.SetIsOnWithoutNotify(state);
    }

    private void RefreshStats()
    {
        if (GameManager.Instance == null || plotText == null) return;

        plotText.text = $"Plots: {GameManager.Instance.GetUnlockedPlotCount()}/{GameManager.Instance.GetMaxPlotCount()}  Ripe: {GameManager.Instance.GetRipePlotCount()}";
        RefreshSaveSlotLabel();
    }

    private void ShowSaveMessage(string message)
    {
        if (saveStatusText == null) return;

        saveStatusText.text = message;
        saveMessageTimer = 1.5f;
    }

    private void SetSaveSlot(int slot)
    {
        if (GameManager.Instance == null)
            return;

        GameManager.Instance.SetActiveSaveSlot(slot);
        RefreshSaveSlotLabel();
    }

    private void RefreshSaveSlotLabel()
    {
        if (saveSlotText == null || GameManager.Instance == null)
            return;

        int slot = GameManager.Instance.GetActiveSaveSlot();
        bool hasData = GameManager.Instance.HasSaveInSlot(slot);
        saveSlotText.text = $"Save Slot: {slot} {(hasData ? "(USED)" : "(EMPTY)")}";
    }

    private void EnsureAutoToggleReference()
    {
        if (autoToggle != null)
            return;

        if (autoText != null)
        {
            autoToggle = autoText.GetComponent<Toggle>();
            if (autoToggle == null)
                autoToggle = autoText.GetComponentInParent<Toggle>();
        }
    }

    private string Format(double num)
    {
        if (GameManager.Instance != null)
            return GameManager.Instance.FormatMoney(num);

        if (num >= 1_000_000_000d)
            return (num / 1_000_000_000d).ToString("F1") + "B";
        if (num >= 1_000_000d)
            return (num / 1_000_000d).ToString("F1") + "M";
        if (num >= 1_000d)
            return (num / 1_000d).ToString("F1") + "K";

        return num.ToString("F1");
    }
}
