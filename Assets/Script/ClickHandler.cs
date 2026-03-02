using UnityEngine;

public class ClickHandler : MonoBehaviour
{
    [SerializeField] private FloatingTextPool pool;

    public void Click()
    {
        GameManager.Instance.AddMoney(1);

        if (pool != null)
            pool.Spawn("+1");
    }
}