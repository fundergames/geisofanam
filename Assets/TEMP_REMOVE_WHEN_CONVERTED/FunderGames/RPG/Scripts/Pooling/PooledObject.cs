using UnityEngine;

[DisallowMultipleComponent]
public class PooledObject : MonoBehaviour
{
    private GameObjectPool pool;

    public void SetPool(GameObjectPool objPool)
    {
        pool = objPool;
    }

    public void Return()
    {
        pool?.Release(this);
    }
}