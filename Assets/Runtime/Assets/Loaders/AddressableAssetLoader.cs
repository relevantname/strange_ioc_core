#if UNITY_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using Com.Udogames.Core.Assets.Core;
using Com.Udogames.Core.Assets.Model;
using Com.Udogames.Core.DebugLogger;
using UnityEngine;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

namespace Com.Udogames.Core.Assets.Loaders
{
  /// <summary>
  /// Asset loader implementation that uses Unity's Addressable Asset System
  /// </summary>
  public class AddressableAssetLoader : IAddressableAssetLoader
  {
    #region Private Fields

    // Dictionary to track loaded assets and their handles
    private Dictionary<string, AssetInfo> _loadedAssets = new();

    // Queue for load operations to prevent concurrent dictionary modifications
    private Queue<QueuedOperation> _loadQueue = new();
    private bool _isProcessingQueue = false;

    private IAssetModuleInternal _module;

    #endregion

    #region Constructor

    /// <summary>
    /// Creates a new addressable asset loader
    /// </summary>
    public AddressableAssetLoader(IAssetModuleInternal module)
    {
      _module = module;
    }

    #endregion

    #region Public Methods

    #region Generic Assets

    /// <summary>
    /// Loads an addressable asset of the specified type with callback
    /// </summary>
    /// <typeparam name="T">UnityEngine.Object derived type to load (e.g., SpriteAtlas)</typeparam>
    /// <param name="key">Addressable key for the asset</param>
    /// <param name="callback">Callback to handle the loaded asset</param>
    /// <param name="retainReference">Whether to retain the reference in the Addressables system</param>
    public void LoadAsset<T>(string key, Action<T> callback, bool retainReference = true) where T : Object
    {
      if (callback == null) return;

      var operation = CreateLoadOperation(key, typeof(T), callback, retainReference);
      EnqueueOperation(operation);
    }

    #endregion

    #region Sprites

    /// <summary>
    /// Loads a sprite asset by addressable key with callback
    /// </summary>
    /// <param name="key">Addressable key for the sprite</param>
    /// <param name="callback">Callback to handle the loaded sprite</param>
    /// <param name="retainReference">Whether to retain the reference in the Addressables system</param>
    public void LoadSprite(string key, Action<Sprite> callback, bool retainReference = true)
    {
      if (callback == null) return;

      // Create operation with callback information
      var operation = CreateLoadOperation(key, typeof(Sprite), callback, retainReference);
      EnqueueOperation(operation);
    }

    #endregion

    #region Prefabs

    /// <summary>
    /// Loads a prefab asset by addressable key with callback
    /// </summary>
    /// <param name="key">Addressable key for the prefab</param>
    /// <param name="callback">Callback to handle the loaded prefab</param>
    /// <param name="retainReference">Whether to retain the reference in the Addressables system</param>
    public void LoadPrefab(string key, Action<GameObject> callback, bool retainReference = true)
    {
      if (callback == null) return;

      // Create operation with callback information
      var operation = CreateLoadOperation(key, typeof(GameObject), callback, retainReference);
      EnqueueOperation(operation);
    }

    /// <summary>
    /// Preloads multiple prefabs by their addressable keys and returns them all in one callback
    /// </summary>
    /// <param name="keys">Collection of prefab keys to load</param>
    /// <param name="onComplete">Callback that provides a dictionary of loaded prefabs keyed by their addressable keys</param>
    /// <param name="retainReference">Whether to retain the references in the Addressables system</param>
    public void PreloadPrefabs(IEnumerable<string> keys, Action<Dictionary<string, GameObject>> onComplete, bool retainReference = true)
    {
      if (keys == null)
      {
        onComplete?.Invoke(new Dictionary<string, GameObject>());
        return;
      }

      // Create a queued operation for preloading prefabs
      var operation = new QueuedOperation(
        PreloadPrefabsCoroutine(keys, onComplete, retainReference),
        null,
        typeof(Dictionary<string, GameObject>),
        onComplete
      );

      EnqueueOperation(operation);
    }

