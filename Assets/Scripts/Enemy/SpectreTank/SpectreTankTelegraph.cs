using Unity.Netcode;
using UnityEngine;

namespace Obrissom.Enemy
{
    /// <summary>
    /// Ground warning for the spectre tank attacks (clients only).
    /// Strip: the dash path. It fills from the back to the front, then the dash comes.
    /// </summary>
    public class SpectreTankTelegraph : MonoBehaviour
    {
        private const float OutlineWidth = 0.08f;
        private const float FillAlpha = 0.45f;
        private const float GroundProbeHeight = 1.5f;
        private const float GroundOffset = 0.06f;
        private const float PulseFrequency = 12f;

        private static Material s_defaultMaterial;

        private enum Mode { Hidden, Telegraph, Impact }

        private LineRenderer _outline;
        private LineRenderer _fill;
        private Mode _mode = Mode.Hidden;

        private Vector3 _start;
        private Vector3 _end;
        private double _startServerTime;
        private float _duration;
        private float _impactEndTime;
        private float _impactDuration;
        private float _warningThreshold;
        private Color _color;
        private Color _warningColor;

        public static SpectreTankTelegraph Create(Material material)
        {
            if (material == null)
            {
                if (s_defaultMaterial == null) s_defaultMaterial = new Material(Shader.Find("Sprites/Default"));
                material = s_defaultMaterial;
            }

            var root = new GameObject("SpectreTankTelegraph");
            // Lines face the transform Z, so Z up keeps them flat on the ground
            root.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);

            var telegraph = root.AddComponent<SpectreTankTelegraph>();
            telegraph._fill = CreateLine(root.transform, "Fill", material);
            telegraph._outline = CreateLine(root.transform, "Outline", material);
            telegraph._outline.loop = true;
            telegraph._outline.widthMultiplier = OutlineWidth;
            root.SetActive(false);
            return telegraph;
        }

        public void ShowStrip(Vector3 start, Vector3 end, float width, double startServerTime, float duration,
                              Color color, Color warningColor, float warningThreshold, LayerMask groundMask)
        {
            _start = OnGround(start, groundMask);
            _end = OnGround(end, groundMask);
            _startServerTime = startServerTime;
            _duration = Mathf.Max(duration, 0.01f);
            _color = color;
            _warningColor = warningColor;
            _warningThreshold = warningThreshold;
            _mode = Mode.Telegraph;

            Vector3 side = Vector3.Cross(Vector3.up, Flat(_end - _start).normalized) * (width * 0.5f);
            _outline.positionCount = 4;
            _outline.SetPosition(0, _start - side);
            _outline.SetPosition(1, _end - side);
            _outline.SetPosition(2, _end + side);
            _outline.SetPosition(3, _start + side);

            _fill.widthMultiplier = width;
            _fill.positionCount = 2;

            gameObject.SetActive(true);
            Refresh();
        }

        // The attack landed: full strip, then it fades
        public void PlayImpact(float duration)
        {
            if (_mode == Mode.Hidden) return;

            _mode = Mode.Impact;
            _impactDuration = Mathf.Max(duration, 0.01f);
            _impactEndTime = Time.time + _impactDuration;
            SetFill(1f);
        }

        public void Hide()
        {
            _mode = Mode.Hidden;
            gameObject.SetActive(false);
        }

        private void Update() => Refresh();

        private void Refresh()
        {
            switch (_mode)
            {
                case Mode.Telegraph:
                {
                    float progress = Mathf.Clamp01((float)((ServerTime - _startServerTime) / _duration));
                    SetFill(progress);

                    Color color = _color;
                    if (progress >= _warningThreshold)
                    {
                        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * PulseFrequency * Mathf.PI * 2f);
                        color = Color.Lerp(_color, _warningColor, pulse);
                    }
                    ApplyColor(color);
                    break;
                }

                case Mode.Impact:
                {
                    float remaining = _impactEndTime - Time.time;
                    if (remaining <= 0f)
                    {
                        Hide();
                        return;
                    }

                    Color faded = _warningColor;
                    faded.a *= Mathf.Clamp01(remaining / _impactDuration);
                    ApplyColor(faded);
                    break;
                }
            }
        }

        // Fill grows from the start of the strip
        private void SetFill(float progress)
        {
            _fill.SetPosition(0, _start);
            _fill.SetPosition(1, Vector3.Lerp(_start, _end, Mathf.Max(progress, 0.02f)));
        }

        private void ApplyColor(Color color)
        {
            _outline.startColor = _outline.endColor = color;
            Color fillColor = color;
            fillColor.a *= FillAlpha;
            _fill.startColor = _fill.endColor = fillColor;
        }

        private static Vector3 OnGround(Vector3 point, LayerMask groundMask)
        {
            if (Physics.Raycast(point + Vector3.up * GroundProbeHeight, Vector3.down, out RaycastHit hit,
                                GroundProbeHeight * 2f, groundMask, QueryTriggerInteraction.Ignore))
                point = hit.point;

            return point + Vector3.up * GroundOffset;
        }

        private static Vector3 Flat(Vector3 vector)
        {
            vector.y = 0f;
            return vector;
        }

        private static double ServerTime =>
            NetworkManager.Singleton != null ? NetworkManager.Singleton.ServerTime.Time : Time.timeAsDouble;

        private static LineRenderer CreateLine(Transform parent, string name, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.alignment = LineAlignment.TransformZ;
            line.numCapVertices = 0;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sharedMaterial = material;
            return line;
        }
    }
}
