using System.Text.RegularExpressions;

namespace KLCodeNav
{
    public static class RegexNullSafe
    {
        public static bool IsMatch(string input, string pattern)
        {
            if (input == null || pattern == null)
            {
                return false;
            }

            return Regex.IsMatch(input, pattern);
        }
    }
}