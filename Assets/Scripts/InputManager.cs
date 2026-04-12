using UnityEngine;
using Obrissom.Player;
public class InputManager : MonoBehaviour
{
    public static InputManager Instance;

    public PlayerInput PlayerInput { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void OnEnable()
    {
        PlayerInput.Enable();
    }

    private void OnDisable()
    {
        PlayerInput.Disable();
    }
}
