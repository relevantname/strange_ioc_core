using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Com.Udogames.Core.Assets.Core;
using Com.Udogames.Core.DebugLogger;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace Com.Udogames.Core.Assets.Loaders
{
  /// <summary>
  /// Handles the loading of assets from URLs and files using coroutines
  /// </summary>
  public class UrlObjectLoader : IRemoteObjectLoader
  {
    private IAssetModuleInternal _module;

    // Cache of in-flight loading operations to avoid duplicate requests
    private Dictionary<string, List<Action<Sprite>>> _spriteLoadingQueue = new();

    public UrlObjectLoader(IAssetModuleInternal module)
    {
      _module = module;
    }

    /// <summary>
    /// Loads a sprite from a URL
    /// </summary>
    public void LoadSprite(string url, Action<Sprite> callback, float pixelsPerUnit = 100f)
    {
      if (string.IsNullOrEmpty(url))
      {
        DebugX.LogError("UrlObjectLoader: URL is null or empty","AssetModule");
        if (callback != null)
          _module.Runner.RunOnMainThread(() => callback(null));
        return;
      }

      // Check if this URL is already being loaded
      if (_spriteLoadingQueue.TryGetValue(url, out List<Action<Sprite>> callbacks))
      {
        // Add this callback to the existing queue
        if (callback != null)
          callbacks.Add(callback);
        return;
      }

      // Create a new queue for this URL
      _spriteLoadingQueue[url] = new List<Action<Sprite>>();
      if (callback != null)
        _spriteLoadingQueue[url].Add(callback);

      // Start loading using the CoroutineRunner
      _module.Runner.RunCoroutine(LoadSpriteCoroutine(url, pixelsPerUnit));
    }

    private IEnumerator LoadSpriteCoroutine(string url, float pixelsPerUnit)
    {
      DebugX.Log($"UrlObjectLoader: Loading sprite from URL: {url}","AssetModule");

      // Move request outside the using block to allow reassignment
      UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);

      // Add retry logic
      int maxRetries = 3;
      int retryCount = 0;
      bool success = false;

      try
      {
        while (!success && retryCount < maxRetries)
        {
          yield return request.SendWebRequest();

          if (request.result == UnityWebRequest.Result.Success)
          {
            success = true;
          }
          else
          {
            retryCount++;
            DebugX.LogWarning($"UrlObjectLoader: Failed to load from URL {url}: {request.error}, retry {retryCount}/{maxRetries}","AssetModule");

            if (retryCount < maxRetries)
            {
              // Wait before retrying
              yield return new WaitForSeconds(0.5f);
              request.Dispose();
              request = UnityWebRequestTexture.GetTexture(url);
            }
          }
        }

        Sprite sprite = null;

        if (success)
        {
          try
          {
            Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;

            if (texture != null)
            {
              sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), pixelsPerUnit);

              DebugX.Log($"UrlObjectLoader: Successfully loaded sprite from URL: {url}","AssetModule");
            }
            else
            {
              DebugX.LogError($"UrlObjectLoader: Downloaded texture is null from URL: {url}","AssetModule");
            }
          }
          catch (Exception ex)
          {
            DebugX.LogError($"UrlObjectLoader: Error creating sprite from URL {url}: {ex.Message}","AssetModule");
          }
        }
        else
        {
          DebugX.LogError($"UrlObjectLoader: Failed to load sprite after {maxRetries} attempts from URL {url}: {request.error}","AssetModule");
        }

        // Process all callbacks
        if (_spriteLoadingQueue.TryGetValue(url, out List<Action<Sprite>> callbacks))
        {
          foreach (var cb in callbacks)
          {
            cb?.Invoke(sprite);
          }

          // Remove from queue
          _spriteLoadingQueue.Remove(url);
        }
      }
      finally
      {
        // Make sure to dispose the request
        request.Dispose();
      }
    }

    /// <summary>
    /// Loads a sprite from a local file
    /// </summary>
    public void LoadSpriteFromFile(string filePath, Action<Sprite> callback, float pixelsPerUnit = 100f)
    {
      if (string.IsNullOrEmpty(filePath))
      {
        DebugX.LogError("UrlObjectLoader: File path is null or empty","AssetModule");
        if (callback != null)
          _module.Runner.RunOnMainThread(() => callback(null));
        return;
      }

      // Unique key for the file path
      string key = "file:" + filePath;

      // Check if this file is already being loaded
      if (_spriteLoadingQueue.TryGetValue(key, out List<Action<Sprite>> callbacks))
      {
        // Add this callback to the existing queue
        if (callback != null)
          callbacks.Add(callback);
        return;
      }

      // Create a new queue for this file
      _spriteLoadingQueue[key] = new List<Action<Sprite>>();
      if (callback != null)
        _spriteLoadingQueue[key].Add(callback);

      // Start loading
      _module.Runner.RunCoroutine(LoadSpriteFromFileCoroutine(filePath, key, pixelsPerUnit));
    }

    private IEnumerator LoadSpriteFromFileCoroutine(string filePath, string key, float pixelsPerUnit)
    {
      Sprite sprite = null;

      if (!File.Exists(filePath))
      {
        DebugX.LogWarning($"UrlObjectLoader: File does not exist: {filePath}","AssetModule");
      }
      else
      {
        try
        {
          byte[] fileData = File.ReadAllBytes(filePath);

          Texture2D texture = new Texture2D(2, 2);
          if (texture.LoadImage(fileData))
          {
            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
              new Vector2(0.5f, 0.5f), pixelsPerUnit);
          }
          else
          {
            Object.Destroy(texture);
            DebugX.LogError($"UrlObjectLoader: Failed to load image data from {filePath}","AssetModule");
          }
        }
        catch (Exception ex)
        {
          DebugX.LogError($"UrlObjectLoader: Error reading file: {ex.Message}","AssetModule");
        }
      }

      // Allow for one frame to ensure all Unity operations complete
      yield return null;

      // Process all callbacks
      if (_spriteLoadingQueue.TryGetValue(key, out List<Action<Sprite>> callbacks))
      {
        foreach (var cb in callbacks)
        {
          cb?.Invoke(sprite);
        }

        // Remove from queue
        _spriteLoadingQueue.Remove(key);
      }
    }

    /// <summary>
    /// Loads a sprite from Resources folder
    /// </summary>
    public IEnumerator LoadFromResourcesCoroutine(string path, Action<Sprite> callback)
    {
      string resourcePath = Path.GetFileNameWithoutExtension(path);
      Sprite sprite = Resources.Load<Sprite>(resourcePath);

      // Wait one frame to ensure Unity operations complete
      yield return null;

      callback?.Invoke(sprite);
    }
  }
}