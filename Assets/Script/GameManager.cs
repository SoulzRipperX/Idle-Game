using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Singleton Pattern
    public static GameManager Instance { get; private set; }

    // Observer Pattern
    public static event Action<double> OnMoneyChanged;
    public static event Action<double> OnIncomeChanged;
    public static event Action<bool> OnAutoStateChanged;

    [Header("Idle Settings")]
    [SerializeField] private float updatesPerSecond = 5f;
    [SerializeField] private StoreUpgrade[] storeUpgrades;

    [Header("Auto Settings")]
    [SerializeField] private double autoClickPower = 1;
    [SerializeField] private float autoInterval = 1f;

    [Header("Save Settings")]
    [SerializeField] private float autoSaveInterval = 10f;

    public double Money { get; private set; }
    public double IncomePerSecond { get; private set; }
    public bool AutoEnabled { get; private set; }
    private double potentialIncomePerSecond;

    private const string SaveKey = "GameSaveData_v2";

    private float idleTimer;
    private float autoTimer;
    private float saveTimer;
    private bool hasPendingSave;

    [Serializable]
    private class UpgradeSave
    {
        public string id;
        public int level;
    }

    [Serializable]
    private class SaveData
    {
        public string money;
        public int autoEnabled;
        public string autoPower;
        public List<UpgradeSave> upgrades = new List<UpgradeSave>();
    }

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

    private void Start()
    {
        LoadGame();
        NotifyObservers();
    }

    private void Update()
    {
        float delta = Time.deltaTime;
        float idleStep = 1f / Mathf.Max(0.01f, updatesPerSecond);

        idleTimer += delta;
        autoTimer += delta;
        saveTimer += delta;

        while (idleTimer >= idleStep)
        {
            ProcessIdleTick(idleStep);
            idleTimer -= idleStep;
        }

        if (AutoEnabled)
        {
            float safeInterval = Mathf.Max(0.05f, autoInterval);
            while (autoTimer >= safeInterval)
            {
                AddMoney(autoClickPower);
                autoTimer -= safeInterval;
            }
        }

        if (saveTimer >= Mathf.Max(1f, autoSaveInterval))
        {
            saveTimer = 0f;
            if (hasPendingSave)
            {
                SaveGame();
            }
        }
    }

    public void AddMoney(double amount)
    {
        if (amount <= 0d) return;

        Money += amount;
        hasPendingSave = true;
        OnMoneyChanged?.Invoke(Money);
    }

    public bool SpendMoney(double amount)
    {
        if (amount <= 0d || Money < amount)
            return false;

        Money -= amount;
        hasPendingSave = true;
        OnMoneyChanged?.Invoke(Money);
        return true;
    }

    public void ToggleAuto()
    {
        AutoEnabled = !AutoEnabled;
        hasPendingSave = true;
        RecalculateIncome();
        OnAutoStateChanged?.Invoke(AutoEnabled);
        SaveGame();
    }

    public void UpgradeAuto(double amount)
    {
        if (amount <= 0d) return;

        autoClickPower += amount;
        hasPendingSave = true;
    }

    public void HandleUpgradeStateChanged()
    {
        RecalculateIncome();
        hasPendingSave = true;
        NotifyObservers();
    }

    public void SaveGame()
    {
        SaveData data = new SaveData
        {
            money = Money.ToString("R", CultureInfo.InvariantCulture),
            autoEnabled = AutoEnabled ? 1 : 0,
            autoPower = autoClickPower.ToString("R", CultureInfo.InvariantCulture)
        };

        foreach (var upgrade in storeUpgrades)
        {
            if (upgrade == null) continue;
            data.upgrades.Add(new UpgradeSave
            {
                id = upgrade.UpgradeId,
                level = upgrade.Level
            });
        }

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
        hasPendingSave = false;
    }

    public void LoadGame()
    {
        if (PlayerPrefs.HasKey(SaveKey))
        {
            ApplySaveData(JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey)));
        }
        else
        {
            LoadLegacySave();
        }

        RecalculateIncome();
        hasPendingSave = false;
    }

    public string FormatMoney(double value)
    {
        if (value >= 1_000_000_000d)
            return (value / 1_000_000_000d).ToString("F1") + "B";
        if (value >= 1_000_000d)
            return (value / 1_000_000d).ToString("F1") + "M";
        if (value >= 1_000d)
            return (value / 1_000d).ToString("F1") + "K";

        return value.ToString("F1");
    }

    private void ProcessIdleTick(float tickDuration)
    {
        RecalculateIncome();

        if (IncomePerSecond <= 0d) return;

        Money += IncomePerSecond * tickDuration;
        hasPendingSave = true;
        OnMoneyChanged?.Invoke(Money);
    }

    private void LoadLegacySave()
    {
        if (PlayerPrefs.HasKey("Money"))
            Money = ParseDouble(PlayerPrefs.GetString("Money"), 0d);

        AutoEnabled = PlayerPrefs.GetInt("AutoEnabled", 0) == 1;

        if (PlayerPrefs.HasKey("AutoPower"))
            autoClickPower = ParseDouble(PlayerPrefs.GetString("AutoPower"), autoClickPower);

        foreach (var upgrade in storeUpgrades)
        {
            if (upgrade == null) continue;
            int level = PlayerPrefs.GetInt("Upgrade_" + upgrade.upgradeName, 0);
            upgrade.SetLevel(level);
        }
    }

    private void ApplySaveData(SaveData data)
    {
        if (data == null)
        {
            LoadLegacySave();
            return;
        }

        Money = ParseDouble(data.money, 0d);
        AutoEnabled = data.autoEnabled == 1;
        autoClickPower = ParseDouble(data.autoPower, autoClickPower);

        foreach (var upgrade in storeUpgrades)
        {
            if (upgrade == null) continue;
            upgrade.SetLevel(0);
        }

        if (data.upgrades == null) return;

        foreach (var saved in data.upgrades)
        {
            if (saved == null || string.IsNullOrWhiteSpace(saved.id)) continue;
            StoreUpgrade upgrade = FindUpgrade(saved.id);
            if (upgrade != null)
                upgrade.SetLevel(saved.level);
        }
    }

    private StoreUpgrade FindUpgrade(string id)
    {
        foreach (var upgrade in storeUpgrades)
        {
            if (upgrade == null) continue;
            if (string.Equals(upgrade.UpgradeId, id, StringComparison.Ordinal))
                return upgrade;
        }

        return null;
    }

    private void RecalculateIncome()
    {
        double sum = 0d;

        foreach (var upgrade in storeUpgrades)
        {
            if (upgrade == null) continue;
            sum += upgrade.CalculateIncomePerSecond();
        }

        potentialIncomePerSecond = sum;
        IncomePerSecond = AutoEnabled ? potentialIncomePerSecond : 0d;
        OnIncomeChanged?.Invoke(IncomePerSecond);
    }

    private void NotifyObservers()
    {
        OnMoneyChanged?.Invoke(Money);
        OnIncomeChanged?.Invoke(IncomePerSecond);
        OnAutoStateChanged?.Invoke(AutoEnabled);
    }

    private static double ParseDouble(string value, double fallback)
    {
        if (string.IsNullOrWhiteSpace(value)) return fallback;

        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
            return result;

        if (double.TryParse(value, out result))
            return result;

        return fallback;
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && hasPendingSave)
            SaveGame();
    }

    private void OnApplicationQuit()
    {
        if (hasPendingSave)
            SaveGame();
    }
}
