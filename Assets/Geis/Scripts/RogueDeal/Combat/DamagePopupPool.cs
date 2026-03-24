using System.Collections.Generic;
using UnityEngine;

namespace RogueDeal.Combat
{
    public class DamagePopupPool
    {
        private readonly GameObject prefab;
        private readonly Transform parent;
        private readonly Queue<GameObject> pool = new Queue<GameObject>();
        private readonly int maxSize;

        public DamagePopupPool(GameObject prefab, Transform parent, int initialSize = 20, int maxSize = 100)
        {
            this.prefab = prefab;
            this.parent = parent;
            this.maxSize = maxSize;

            for (int i = 0; i < initialSize; i++)
            {
                CreateNewObject();
            }
        }

        private GameObject CreateNewObject()
        {
            GameObject obj = Object.Instantiate(prefab, parent);
            obj.SetActive(false);
            obj.name = $"Pooled_{prefab.name}";
            return obj;
        }

        public GameObject Get()
        {
            GameObject obj;

            if (pool.Count > 0)
            {
                obj = pool.Dequeue();
            }
            else
            {
                obj = CreateNewObject();
            }

            obj.SetActive(true);
            obj.name = $"Active_{prefab.name}";
            return obj;
        }

        public void Return(GameObject obj)
        {
            if (obj == null) return;

            obj.SetActive(false);
            obj.name = $"Pooled_{prefab.name}";
            obj.transform.SetParent(parent);

            if (pool.Count < maxSize)
            {
                pool.Enqueue(obj);
            }
            else
            {
                Object.Destroy(obj);
            }
        }
    }
}