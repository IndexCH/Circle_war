using UnityEngine;

namespace CircleWar
{
    public class CircleSegmentSpriteFactory
    {
        private const string GeneratedAssetsResourceFolder = "Scence/盐碱地/GeneratedAssets";

        private Sprite[] segmentSprites;

        public Sprite GetSegmentSprite()
        {
            if (segmentSprites == null)
            {
                segmentSprites = Resources.LoadAll<Sprite>(GeneratedAssetsResourceFolder);
            }

            if (segmentSprites.Length == 0)
            {
                return null;
            }

            return segmentSprites[Random.Range(0, segmentSprites.Length)];

            //TODO: 根据contentType返回对应的sprite
        }
    }
}
