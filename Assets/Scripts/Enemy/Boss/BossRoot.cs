using Unity.Netcode;
using UnityEngine;

namespace Obrissom.Enemy
{
    public class BossRoot : MonoBehaviour
    {
        [SerializeField] private BossDamageTrigger _damageTrigger;

        [Header("Placeholder timeline")]
        [Tooltip("Turn off once an animation shows the root and toggles its collider.")]
        [SerializeField] private bool _useTimeline = true;
        [Tooltip("Grows out of the ground during the timeline. Its pivot is the base of the root.")]
        [SerializeField] private Transform _visual;
        [SerializeField] private float _warningTime = 1f;
        [SerializeField] private float _riseTime = 0.12f;
        [Tooltip("Seconds the collider deals damage, counted from the moment the root comes out.")]
        [SerializeField] private float _hitTime = 0.3f;
        [SerializeField] private float _lingerTime = 0.8f;
        [SerializeField] private float _sinkTime = 0.3f;
        [SerializeField] private float _warningRadius = 1f;
        [SerializeField] private Color _warningColor = new Color(1f, 0.55f, 0.1f, 0.8f);
        [SerializeField] private Color _impactColor = new Color(1f, 0.1f, 0.05f, 1f);

        [Header("Safety")]
        [Tooltip("Back to the pool after this long, even if no animation event calls ReturnToPool.")]
        [SerializeField] private float _maxLifetime = 6f;

        private ShoebillAreaIndicator _indicator;
        private Vector3 _visualPosition;
        private Vector3 _visualScale = Vector3.one;
        private double _startServerTime;
        private float _activeSince;
        private bool _impactShown;
        private int _groundMask;

        public bool IsActive => gameObject.activeSelf;

        public float ActiveSince => _activeSince;

        private static double ServerTime =>
            NetworkManager.Singleton != null ? NetworkManager.Singleton.ServerTime.Time : Time.timeAsDouble;

        private void Awake()
        {
            if (_visual == null) return;
            _visualPosition = _visual.localPosition;
            _visualScale = _visual.localScale;
        }

        private void OnDestroy()
        {
            if (_indicator != null) Destroy(_indicator.gameObject);
        }

        public void Activate(Vector3 position, double startServerTime, float damage, int groundMask)
        {
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            _startServerTime = startServerTime;
            _activeSince = Time.time;
            _impactShown = false;
            _groundMask = groundMask;

            if (_damageTrigger != null) _damageTrigger.Damage = damage;

            gameObject.SetActive(true);

            if (!_useTimeline) return;

            if (_damageTrigger != null) _damageTrigger.SetActive(false);
            SetGrowth(0f);
            GetIndicator().ShowTelegraph(position, _warningRadius, startServerTime, _warningTime,
                                         _warningColor, _impactColor, groundMask);
        }

        public void ReturnToPool()
        {
            if (_useTimeline && _damageTrigger != null) _damageTrigger.SetActive(false);
            if (_indicator != null) _indicator.Hide();
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (Time.time - _activeSince >= _maxLifetime)
            {
                ReturnToPool();
                return;
            }

            if (_useTimeline) UpdateTimeline((float)(ServerTime - _startServerTime));
        }

        private void UpdateTimeline(float time)
        {
            float outTime = time - _warningTime;
            if (outTime < 0f)
            {
                SetGrowth(0f);
                return;
            }

            if (!_impactShown)
            {
                _impactShown = true;
                GetIndicator().PlayImpact(transform.position, _warningRadius, _hitTime, _impactColor, _groundMask);
            }

            if (_damageTrigger != null)
            {
                bool hitting = outTime < _hitTime;
                if (_damageTrigger.IsActive != hitting) _damageTrigger.SetActive(hitting);
            }

            float sinkStart = _riseTime + _lingerTime;
            if (outTime < _riseTime) SetGrowth(outTime / _riseTime);
            else if (outTime < sinkStart) SetGrowth(1f);
            else if (outTime < sinkStart + _sinkTime) SetGrowth(1f - (outTime - sinkStart) / _sinkTime);
            else ReturnToPool();
        }

        private void SetGrowth(float amount)
        {
            if (_visual == null) return;

            amount = Mathf.Clamp01(amount);
            _visual.gameObject.SetActive(amount > 0f);
            _visual.localPosition = _visualPosition * amount;
            _visual.localScale = new Vector3(_visualScale.x, _visualScale.y * amount, _visualScale.z);
        }

        private ShoebillAreaIndicator GetIndicator()
        {
            if (_indicator != null) return _indicator;

            _indicator = ShoebillAreaIndicator.CreateFallback(null);
            _indicator.transform.SetParent(transform.parent, false);
            return _indicator;
        }
    }
}
