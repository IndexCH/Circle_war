using UnityEngine;

namespace CircleWar
{
    public sealed class FeelDeathRing : MonoBehaviour
    {
        private LineRenderer ring;
        private Color color;
        private float age;
        public void Configure(Material material, int sortingLayer, int sortingOrder, Color tint)
        {
            var child = new GameObject("Death Shockwave");
            child.transform.SetParent(transform, false);
            ring = child.AddComponent<LineRenderer>();
            ring.sharedMaterial = material;
            ring.sortingLayerID = sortingLayer;
            ring.sortingOrder = sortingOrder;
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = 40;
            for (int i = 0; i < 40; i++)
            {
                float angle = i * Mathf.PI * 2f / 40;
                ring.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f));
            }
            color = tint;
            UpdateRing();
        }
        private void Update() { age += Time.unscaledDeltaTime; UpdateRing(); }
        private void UpdateRing()
        {
            if (ring == null) return;
            float t = Mathf.Clamp01(age / .45f);
            ring.transform.localScale = Vector3.one * Mathf.Lerp(.12f, .85f, t);
            ring.widthMultiplier = Mathf.Lerp(.06f, .005f, t);
            Color faded = color;
            faded.a = 1f - t;
            ring.startColor = ring.endColor = faded;
            if (t >= 1f) { ring.enabled = false; enabled = false; }
        }
    }
}
