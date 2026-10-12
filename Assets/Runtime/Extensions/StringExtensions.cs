namespace Com.Udogames.Core.Extensions
{
  public static class StringExtensions
  {
    public static string FirstCharToLower(this string input)
    {
      if (string.IsNullOrEmpty(input))
        return input;

      if (input.Length == 1)
        return input.ToLower();

      return char.ToLower(input[0]) + input.Substring(1);
    }
  }
}