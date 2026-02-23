using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

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
    }

    // ---------------- Observer ----------------
    public static event Action OnMoneyChanged;
    public static event Action OnIncomeChanged;

    [SerializeField] private float updatesPerSecond = 5f;
    [SerializeField] private StoreUpgrade[] storeUpgrades;

    public double Money { get; private set; }
    public double IncomePerSecond { get; private set; }

    private float timer;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= 1f / updatesPerSecond)
        {
            CalculateIdle();
            timer = 0f;
        }
    }

    public void AddMoney(double amount)
    {
        Money += amount;
        OnMoneyChanged?.Invoke();
    }

    public bool SpendMoney(double amount)
    {
        if (Money >= amount)
        {
            Money -= amount;
            OnMoneyChanged?.Invoke();
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

        OnIncomeChanged?.Invoke();
        OnMoneyChanged?.Invoke();
    }
}
