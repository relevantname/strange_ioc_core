using System;
using Com.Udogames.Core.Assets.Core;
using Com.Udogames.Core.PromiseTool;
using UnityEngine;

namespace Com.Udogames.Core.Assets
{
  public interface IAssetModule
  {
    IAssetLoader AssetLoader { get; }

    [Obsolete]
    IAddressableAssetLoader AddressableAssetLoader { get; }

    void Init();

    IPromise<T> LoadAssetAsync<T>(string key);

    IPromise<Sprite> LoadSprite(string key);
  }
}