    /// <summary>
    /// Coroutine to preload multiple prefabs
    /// </summary>
    private IEnumerator PreloadPrefabsCoroutine(IEnumerable<string> keys, Action<Dictionary<string, GameObject>> onComplete, bool retainReference)
    {
      // Create a copy to avoid enumeration issues
      HashSet<string> uniqueKeys = new HashSet<string>(keys);
      Dictionary<string, GameObject> loadedPrefabs = new Dictionary<string, GameObject>();

      if (uniqueKeys.Count == 0)
      {
        onComplete?.Invoke(loadedPrefabs);
        yield break;
      }

      int prefabsToLoad = uniqueKeys.Count;

      foreach (string key in uniqueKeys)
      {
        bool prefabLoaded = false;
        GameObject loadedPrefab = null;

        // Check if already loaded
        if (_loadedAssets.TryGetValue(key, out AssetInfo assetInfo) && assetInfo.isLoaded)
        {
          loadedPrefab = assetInfo.asset as GameObject;
          prefabLoaded = true;
        }
        else
        {
          // Need to load the prefab
          yield return LoadAssetCoroutine<GameObject>(key, prefab =>
          {
            loadedPrefab = prefab;
            prefabLoaded = true;
          }, retainReference);

          // Wait for the load to complete
          while (!prefabLoaded)
          {
            yield return null;
          }
        }

        // Add to dictionary of loaded prefabs
        if (loadedPrefab != null)
        {
          loadedPrefabs[key] = loadedPrefab;
        }

        prefabsToLoad--;
        if (prefabsToLoad == 0)
        {
          DebugX.Log($"AddressableAssetLoader: Preloaded {loadedPrefabs.Count}/{uniqueKeys.Count} prefabs","AssetModule");

          onComplete?.Invoke(loadedPrefabs);
          yield break;
        }
      }

      // Fallback in case something went wrong
      onComplete?.Invoke(loadedPrefabs);
    }

    /// <summary>
    /// Instantiates a prefab from an addressable key with callback
    /// </summary>
    /// <param name="key">Addressable key for the prefab</param>
    /// <param name="callback">Callback to handle the instantiated GameObject</param>
    /// <param name="position">Position to instantiate at</param>
    /// <param name="rotation">Rotation to instantiate with</param>
    public void InstantiatePrefab(string key, Action<GameObject> callback, Vector3 position = default, Quaternion rotation = default)
    {
      if (callback == null) return;

      // Create a queued operation that includes the callback
      var operation = new QueuedOperation(
        InstantiatePrefabCoroutine(key, callback, position, rotation),
        key,
        typeof(GameObject),
        callback
      );

      EnqueueOperation(operation);
    }

    private IEnumerator InstantiatePrefabCoroutine(string key, Action<GameObject> callback, Vector3 position, Quaternion rotation)
    {
#if UNITY_ADDRESSABLES
      AsyncOperationHandle<GameObject> handle = default;

      try
      {
        handle = Addressables.InstantiateAsync(key, position, rotation);
      }
      catch (Exception ex)
      {
        DebugX.LogError($"AddressableAssetLoader: Error starting instantiation of prefab with key {key}: {ex.Message}");
        callback?.Invoke(null);
        yield break;
      }

      // Wait for the operation to complete
      while (!handle.IsDone)
      {
        yield return null;
      }

      try
      {
        // Check if the operation was successful
        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
          DebugX.LogError($"AddressableAssetLoader: Failed to instantiate prefab with key {key}");
          Addressables.Release(handle);
          callback?.Invoke(null);
          yield break;
        }

        callback?.Invoke(handle.Result);
      }
      catch (Exception ex)
      {
        DebugX.LogError($"AddressableAssetLoader: Error processing instantiated prefab with key {key}: {ex.Message}");
        if (handle.IsValid())
        {
          Addressables.Release(handle);
        }

        callback?.Invoke(null);
      }
#else
      yield return null;
#endif
    }

