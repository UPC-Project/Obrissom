using UnityEngine;

public class GameStateController : MonoBehaviour
{
    public void SetState(GameState state)
    {
        var input = InputManager.Instance.PlayerInput;

        // primero desactivás todo
        input.PlayerLocomotionMap.Disable();
        input.UIMap.Disable();
        input.CombatMap.Disable();

        // activás lo necesario
        switch (state)
        {
            case GameState.Gameplay:
                input.PlayerLocomotionMap.Enable();
                break;

            case GameState.UI:
                input.UIMap.Enable();
                break;

            case GameState.Paused:
                input.CombatMap.Enable();
                break;
        }
    }
}