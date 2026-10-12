using Com.Udogames.Core.Assets.Utils;

namespace Com.Udogames.Core.Assets.Core
{
  public interface IAssetModuleInternal : IAssetModule
  {
    CoroutineRunner Runner { get; }
    
    IRemoteObjectLoader UrlObjectLoader { get; }
  }
}