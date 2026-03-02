using UnityEngine;
using System.Collections.Generic;

public class FloatingTextPool : MonoBehaviour
{
    [SerializeField] private FloatingText prefab;
    [SerializeField] private int poolSize = 10;

    private List<FloatingText> pool = new List<FloatingText>();

    void Start()
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
        foreach (var obj in pool)
        {
            if (!obj.gameObject.activeInHierarchy)
            {
                obj.Show(message);
                return;
            }
        }
    }
}