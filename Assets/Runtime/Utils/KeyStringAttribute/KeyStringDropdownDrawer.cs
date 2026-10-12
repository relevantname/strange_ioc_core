#if UNITY_EDITOR

using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Com.Udogames.Core.Utils.KeyStringAttribute
{
  [CustomPropertyDrawer(typeof(KeyStringDropdownAttribute))]
  public class KeyStringDropdownDrawer : PropertyDrawer
  {
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
      var dropdownAttribute = (KeyStringDropdownAttribute)attribute;
      var type = dropdownAttribute.SourceType;

      // Static const string alanlarını yansıt
      var options = type
        .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
        .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
        .Select(f => f.GetValue(null).ToString())
        .ToArray();

      if (options.Length == 0)
      {
        EditorGUI.LabelField(position, label.text, $"No const strings in {type.Name}");
        return;
      }

      // mevcut değer bulunamazsa, onu kaybetmemek için "(missing)" seçeneği olarak göster
      int foundIndex = Array.IndexOf(options, property.stringValue);
      if (foundIndex < 0)
      {
        var displayOptions = new string[options.Length + 1];
        displayOptions[0] = string.IsNullOrEmpty(property.stringValue)
          ? "<none> (missing)"
          : $"{property.stringValue} (missing)";
        Array.Copy(options, 0, displayOptions, 1, options.Length);

        int newIndex = EditorGUI.Popup(position, label.text, 0, displayOptions);
        // 0 index, mevcut (missing) değer; kullanıcı başka bir şey seçerse güncelle
        if (newIndex > 0)
        {
          property.stringValue = options[newIndex - 1];
        }
      }
      else
      {
        int newIndex = EditorGUI.Popup(position, label.text, foundIndex, options);
        if (newIndex >= 0 && newIndex < options.Length && newIndex != foundIndex)
        {
          property.stringValue = options[newIndex];
        }
      }
    }
  }
}

#endif