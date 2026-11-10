using UnityEngine;

namespace Holedown
{
    // Draws the aiming beam as a stepped quad strip: a flat ice-blue body with a bright segment
    // that travels towards the first-contact point, a pixel-stepped tip and hard cross bands.
    // It only presents; AimInput still owns the geometry, the radius and the collision guess.
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class AimBeam : MonoBehaviour
    {
        [SerializeField] Material material;
        [SerializeField, Tooltip("Beam width in pixels at the 540-wide design stage.")]
        [Min(1f)] float designWidthPixels = 10f;
        [SerializeField, Tooltip("World width of the stage the design width refers to.")]
        [Min(0.1f)] float designStageWidth = 9.4f;
        [SerializeField, Min(1)] int designScreenWidth = 540;
        [SerializeField, Tooltip("Seconds for the highlight to travel from the launcher to the tip.")]
        [Min(0.05f)] float sweepSeconds = 1.1f;
        [SerializeField, Tooltip("Segments along the beam; more segments give a finer stepped tip.")]
        [Range(4, 64)] int segments = 24;
        [SerializeField, Tooltip("Length of the stepped tip as a fraction of the beam.")]
        [Range(0.02f, 0.4f)] float tipFraction = 0.12f;
        [SerializeField] int sortingOrder = 2;
        Mesh mesh;
        MeshRenderer meshRenderer;
        MaterialPropertyBlock block;
        static readonly int SweepId = Shader.PropertyToID("_Sweep");
        float sweep;
        public bool IsVisible => meshRenderer && meshRenderer.enabled;
        public Vector2 Start { get; private set; }
        public Vector2 End { get; private set; }
        public float Width => designWidthPixels * designStageWidth / Mathf.Max(1, designScreenWidth);
        public int SortingOrder => sortingOrder;
        public float Sweep => sweep;

        void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.sortingOrder = sortingOrder;
            meshRenderer.enabled = false;
            block = new MaterialPropertyBlock();
        }
        public void Hide()
        {
            if (meshRenderer) meshRenderer.enabled = false;
        }
        public void Show(Vector2 origin, Vector2 end)
        {
            if (!meshRenderer) Awake();
            Start = origin; End = end;
            Build(origin, end, Width);
            meshRenderer.enabled = true;
            ApplySweep();
        }
        void Update()
        {
            if (!IsVisible || sweepSeconds <= 0f) return;
            sweep = Mathf.Repeat(sweep + Time.deltaTime / sweepSeconds, 1f);
            ApplySweep();
        }
        void ApplySweep()
        {
            if (!meshRenderer) return;
            meshRenderer.GetPropertyBlock(block);
            block.SetFloat(SweepId, sweep);
            meshRenderer.SetPropertyBlock(block);
        }
        // A stepped taper at the tip keeps the pixel look: the last segments lose width in
        // whole steps instead of shrinking smoothly.
        public float WidthAt(float t)
        {
            if (t >= 1f) return 0f;
            float tipStart = 1f - Mathf.Clamp(tipFraction, 0.02f, 0.4f);
            if (t <= tipStart) return Width;
            float k = Mathf.InverseLerp(tipStart, 1f, t);
            float steps = Mathf.Max(1f, Mathf.Round(segments * (1f - tipStart)));
            float stepped = Mathf.CeilToInt((1f - k) * steps) / steps;
            return Width * stepped;
        }
        void Build(Vector2 origin, Vector2 end, float width)
        {
            if (mesh == null)
            {
                mesh = new Mesh { name = "AimBeam" };
                mesh.MarkDynamic();
                GetComponent<MeshFilter>().sharedMesh = mesh;
            }
            Vector2 direction = end - origin;
            if (direction.sqrMagnitude < 0.0001f) direction = Vector2.down;
            direction.Normalize();
            Vector2 normal = new Vector2(-direction.y, direction.x);
            int count = Mathf.Max(4, segments);
            var vertices = new Vector3[(count + 1) * 2];
            var uv = new Vector2[vertices.Length];
            var colors = new Color[vertices.Length];
            float length = Vector2.Distance(origin, end);
            for (int i = 0; i <= count; i++)
            {
                float t = (float)i / count;
                Vector2 center = origin + direction * (length * t);
                float half = WidthAt(t) * 0.5f;
                vertices[i * 2] = center - normal * half;
                vertices[i * 2 + 1] = center + normal * half;
                uv[i * 2] = new Vector2(t, 0f);
                uv[i * 2 + 1] = new Vector2(t, 1f);
                colors[i * 2] = Color.white;
                colors[i * 2 + 1] = Color.white;
            }
            var triangles = new int[count * 6];
            for (int i = 0; i < count; i++)
            {
                int v = i * 2;
                triangles[i * 6] = v;
                triangles[i * 6 + 1] = v + 1;
                triangles[i * 6 + 2] = v + 2;
                triangles[i * 6 + 3] = v + 1;
                triangles[i * 6 + 4] = v + 3;
                triangles[i * 6 + 5] = v + 2;
            }
            mesh.Clear();
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
        }
    }
}