    #endregion

    #region Queue Processing

    /// <summary>
    /// Creates a load operation with associated metadata
    /// </summary>
    private QueuedOperation CreateLoadOperation<T>(string key, Type assetType, Action<T> callback, bool retainReference) where T : Object
    {
      DebugX.Log("AddressableAssetLoader.CreateLoadOperation key: " + key,"AssetModule");
      return new QueuedOperation(
        LoadAssetCoroutine(key, callback, retainReference),
        key,
        assetType,
        callback
      );
    }

    /// <summary>
    /// Adds a loading operation to the queue and starts processing if not already running
    /// </summary>
    private void EnqueueOperation(QueuedOperation operation)
    {
      _loadQueue.Enqueue(operation);

      // Start processing the queue if not already running
      if (!_isProcessingQueue)
      {
        _isProcessingQueue = true;
        _module.Runner.RunCoroutine(ProcessLoadQueue());
      }
    }

    /// <summary>
    /// Processes queued load operations sequentially to avoid collection modification issues
    /// </summary>
    private IEnumerator ProcessLoadQueue()
    {
      while (_loadQueue.Count > 0)
      {
        QueuedOperation operation = _loadQueue.Dequeue();
        yield return _module.Runner.RunCoroutineAndWait(operation.coroutine);
      }

      _isProcessingQueue = false;
    }

    #endregion

