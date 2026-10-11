# 公司标识与启动流程

公司名称暂用“XX激光”。本目录 company-logo.svg 是原创临时标识，不是该公司官方商标；可替换为正式授权素材。

- SVG 原稿：Assets/Brand/company-logo.svg。
- 透明 PNG：Assets/Brand/company-logo.png，256 × 256，便于其他页面或文档复用。
- WPF 矢量资源：Resources/Branding.xaml，资源键 CompanyLogo。
- 复用方式：`<Image Source="{StaticResource CompanyLogo}" Width="48" Height="48"/>`。
- 修改公司名、英文名、产品名和启动时长：Configuration/startup.json。SplashDurationSeconds 默认 2，支持 0～30 秒；配置随构建复制到输出目录。
- 启动流程：SplashView → LoginView → WelcomeView。启动页显示后开始计时。
- 演示账号 operator，密码 Demo@123。当前认证仅验证 UI 流程，不可作为生产认证；后续替换 IDemoAuthenticationService 实现。
- 登录页退出时清空密码，不保存凭据；当前不连接设备。
- 登录窗口为 760 × 540 DIP，无系统标题栏；顶部空白处可拖动，右上角支持最小化和关闭。进入系统后恢复普通可调整窗口，退出登录后恢复紧凑窗口。
- 右上角支持中文/英文和浅色/深色即时切换，选择在当前运行期间保留，重启默认中文浅色。
- 多语言与主题由 Services/AppearanceService.cs 集中维护。新页面文案使用 `{DynamicResource Text.LoginTitle}` 等键，颜色使用 `{DynamicResource SurfaceBrush}` 等键，勿写死业务文案或主题颜色。
