using Obrissom.Audio;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Testing only. F5: play the configured sound in 2D. F6: play it in 3D at the sound point.
/// </summary>
public class AudioTester : MonoBehaviour
{
    [SerializeField] private AudioID _sound = AudioID.SlasherAttack;

    [Tooltip("Where the 3D sound plays. Uses this object's position if empty.")]
    [SerializeField] private Transform _soundPoint;

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.f5Key.wasPressedThisFrame)
            AudioManager.Instance.PlaySound(_sound);

        if (kb.f6Key.wasPressedThisFrame)
        {
            Vector3 position = _soundPoint != null ? _soundPoint.position : transform.position;
            AudioManager.Instance.PlaySound(_sound, position);
        }
    }
}
