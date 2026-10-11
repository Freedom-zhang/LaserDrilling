using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Prism.Mvvm;

namespace LaserDrilling.Hmi.Services
{
    // Shared application resources: new pages can reuse the same text and color keys.
    public sealed class AppearanceService : BindableBase
    {
        public bool IsEnglish { get; private set; }
        public bool IsDark { get; private set; }
        private readonly Dictionary<string, string[]> _texts = new Dictionary<string, string[]>
        {
            ["LoginTitle"] = new[] { "系统登录", "Sign in" },
            ["LoginSubtitle"] = new[] { "请输入账号和密码，进入系统。", "Enter your credentials to continue." },
            ["Account"] = new[] { "账号", "Account" },
            ["Password"] = new[] { "密码", "Password" },
            ["SignIn"] = new[] { "登 录", "Sign in" },
            ["SigningIn"] = new[] { "正在验证…", "Signing in…" },
            ["DemoTitle"] = new[] { "本地演示账号", "Local demo account" },
            ["DemoNote"] = new[] { "仅用于界面演示，未接入正式认证。", "UI demonstration only. No production authentication." },
            ["Offline"] = new[] { "未连接真实硬件", "Hardware disconnected" },
            ["Product"] = new[] { "激光打孔控制系统", "Laser drilling control system" },
            ["Tagline"] = new[] { "精密制造 · 专注每一个孔位", "Precision in every hole" },
            ["DemoVersion"] = new[] { "V0.1 / 本地演示", "V0.1 / Local demo" },
            ["Loading"] = new[] { "正在进入登录界面…", "Opening sign-in…" },
            ["SplashError"] = new[] { "登录界面加载失败，请重新启动。", "Unable to open sign-in. Please restart." },
            ["AccountRequired"] = new[] { "请输入账号。", "Enter your account." },
            ["PasswordRequired"] = new[] { "请输入密码。", "Enter your password." },
            ["InvalidCredentials"] = new[] { "账号或密码不正确。", "Incorrect account or password." },
            ["NavigationError"] = new[] { "系统界面加载失败，请重试。", "Unable to open the system. Try again." },
            ["ServiceError"] = new[] { "登录服务暂不可用，请重试。", "Sign-in is unavailable. Try again." },
            ["Success"] = new[] { "登录成功", "Signed in" },
            ["Ready"] = new[] { "启动与登录流程已就绪", "Startup and sign-in are ready" },
            ["NextModules"] = new[] { "后续在此接入激光打孔操作界面与设备业务模块。", "Device controls and drilling modules will be added here." },
            ["SignOut"] = new[] { "退出登录", "Sign out" },
            ["Close"] = new[] { "关闭", "Close" },
            ["Minimize"] = new[] { "最小化", "Minimize" },
            ["Language"] = new[] { "切换语言", "Change language" },
            ["Theme"] = new[] { "切换主题", "Change theme" }
        };
        public string Text(string key) => string.IsNullOrEmpty(key) ? "" : _texts[key][IsEnglish ? 1 : 0];
        public void SetProductName(string name) => _texts["Product"][0] = name;
        public void SetLanguage(bool english)
        {
            IsEnglish = english;
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(english ? "en-US" : "zh-CN");
            HandyControl.Tools.ConfigHelper.Instance.SetLang(english ? "en-US" : "zh-CN");
            foreach (var pair in _texts) Application.Current.Resources["Text." + pair.Key] = Text(pair.Key);
            Application.Current.Resources["Text.ThemeChoice"] = IsDark ? (english ? "Light" : "浅色") : (english ? "Dark" : "深色");
            RaisePropertyChanged(nameof(IsEnglish));
        }
        public void SetTheme(bool dark)
        {
            IsDark = dark;
            // Replace only the HandyControl skin, retaining the app's own resource precedence.
            var dictionaries = Application.Current.Resources.MergedDictionaries;
            for (var i = 0; i < dictionaries.Count; i++)
            {
                var source = dictionaries[i].Source?.OriginalString;
                if (source != null && source.Contains("HandyControl;component/Themes/Skin"))
                {
                    var skin = dark ? "SkinDark" : "SkinDefault";
                    var uri = new System.Uri($"pack://application:,,,/HandyControl;component/Themes/{skin}.xaml");
                    if (dictionaries[i].Source != uri) dictionaries[i] = new ResourceDictionary { Source = uri };
                    break;
                }
            }
            SetBrush("InkBrush", dark ? "#E4EDF3" : "#203D52");
            SetBrush("MutedBrush", dark ? "#A1B4C3" : "#6B7E8C");
            SetBrush("AccentBrush", dark ? "#208A98" : "#187F8D");
            SetBrush("LineBrush", dark ? "#3C5162" : "#D5DFE5");
            SetBrush("PageBrush", dark ? "#172633" : "#F4F7FA");
            SetBrush("SurfaceBrush", dark ? "#203342" : "#FFFFFF");
            SetBrush("InputBrush", dark ? "#152532" : "#FAFCFD");
            SetBrush("SubtleBrush", dark ? "#293E4E" : "#EBF0F4");
            SetBrush("ErrorBrush", dark ? "#FFABA3" : "#B23732");
            Application.Current.Resources["Text.ThemeChoice"] = dark ? (IsEnglish ? "Light" : "浅色") : (IsEnglish ? "Dark" : "深色");
            RaisePropertyChanged(nameof(IsDark));
        }
        private static void SetBrush(string key, string color)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
            brush.Freeze();
            Application.Current.Resources[key] = brush;
        }
    }
}
