using Obrissom.Audio;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Testing only.
/// F5: 2D local. F6: 3D local at the sound point.
/// F7 (server): PlayForEveryone at the server's player. F8 (any client): PlayFromOwner on the local player.
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

        if (kb.f7Key.wasPressedThisFrame && TryGetLocalPlayerEmitter(out NetworkSoundEmitter serverEmitter))
            serverEmitter.PlayForEveryone(_sound);

        if (kb.f8Key.wasPressedThisFrame && TryGetLocalPlayerEmitter(out NetworkSoundEmitter ownerEmitter))
            ownerEmitter.PlayFromOwner(_sound);
    }

    private bool TryGetLocalPlayerEmitter(out NetworkSoundEmitter emitter)
    {
        emitter = null;

        NetworkManager manager = NetworkManager.Singleton;
        if (manager == null || manager.LocalClient == null || manager.LocalClient.PlayerObject == null) return false;

        emitter = manager.LocalClient.PlayerObject.GetComponent<NetworkSoundEmitter>();
        if (emitter == null) Debug.LogWarning("[AudioTester] Local player has no NetworkSoundEmitter.");
        return emitter != null;
    }
}
