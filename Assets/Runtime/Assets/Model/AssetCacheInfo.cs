using System;
using Object = UnityEngine.Object;

namespace Com.Udogames.Core.Assets.Model
{
  /// <summary>
  /// Information about a cached asset
  /// </summary>
  internal class AssetCacheInfo
  {
    public string SourcePath { get; set; }
    public Object Asset { get; set; }
    public bool IsLoaded { get; set; }
    public DateTime LastAccessed { get; set; }
    public string CachePath { get; set; }
  }
}