using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Serialization;

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
        public string lastSavedUtc;
        public List<UpgradeSave> upgrades = new List<UpgradeSave>();
        public List<float> gardenProgress = new List<float>();
        public List<float> GardenProgress = new List<float>();
    }

    [Header("Core")]
    [FormerlySerializedAs("Garden")]
    [FormerlySerializedAs("Gardens")]
    [SerializeField] private PlantGarden[] gardens;

    [Header("Economy")]
    [SerializeField] private double baseCoinReward = 1d;
    [SerializeField] private double baseGrowthPerClick = 0.2d;
    [SerializeField] private double coinBonusPerLevel = 0.08d;
    [SerializeField] private double clickBonusPerLevel = 0.08d;
    [SerializeField] private double growthSpeedBonusPerLevel = 0.1d;

    [Header("Auto")]
    [SerializeField] private float autoInterval = 2.2f;
    [SerializeField] private float autoGrowthMultiplier = 0.3f;

    [Header("Offline Progress")]
    [SerializeField] private bool enableOfflineProgress = true;
    [SerializeField] private float maxOfflineHours = 8f;
    [SerializeField] private float offlineGrowthMultiplier = 0.85f;

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
    private double lastOfflineEarned;
    private double lastOfflineSeconds;

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
                ApplyWaterToActiveGardens(GetAutoGrowthPerTick());
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
        return ApplyWaterToActiveGardens((float)(GetManualGrowthPerClick() * GetGrowthSpeedMultiplier()));
    }

    public bool WaterGardenByClick(PlantGarden targetGarden)
    {
        if (targetGarden == null)
            return false;

        if (!IsGardenWaterable(targetGarden))
            return false;

        float amount = (float)(GetManualGrowthPerClick() * GetGrowthSpeedMultiplier());
        bool becameRipe = targetGarden.AddGrowth(amount);
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
        int activeGardens = GetUnlockedGardenCount();

        for (int i = 0; i < gardens.Length; i++)
        {
            if (gardens[i] == null || i >= activeGardens)
                continue;

            if (gardens[i].HarvestAndReset())
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

        if (type == UpgradeType.GardenUnlock)
            ApplyGardenUnlocks();

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
            if (type == UpgradeType.GardenUnlock)
                return Mathf.Min(configuredMax, Mathf.Max(0, gardens.Length - 1));

            return configuredMax;
        }

        if (type == UpgradeType.GardenUnlock)
            return Mathf.Max(0, gardens.Length - 1);
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
                return 25d * Math.Pow(1.40d, level);
            case UpgradeType.ClickPower:
                return 20d * Math.Pow(1.45d, level);
            case UpgradeType.GrowthSpeed:
                return 35d * Math.Pow(1.50d, level);
            case UpgradeType.GardenUnlock:
                return 150d * Math.Pow(2.40d, level);
            case UpgradeType.AutoSell:
                return 100d * Math.Pow(1.70d, level);
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
            case UpgradeType.CoinValue: return "Coin Multiplier";
            case UpgradeType.ClickPower: return "Click Power";
            case UpgradeType.GrowthSpeed: return "Growth Speed";
            case UpgradeType.GardenUnlock: return "Unlock Garden";
            case UpgradeType.AutoSell: return "Auto Sell";
            default: return type.ToString();
        }
    }

    public int GetUnlockedGardenCount()
    {
        return Mathf.Clamp(1 + GetUpgradeLevel(UpgradeType.GardenUnlock), 1, Mathf.Max(1, gardens.Length));
    }

    public int GetMaxGardenCount()
    {
        return Mathf.Max(1, gardens.Length);
    }

    public int GetRipeGardenCount()
    {
        int ripe = 0;
        int activeGardens = GetUnlockedGardenCount();

        for (int i = 0; i < gardens.Length; i++)
        {
            if (gardens[i] == null || i >= activeGardens)
                continue;

            if (gardens[i].IsRipe)
                ripe++;
        }

        return ripe;
    }

    public int GetReadyGardenCount()
    {
        return GetRipeGardenCount();
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
        if (lastOfflineEarned > 0d)
            OnSaveMessage?.Invoke($"Loaded +{FormatMoney(lastOfflineEarned)} ({FormatOfflineTime(lastOfflineSeconds)} offline)");
        else
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
#if UNITY_WEBGL || UNITY_ANDROID || UNITY_IOS
        OnSaveMessage?.Invoke("Open Save Folder is only available on PC build");
        return;
#else
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
#endif
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
                File.SetAttributes(filePath, FileAttributes.Normal);
                File.Delete(filePath);
            }
        }
        catch (Exception e)
        {
            OnSaveMessage?.Invoke($"Delete failed: {e.Message}");
            return;
        }

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

    private double ApplyWaterToActiveGardens(float growthAmount)
    {
        if (growthAmount <= 0f) return 0d;

        int becameRipeCount = 0;
        int activeGardens = GetUnlockedGardenCount();

        for (int i = 0; i < gardens.Length; i++)
        {
            if (gardens[i] == null || i >= activeGardens)
                continue;

            if (gardens[i].AddGrowth(growthAmount))
                becameRipeCount++;
        }

        OnGameStateChanged?.Invoke();
        return becameRipeCount;
    }

    private bool IsGardenWaterable(PlantGarden garden)
    {
        int activeGardens = GetUnlockedGardenCount();

        for (int i = 0; i < gardens.Length; i++)
        {
            if (gardens[i] == null || i >= activeGardens)
                continue;

            if (ReferenceEquals(gardens[i], garden))
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

    private void ApplyGardenUnlocks()
    {
        int unlocked = GetUnlockedGardenCount();

        for (int i = 0; i < gardens.Length; i++)
        {
            if (gardens[i] == null) continue;
            gardens[i].SetUnlocked(i < unlocked);
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

        int activeGardens = GetUnlockedGardenCount();
        if (activeGardens <= 0)
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

        double growthPerSecondPerGarden = GetAutoGrowthPerTick() / Mathf.Max(0.3f, autoInterval);
        double harvestPerSecondPerGarden = growthPerSecondPerGarden / 3d;
        IncomePerSecond = activeGardens * harvestPerSecondPerGarden * GetCoinRewardPerHarvest();
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

        if (gardens == null)
            return;

        for (int i = 0; i < gardens.Length; i++)
        {
            if (gardens[i] == null) continue;
            gardens[i].SetUnlocked(i == 0);
            gardens[i].SetGrowthProgress(0f);
        }
    }

    private void RefreshAll()
    {
        ApplyGardenUnlocks();
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
            autoEnabled = AutoEnabled ? 1 : 0,
            lastSavedUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };

        foreach (KeyValuePair<UpgradeType, int> pair in upgradeLevels)
        {
            data.upgrades.Add(new UpgradeSave
            {
                type = pair.Key.ToString(),
                level = pair.Value
            });
        }

        for (int i = 0; i < gardens.Length; i++)
        {
            data.gardenProgress.Add(gardens[i] != null ? gardens[i].GrowthProgress : 0f);
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
        lastOfflineEarned = 0d;
        lastOfflineSeconds = 0d;
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

        ApplyGardenUnlocks();

        List<float> loadedProgress = data.gardenProgress;
        if ((loadedProgress == null || loadedProgress.Count == 0) && data.GardenProgress != null && data.GardenProgress.Count > 0)
            loadedProgress = data.GardenProgress;

        if (loadedProgress != null)
        {
            for (int i = 0; i < gardens.Length && i < loadedProgress.Count; i++)
            {
                if (gardens[i] == null) continue;
                gardens[i].SetGrowthProgress(loadedProgress[i]);
            }
        }

        ApplyOfflineProgress(data.lastSavedUtc);

        hasPendingSave = false;
    }

    private void ApplyOfflineProgress(string lastSavedUtc)
    {
        if (!enableOfflineProgress || gardens == null || gardens.Length == 0)
            return;

        if (!TryParseUtc(lastSavedUtc, out DateTime savedAt))
            return;

        double elapsedSeconds = (DateTime.UtcNow - savedAt).TotalSeconds;
        if (elapsedSeconds <= 1d)
            return;

        double cappedSeconds = Math.Min(elapsedSeconds, Math.Max(0d, maxOfflineHours * 3600d));
        double effectiveSeconds = cappedSeconds * Math.Max(0f, offlineGrowthMultiplier);
        if (effectiveSeconds <= 0d)
            return;

        int activeGardens = GetUnlockedGardenCount();
        if (activeGardens <= 0)
            return;

        bool hasAutoSell = GetUpgradeLevel(UpgradeType.AutoSell) > 0;
        bool canRunAutoCycle = AutoEnabled;
        double growthPerSecondPerGarden = GetAutoGrowthPerTick() / Math.Max(0.3f, autoInterval);
        if (growthPerSecondPerGarden <= 0d)
            return;

        int harvestCount = 0;
        for (int i = 0; i < gardens.Length; i++)
        {
            if (i >= activeGardens || gardens[i] == null)
                continue;

            float progress = gardens[i].GrowthProgress;

            if (!canRunAutoCycle)
            {
                float nextProgress = Mathf.Min(3f, progress + (float)effectiveSeconds * (float)growthPerSecondPerGarden);
                gardens[i].SetGrowthProgress(nextProgress);
                continue;
            }

            if (!hasAutoSell)
            {
                float nextProgress = Mathf.Min(3f, progress + (float)effectiveSeconds * (float)growthPerSecondPerGarden);
                gardens[i].SetGrowthProgress(nextProgress);
                continue;
            }

            double totalProgress = progress + (effectiveSeconds * growthPerSecondPerGarden);
            int harvested = Mathf.FloorToInt((float)(totalProgress / 3d));
            float remain = (float)(totalProgress - (harvested * 3d));

            if (harvested > 0)
                harvestCount += harvested;

            gardens[i].SetGrowthProgress(remain);
        }

        if (harvestCount > 0)
        {
            double earned = harvestCount * GetCoinRewardPerHarvest();
            if (earned > 0d)
            {
                Money += earned;
                lastOfflineEarned = earned;
                hasPendingSave = true;
            }
        }

        lastOfflineSeconds = effectiveSeconds;
    }

    private static bool TryParseUtc(string input, out DateTime value)
    {
        return DateTime.TryParse(
            input,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
            out value);
    }

    private static string FormatOfflineTime(double seconds)
    {
        TimeSpan t = TimeSpan.FromSeconds(Math.Max(0d, seconds));
        if (t.TotalHours >= 1d)
            return $"{(int)t.TotalHours}h {t.Minutes}m";
        if (t.TotalMinutes >= 1d)
            return $"{t.Minutes}m {t.Seconds}s";
        return $"{Mathf.Max(0, (int)Math.Round(t.TotalSeconds))}s";
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
        if (pause)
            SaveGame();
    }

    private void OnApplicationQuit()
    {
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
