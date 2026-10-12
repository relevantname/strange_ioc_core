using System;
using TMPro;
using UnityEngine;

namespace Com.Udogames.Core.Localization.Views
{
  [RequireComponent(typeof(TextMeshPro))]
  public class TranslateMesh : MonoBehaviour,ITranslate
  {
    [field: SerializeField]
    public string key { get; set; }
    
    private TextMeshPro _text;
    
    public Action<ITranslate> onKeyChanged { get; set; }

    public void Init()
    {
      _text = gameObject.GetComponent<TextMeshPro>();
      if (_text == null) return;
      if (key == string.Empty)
        key = gameObject.name;

      if (key == string.Empty)
        key = _text.text;
    }

    public void SetText(string value)
    {
      if (_text == null)
        return;

      _text.text = value;
    }

    public void SetKey(string value)
    {
      key = value;
      onKeyChanged?.Invoke(this);
    }
  }
}