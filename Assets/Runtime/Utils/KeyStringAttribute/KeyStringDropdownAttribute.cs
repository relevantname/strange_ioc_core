using System;
using UnityEngine;

namespace Com.Udogames.Core.Utils.KeyStringAttribute
{
  [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
  public class KeyStringDropdownAttribute : PropertyAttribute
  {
    public Type SourceType { get; }

    public KeyStringDropdownAttribute(Type sourceType)
    {
      SourceType = sourceType;
    }
  }
}