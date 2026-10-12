using Com.Udogames.Core.Localization.Enums;
using strange.extensions.injector;
using strange.extensions.mediation.impl;

namespace Com.Udogames.Core.Localization.Views
{
  public class TranslateMediator : EventMediator
  {
    [Inject]
    public ILocalizationModule localizationModule { get; set; }

    public override void OnRegister()
    {
      dispatcher.AddListener(LocalizationEvent.LanguageChanged, OnLanguageChanged);
    }

    public override void OnEnabled()
    {
      var translates = gameObject.GetComponentsInChildren<ITranslate>(true);
      for (var i = 0; i < translates.Length; i++)
      {
        var translate = translates[i];
        translate.Init();
        translate.SetText(localizationModule.GetText(translate.key));
        translate.onKeyChanged += OnKeyChanged;
      }
    }

    private void OnKeyChanged(ITranslate translateUi)
    {
      translateUi.SetText(localizationModule.GetText(translateUi.key));
    }

    private void OnLanguageChanged(object payload)
    {
      var translates = gameObject.GetComponentsInChildren<ITranslate>(true);
      for (var i = 0; i < translates.Length; i++)
      {
        var translate = translates[i];
        translate.SetText(localizationModule.GetText(translate.key));
      }
    }

    public override void OnRemove()
    {
      dispatcher.RemoveListener(LocalizationEvent.LanguageChanged, OnLanguageChanged);
    }
  }
}