using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Prism.Mvvm;

namespace LaserDrilling.Hmi.Services
{
    /// <summary>
    /// 管理应用语言和主题资源，同步第三方控件库的外观设置。
    /// </summary>
    public sealed class AppearanceService : BindableBase
    {
        public bool IsEnglish { get; private set; }
        public bool IsDark { get; private set; }
        public AppearanceService()
        {
            foreach (var entry in MachineVocabulary.All) _texts["Machine." + entry.Key] = entry.Value;
        }
        private readonly Dictionary<string, string[]> _texts = new Dictionary<string, string[]>
        {
            ["SetupTitle"] = new[] { "创建管理员", "Create administrator" },
            ["SetupHint"] = new[] { "首次运行：设置管理员账号和密码（至少 8 位）。", "First run: create an administrator. Password: 8+ characters." },
            ["SetupAction"] = new[] { "创建并登录", "Create and sign in" },
            ["ConfirmPassword"] = new[] { "确认密码", "Confirm password" },
            ["PasswordMismatch"] = new[] { "两次输入的密码不一致。", "Passwords do not match." },
            ["SetupCompleted"] = new[] { "管理员已创建，请重新打开登录页。", "Setup already completed. Reopen sign-in." },
            ["InvalidUserName"] = new[] { "账号需为 3–32 位字母、数字、下划线、点或短横线。", "Account: 3–32 letters, digits, underscores, dots or hyphens." },
            ["InvalidPassword"] = new[] { "密码长度必须大于8位,并使用大小写字母混入。", "The password length must be more than 8 characters and should include both uppercase and lowercase letters." },
            ["DuplicateUser"] = new[] { "此账号已存在（不区分大小写）。", "This account already exists (case insensitive)." },
            ["AccessDenied"] = new[] { "当前账号无权执行此操作。", "Your account cannot perform this action." },
            ["Administrator"] = new[] { "管理员", "Administrator" },
            ["Operator"] = new[] { "操作员", "Operator" },
            ["UserManagement"] = new[] { "用户管理", "User management" },
            ["NewAccount"] = new[] { "新增账号", "New account" },
            ["GrantAdmin"] = new[] { "授予管理员权限", "Grant administrator access" },
            ["CreateUser"] = new[] { "新增用户", "Create user" },
            ["UserCreated"] = new[] { "用户已创建，可以使用新账号登录。", "User created. The new account can now sign in." },
            ["StoreError"] = new[] { "保存失败，请检查数据目录或稍后重试。", "Save failed. Check the data folder or retry later." },
            ["ProcessSettings"] = new[] { "工艺参数", "Process parameters" },
            ["Power"] = new[] { "功率比例（%）", "Power (%)" },
            ["Frequency"] = new[] { "频率（kHz）", "Frequency (kHz)" },
            ["Pulses"] = new[] { "脉冲数量", "Pulse count" },
            ["SaveParameters"] = new[] { "保存参数", "Save parameters" },
            ["ParametersSaved"] = new[] { "参数已保存到本机，未下发设备。", "Parameters saved locally. Nothing sent to hardware." },
            ["InvalidParameters"] = new[] { "有效范围：功率 0–100，频率大于 0 且不超过 1000，脉冲数 1–1000000。", "Ranges: power 0–100, frequency >0 to 1000, pulses 1–1000000." },
            ["ReadOnlyHint"] = new[] { "操作员仅可查看；用户管理和参数修改需要管理员权限。", "Operators have read-only access. Changes require an administrator." },
            ["ParameterHint"] = new[] { "模拟工艺参数，范围仅用于界面校验，不代表设备规格。", "Simulated parameters. Validation ranges are not hardware specifications." },
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
            // 仅替换 HandyControl 皮肤，保留应用自定义资源的优先级。
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
            SetBrush("InkBrush", dark ? "#F1F5FA" : "#10283D");
            SetBrush("MutedBrush", dark ? "#C2D0DE" : "#42596B");
            SetBrush("AccentBrush", dark ? "#57D0EE" : "#006B91");
            SetBrush("ActionBrush", dark ? "#00698F" : "#006B91");
            SetBrush("LineBrush", dark ? "#637B91" : "#8196A8");
            SetBrush("PageBrush", dark ? "#101D2B" : "#DFE7EE");
            SetBrush("SurfaceBrush", dark ? "#203246" : "#FFFFFF");
            SetBrush("InputBrush", dark ? "#102030" : "#FFFFFF");
            SetBrush("SubtleBrush", dark ? "#314A61" : "#DCE6F0");
            SetBrush("ErrorBrush", dark ? "#FF9B96" : "#B8202A");
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
