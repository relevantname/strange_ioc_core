using System;
using System.Collections;
using UnityEngine;

namespace Com.Udogames.Core.Assets.Core
{
  public interface IRemoteObjectLoader
  {
    /// <summary>
    /// Loads a sprite from a URL
    /// </summary>
    void LoadSprite(string url, Action<Sprite> callback, float pixelsPerUnit = 100f);

    /// <summary>
    /// Loads a sprite from a local file
    /// </summary>
    void LoadSpriteFromFile(string filePath, Action<Sprite> callback, float pixelsPerUnit = 100f);

    /// <summary>
    /// Loads a sprite from Resources folder
    /// </summary>
    IEnumerator LoadFromResourcesCoroutine(string path, Action<Sprite> callback);
  }
}