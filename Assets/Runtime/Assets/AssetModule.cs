using System;
using System.Collections.Generic;
using Com.Udogames.Core.Assets.Core;
using Com.Udogames.Core.Assets.Loaders;
using Com.Udogames.Core.Assets.Utils;
using Com.Udogames.Core.DebugLogger;
using Com.Udogames.Core.PromiseTool;
using strange.extensions.context.api;
using strange.extensions.injector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.U2D;

namespace Com.Udogames.Core.Assets
{
  public class AssetModule : IAssetModuleInternal, IDisposable
  {
    [Inject(ContextKeys.CONTEXT_VIEW)]
    public GameObject contextView { get; set; }

    public static string cacheFolderName { get; set; } = "UdoCore_AssetCache";

    public static int maxCachedAssets { get; set; } = 100;

    private IAddressableAssetLoader _addressableAssetLoader;

    public IAddressableAssetLoader AddressableAssetLoader => _addressableAssetLoader;

    private IAssetLoader _assetLoader;

    public IAssetLoader AssetLoader => _assetLoader;

    private CoroutineRunner _coroutineRunner;

    public CoroutineRunner Runner => _coroutineRunner;

    private IRemoteObjectLoader _urlObjectLoader;

    public IRemoteObjectLoader UrlObjectLoader => _urlObjectLoader;

    private bool _inited;

    [PostConstruct]
    public void OnPostConstruct()
    {
      Init();
    }

    #region Sprite Atlas Loading

    private static Dictionary<string, Action<SpriteAtlas>> _waitRegisterQueue = new();

    private static IAssetModule _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void SpriteAtlasFixer()
    {
      SpriteAtlasManager.atlasRegistered += OnSpriteAtlasRegistered;
      SpriteAtlasManager.atlasRequested += OnSpriteAtlasRequested;
    }

    private static void OnSpriteAtlasRequested(string tag, Action<SpriteAtlas> callback)
    {
      // DebugX.Log("OnAtlasRequested " + tag, "AssetModule");
      if (!_waitRegisterQueue.ContainsKey(tag))
      {
        _waitRegisterQueue.Add(tag, callback);
      }

      if (_instance != null)
        _instance.LoadAssetAsync<SpriteAtlas>(tag).Then(callback);
    }

    private static void OnSpriteAtlasRegistered(SpriteAtlas atlas)
    {
      // DebugX.Log("Registered " + atlas.name, "AssetModule");
      _waitRegisterQueue.Remove(atlas.name);
    }

    #endregion


    public void Init()
    {
      if (_inited)
        return;

      _coroutineRunner = CoroutineRunner.GetOrCreate(contextView);

      _urlObjectLoader = new UrlObjectLoader(this);
      _addressableAssetLoader = new AddressableAssetLoader(this);
      _assetLoader = new AssetLoader(this);
      _assetLoader.ClearCache();

      if (_instance == null)
      {
        _instance = this;
        foreach (KeyValuePair<string, Action<SpriteAtlas>> pair in _waitRegisterQueue)
        {
          _instance.LoadAssetAsync<SpriteAtlas>(pair.Key).Then(pair.Value);
        }
      }
      

      _inited = true;
    }

    public IPromise<T> LoadAssetAsync<T>(string key)
    {
      Promise<T> promise = new();
      var op = Addressables.LoadAssetAsync<T>(key);
      op.Completed += handle =>
      {
        if (handle.Status == AsyncOperationStatus.Succeeded)
          promise.Resolve(handle.Result);
        else
          promise.Reject(handle.OperationException);
      };
      return promise;
    }

    public IPromise<Sprite> LoadSprite(string key)
    {
      Promise<Sprite> p = new Promise<Sprite>();

      if (string.IsNullOrEmpty(key))
        return p;
      
      AssetLoader.LoadSprite(key, (sprite) => { p.Resolve(sprite); });

      return p;
    }

    public void Dispose()
    {
      // DebugX.Log("Disposed", "AssetModule");
      _instance = null;
      SpriteAtlasManager.atlasRegistered -= OnSpriteAtlasRegistered;
      SpriteAtlasManager.atlasRequested -= OnSpriteAtlasRequested;
    }
  }
}