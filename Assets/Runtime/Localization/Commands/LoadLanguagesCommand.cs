using Com.Udogames.Core.DebugLogger;
using strange.extensions.command.impl;
using strange.extensions.injector;

namespace Com.Udogames.Core.Localization.Commands
{
  public class LoadLanguagesCommand : EventCommand
  {
    [Inject]
    public ILocalizationModule localizationModule { get; set; }
    
    public override void Execute()
    {
      Retain();
      // set language
      localizationModule.InitialLoad().Then(() =>
      {
        Release();
      }).Catch(exception =>
      {
        DebugX.LogError(exception.Message,"LocalizationModule");
      });
    }
  }
}