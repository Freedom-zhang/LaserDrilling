# LaserDrilling

基于 **.NET 8、WPF 和 Prism** 开发的激光打孔设备上位机桌面应用，采用 MVVM 模式组织界面与交互逻辑。

项目目前处于界面原型与基础功能搭建阶段，已实现公司启动展示、演示登录、中英文切换和深浅主题切换。当前不连接真实设备，不包含实际激光加工控制功能。

## 当前功能

- **公司启动页**：展示公司标识与产品名称，默认显示 2 秒后进入登录页，支持配置显示时长。
- **无标题栏登录窗口**：760 × 540 DIP，支持拖动、最小化和关闭。
- **演示登录**：支持必填校验、账号密码验证、登录成功页面和退出登录；退出时清空密码。
- **多语言**：中文、英文即时切换，包含登录校验提示。
- **主题切换**：浅色、深色即时切换，使用应用共享颜色资源。
- **可复用标识**：提供 SVG、透明 PNG 和 WPF 矢量资源。

语言与主题选择在本次运行期间保留，重启后默认使用中文和浅色主题。登录成功后目前进入占位欢迎页，后续再接入业务模块。

## 开发环境

| 项目 | 配置 |
| --- | --- |
| 操作系统 | Windows，需支持 .NET 8 与 WPF |
| IDE | Visual Studio 2022，安装“.NET 桌面开发”工作负载 |
| 目标框架 | `net8.0-windows` |
| SDK | .NET 8 SDK 或支持该目标框架的更高版本 SDK |
| 当前验证环境 | Visual Studio 2022 17.14、.NET SDK 9.0.318、.NET 8 Windows Desktop Runtime |

本项目是 Windows 桌面程序，不能直接作为网页或在 Linux/macOS 上运行。使用命令行启动时，除 SDK 外还需安装 .NET 8 Windows Desktop Runtime。

## 主要技术与第三方库

| 技术 / 库 | 用途 |
| --- | --- |
| C# / .NET 8 | 应用开发与基础运行环境 |
| WPF / XAML | 桌面界面、数据绑定、样式与资源 |
| Prism.DryIoc **8.1.97** | MVVM、命令、依赖注入及 Region 页面导航 |
| DryIoc（由 Prism.DryIoc 引入） | 依赖注入容器 |
| System.Text.Json（.NET 内置） | 读取启动配置 |
| WPF DynamicResource | 运行时切换语言文案和主题颜色 |

当前没有引入数据库、CAD 解析库或设备厂商 SDK。

## 项目结构

```text
LaserDrilling/
├─ LaserDrilling.sln                 # Visual Studio 解决方案
├─ LaserDrilling.Hmi/
│  ├─ Assets/Brand/                  # Logo 原稿、PNG 与使用说明
│  ├─ Configuration/startup.json     # 公司信息及启动时长
│  ├─ Resources/                    # 品牌资源、公共控件样式
│  ├─ Services/                     # 启动配置、演示认证、语言与主题
│  ├─ ViewModels/                   # 页面状态与交互逻辑
│  ├─ Views/                        # 启动页、登录页、欢迎页、主窗口
│  └─ App.xaml                      # 应用资源与 Prism 启动入口
└─ work/StartupSmoke/               # 启动、登录及主题语言冒烟检查工具
```

目前为单个 WPF 应用项目，代码按职责分目录组织。设备通信、控制卡适配、加工流程与 Mock 设备服务尚未实现，后续可根据业务边界拆分为独立类库。

## 运行方式

### Visual Studio

1. 将仓库克隆到本地，打开 `LaserDrilling.sln`。
2. 等待 NuGet 自动还原；如未还原，右键解决方案选择“还原 NuGet 程序包”。
3. 将 `LaserDrilling.Hmi` 设置为启动项目。
4. 按 **F5** 调试运行，或按 **Ctrl + F5** 直接运行。
5. 启动页默认显示 2 秒后进入登录界面。

### 命令行

在仓库根目录执行：

```powershell
dotnet restore LaserDrilling.sln
dotnet build LaserDrilling.sln -c Debug
dotnet run --project LaserDrilling.Hmi/LaserDrilling.Hmi.csproj
```

### 演示账号

| 账号 | 密码 |
| --- | --- |
| `operator` | `Demo@123` |

此账号是源代码中公开的演示数据，仅用于验证界面流程，不代表正式用户身份，也不用于设备授权。正式使用前需替换 `IDemoAuthenticationService` 的实现并设计用户权限。

### 冒烟检查

在 Windows 图形桌面会话中执行：

```powershell
dotnet run --project work/StartupSmoke/StartupSmoke.csproj
```

检查工具会打开实际 WPF 窗口，验证启动跳转、登录校验、退出登录、窗口尺寸以及语言和主题切换。预览图写入 `work/previews/`；同时从矢量资源重新生成 `Assets/Brand/company-logo.png`。

## 配置与资源复用

编辑 `LaserDrilling.Hmi/Configuration/startup.json`：

```json
{
  "CompanyName": "星驰激光",
  "CompanyEnglishName": "STAR LASER",
  "ProductName": "激光打孔控制系统",
  "SplashDurationSeconds": 2
}
```

`SplashDurationSeconds` 支持 0～30 秒，默认 2 秒。配置在构建时复制到输出目录，重新启动后读取。`ProductName` 用于中文产品名称，英文产品名称目前在语言资源中定义。

- 语言文案与主题颜色：`Services/AppearanceService.cs`。
- 公共控件样式：`Resources/Theme.xaml`。
- WPF Logo：`Resources/Branding.xaml`，资源键 `CompanyLogo`。
- Logo 原始文件：`Assets/Brand/company-logo.svg`、`company-logo.png`。

WPF 页面可直接复用：

```xml
<Image Source="{StaticResource CompanyLogo}" Width="48" Height="48" />
<TextBlock Text="{DynamicResource Text.LoginTitle}"
           Foreground="{DynamicResource InkBrush}" />
```

## 项目状态与品牌说明

本项目是激光打孔设备 HMI 的开发原型，并非设备厂商发布的正式控制软件。公司名称为暂用展示内容；Logo 为临时原创设计，不是该公司的官方商标，可后续替换为正式素材。

仓库目前未指定开源许可证；第三方依赖遵循各自的许可条款。
