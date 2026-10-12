using strange.extensions.command.impl;
using strange.extensions.injector;

namespace Com.Udogames.Core.UserInput
{
  public class SetupUserInputCommand : EventCommand
  {
    [Inject]
    public IUserInputModel userInputModel { get; set; }

    public override void Execute()
    {
      UserInput.Init(userInputModel);
    }
  }
}