using TMPro;
using UnityEngine;

namespace Obrissom.UI
{
    public class DamagePopUpAnimation : Billboard
    {
        public TMP_Text damageText;
        [SerializeField] private float time = 0;
        private Vector3 _origin;

        [Header("Animation")]
        public float displayDuration = 0.5f;
        [SerializeField] private AnimationCurve _opacityCurve;
        [SerializeField] private AnimationCurve _scaleCurve;
        [SerializeField] private AnimationCurve _heightCurve;

        private void Start()
        {
            _origin = transform.position;
        }
        public void Init(string text)
        {
            damageText.text = text;
            time = 0f;
            _origin = transform.position;
        }

        private void Update()
        {
            damageText.color = new Color(damageText.color.r, damageText.color.g, damageText.color.b, _opacityCurve.Evaluate(time));
            transform.localScale = Vector3.one * _scaleCurve.Evaluate(time);
            transform.position = _origin + Vector3.up * _heightCurve.Evaluate(time);
            time += Time.deltaTime;
        }
    }
}
