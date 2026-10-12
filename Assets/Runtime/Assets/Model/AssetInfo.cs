using System;
using Object = UnityEngine.Object;

namespace Com.Udogames.Core.Assets.Model
{
    internal class AssetInfo
    {
        public string key { get; set; }
        public Object asset { get; set; }
        
        public object operationHandle { get; set; }
        public bool isLoaded { get; set; }
        
        public DateTime lastAccessed { get; set; }
    }
}