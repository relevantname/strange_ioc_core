using System.Collections.Generic;
using Com.Udogames.Core.PromiseTool;

namespace Com.Udogames.Core.Localization
{
  public interface ILocalizationModule
  {
    string CurrentLanguageCode { get; }
    string GetText(string key, object[] args = null);

    void AddText(string key, string value);

    IPromise SetLanguageByCode(string code);

    string GetDefaultLanguageCode();

    IPromise InitialLoad();

    List<string> GetLanguageCodeList();

    List<string> GetLanguageNameList();
  }
}