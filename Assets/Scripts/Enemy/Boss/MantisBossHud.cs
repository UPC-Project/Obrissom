using TMPro;
using UnityEngine;

namespace Obrissom.Enemy
{
    public class MantisBossHud : MonoBehaviour
    {
        [Header("Health bar")]
        [SerializeField] private GameObject _bar;
        [Tooltip("Stretched inside the bar. The script moves its right edge with the health.")]
        [SerializeField] private RectTransform _fill;
        [SerializeField] private TMP_Text _nameText;
        [Tooltip("How fast the bar drains after a hit.")]
        [SerializeField] private float _drainSpeed = 6f;

        [Header("Victory")]
        [SerializeField] private CanvasGroup _banner;
        [SerializeField] private TMP_Text _bannerText;
        [SerializeField] private float _bannerFadeTime = 0.6f;

        private float _shownHealth = 1f;
        private float _targetHealth = 1f;
        private float _bannerStartTime;
        private float _bannerDuration;

        private bool IsBannerShowing => _banner != null && _banner.gameObject.activeSelf;

        private void Awake()
        {
            if (_bar != null) _bar.SetActive(false);
            if (_banner != null) _banner.gameObject.SetActive(false);
            ApplyHealth(1f);
        }

        public void SetBossName(string bossName)
        {
            if (_nameText != null) _nameText.text = bossName;
        }

        public void SetBarVisible(bool visible)
        {
            if (_bar != null && _bar.activeSelf != visible) _bar.SetActive(visible);
        }

        public void SetHealth(float normalized, bool instant)
        {
            _targetHealth = Mathf.Clamp01(normalized);
            if (instant) ApplyHealth(_targetHealth);
        }

        public void ShowVictory(string text, float duration)
        {
            if (_banner == null) return;

            if (_bannerText != null) _bannerText.text = text;
            _bannerStartTime = Time.time;
            _bannerDuration = Mathf.Max(duration, _bannerFadeTime * 2f);
            _banner.alpha = 0f;
            _banner.gameObject.SetActive(true);
            SetBarVisible(false);
        }

        private void Update()
        {
            if (Mathf.Abs(_shownHealth - _targetHealth) > 0.001f)
                ApplyHealth(Mathf.Lerp(_shownHealth, _targetHealth, 1f - Mathf.Exp(-_drainSpeed * Time.deltaTime)));
            else if (_shownHealth != _targetHealth)
                ApplyHealth(_targetHealth);

            if (IsBannerShowing) UpdateBanner();
        }

        private void UpdateBanner()
        {
            float elapsed = Time.time - _bannerStartTime;
            if (elapsed >= _bannerDuration)
            {
                _banner.gameObject.SetActive(false);
                return;
            }

            float fadeIn = elapsed / _bannerFadeTime;
            float fadeOut = (_bannerDuration - elapsed) / _bannerFadeTime;
            _banner.alpha = Mathf.Clamp01(Mathf.Min(fadeIn, fadeOut));
        }

        private void ApplyHealth(float health)
        {
            _shownHealth = health;
            if (_fill != null) _fill.anchorMax = new Vector2(health, _fill.anchorMax.y);
        }
    }
}
