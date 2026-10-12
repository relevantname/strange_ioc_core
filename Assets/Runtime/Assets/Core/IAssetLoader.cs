using System;
using System.Collections.Generic;
using UnityEngine;

namespace Com.Udogames.Core.Assets.Core
{
  public interface IAssetLoader
  {
    /// <summary>
    /// Loads a sprite asset by path 
    /// </summary>
    void LoadSprite(string path, Action<Sprite> callback, float pixelsPerUnit = 100f, bool shouldCache = true);

    /// <summary>
    /// Preloads multiple sprites 
    /// </summary>
    /// <param name="paths">Collection of sprite paths to preload</param>
    /// <param name="onComplete">Optional callback that receives a dictionary of loaded sprites (path -> sprite) when all sprites are loaded</param>
    void PreloadSprites(IEnumerable<string> paths, Action<Dictionary<string, Sprite>> onComplete = null);

    /// <summary>
    /// Unloads an asset from memory
    /// </summary>
    void UnloadAsset(string path);

    /// <summary>
    /// Clears all assets from memory but keeps cache files
    /// </summary>
    void ClearMemory(Action onComplete = null);

    /// <summary>
    /// Clears the entire cache (memory and files)
    /// </summary>
    void ClearCache(Action onComplete = null);

    /// <summary>
    /// Sets the maximum number of assets to keep in memory
    /// </summary>
    void SetMaxCachedAssets(int maxAssets);

    /// <summary>
    /// Gets the asset cache information
    /// </summary>
    (int memoryCount, int fileCount, long sizeBytes) GetCacheInfo();
  }
}