using UnityEngine;

public class ClickHandler : MonoBehaviour
{
    [SerializeField] private FloatingTextPool pool;
    [SerializeField] private int clickPower = 1;

    public void Click()
    {
        GameManager.Instance.AddMoney(clickPower);

        if (pool != null)
        {
            pool.Spawn("+" + clickPower);
        }
    }
}