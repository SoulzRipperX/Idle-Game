using System.Collections.Generic;
using UnityEngine;

public class FloatingTextPool : MonoBehaviour
{
    [SerializeField] private FloatingText prefab;
    [SerializeField] private int poolSize = 10;

    private List<FloatingText> pool = new List<FloatingText>();

    private void Start()
    {
        for (int i = 0; i < poolSize; i++)
        {
            FloatingText obj = Instantiate(prefab, transform);
            obj.gameObject.SetActive(false);
            pool.Add(obj);
        }
    }

    public void Spawn(string message)
    {
        FloatingText obj = GetFromPool();

        obj.transform.SetParent(transform, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchoredPosition = Vector2.zero;

        obj.gameObject.SetActive(true);
        obj.Setup(message);
    }

    private FloatingText GetFromPool()
    {
        foreach (FloatingText obj in pool)
        {
            if (!obj.gameObject.activeInHierarchy)
                return obj;
        }

        FloatingText newObj = Instantiate(prefab, transform);
        newObj.gameObject.SetActive(false);
        pool.Add(newObj);
        return newObj;
    }
}