using System;
using System.Collections.Generic;
using Com.Udogames.Core.DebugLogger;
using Com.Udogames.Core.Localization.Enums;
using Com.Udogames.Core.Localization.Utils;
using Com.Udogames.Core.PromiseTool;
using strange.extensions.context.api;
using strange.extensions.dispatcher.eventdispatcher.api;
using strange.extensions.injector;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Com.Udogames.Core.Localization.Imp
{
  public class LocalLocalizationModule : ILocalizationModule,IDisposable
  {
    private string _defaultLanguageCode;

    private Promise _initialPromise;

    private readonly List<string> _languageCodeList = new();

    private readonly List<string> _languageNameList = new();

    private Promise _loadPromise;

    private StringTable _table;

    [Inject(ContextKeys.CROSS_CONTEXT_DISPATCHER)]
    public IEventDispatcher dispatcher { get; set; }
    
    public string GetText(string key, object[] args = null)
    {
      if (_table == null)
      {
        // DebugX.LogError("No language file loaded yet.","LocalizationModule");
        return key;
      }

      if (string.IsNullOrEmpty(key)) return "";

      var cleanKey = key.Trim();

      var entry = _table.GetEntry(cleanKey);
      if (entry == null)
        return key;

      return entry.GetLocalizedString().Trim();
    }

    public void AddText(string key, string value)
    {
      throw new NotImplementedException();
    }

    public string CurrentLanguageCode => LocalizationSettings.SelectedLocale.Identifier.Code;

    public IPromise SetLanguageByCode(string code)
    {
      if (_loadPromise != null)
        _loadPromise.Cancel();

      _loadPromise = new Promise();

      var index = _languageCodeList.IndexOf(code);
      if (index < 0)
      {
        _loadPromise.Reject(new Exception("Language not found with code " + code));
        return _loadPromise;
      }

      var locale = LocalizationSettings.AvailableLocales.Locales[index];
      LocalizationSettings.SelectedLocale = locale;

      return _loadPromise;
    }

    public string GetDefaultLanguageCode()
    {
      return _defaultLanguageCode;
    }

    public IPromise InitialLoad()
    {
      if (_initialPromise != null)
        _initialPromise.Cancel();

      _initialPromise = new Promise();

      AsyncOperationHandle operationHandle = LocalizationSettings.SelectedLocaleAsync;
      if (operationHandle.IsDone)
        InitializeCompleted(operationHandle);
      else
        operationHandle.Completed += InitializeCompleted;

      return _initialPromise;
    }

    public List<string> GetLanguageCodeList()
    {
      return _languageCodeList;
    }

    public List<string> GetLanguageNameList()
    {
      return _languageNameList;
    }

    [PostConstruct]
    public void OnPostConstruct()
    {
      _defaultLanguageCode = LanguageUtility.Get2LetterIsoCodeFromSystemLanguage();
      LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
    }

    private void InitializeCompleted(AsyncOperationHandle obj)
    {
      var locales = LocalizationSettings.AvailableLocales.Locales;
      for (var i = 0; i < locales.Count; ++i)
      {
        var locale = locales[i];

        var displayName = locales[i].Identifier.CultureInfo != null ? locales[i].Identifier.CultureInfo.NativeName : locales[i].ToString();
        _languageNameList.Add(displayName);
        _languageCodeList.Add(locale.Identifier.Code);
      }

      LoadLangFile().Then(() => { _initialPromise.Resolve(); }).Catch(_initialPromise.Reject);
    }

    private void OnSelectedLocaleChanged(Locale locale)
    {
      DebugX.Log("OnSelectedLocaleChanged " + locale.Identifier.Code,"LocalizationModule");
      LoadLangFile().Then(() => DebugX.Log(locale.Identifier.Code + " language loaded.","LocalizationModule")).Catch(exception =>
      {
        DebugX.LogError(exception.Message,"LocalizationModule");
      });
    }

    public IPromise LoadLangFile()
    {
      if (_loadPromise != null)
        _loadPromise.Cancel();

      _loadPromise = new Promise();

      var loadingOperation = LocalizationSettings.StringDatabase.GetTableAsync("Main");

      loadingOperation.Completed += handle =>
      {
        if (handle.IsDone)
        {
          _table = handle.Result;
          dispatcher.Dispatch(LocalizationEvent.LanguageChanged);
          _loadPromise.Resolve();
          return;
        }

        _loadPromise.Reject(handle.OperationException);
      };

      return _loadPromise;
    }


    public void Dispose()
    {
      LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
    }
  }
}