using System;
using UnityEngine;

public class ProjectileTrigger : MonoBehaviour
{
    public event Action<Collider> OnHit;

    public void ClearSubscriptions()
    {
        OnHit = null;
    }

    private void OnTriggerEnter(Collider col)
    {
        OnHit?.Invoke(col);
    }
}
