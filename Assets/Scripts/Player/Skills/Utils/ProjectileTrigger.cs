using System;
using UnityEngine;

public class ProjectileTrigger : MonoBehaviour
{
    public event Action<Collider> OnHit;
    private bool _hasHit = false;

    private void OnEnable()
    {
        _hasHit = false;
    }

    public void ClearSubscriptions()
    {
        OnHit = null;
    }

    private void OnTriggerEnter(Collider col)
    {
        if(col.CompareTag("Enemy") && !_hasHit)
        {
            OnHit?.Invoke(col);
            _hasHit = true;
        }
    }
}
