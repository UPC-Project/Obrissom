using UnityEngine;

public interface IInteractable
{
    Vector3 Position { get; }
    void SetInteractPromptActive(bool active);
}

