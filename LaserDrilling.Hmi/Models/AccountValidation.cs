using System.Text.RegularExpressions;

namespace LaserDrilling.Hmi.Models
{
    /// <summary>
    /// 集中校验用户账户信息，供界面与账户服务复用。
    /// </summary>
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
