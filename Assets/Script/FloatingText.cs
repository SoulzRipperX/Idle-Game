using TMPro;
using UnityEngine;

public class FloatingText : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 120f;
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private float randomXSpeed = 80f;

    private TextMeshProUGUI text;
    private RectTransform rectTransform;
    private float timer;
    private float xSpeed;

    private void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
        rectTransform = GetComponent<RectTransform>();
    }

    public void Setup(string message)
    {
        text.text = message;
        timer = lifeTime;

        rectTransform.localScale = Vector3.one;

        xSpeed = Random.Range(-randomXSpeed, randomXSpeed);
    }

    private void Update()
    {
        rectTransform.anchoredPosition += new Vector2(xSpeed, moveSpeed) * Time.deltaTime;

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            gameObject.SetActive(false);
        }
    }
}