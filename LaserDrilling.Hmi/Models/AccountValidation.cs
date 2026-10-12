using System.Text.RegularExpressions;

namespace LaserDrilling.Hmi.Models
{
    public static class AccountValidation
    {
        public static string Validate(string name, string password)
        {
            if (name == null || !Regex.IsMatch(name.Trim(), "\\A[a-zA-Z0-9_.-]{3,32}\\z")) return "InvalidUserName";
            if (password == null || password.Length < 8 || password.Length > 128) return "InvalidPassword";
            return null;
        }
    }
}
