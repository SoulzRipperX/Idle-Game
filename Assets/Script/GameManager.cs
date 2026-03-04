using UnityEngine;
using TMPro;
using System;

public class GameManager : MonoBehaviour
{
    // ---------------- Singleton ----------------
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ---------------- Observer ----------------
    public static event Action<double> OnMoneyChanged;
    public static event Action<double> OnIncomeChanged;
    public static event Action<bool> OnAutoStateChanged;

    [Header("Idle Settings")]
    [SerializeField] private float updatesPerSecond = 5f;
    [SerializeField] private StoreUpgrade[] storeUpgrades;

    [Header("Auto Settings")]
    [SerializeField] private double autoClickPower = 1;
    [SerializeField] private float autoInterval = 1f;

    public double Money { get; private set; }
    public double IncomePerSecond { get; private set; }

    public bool AutoEnabled { get; private set; }

    private float timer;
    private float autoTimer;

    void Start()
    {
        LoadGame();
        OnMoneyChanged?.Invoke(Money);
    }

    void Update()
    {
        timer += Time.deltaTime;
        autoTimer += Time.deltaTime;

        if (timer >= 1f / updatesPerSecond)
        {
            CalculateIdle();
            timer = 0f;
        }

        if (AutoEnabled && autoTimer >= autoInterval)
        {
            AddMoney(autoClickPower);
            autoTimer = 0f;
        }
    }


    public void AddMoney(double amount)
    {
        Money += amount;
        OnMoneyChanged?.Invoke(Money);
    }

    public bool SpendMoney(double amount)
    {
        if (Money >= amount)
        {
            Money -= amount;
            OnMoneyChanged?.Invoke(Money);
            return true;
        }
        return false;
    }

    void CalculateIdle()
    {
        double sum = 0;

        foreach (var upgrade in storeUpgrades)
            sum += upgrade.CalculateIncomePerSecond();

        IncomePerSecond = sum;

        Money += sum / updatesPerSecond;

        OnIncomeChanged?.Invoke(IncomePerSecond);
        OnMoneyChanged?.Invoke(Money);
    }


    public void ToggleAuto()
    {
        AutoEnabled = !AutoEnabled;
        OnAutoStateChanged?.Invoke(AutoEnabled);
        SaveGame();
    }

    public void UpgradeAuto(double amount)
    {
        autoClickPower += amount;
        SaveGame();
    }


    public void SaveGame()
    {
        PlayerPrefs.SetString("Money", Money.ToString());
        PlayerPrefs.SetInt("AutoEnabled", AutoEnabled ? 1 : 0);
        PlayerPrefs.SetString("AutoPower", autoClickPower.ToString());

        foreach (var upgrade in storeUpgrades)
        {
            PlayerPrefs.SetInt("Upgrade_" + upgrade.upgradeName, upgrade.level);
        }

        PlayerPrefs.Save();
    }

    public void LoadGame()
    {
        if (PlayerPrefs.HasKey("Money"))
            Money = double.Parse(PlayerPrefs.GetString("Money"));

        AutoEnabled = PlayerPrefs.GetInt("AutoEnabled", 0) == 1;

        if (PlayerPrefs.HasKey("AutoPower"))
            autoClickPower = double.Parse(PlayerPrefs.GetString("AutoPower"));

        foreach (var upgrade in storeUpgrades)
        {
            upgrade.level = PlayerPrefs.GetInt("Upgrade_" + upgrade.upgradeName, 0);
        }

        OnAutoStateChanged?.Invoke(AutoEnabled);
    }
    public string FormatMoney(float value)
    {
        if (value >= 1000000000)
            return (value / 1000000000f).ToString("F1") + "B";
        if (value >= 1000000)
            return (value / 1000000f).ToString("F1") + "M";
        if (value >= 1000)
            return (value / 1000f).ToString("F1") + "K";

        return value.ToString("F1");
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }
}