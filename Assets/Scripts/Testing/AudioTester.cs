using Obrissom.Audio;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Testing only. F5: play the configured sound in 2D.
/// </summary>
public class AudioTester : MonoBehaviour
{
    [SerializeField] private AudioID _sound = AudioID.SlasherAttack;

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.f5Key.wasPressedThisFrame)
            AudioManager.Instance.PlaySound(_sound);
    }
}
