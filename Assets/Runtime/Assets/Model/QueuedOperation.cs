using System;
using System.Collections;

namespace Com.Udogames.Core.Assets.Model
{
  internal class QueuedOperation
  {
    public IEnumerator coroutine { get; set; }
    public string key { get; set; }
    public Type assetType { get; set; }
    public Delegate callback { get; set; }

    public QueuedOperation(IEnumerator coroutine, string key = null, Type assetType = null, Delegate callback = null)
    {
      this.coroutine = coroutine;
      this.key = key;
      this.assetType = assetType;
      this.callback = callback;
    }
  }
}