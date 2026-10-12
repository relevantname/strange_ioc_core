using Com.Udogames.Core.Localization;

namespace Com.Udogames.Core.Timer
{
  public static class TimeFormatter
  {
    /// <summary>
    /// Converts seconds to a formatted string like "2d 3h 15m 12s".
    /// </summary>
    /// <param name="totalSeconds">Total time in seconds</param>
    /// <param name="loc">Function to localize unit symbols (optional)</param>
    public static string FormatTime(int totalSeconds, ILocalizationModule loc = null)
    {
      int days = totalSeconds / 86400;
      int hours = (totalSeconds % 86400) / 3600;
      int minutes = (totalSeconds % 3600) / 60;
      int seconds = totalSeconds % 60;

      string result = "";

      if (days > 0) result += $"{days}{(loc != null ? loc.GetText("d") : "d")} ";
      if (hours > 0 || days > 0) result += $"{hours}{(loc != null ? loc.GetText("h") : "h")} ";
      if (minutes > 0 || hours > 0 || days > 0) result += $"{minutes}{(loc != null ? loc.GetText("m") : "m")} ";
      result += $"{seconds}{(loc != null ? loc.GetText("s") : "s")}";

      return result.Trim();
    }
  }
}