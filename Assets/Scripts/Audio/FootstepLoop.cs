using UnityEngine;

namespace Obrissom.Audio
{
    /// <summary>
    /// Loops a footsteps clip while the character walks on the ground.
    /// Runs on every client: remote players use the synced animator and their interpolated position.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class FootstepLoop : MonoBehaviour
    {
        private static readonly int IsMovingHash = Animator.StringToHash("isMoving");
        private static readonly int IsGroundedHash = Animator.StringToHash("isGrounded");

        [Tooltip("Below this horizontal speed the steps stop (m/s).")]
        [Min(0f)] [SerializeField] private float _minSpeed = 0.5f;

        [Tooltip("Above this speed it's a teleport or respawn, not a step (m/s).")]
        [Min(0f)] [SerializeField] private float _maxSpeed = 20f;

        [Tooltip("Seconds to fade the steps in and out.")]
        [Min(0.01f)] [SerializeField] private float _fadeTime = 0.15f;

        private AudioSource _source;
        private Animator _animator;
        private float _volume;
        private Vector3 _lastPosition;
        private bool _paused;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _animator = GetComponent<Animator>();

            // The AudioSource volume set in the prefab is the walking volume
            _volume = _source.volume;
            _source.loop = true;
            _source.playOnAwake = false;
            _source.volume = 0f;
        }

        private void OnEnable() => _lastPosition = transform.position;

        private void Update()
        {
            float target = IsWalking() ? _volume : 0f;
            _source.volume = Mathf.MoveTowards(_source.volume, target, _volume * Time.deltaTime / _fadeTime);

            if (_source.volume > 0f && !_source.isPlaying)
            {
                // Resume where it stopped so the loop doesn't always start on the same step
                if (_paused) _source.UnPause();
                else _source.Play();
                _paused = false;
            }
            else if (_source.volume <= 0f && _source.isPlaying)
            {
                _source.Pause();
                _paused = true;
            }
        }

        private bool IsWalking()
        {
            Vector3 delta = transform.position - _lastPosition;
            _lastPosition = transform.position;
            delta.y = 0f;

            if (Time.deltaTime <= 0f) return false;

            float speed = delta.magnitude / Time.deltaTime;
            if (speed < _minSpeed || speed > _maxSpeed) return false;

            // No animator: speed alone decides
            if (_animator == null) return true;
            return _animator.GetBool(IsMovingHash) && _animator.GetBool(IsGroundedHash);
        }
    }
}
