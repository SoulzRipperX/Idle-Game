using UnityEngine;

public class ClickHandler : MonoBehaviour
{
    [SerializeField] private FloatingTextPool pool;
    [SerializeField] private double clickPower = 1;

    public void Click()
    {
        if (GameManager.Instance == null)
            return;

        GameManager.Instance.AddMoney(clickPower);

        FloatingTextPool targetPool = pool != null ? pool : FloatingTextPool.Instance;
        if (targetPool != null)
        {
            targetPool.Spawn("+" + clickPower.ToString("F0"));
        }
    }
}
