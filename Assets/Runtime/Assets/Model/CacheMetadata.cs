using System;
using System.Collections.Generic;

namespace Com.Udogames.Core.Assets.Model
{
  [Serializable]
  internal class CacheMetadata
  {
    public List<CacheEntry> assets = new ();
  }
}