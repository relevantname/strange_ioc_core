using System;

namespace Com.Udogames.Core.Assets.Model
{
  [Serializable]
  internal class CacheEntry
  {
    public string id;

    public string sourcePath;

    public long lastAccessed;

    public string cachePath;
  }
}