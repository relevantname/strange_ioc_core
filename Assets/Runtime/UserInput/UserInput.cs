using UnityEngine;

namespace Com.Udogames.Core.UserInput
{
  public class UserInput
  {
    private static IUserInputModel _model;
    
    public static void Init(IUserInputModel value)
    {
      _model = value;
    }
    
    public static bool IfClose()
    {
      return _model.IfClose();
    }
    
    public static bool IfPressed()
    {
      return _model.IfPressed();
    }
    
    public static bool IfReleased()
    {
      return _model.IfReleased();
    }
    
    public static Vector2 mousePosition
    {
      get
      {
        return _model.mousePosition;
      }
    }
  }
}