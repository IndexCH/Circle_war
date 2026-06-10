using UnityEngine;

namespace CircleWar
{
    public class CircleSegmentSpriteFactory
    {
        private const string GeneratedAssetsResourceFolder = "Scence/盐碱地/GeneratedAssets";

        public Sprite GetSegmentSprite(string spriteName)
        {
            return Resources.Load<Sprite>(GeneratedAssetsResourceFolder + "/" + spriteName);
        }
    }
}
