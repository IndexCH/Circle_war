using System.Collections;
using UnityEngine;

namespace CircleWar
{
    public sealed class FeelEffectLifetime : MonoBehaviour
    {
        public void Begin(float duration)
        {
            StartCoroutine(Cleanup(duration));
        }

        private IEnumerator Cleanup(float duration)
        {
            yield return new WaitForSecondsRealtime(duration);
            Destroy(gameObject);
        }
    }
}
