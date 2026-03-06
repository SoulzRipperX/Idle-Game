using UnityEngine;
using System.Collections;

public class FontPopIn : MonoBehaviour
{
    public float popDuration = 0.4f;
    public float loopScale = 1.1f;
    public float loopSpeed = 2f;

    private Vector3 originalScale;
    private Coroutine loopRoutine;

    void Awake()
    {
        originalScale = transform.localScale;
    }

    void OnEnable()
    {
        StopAllCoroutines();
        transform.localScale = Vector3.zero;
        StartCoroutine(PopIn());
    }

    IEnumerator PopIn()
    {
        float time = 0;

        while (time < popDuration)
        {
            time += Time.deltaTime;
            float t = time / popDuration;

            float scale = Mathf.LerpUnclamped(0, 1, EaseOutBack(t));
            transform.localScale = originalScale * scale;

            yield return null;
        }

        transform.localScale = originalScale;
        loopRoutine = StartCoroutine(PulseLoop());
    }

    IEnumerator PulseLoop()
    {
        while (true)
        {
            float scale = 1 + Mathf.Sin(Time.time * loopSpeed) * (loopScale - 1);
            transform.localScale = originalScale * scale;
            yield return null;
        }
    }

    float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);
    }

    void OnDisable()
    {
        StopAllCoroutines();
        transform.localScale = originalScale;
    }
}
