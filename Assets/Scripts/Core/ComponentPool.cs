using System.Collections.Generic;
using UnityEngine;

namespace KeepCoreSafe.Core
{
    public sealed class ComponentPool<T> where T : Component
    {
        private readonly T prefab;
        private readonly Transform parent;
        private readonly Stack<T> available = new();
        private readonly HashSet<T> pooled = new();

        public ComponentPool(T prefab, int initialSize, Transform parent)
        {
            this.prefab = prefab;
            this.parent = parent;

            for (int i = 0; i < Mathf.Max(0, initialSize); i++)
                Return(Create());
        }

        public T Rent()
        {
            T instance = null;
            while (available.Count > 0 && instance == null)
                instance = available.Pop();

            if (instance == null)
                instance = Create();

            if (instance == null)
                return null;

            pooled.Remove(instance);
            instance.gameObject.SetActive(true);
            return instance;
        }

        public void Return(T instance)
        {
            if (instance == null || !pooled.Add(instance))
                return;

            instance.gameObject.SetActive(false);
            available.Push(instance);
        }

        private T Create()
        {
            if (prefab == null)
                return null;

            T instance = Object.Instantiate(prefab, parent);
            instance.name = prefab.name;
            return instance;
        }
    }
}
