using System.Collections.Generic;
using Com.Udogames.Core.PromiseTool;

namespace Com.Udogames.Core.Localization.Imp
{
  public class DummyLocalizationModule : ILocalizationModule
  {
    private Dictionary<string, string> _map = new();

    public string GetText(string key, object[] args = null)
    {
      string text = _map.GetValueOrDefault(key, key);

      if (args == null)
        return text;
      else
        return string.Format(text, args);
    }

    public void AddText(string key, string value)
    {
      _map.TryAdd(key, value);
    }

    public string CurrentLanguageCode { get; private set; }

    public IPromise SetLanguageByCode(string code)
    {
      Promise promise = new();

      CurrentLanguageCode = code;

      promise.Resolve();
      return promise;
    }

    public string GetDefaultLanguageCode()
    {
      return null;
    }

    public IPromise InitialLoad()
    {
      return Promise.Resolved();
    }

    public List<string> GetLanguageCodeList()
    {
      return null;
    }

    public List<string> GetLanguageNameList()
    {
      return null;
    }
  }
}