using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Com.Udogames.Core.Assets.Core;
using Com.Udogames.Core.Assets.Model;
using Com.Udogames.Core.DebugLogger;
using UnityEngine;

namespace Com.Udogames.Core.Assets.Loaders
{
  /// <summary>
  /// Manages loading and caching of assets, primarily sprites
  /// </summary>
  public class AssetLoader : IAssetLoader
  {
    #region Private Fields

    private IAssetModuleInternal _module;

    // Cache of loaded assets
    private Dictionary<string, AssetCacheInfo> _cachedAssets = new();

    // Base path for the cache
    private string _cacheFolderPath;

    // Maximum number of assets to keep in memory
    private int _maxCachedAssets = 100;

    // Cancellation token source for background operations
    private CancellationTokenSource _metadataSaveCts;

    #endregion

    #region Constructor and Initialization

    /// <summary>
    /// Creates a new asset loader
    /// </summary>
    public AssetLoader(IAssetModuleInternal module)
    {
      _module = module;
      InitializeCache(AssetModule.cacheFolderName, AssetModule.maxCachedAssets);
    }

    /// <summary>
    /// Initializes the cache folder and loads cache metadata
    /// </summary>
    private void InitializeCache(string cacheFolderName, int maxCachedAssets)
    {
      // Set up cache folder path
      _cacheFolderPath = Path.Combine(Application.persistentDataPath, cacheFolderName);

      // Set up max cached assets
      SetMaxCachedAssets(maxCachedAssets);

      // Create cache directory if it doesn't exist
      if (!Directory.Exists(_cacheFolderPath))
      {
        Directory.CreateDirectory(_cacheFolderPath);
      }

      // Create the files directory if it doesn't exist
      string cacheFilesPath = Path.Combine(_cacheFolderPath, "Files");
      if (!Directory.Exists(cacheFilesPath))
      {
        Directory.CreateDirectory(cacheFilesPath);
      }

      // Load cache metadata if available
      LoadCacheMetadata();

      DebugX.Log($"AssetLoader: Cache initialized at {_cacheFolderPath}","AssetModule");
    }

    /// <summary>
    /// Loads cache metadata from disk
    /// </summary>
    private void LoadCacheMetadata()
    {
      string metadataPath = Path.Combine(_cacheFolderPath, "metadata.json");

      if (!File.Exists(metadataPath))
      {
        return;
      }

      try
      {
        string json = File.ReadAllText(metadataPath);
        CacheMetadata metadata = JsonUtility.FromJson<CacheMetadata>(json);

        if (metadata != null && metadata.assets != null)
        {
          foreach (var entry in metadata.assets)
          {
            _cachedAssets[entry.id] = new AssetCacheInfo
            {
              SourcePath = entry.sourcePath,
              IsLoaded = false,
              LastAccessed = DateTime.FromFileTimeUtc(entry.lastAccessed),
              CachePath = entry.cachePath
            };
          }

          DebugX.Log($"AssetLoader: Loaded metadata for {_cachedAssets.Count} assets","AssetModule");
        }
      }
      catch (Exception ex)
      {
        DebugX.Log($"AssetLoader: Error loading cache metadata: {ex.Message}","AssetModule");
      }
    }

