using System;
using UnityEngine;

namespace Com.Udogames.Core.DebugLogger
{
  [Serializable]
  public class LoggerVo
  {
    public string tag;
    
    public Color color = Color.white;

    public bool active;

    public LoggerIgnoreKeyVo[] ignoreKeys;
  }
}