// Copyright (c) 2014-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.

using SiliconStudio.Core;

namespace SiliconStudio.Xenko.Rendering
{
    /// <summary>
    /// Sort elements according to the pattern: [RenderFeature Sort Key 8 bits] [Distance back to front 32 bits] [RenderObject states 24 bits]
    /// </summary>
    [DataContract("BackToFrontSortMode")]
    public class BackToFrontSortMode : SortModeDistance
    {
        public BackToFrontSortMode() : base(true)
        {
            distancePrecision = 32;
            distancePosition = 24;

            statePrecision = 24;
        }
    }
}
