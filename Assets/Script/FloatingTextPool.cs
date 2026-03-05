using System.Collections.Generic;
using UnityEngine;

public class FloatingTextPool : MonoBehaviour
{
    // Singleton Pattern for pooled floating-text access
    public static FloatingTextPool Instance { get; private set; }

    [SerializeField] private FloatingText prefab;
    [SerializeField] private int poolSize = 10;

    private readonly Queue<FloatingText> available = new Queue<FloatingText>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        Prewarm(Mathf.Max(1, poolSize));
    }

    public void Spawn(string message)
    {
        FloatingText obj = GetFromPool();

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.anchoredPosition = Vector2.zero;

        obj.gameObject.SetActive(true);
        obj.Setup(message);
    }

    public void Return(FloatingText obj)
    {
        if (obj == null) return;

        obj.gameObject.SetActive(false);
        obj.transform.SetParent(transform, false);
        available.Enqueue(obj);
    }

    private void Prewarm(int count)
    {
        for (int i = 0; i < count; i++)
        {
            FloatingText obj = CreateNew();
            available.Enqueue(obj);
        }
    }

    private FloatingText GetFromPool()
    {
        if (available.Count > 0)
            return available.Dequeue();

        return CreateNew();
    }

    private FloatingText CreateNew()
    {
        FloatingText obj = Instantiate(prefab, transform);
        obj.SetOwner(this);
        obj.gameObject.SetActive(false);
        return obj;
    }
}
