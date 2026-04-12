using UnityEngine;

namespace Obrissom.Player
{
    public class PlayerAnimation : MonoBehaviour
    {
       [SerializeField] private Animator _animator;

        private PlayerLocomotionInput _playerLocomotionInput;

        // reference to animator parameters
        private static int InputXHash = Animator.StringToHash("InputX");
    }
}
