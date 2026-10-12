using System;
using System.Collections.Generic;
using UnityEngine;

namespace Com.Udogames.Core.Assets.Core
{
  public interface IAddressableAssetLoader
  {
    /// <summary>
    /// Loads an addressable asset of the specified type with callback
    /// </summary>
    /// <typeparam name="T">UnityEngine.Object derived type to load (e.g., SpriteAtlas)</typeparam>
    /// <param name="key">Addressable key for the asset</param>
    /// <param name="callback">Callback to handle the loaded asset</param>
    /// <param name="retainReference">Whether to retain the reference in the Addressables system</param>
    void LoadAsset<T>(string key, Action<T> callback, bool retainReference = true) where T : UnityEngine.Object;

    /// <summary>
    /// Loads a sprite asset by addressable key with callback
    /// </summary>
    /// <param name="key">Addressable key for the sprite</param>
    /// <param name="callback">Callback to handle the loaded sprite</param>
    /// <param name="retainReference">Whether to retain the reference in the Addressables system</param>
    void LoadSprite(string key, Action<Sprite> callback, bool retainReference = true);

    /// <summary>
    /// Loads a prefab asset by addressable key with callback
    /// </summary>
    /// <param name="key">Addressable key for the prefab</param>
    /// <param name="callback">Callback to handle the loaded prefab</param>
    /// <param name="retainReference">Whether to retain the reference in the Addressables system</param>
    void LoadPrefab(string key, Action<GameObject> callback, bool retainReference = true);

    /// <summary>
    /// Preloads multiple prefabs by their addressable keys and returns them all in one callback
    /// </summary>
    /// <param name="keys">Collection of prefab keys to load</param>
    /// <param name="onComplete">Callback that provides a dictionary of loaded prefabs keyed by their addressable keys</param>
    /// <param name="retainReference">Whether to retain the references in the Addressables system</param>
    void PreloadPrefabs(IEnumerable<string> keys, Action<Dictionary<string, GameObject>> onComplete, bool retainReference = true);

    /// <summary>
    /// Instantiates a prefab from an addressable key with callback
    /// </summary>
    /// <param name="key">Addressable key for the prefab</param>
    /// <param name="callback">Callback to handle the instantiated GameObject</param>
    /// <param name="position">Position to instantiate at</param>
    /// <param name="rotation">Rotation to instantiate with</param>
    void InstantiatePrefab(string key, Action<GameObject> callback, Vector3 position = default, Quaternion rotation = default);

    /// <summary>
    /// Preloads multiple sprites by their addressable keys
    /// </summary>
    /// <param name="keys">Collection of addressable keys to preload</param>
    /// <param name="onComplete">Callback when all sprites are preloaded</param>
    void PreloadSprites(IEnumerable<string> keys, Action onComplete = null);

    /// <summary>
    /// Unloads an asset by its addressable key
    /// </summary>
    /// <param name="key">Addressable key of the asset to unload</param>
    void UnloadAsset(string key);

    /// <summary>
    /// Clears all assets loaded by this loader
    /// </summary>
    void ClearAll();
  }
}