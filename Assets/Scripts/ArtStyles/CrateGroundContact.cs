using UnityEngine;
using UnityEngine.Rendering;

namespace CircleWar
{
    /// <summary>A pooled salt-crust strip bent along the map surface, with one buried crate corner.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class CrateGroundContact : MonoBehaviour
    {
        private const int ArcSegments = 32;
        private const float WidthMultiplier = 1.22f;
        private static readonly int MainTexture = Shader.PropertyToID("_MainTex");
        private Mesh contactMesh;
        private MeshRenderer contactRenderer;
        private MaterialPropertyBlock properties;
        private Vector3[] vertices;
        private Vector2[] uvs;

        public void Hide()
        {
            if (contactRenderer != null) contactRenderer.enabled = false;
        }

        public void Configure(SpriteRenderer owner, Sprite texture, Vector2 circleCenter, float surfaceRadius)
        {
            if (owner == null || owner.sprite == null || texture == null || surfaceRadius <= 0f)
            {
                Hide();
                return;
            }

            Initialize();
            if (contactRenderer.sharedMaterial == null)
            {
                Hide();
                return;
            }

            Bounds box = owner.sprite.bounds;
            Vector2 left = owner.transform.TransformPoint(new Vector3(box.min.x, box.min.y, 0f));
            Vector2 right = owner.transform.TransformPoint(new Vector3(box.max.x, box.min.y, 0f));
            Vector2 bottom = (left + right) * 0.5f;
            Vector2 outward = (bottom - circleCenter).normalized;
            Vector2 tangent = new Vector2(-outward.y, outward.x);
            float width = Vector2.Distance(left, right) * WidthMultiplier;
            float height = owner.transform.TransformVector(Vector3.up * box.size.y).magnitude;

            // Choose the corner already sitting deepest in the ground; ties bury the left corner.
            float leftRadius = Vector2.Distance(left, circleCenter);
            float rightRadius = Vector2.Distance(right, circleCenter);
            Vector2 buriedCorner = leftRadius >= rightRadius - 0.01f ? left : right;
            float cornerRadius = Vector2.Distance(buriedCorner, circleCenter);
            float cornerSide = Vector2.Dot(buriedCorner - bottom, tangent) < 0f ? -1f : 1f;
            float moundCenter = 0.5f + cornerSide * 0.30f;
            float buryHeight = Mathf.Clamp(height * 0.28f, 0.14f, 0.26f);
            float moundHeight = Mathf.Clamp(surfaceRadius - cornerRadius + buryHeight + 0.07f, 0.14f, 0.32f);
            float baseDepth = Mathf.Clamp(width * 0.06f, 0.06f, 0.10f);
            Rect rect = texture.rect;
            float textureWidth = texture.texture.width;
            float textureHeight = texture.texture.height;

            for (int column = 0; column <= ArcSegments; column++)
            {
                float u = column / (float)ArcSegments;
                float angle = (u - 0.5f) * width / surfaceRadius;
                Vector2 direction = outward * Mathf.Cos(angle) + tangent * Mathf.Sin(angle);
                float mound = Mathf.Max(0f, 1f - Mathf.Abs(u - moundCenter) / 0.29f);
                mound = Mathf.SmoothStep(0f, 1f, mound);
                float innerRadius = surfaceRadius - Mathf.Lerp(0.045f, moundHeight, mound);
                for (int row = 0; row < 2; row++)
                {
                    int vertex = column * 2 + row;
                    // Texture bottom stays inside the terrain; the upper edge lifts into the crate.
                    float radius = row == 0 ? surfaceRadius + baseDepth : innerRadius;
                    Vector2 world = circleCenter + direction * radius;
                    vertices[vertex] = transform.InverseTransformPoint(new Vector3(world.x, world.y, owner.transform.position.z));
                    uvs[vertex] = new Vector2((rect.x + rect.width * u) / textureWidth,
                        (rect.y + rect.height * row) / textureHeight);
                }
            }

            contactMesh.vertices = vertices;
            contactMesh.uv = uvs;
            contactMesh.RecalculateBounds();
            contactRenderer.sortingLayerID = owner.sortingLayerID;
            contactRenderer.sortingOrder = owner.sortingOrder + 1;
            properties.SetTexture(MainTexture, texture.texture);
            contactRenderer.SetPropertyBlock(properties);
            contactRenderer.enabled = owner.enabled;
        }

        private void Initialize()
        {
            if (contactMesh != null) return;
            contactRenderer = GetComponent<MeshRenderer>();
            contactRenderer.sharedMaterial = Resources.Load<Material>("ArtStyles/CrateGroundContact");
            contactRenderer.shadowCastingMode = ShadowCastingMode.Off;
            contactRenderer.receiveShadows = false;
            contactRenderer.lightProbeUsage = LightProbeUsage.Off;
            contactRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            properties = new MaterialPropertyBlock();
            int count = (ArcSegments + 1) * 2;
            vertices = new Vector3[count];
            uvs = new Vector2[count];
            var colors = new Color32[count];
            var normals = new Vector3[count];
            var tangents = new Vector4[count];
            for (int i = 0; i < count; i++)
            {
                colors[i] = new Color32(255, 255, 255, 255);
                normals[i] = Vector3.back;
                tangents[i] = new Vector4(1f, 0f, 0f, -1f);
            }
            var triangles = new int[ArcSegments * 6];
            for (int i = 0; i < ArcSegments; i++)
            {
                int vertex = i * 2;
                int triangle = i * 6;
                triangles[triangle] = vertex;
                triangles[triangle + 1] = vertex + 1;
                triangles[triangle + 2] = vertex + 2;
                triangles[triangle + 3] = vertex + 1;
                triangles[triangle + 4] = vertex + 3;
                triangles[triangle + 5] = vertex + 2;
            }
            contactMesh = new Mesh { name = "Curved crate salt crust", hideFlags = HideFlags.DontSave };
            contactMesh.MarkDynamic();
            contactMesh.vertices = vertices;
            contactMesh.colors32 = colors;
            contactMesh.normals = normals;
            contactMesh.tangents = tangents;
            contactMesh.triangles = triangles;
            GetComponent<MeshFilter>().sharedMesh = contactMesh;
        }

        private void OnDestroy()
        {
            if (contactMesh == null) return;
            if (Application.isPlaying) Destroy(contactMesh);
            else DestroyImmediate(contactMesh);
        }
    }
}
