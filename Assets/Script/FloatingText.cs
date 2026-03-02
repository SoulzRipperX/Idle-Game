using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;
    private float lifeTime = 1f;

    public void Show(string message)
    {
        text.text = message;
        gameObject.SetActive(true);
        Invoke(nameof(Hide), lifeTime);
    }

    void Hide()
    {
        gameObject.SetActive(false);
    }
}