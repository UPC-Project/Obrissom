using UnityEngine;

namespace Obrissom.Player
{
    public class PlayerState : MonoBehaviour
    {
        // enum state machine
        //public static PlayerState Instance { get; private set; }
        //private void Awake()
        //{
        //    if (Instance != null && Instance != this)
        //    {
        //        Destroy(gameObject);
        //        return;
        //    }
        //    Instance = this;
        //}

        [field: SerializeField] public PlayerMovementState CurrentPlayerMovementState { get; private set; } = PlayerMovementState.Idle;

        public void SetPlayerMovementState(PlayerMovementState playerMovementState)
        {
            CurrentPlayerMovementState = playerMovementState;
        }
    }
    public enum PlayerMovementState
    {
        Idle = 0,
        Walk = 1,
        Run = 2,
        Jump = 3,
        Fall = 4,
    }
}