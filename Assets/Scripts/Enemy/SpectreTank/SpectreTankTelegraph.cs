using Unity.Netcode;
using UnityEngine;

namespace Obrissom.Enemy
{
    /// <summary>
    /// Ground warning for the spectre tank attacks (clients only).
    /// Strip: the dash path, it fills from the back to the front.
    /// Circle: a slam ring, it fills from the center to the edge.
    /// </summary>
    public class SpectreTankTelegraph : MonoBehaviour
    {
        private const int CircleSegments = 48;
        private const float OutlineWidth = 0.08f;
        private const float FillAlpha = 0.45f;
        private const float MinFill = 0.02f;
        private const float GroundProbeHeight = 1.5f;
        private const float GroundOffset = 0.06f;
        private const float PulseFrequency = 12f;

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private static Material s_defaultMaterial;
        private static Mesh s_discMesh;

        private enum Mode { Hidden, Telegraph, Impact }

        private LineRenderer _outline;
        private LineRenderer _stripFill;
        private MeshRenderer _discFill;
        private MaterialPropertyBlock _propertyBlock;
        private bool _isCircle;
        private Mode _mode = Mode.Hidden;

        private Vector3 _start;
        private Vector3 _end;
        private float _radius;
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
            telegraph._propertyBlock = new MaterialPropertyBlock();
            telegraph._stripFill = CreateLine(root.transform, "Strip Fill", material);
            telegraph._discFill = CreateDisc(root.transform, material);
            telegraph._outline = CreateLine(root.transform, "Outline", material);
            telegraph._outline.loop = true;
            telegraph._outline.widthMultiplier = OutlineWidth;
            root.SetActive(false);
            return telegraph;
        }

        public void ShowStrip(Vector3 start, Vector3 end, float width, double startServerTime, float duration,
                              Color color, Color warningColor, float warningThreshold, LayerMask groundMask)
        {
            _isCircle = false;
            _start = OnGround(start, groundMask);
            _end = OnGround(end, groundMask);

            Vector3 side = Vector3.Cross(Vector3.up, Flat(_end - _start).normalized) * (width * 0.5f);
            _outline.positionCount = 4;
            _outline.SetPosition(0, _start - side);
            _outline.SetPosition(1, _end - side);
            _outline.SetPosition(2, _end + side);
            _outline.SetPosition(3, _start + side);

            _stripFill.widthMultiplier = width;
            _stripFill.positionCount = 2;
            _stripFill.enabled = true;
            _discFill.enabled = false;

            Begin(startServerTime, duration, color, warningColor, warningThreshold);
        }

        public void ShowCircle(Vector3 center, float radius, double startServerTime, float duration,
                               Color color, Color warningColor, float warningThreshold, LayerMask groundMask)
        {
            _isCircle = true;
            _radius = radius;
            Vector3 ground = OnGround(center, groundMask);
            transform.position = ground;

            _outline.positionCount = CircleSegments;
            for (int i = 0; i < CircleSegments; i++)
            {
                float angle = i / (float)CircleSegments * Mathf.PI * 2f;
                _outline.SetPosition(i, ground + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius);
            }

            _stripFill.enabled = false;
            _discFill.enabled = true;

            Begin(startServerTime, duration, color, warningColor, warningThreshold);
        }

        // The attack landed: full shape, then it fades
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

        private void Begin(double startServerTime, float duration, Color color, Color warningColor, float warningThreshold)
        {
            _startServerTime = startServerTime;
            _duration = Mathf.Max(duration, 0.01f);
            _color = color;
            _warningColor = warningColor;
            _warningThreshold = warningThreshold;
            _mode = Mode.Telegraph;

            gameObject.SetActive(true);
            Refresh();
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

        private void SetFill(float progress)
        {
            progress = Mathf.Max(progress, MinFill);

            if (_isCircle)
            {
                float size = _radius * progress;
                _discFill.transform.localScale = new Vector3(size, size, 1f);
                return;
            }

            _stripFill.SetPosition(0, _start);
            _stripFill.SetPosition(1, Vector3.Lerp(_start, _end, progress));
        }

        private void ApplyColor(Color color)
        {
            _outline.startColor = _outline.endColor = color;

            Color fillColor = color;
            fillColor.a *= FillAlpha;
            _stripFill.startColor = _stripFill.endColor = fillColor;

            _propertyBlock.SetColor(ColorId, fillColor);
            _propertyBlock.SetColor(BaseColorId, fillColor);
            _discFill.SetPropertyBlock(_propertyBlock);
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

        // Flat disc of radius 1, scaled to the circle size
        private static MeshRenderer CreateDisc(Transform parent, Material material)
        {
            var go = new GameObject("Disc Fill");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = GetDiscMesh();

            var disc = go.AddComponent<MeshRenderer>();
            disc.sharedMaterial = material;
            disc.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            disc.receiveShadows = false;
            return disc;
        }

        private static Mesh GetDiscMesh()
        {
            if (s_discMesh != null) return s_discMesh;

            var vertices = new Vector3[CircleSegments + 1];
            var colors = new Color32[CircleSegments + 1];
            var triangles = new int[CircleSegments * 3];
            colors[0] = new Color32(255, 255, 255, 255);

            for (int i = 0; i < CircleSegments; i++)
            {
                float angle = i / (float)CircleSegments * Mathf.PI * 2f;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                colors[i + 1] = colors[0];

                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 1 == CircleSegments ? 1 : i + 2;
            }

            s_discMesh = new Mesh { name = "SpectreTankDisc", vertices = vertices, colors32 = colors, triangles = triangles };
            s_discMesh.RecalculateBounds();
            return s_discMesh;
        }
    }
}
