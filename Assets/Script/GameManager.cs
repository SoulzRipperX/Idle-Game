using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Diagnostics;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public static event Action<double> OnMoneyChanged;
    public static event Action<double> OnIncomeChanged;
    public static event Action<bool> OnAutoStateChanged;
    public static event Action OnGameStateChanged;
    public static event Action<string> OnSaveMessage;

    [Serializable]
    private class UpgradeBalance
    {
        public UpgradeType type;
        public string displayName;
        public double basePrice = 10;
        public double priceMultiplier = 1.4;
        public int maxLevel = 20;
    }

    [Serializable]
    private class UpgradeSave
    {
        public string type;
        public int level;
    }

    [Serializable]
    private class SaveData
    {
        public string money;
        public int autoEnabled;
        public List<UpgradeSave> upgrades = new List<UpgradeSave>();
        public List<float> plotProgress = new List<float>();
    }

    [Header("Core")]
    [SerializeField] private PlantPlot[] plots;

    [Header("Economy")]
    [SerializeField] private double baseCoinReward = 1d;
    [SerializeField] private double baseGrowthPerClick = 0.35d;
    [SerializeField] private double coinBonusPerLevel = 0.1d;
    [SerializeField] private double clickBonusPerLevel = 0.25d;
    [SerializeField] private double growthSpeedBonusPerLevel = 0.15d;

    [Header("Auto")]
    [SerializeField] private float autoInterval = 1.8f;
    [SerializeField] private float autoGrowthMultiplier = 0.4f;

    [Header("Save")]
    [SerializeField] private float autoSaveInterval = 10f;

    [Header("Upgrade Config")]
    [SerializeField] private UpgradeBalance[] upgradeBalances;

    public double Money { get; private set; }
    public bool AutoEnabled { get; private set; }
    public double IncomePerSecond { get; private set; }

    private readonly Dictionary<UpgradeType, int> upgradeLevels = new Dictionary<UpgradeType, int>();

    private const string SaveKeyPrefix = "FarmIdleSave_v1_slot_";
    private const string ActiveSlotKey = "FarmIdleSave_ActiveSlot";
    private const int SlotCount = 3;
    private const string SaveFolderName = "Saves";
    private float autoTimer;
    private float autoSellTimer;
    private float saveTimer;
    private bool hasPendingSave;
    private int activeSaveSlot = 1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        activeSaveSlot = Mathf.Clamp(PlayerPrefs.GetInt(ActiveSlotKey, 1), 1, SlotCount);
        EnsureUpgradeDictionary();
    }

    private void Start()
    {
        LoadGame();
        RefreshAll();
    }

    private void Update()
    {
        float delta = Time.deltaTime;
        autoTimer += delta;
        saveTimer += delta;

        if (AutoEnabled)
        {
            float safeInterval = Mathf.Max(0.3f, autoInterval);
            while (autoTimer >= safeInterval)
            {
                ApplyWaterToActivePlots(GetAutoGrowthPerTick());
                autoTimer -= safeInterval;
            }

            if (GetUpgradeLevel(UpgradeType.AutoSell) > 0)
            {
                float sellInterval = GetAutoSellInterval();
                autoSellTimer += delta;

                while (autoSellTimer >= sellInterval)
                {
                    SellAllRipeInternal();
                    autoSellTimer -= sellInterval;
                }
            }
            else
            {
                autoSellTimer = 0f;
            }
        }

        if (saveTimer >= Mathf.Max(1f, autoSaveInterval))
        {
            saveTimer = 0f;
            if (hasPendingSave)
                SaveGame();
        }
    }

    public void ToggleAuto()
    {
        SetAutoEnabled(!AutoEnabled);
    }

    public void SetAutoEnabled(bool enabled)
    {
        if (AutoEnabled == enabled)
            return;

        AutoEnabled = enabled;
        hasPendingSave = true;
        RecalculateIncome();
        OnAutoStateChanged?.Invoke(AutoEnabled);
        OnGameStateChanged?.Invoke();
    }

    public double WaterByClick()
    {
        return ApplyWaterToActivePlots((float)(GetManualGrowthPerClick() * GetGrowthSpeedMultiplier()));
    }

    public bool WaterPlotByClick(PlantPlot targetPlot)
    {
        if (targetPlot == null)
            return false;

        if (!IsPlotWaterable(targetPlot))
            return false;

        float amount = (float)(GetManualGrowthPerClick() * GetGrowthSpeedMultiplier());
        bool becameRipe = targetPlot.AddGrowth(amount);
        OnGameStateChanged?.Invoke();
        return becameRipe;
    }

    public double SellAllRipe()
    {
        return SellAllRipeInternal();
    }

    private double SellAllRipeInternal()
    {
        int ripeCount = 0;
        int activePlots = GetUnlockedPlotCount();

        for (int i = 0; i < plots.Length; i++)
        {
            if (plots[i] == null || i >= activePlots)
                continue;

            if (plots[i].HarvestAndReset())
                ripeCount++;
        }

        if (ripeCount <= 0)
        {
            OnGameStateChanged?.Invoke();
            return 0d;
        }

        double earned = ripeCount * GetCoinRewardPerHarvest();
        AddMoney(earned);
        OnGameStateChanged?.Invoke();
        return earned;
    }

    public bool PurchaseUpgrade(UpgradeType type)
    {
        int level = GetUpgradeLevel(type);
        int maxLevel = GetUpgradeMaxLevel(type);

        if (level >= maxLevel)
            return false;

        double price = GetUpgradePrice(type);
        if (!SpendMoney(price))
            return false;

        upgradeLevels[type] = level + 1;

        if (type == UpgradeType.PlotUnlock)
            ApplyPlotUnlocks();

        hasPendingSave = true;
        RecalculateIncome();
        OnGameStateChanged?.Invoke();
        return true;
    }

    public int GetUpgradeLevel(UpgradeType type)
    {
        return upgradeLevels.TryGetValue(type, out int level) ? level : 0;
    }

    public int GetUpgradeMaxLevel(UpgradeType type)
    {
        UpgradeBalance cfg = GetUpgradeConfig(type);
        if (cfg != null)
        {
            int configuredMax = Mathf.Max(1, cfg.maxLevel);
            if (type == UpgradeType.PlotUnlock)
                return Mathf.Min(configuredMax, Mathf.Max(0, plots.Length - 1));

            return configuredMax;
        }

        if (type == UpgradeType.PlotUnlock)
            return Mathf.Max(0, plots.Length - 1);
        if (type == UpgradeType.AutoSell)
            return 10;

        return 20;
    }

    public bool IsUpgradeMaxed(UpgradeType type)
    {
        return GetUpgradeLevel(type) >= GetUpgradeMaxLevel(type);
    }

    public double GetUpgradePrice(UpgradeType type)
    {
        UpgradeBalance cfg = GetUpgradeConfig(type);
        int level = GetUpgradeLevel(type);
        if (cfg != null)
            return cfg.basePrice * Math.Pow(cfg.priceMultiplier, level);

        switch (type)
        {
            case UpgradeType.CoinValue:
                return 20d * Math.Pow(1.35d, level);
            case UpgradeType.ClickPower:
                return 15d * Math.Pow(1.30d, level);
            case UpgradeType.GrowthSpeed:
                return 30d * Math.Pow(1.40d, level);
            case UpgradeType.PlotUnlock:
                return 120d * Math.Pow(2.20d, level);
            case UpgradeType.AutoSell:
                return 75d * Math.Pow(1.55d, level);
            default:
                return 999999d;
        }
    }

    public string GetUpgradeName(UpgradeType type)
    {
        UpgradeBalance cfg = GetUpgradeConfig(type);
        if (cfg != null && !string.IsNullOrWhiteSpace(cfg.displayName))
            return cfg.displayName;

        switch (type)
        {
            case UpgradeType.CoinValue: return "Coin Value";
            case UpgradeType.ClickPower: return "Click Power";
            case UpgradeType.GrowthSpeed: return "Growth Speed";
            case UpgradeType.PlotUnlock: return "Unlock Plot";
            case UpgradeType.AutoSell: return "Auto Sell";
            default: return type.ToString();
        }
    }

    public int GetUnlockedPlotCount()
    {
        return Mathf.Clamp(1 + GetUpgradeLevel(UpgradeType.PlotUnlock), 1, Mathf.Max(1, plots.Length));
    }

    public int GetMaxPlotCount()
    {
        return Mathf.Max(1, plots.Length);
    }

    public int GetRipePlotCount()
    {
        int ripe = 0;
        int activePlots = GetUnlockedPlotCount();

        for (int i = 0; i < plots.Length; i++)
        {
            if (plots[i] == null || i >= activePlots)
                continue;

            if (plots[i].IsRipe)
                ripe++;
        }

        return ripe;
    }

    public double GetCoinRewardPerHarvest()
    {
        return baseCoinReward * (1d + coinBonusPerLevel * GetUpgradeLevel(UpgradeType.CoinValue));
    }

    public float GetAutoSellInterval()
    {
        int level = GetUpgradeLevel(UpgradeType.AutoSell);
        if (level <= 0)
            return 999f;

        return Mathf.Max(0.35f, 2.2f - (0.2f * level));
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

    public void ManualSave()
    {
        SaveGame();
        OnSaveMessage?.Invoke($"Saved (Slot {activeSaveSlot})");
    }

    public void ManualLoad()
    {
        LoadGame();
        autoTimer = 0f;
        autoSellTimer = 0f;
        RefreshAll();
        OnSaveMessage?.Invoke($"Loaded (Slot {activeSaveSlot})");
    }

    public int GetActiveSaveSlot()
    {
        return activeSaveSlot;
    }

    public int GetSaveSlotCount()
    {
        return SlotCount;
    }

    public string GetSaveFolderPath()
    {
        return GetSaveDirectoryPath();
    }

    public bool HasSaveInSlot(int slot)
    {
        int safeSlot = Mathf.Clamp(slot, 1, SlotCount);
        return File.Exists(GetSaveFilePath(safeSlot));
    }

    public void SetActiveSaveSlot(int slot)
    {
        int safeSlot = Mathf.Clamp(slot, 1, SlotCount);
        if (safeSlot == activeSaveSlot)
            return;

        activeSaveSlot = safeSlot;
        PlayerPrefs.SetInt(ActiveSlotKey, activeSaveSlot);
        PlayerPrefs.Save();
        OnGameStateChanged?.Invoke();
        OnSaveMessage?.Invoke($"Active Slot: {activeSaveSlot}");
    }

    public void OpenSaveFolder()
    {
        string folder = GetSaveDirectoryPath();
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        try
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            Process.Start("explorer.exe", folder.Replace("/", "\\"));
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            Process.Start("open", "\"" + folder + "\"");
#elif UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
            Process.Start("xdg-open", "\"" + folder + "\"");
#else
            Application.OpenURL("file://" + folder);
#endif
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning("Cannot open save folder: " + e.Message);
        }
    }

    public void DeleteActiveSaveSlot()
    {
        string filePath = GetSaveFilePath(activeSaveSlot);
        if (!File.Exists(filePath))
        {
            OnSaveMessage?.Invoke($"Slot {activeSaveSlot} is empty");
            return;
        }

        try
        {
            File.Delete(filePath);
            if (File.Exists(filePath))
            {
                // Last-resort: clear attributes and retry delete.
                File.SetAttributes(filePath, FileAttributes.Normal);
                File.Delete(filePath);
            }
        }
        catch (Exception e)
        {
            OnSaveMessage?.Invoke($"Delete failed: {e.Message}");
            return;
        }

        // Cleanup legacy key if an old PlayerPrefs save exists.
        PlayerPrefs.DeleteKey(GetSaveKey(activeSaveSlot));
        PlayerPrefs.Save();

        Money = 0d;
        AutoEnabled = false;
        autoTimer = 0f;
        autoSellTimer = 0f;
        saveTimer = 0f;
        hasPendingSave = false;
        EnsureUpgradeDictionary();
        RefreshAll();

        OnSaveMessage?.Invoke($"Deleted Slot {activeSaveSlot}");
    }

    private double ApplyWaterToActivePlots(float growthAmount)
    {
        if (growthAmount <= 0f) return 0d;

        int becameRipeCount = 0;
        int activePlots = GetUnlockedPlotCount();

        for (int i = 0; i < plots.Length; i++)
        {
            if (plots[i] == null || i >= activePlots)
                continue;

            if (plots[i].AddGrowth(growthAmount))
                becameRipeCount++;
        }

        OnGameStateChanged?.Invoke();
        return becameRipeCount;
    }

    private bool IsPlotWaterable(PlantPlot plot)
    {
        int activePlots = GetUnlockedPlotCount();

        for (int i = 0; i < plots.Length; i++)
        {
            if (plots[i] == null || i >= activePlots)
                continue;

            if (ReferenceEquals(plots[i], plot))
                return true;
        }

        return false;
    }

    private double GetManualGrowthPerClick()
    {
        return Math.Max(0.01d, baseGrowthPerClick + clickBonusPerLevel * GetUpgradeLevel(UpgradeType.ClickPower));
    }

    private float GetGrowthSpeedMultiplier()
    {
        return (float)(1d + growthSpeedBonusPerLevel * GetUpgradeLevel(UpgradeType.GrowthSpeed));
    }

    private float GetAutoGrowthPerTick()
    {
        return (float)(GetManualGrowthPerClick() * autoGrowthMultiplier * GetGrowthSpeedMultiplier());
    }

    private void AddMoney(double amount)
    {
        if (amount <= 0d) return;
        Money += amount;
        hasPendingSave = true;
        OnMoneyChanged?.Invoke(Money);
        RecalculateIncome();
    }

    private bool SpendMoney(double amount)
    {
        if (amount <= 0d || Money < amount)
            return false;

        Money -= amount;
        hasPendingSave = true;
        OnMoneyChanged?.Invoke(Money);
        return true;
    }

    private void ApplyPlotUnlocks()
    {
        int unlocked = GetUnlockedPlotCount();

        for (int i = 0; i < plots.Length; i++)
        {
            if (plots[i] == null) continue;
            plots[i].SetUnlocked(i < unlocked);
        }
    }

    private void RecalculateIncome()
    {
        if (!AutoEnabled)
        {
            IncomePerSecond = 0d;
            OnIncomeChanged?.Invoke(IncomePerSecond);
            return;
        }

        int activePlots = GetUnlockedPlotCount();
        if (activePlots <= 0)
        {
            IncomePerSecond = 0d;
            OnIncomeChanged?.Invoke(IncomePerSecond);
            return;
        }

        if (GetUpgradeLevel(UpgradeType.AutoSell) <= 0)
        {
            IncomePerSecond = 0d;
            OnIncomeChanged?.Invoke(IncomePerSecond);
            return;
        }

        double growthPerSecondPerPlot = GetAutoGrowthPerTick() / Mathf.Max(0.3f, autoInterval);
        double harvestPerSecondPerPlot = growthPerSecondPerPlot / 3d;
        IncomePerSecond = activePlots * harvestPerSecondPerPlot * GetCoinRewardPerHarvest();
        OnIncomeChanged?.Invoke(IncomePerSecond);
    }

    private UpgradeBalance GetUpgradeConfig(UpgradeType type)
    {
        foreach (UpgradeBalance cfg in upgradeBalances)
        {
            if (cfg != null && cfg.type == type)
                return cfg;
        }

        return null;
    }

    private void EnsureUpgradeDictionary()
    {
        foreach (UpgradeType type in Enum.GetValues(typeof(UpgradeType)))
        {
            if (!upgradeLevels.ContainsKey(type))
                upgradeLevels[type] = 0;
        }

        for (int i = 0; i < plots.Length; i++)
        {
            if (plots[i] == null) continue;
            plots[i].SetUnlocked(i == 0);
            plots[i].SetGrowthProgress(0f);
        }
    }

    private void RefreshAll()
    {
        ApplyPlotUnlocks();
        RecalculateIncome();
        OnMoneyChanged?.Invoke(Money);
        OnAutoStateChanged?.Invoke(AutoEnabled);
        OnGameStateChanged?.Invoke();
    }

    private void SaveGame()
    {
        SaveData data = new SaveData
        {
            money = Money.ToString("R", CultureInfo.InvariantCulture),
            autoEnabled = AutoEnabled ? 1 : 0
        };

        foreach (KeyValuePair<UpgradeType, int> pair in upgradeLevels)
        {
            data.upgrades.Add(new UpgradeSave
            {
                type = pair.Key.ToString(),
                level = pair.Value
            });
        }

        for (int i = 0; i < plots.Length; i++)
        {
            data.plotProgress.Add(plots[i] != null ? plots[i].GrowthProgress : 0f);
        }

        string folder = GetSaveDirectoryPath();
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(GetSaveFilePath(activeSaveSlot), json);
        hasPendingSave = false;
    }

    private void LoadGame()
    {
        EnsureUpgradeDictionary();

        string filePath = GetSaveFilePath(activeSaveSlot);
        if (!File.Exists(filePath))
        {
            Money = 0d;
            AutoEnabled = false;
            return;
        }

        string json = File.ReadAllText(filePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        if (data == null)
            return;

        Money = ParseDouble(data.money, 0d);
        AutoEnabled = data.autoEnabled == 1;

        foreach (UpgradeType type in Enum.GetValues(typeof(UpgradeType)))
            upgradeLevels[type] = 0;

        if (data.upgrades != null)
        {
            foreach (UpgradeSave item in data.upgrades)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.type))
                    continue;

                if (Enum.TryParse(item.type, out UpgradeType type))
                {
                    int max = GetUpgradeMaxLevel(type);
                    upgradeLevels[type] = Mathf.Clamp(item.level, 0, max);
                }
            }
        }

        ApplyPlotUnlocks();

        if (data.plotProgress != null)
        {
            for (int i = 0; i < plots.Length && i < data.plotProgress.Count; i++)
            {
                if (plots[i] == null) continue;
                plots[i].SetGrowthProgress(data.plotProgress[i]);
            }
        }

        hasPendingSave = false;
    }

    private static double ParseDouble(string value, double fallback)
    {
        if (string.IsNullOrWhiteSpace(value)) return fallback;

        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
            return parsed;

        if (double.TryParse(value, out parsed))
            return parsed;

        return fallback;
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause && hasPendingSave)
            SaveGame();
    }

    private void OnApplicationQuit()
    {
        if (hasPendingSave)
            SaveGame();
    }

    private static string GetSaveKey(int slot)
    {
        return SaveKeyPrefix + slot;
    }

    private static string GetSaveDirectoryPath()
    {
        return Path.Combine(Application.persistentDataPath, SaveFolderName);
    }

    private static string GetSaveFilePath(int slot)
    {
        return Path.Combine(GetSaveDirectoryPath(), GetSaveKey(slot) + ".json");
    }
}
