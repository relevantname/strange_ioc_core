using System;

namespace Com.Udogames.Core.Localization.Views
{
  public interface ITranslate
  {
    string key { get; set; }
    Action<ITranslate> onKeyChanged { get; set; }

    void Init();

    void SetText(string value);

    void SetKey(string value);
  }
}