    /// <summary>
    /// Saves cache metadata to disk
    /// </summary>
    private async Task SaveCacheMetadataAsync()
    {
      string metadataPath = Path.Combine(_cacheFolderPath, "metadata.json");

      try
      {
        CacheMetadata metadata = new CacheMetadata();

        foreach (var pair in _cachedAssets)
        {
          metadata.assets.Add(new CacheEntry
          {
            id = pair.Key,
            sourcePath = pair.Value.SourcePath,
            lastAccessed = pair.Value.LastAccessed.ToFileTimeUtc(),
            cachePath = pair.Value.CachePath
          });
        }

        string json = JsonUtility.ToJson(metadata, true);
        await File.WriteAllTextAsync(metadataPath, json);

        DebugX.Log($"AssetLoader: Saved metadata for {_cachedAssets.Count} assets","AssetModule");
      }
      catch (Exception ex)
      {
        DebugX.Log($"AssetLoader: Error saving cache metadata: {ex.Message}","AssetModule");
      }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Loads a sprite asset by path 
    /// </summary>
    public void LoadSprite(string path, Action<Sprite> callback, float pixelsPerUnit = 100f, bool shouldCache = true)
    {
      if (string.IsNullOrEmpty(path))
      {
        DebugX.LogError("AssetLoader: Cannot load sprite with empty path","AssetModule");
        callback?.Invoke(null);
        return;
      }

      string assetId = GetAssetId(path);

      // Check if the asset is already in memory
      if (_cachedAssets.TryGetValue(assetId, out AssetCacheInfo cacheInfo) && cacheInfo.IsLoaded)
      {
        cacheInfo.LastAccessed = DateTime.UtcNow;
        callback?.Invoke(cacheInfo.Asset as Sprite);
        return;
      }

      // Try to load from cache first
      if (_cachedAssets.TryGetValue(assetId, out cacheInfo) && !string.IsNullOrEmpty(cacheInfo.CachePath))
      {
        string fullCachePath = Path.Combine(_cacheFolderPath, "Files", cacheInfo.CachePath);
        if (File.Exists(fullCachePath))
        {
          _module.UrlObjectLoader.LoadSpriteFromFile(fullCachePath, (sprite) =>
          {
            if (sprite != null)
            {
              if (shouldCache)
              {
                cacheInfo.Asset = sprite;
                cacheInfo.IsLoaded = true;
                cacheInfo.LastAccessed = DateTime.UtcNow;
              }

              callback?.Invoke(sprite);
            }
            else
            {
              // Cache file is corrupted or not loadable, remove from cache
              _cachedAssets.Remove(assetId);

              // Try to load from source
              LoadSpriteFromSource(path, assetId, callback, pixelsPerUnit, shouldCache);
            }
          }, pixelsPerUnit);
          return;
        }
      }

      // Load from source
      LoadSpriteFromSource(path, assetId, callback, pixelsPerUnit, shouldCache);
    }

    /// <summary>
    /// Preloads multiple sprites 
    /// </summary>
    /// <param name="paths">Collection of sprite paths to preload</param>
    /// <param name="onComplete">Optional callback that receives a dictionary of loaded sprites (path -> sprite) when all sprites are loaded</param>
    public void PreloadSprites(IEnumerable<string> paths, Action<Dictionary<string, Sprite>> onComplete = null)
    {
      if (paths == null)
      {
        onComplete?.Invoke(new Dictionary<string, Sprite>());
        return;
      }

      // Create a copy to avoid enumeration issues
      List<string> pathsList = new List<string>(paths);
      if (pathsList.Count == 0)
      {
        onComplete?.Invoke(new Dictionary<string, Sprite>());
        return;
      }

      Dictionary<string, Sprite> loadedSprites = new Dictionary<string, Sprite>();
      int spritesToLoad = pathsList.Count;

      foreach (string path in pathsList)
      {
        LoadSprite(path, (sprite) =>
        {
          if (sprite != null)
          {
            loadedSprites[path] = sprite;
          }

          spritesToLoad--;
          if (spritesToLoad == 0)
          {
            DebugX.Log($"AssetLoader: Preloaded {loadedSprites.Count}/{pathsList.Count} sprites","AssetModule");

            onComplete?.Invoke(loadedSprites);
          }
        });
      }
    }

    /// <summary>
    /// Unloads an asset from memory
    /// </summary>
    public void UnloadAsset(string path)
    {
      string assetId = GetAssetId(path);

      if (_cachedAssets.TryGetValue(assetId, out AssetCacheInfo cacheInfo))
      {
        if (cacheInfo.IsLoaded && cacheInfo.Asset != null)
        {
          // If it's a sprite, destroy the texture as well
          if (cacheInfo.Asset is Sprite sprite && sprite.texture != null)
          {
            UnityEngine.Object.Destroy(sprite.texture);
          }

          UnityEngine.Object.Destroy(cacheInfo.Asset);
          cacheInfo.Asset = null;
          cacheInfo.IsLoaded = false;
        }
      }
    }

    /// <summary>
    /// Clears all assets from memory but keeps cache files
    /// </summary>
    public void ClearMemory(Action onComplete = null)
    {
      foreach (var pair in _cachedAssets)
      {
        if (pair.Value.IsLoaded && pair.Value.Asset != null)
        {
          // If it's a sprite, destroy the texture as well
          if (pair.Value.Asset is Sprite sprite && sprite.texture != null)
          {
            UnityEngine.Object.Destroy(sprite.texture);
          }

          UnityEngine.Object.Destroy(pair.Value.Asset);
          pair.Value.Asset = null;
          pair.Value.IsLoaded = false;
        }
      }

      // Save metadata
      _metadataSaveCts = new CancellationTokenSource();
      Task.Run(async () =>
      {
        await SaveCacheMetadataAsync();

        DebugX.Log("AssetLoader: Memory cleared","AssetModule");

        // Execute callback on main thread
        if (onComplete != null)
        {
          _module.Runner.RunOnMainThread(onComplete);
        }
      }, _metadataSaveCts.Token);
    }

    /// <summary>
    /// Clears the entire cache (memory and files)
    /// </summary>
    public void ClearCache(Action onComplete = null)
    {
      // Clear memory first
      ClearMemory(() =>
      {
        // Then delete cache files
        try
        {
          string cacheFilesPath = Path.Combine(_cacheFolderPath, "Files");

          if (Directory.Exists(cacheFilesPath))
          {
            Directory.Delete(cacheFilesPath, true);
            Directory.CreateDirectory(cacheFilesPath);
          }

          // Clear cache metadata
          _cachedAssets.Clear();

          // Save metadata
          _metadataSaveCts = new CancellationTokenSource();
          Task.Run(async () =>
          {
            await SaveCacheMetadataAsync();

            DebugX.Log("AssetLoader: Cache cleared","AssetModule");

            // Execute callback on main thread
            if (onComplete != null)
            {
              _module.Runner.RunOnMainThread(onComplete);
            }
          }, _metadataSaveCts.Token);
        }
        catch (Exception ex)
        {
          DebugX.LogError($"AssetLoader: Error clearing cache: {ex.Message}");

          if (onComplete != null)
          {
            onComplete();
          }
        }
      });
    }

    /// <summary>
    /// Sets the maximum number of assets to keep in memory
    /// </summary>
    public void SetMaxCachedAssets(int maxAssets)
    {
      _maxCachedAssets = Mathf.Max(1, maxAssets);
      EnforceMemoryLimit();
    }

    /// <summary>
    /// Gets the asset cache information
    /// </summary>
    public (int memoryCount, int fileCount, long sizeBytes) GetCacheInfo()
    {
      int memoryCount = 0;
      int fileCount = 0;
      long sizeBytes = 0;

      foreach (var pair in _cachedAssets)
      {
        if (pair.Value.IsLoaded)
        {
          memoryCount++;
        }

        if (!string.IsNullOrEmpty(pair.Value.CachePath))
        {
          fileCount++;

          string fullPath = Path.Combine(_cacheFolderPath, "Files", pair.Value.CachePath);
          if (File.Exists(fullPath))
          {
            sizeBytes += new FileInfo(fullPath).Length;
          }
        }
      }

      return (memoryCount, fileCount, sizeBytes);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Loads a sprite from its source (URL or file)
    /// </summary>
    private void LoadSpriteFromSource(
      string path,
      string assetId,
      Action<Sprite> callback,
      float pixelsPerUnit,
      bool shouldCache)
    {
      // Check if the path is a URL
      bool isUrl = path.StartsWith("http://") || path.StartsWith("https://") || path.StartsWith("ftp://");

      if (isUrl)
      {
        // Handle remote URL loading
        _module.UrlObjectLoader.LoadSprite(path, (sprite) =>
        {
          if (sprite != null && shouldCache)
          {
            AddToCache(assetId, path, sprite);
          }

          callback?.Invoke(sprite);
        }, pixelsPerUnit);
      }
      else
      {
        // Local file loading
        if (File.Exists(path))
        {
          _module.UrlObjectLoader.LoadSpriteFromFile(path, (sprite) =>
          {
            if (sprite != null && shouldCache)
            {
              AddToCache(assetId, path, sprite);
            }

            callback?.Invoke(sprite);
          }, pixelsPerUnit);
        }
        else
        {
          // Try Resources folder as a fallback
          _module.Runner.RunCoroutine(
            _module.UrlObjectLoader.LoadFromResourcesCoroutine(path, (sprite) =>
            {
              if (sprite != null && shouldCache)
              {
                AddToCache(assetId, path, sprite);
              }

              callback?.Invoke(sprite);
            })
          );
        }
      }
    }

    /// <summary>
    /// Gets a unique ID for an asset path
    /// </summary>
    private string GetAssetId(string path)
    {
      // Simple hashing for now - could use MD5 or SHA1 for more complex needs
      return path.GetHashCode().ToString();
    }

    /// <summary>
    /// Adds an asset to the cache
    /// </summary>
    private void AddToCache(string assetId, string path, UnityEngine.Object asset)
    {
      // Check if we need to enforce memory limits
      if (_cachedAssets.Count >= _maxCachedAssets)
      {
        EnforceMemoryLimit();
      }

      // Generate cache file path
      string extension = Path.GetExtension(path);
      if (string.IsNullOrEmpty(extension))
      {
        extension = ".bytes";
      }

      string cacheFileName = assetId + extension;

      // Create or update cache info
      AssetCacheInfo cacheInfo;
      if (_cachedAssets.TryGetValue(assetId, out cacheInfo))
      {
        cacheInfo.SourcePath = path;
        cacheInfo.Asset = asset;
        cacheInfo.IsLoaded = true;
        cacheInfo.LastAccessed = DateTime.UtcNow;

        // The cache path might already exist
        if (string.IsNullOrEmpty(cacheInfo.CachePath))
        {
          cacheInfo.CachePath = cacheFileName;
        }
      }
      else
      {
        cacheInfo = new AssetCacheInfo
        {
          SourcePath = path,
          Asset = asset,
          IsLoaded = true,
          LastAccessed = DateTime.UtcNow,
          CachePath = cacheFileName
        };

        _cachedAssets[assetId] = cacheInfo;
      }

      // Save to cache file
      if (asset is Sprite sprite)
      {
        SaveSpriteToCache(sprite, cacheInfo.CachePath);
      }

      // Periodically save metadata
      if (UnityEngine.Random.value < 0.1f) // 10% chance
      {
        TriggerMetadataSave();
      }
    }

    /// <summary>
    /// Triggers metadata save after a delay to avoid frequent writes
    /// </summary>
    private void TriggerMetadataSave()
    {
      // Cancel any existing save operation
      if (_metadataSaveCts != null)
      {
        _metadataSaveCts.Cancel();
        _metadataSaveCts.Dispose();
      }

      _metadataSaveCts = new CancellationTokenSource();
      var token = _metadataSaveCts.Token;

      // Save metadata after a delay
      Task.Delay(1000, token).ContinueWith(async task =>
      {
        if (task.IsCanceled) return;

        await SaveCacheMetadataAsync();

        _metadataSaveCts.Dispose();
        _metadataSaveCts = null;
      }, TaskScheduler.Default);
    }

    /// <summary>
    /// Saves a sprite to the cache directory
    /// </summary>
    private void SaveSpriteToCache(Sprite sprite, string cacheFileName)
    {
      // For sprites, we need to save the texture data
      string fullPath = Path.Combine(_cacheFolderPath, "Files", cacheFileName);

      if (sprite.texture.isReadable)
      {
        byte[] pngData = sprite.texture.EncodeToPNG();

        if (pngData != null)
        {
          try
          {
            File.WriteAllBytes(fullPath, pngData);
          }
          catch (Exception ex)
          {
            DebugX.LogError($"AssetLoader: Error saving to cache: {ex.Message}");
          }
        }
      }
      else
      {
        DebugX.LogWarning("AssetLoader: Cannot cache sprite with non-readable texture");
      }
    }

    /// <summary>
    /// Ensures we don't exceed the memory limit by unloading least recently used assets
    /// </summary>
    private void EnforceMemoryLimit()
    {
      int loadedCount = 0;
      List<KeyValuePair<string, AssetCacheInfo>> loadedAssets = new List<KeyValuePair<string, AssetCacheInfo>>();

      foreach (var pair in _cachedAssets)
      {
        if (pair.Value.IsLoaded)
        {
          loadedCount++;
          loadedAssets.Add(pair);
        }
      }

      if (loadedCount <= _maxCachedAssets)
      {
        return;
      }

      // Sort by last accessed time (oldest first)
      loadedAssets.Sort((a, b) => a.Value.LastAccessed.CompareTo(b.Value.LastAccessed));

      // Calculate how many to unload
      int unloadCount = loadedCount - _maxCachedAssets;

      // Unload the oldest assets
      for (int i = 0; i < unloadCount; i++)
      {
        var pair = loadedAssets[i];
        UnloadAsset(pair.Value.SourcePath);
      }

      DebugX.Log($"AssetLoader: Unloaded {unloadCount} assets to enforce memory limit","AssetModule");
    }

    #endregion
  }
}