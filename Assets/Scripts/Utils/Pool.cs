using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// All pools will heredate from this generic Pool class.
/// </summary>
public abstract class Pool<T, TContext> : MonoBehaviour where T : Component
{
    [SerializeField] protected GameObject prefab;
    [SerializeField] protected int initialSize = 10;

    private Queue<T> _pool = new Queue<T>();

    protected virtual void Awake()
    {
        for (int i = 0; i < initialSize; i++)
        {
            _pool.Enqueue(CreateNew());
        }
    }

    private T CreateNew()
    {
        var obj = Instantiate(prefab, transform);
        obj.SetActive(false);
        return obj.GetComponent<T>();
    }

    public T Get(Vector3 position, TContext context)
    {
        var obj = _pool.Count > 0 ? _pool.Dequeue() : CreateNew();
        InstantiateObject(obj, position, context);
        return obj;
    }

    public T Get(Vector3 position)
    {
        var obj = _pool.Count > 0 ? _pool.Dequeue() : CreateNew();
        obj.transform.position = position;
        obj.gameObject.SetActive(true);
        return obj;
    }

    public void Return(T obj)
    {
        obj.gameObject.SetActive(false);
        _pool.Enqueue(obj);
    }

    protected virtual void InstantiateObject(T obj, Vector3 position, TContext context) { }
}
