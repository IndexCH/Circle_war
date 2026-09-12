using System.Collections;
using UnityEngine;

namespace CircleWar
{
    /// <summary>Starts an isolated art-preview scene in its authored season after normal game bootstrap.</summary>
    [DisallowMultipleComponent]
    public sealed class SeasonArtPreview : MonoBehaviour
    {
        [SerializeField] private GameHud gameHud;
        [SerializeField] private SeasonDefinition season;
        [SerializeField] private string arrivalMessage;

        private IEnumerator Start()
        {
            // GameHud and CircleMapView both initialize in Start; apply the preview after both finish.
            yield return null;
            if (gameHud == null || season == null) yield break;
            GameRuntimeData runtime = gameHud.RuntimeData;
            runtime.SetCalendar(1, season.DefinitionId, season.DisplayName, 18, 0);
            runtime.SetSeasonContext(season, season.Region, 0f);
            if (season.Region != null)
            {
                runtime.SetRegionStatus(season.Region.DefinitionId, season.Region.DisplayName, true,
                    new[] { new HudFeedEntryRuntimeData(18, 0, arrivalMessage) });
            }
        }
    }
}
