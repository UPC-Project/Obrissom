using UnityEngine;

public class RotateEffect : MonoBehaviour
{
    [SerializeField] private float _speed = 30f;

    private void Update()
    {
        transform.Rotate(0f, 0f, _speed * Time.deltaTime);
    }
}