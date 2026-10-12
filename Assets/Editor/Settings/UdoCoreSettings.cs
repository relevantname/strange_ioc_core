using UnityEditor;

namespace Com.Udogames.Core.Editor.Settings
{
  [FilePath("ProjectSettings/UdoCoreSettings.asset", FilePathAttribute.Location.ProjectFolder)]
  public class UdoCoreSettings : ScriptableSingleton<UdoCoreSettings>
  {
    public bool multiplayer;

    public void Save()
    {
      Save(true);
    }
  }
}