    /// <summary>
    /// Generic method to load any type of addressable asset
    /// </summary>
    /// <typeparam name="T">Type of asset to load</typeparam>
    /// <param name="key">Addressable key for the asset</param>
    /// <param name="callback">Callback to handle the loaded asset</param>
    /// <param name="retainReference">Whether to retain the reference in the Addressables system</param>
    private IEnumerator LoadAssetCoroutine<T>(string key, Action<T> callback, bool retainReference) where T : Object
    {
#if UNITY_ADDRESSABLES
      if (string.IsNullOrEmpty(key))
      {
        DebugX.LogError($"AddressableAssetLoader: Cannot load {typeof(T).Name} with empty key","AssetModule");
        callback?.Invoke(null);
        yield break;
      }

      // Check if the asset is already loaded
      if (_loadedAssets.TryGetValue(key, out AssetInfo assetInfo) && assetInfo.isLoaded)
      {
        assetInfo.lastAccessed = DateTime.UtcNow;
        callback?.Invoke(assetInfo.asset as T);
        yield break;
      }

      AsyncOperationHandle<T> handle = default;
      T asset = null;

      try
      {
        // Start the async operation to load the asset
        handle = Addressables.LoadAssetAsync<T>(key);
      }
      catch (Exception ex)
      {
        DebugX.LogError($"AddressableAssetLoader: Error starting load of {typeof(T).Name} with key {key}: {ex.Message}","AssetModule");
        callback?.Invoke(null);
        yield break;
      }

      // Wait for the operation to complete
      while (!handle.IsDone)
      {
        yield return null;
      }

      try
      {
        // Check if the operation was successful
        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
          DebugX.LogError($"AddressableAssetLoader: Failed to load {typeof(T).Name} with key {key}","AssetModule");
          Addressables.Release(handle);
          callback?.Invoke(null);
          yield break;
        }

        asset = handle.Result;

        // Add to loaded assets dictionary
        _loadedAssets[key] = new AssetInfo
        {
          key = key,
          asset = asset,
          operationHandle = handle,
          isLoaded = true,
          lastAccessed = DateTime.UtcNow
        };

        DebugX.Log($"AddressableAssetLoader: Loaded {typeof(T).Name} with key {key}","AssetModule");

        callback?.Invoke(asset);
      }
      catch (Exception ex)
      {
        DebugX.LogError($"AddressableAssetLoader: Error processing {typeof(T).Name} with key {key}: {ex.Message}","AssetModule");
        if (handle.IsValid())
        {
          Addressables.Release(handle);
        }

        callback?.Invoke(null);
      }
#else
      yield return null;
#endif
    }

    /// <summary>
    /// Preloads multiple sprites by their addressable keys
    /// </summary>
    /// <param name="keys">Collection of addressable keys to preload</param>
    /// <param name="onComplete">Callback when all sprites are preloaded</param>
    public void PreloadSprites(IEnumerable<string> keys, Action onComplete = null)
    {
      if (keys == null)
      {
        onComplete?.Invoke();
        return;
      }

      // Queue the preload operation to ensure it runs after current operations
      var operation = new QueuedOperation(
        PreloadSpritesCoroutine(keys, onComplete),
        null,
        typeof(IEnumerable<Sprite>),
        onComplete
      );

      EnqueueOperation(operation);
    }

    private IEnumerator PreloadSpritesCoroutine(IEnumerable<string> keys, Action onComplete)
    {
      List<string> keyList = new List<string>(keys);
      int totalCount = keyList.Count;
      int loadedCount = 0;

      foreach (string key in keyList)
      {
        // Create a wrapped loader that updates our state when the sprite is loaded
        bool isLoading = true;
        Action<Sprite> wrappedCallback = (sprite) =>
        {
          isLoading = false;
          loadedCount++;
        };

        // Use the load coroutine directly to avoid queue-within-queue
        yield return LoadAssetCoroutine<Sprite>(key, wrappedCallback, true);

        // Wait for the sprite to finish loading (already done by the coroutine)
        while (isLoading)
        {
          yield return null;
        }
      }

      DebugX.Log($"AddressableAssetLoader: Preloaded {loadedCount}/{totalCount} sprites","AssetModule");

      onComplete?.Invoke();
    }

    /// <summary>
    /// Unloads an asset by its addressable key
    /// </summary>
    /// <param name="key">Addressable key of the asset to unload</param>
    public void UnloadAsset(string key)
    {
      if (_loadedAssets.TryGetValue(key, out AssetInfo assetInfo) && assetInfo.isLoaded)
      {
        var operation = new QueuedOperation(
          UnloadAssetCoroutine(key),
          key,
          null,
          null
        );
        EnqueueOperation(operation);
      }
    }

    private IEnumerator UnloadAssetCoroutine(string key)
    {
#if UNITY_ADDRESSABLES
      if (_loadedAssets.TryGetValue(key, out AssetInfo assetInfo) && assetInfo.isLoaded)
      {
        // Release the handle in the Addressables system
        Addressables.Release(assetInfo.operationHandle);

        // Update the asset info
        assetInfo.asset = null;
        assetInfo.isLoaded = false;

        DebugX.Log($"AddressableAssetLoader: Unloaded asset with key {key}","AssetModule");
      }

#endif
      yield break;
    }

    /// <summary>
    /// Clears all assets loaded by this loader
    /// </summary>
    public void ClearAll()
    {
      var operation = new QueuedOperation(
        ClearAllCoroutine(),
        null,
        null,
        null
      );
      EnqueueOperation(operation);
    }

    private IEnumerator ClearAllCoroutine()
    {
#if UNITY_ADDRESSABLES
      // Make a copy of the dictionary to prevent collection modification issues
      var assetsToRelease = new Dictionary<string, AssetInfo>(_loadedAssets);

      foreach (var pair in assetsToRelease)
      {
        if (pair.Value.isLoaded)
        {
          // Release the handle in the Addressables system
          Addressables.Release(pair.Value.operationHandle);
        }
      }

      _loadedAssets.Clear();

      DebugX.Log("AddressableAssetLoader: All assets cleared","AssetModule");
#endif
      yield break;
    }

    #endregion
  }
}