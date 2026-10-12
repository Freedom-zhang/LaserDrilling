# LaserDrilling

基于 **.NET 8、WPF 和 Prism** 开发的激光打孔设备上位机桌面应用，采用 MVVM 模式组织界面与交互逻辑。

项目目前处于界面原型与基础功能搭建阶段，已实现公司启动展示、本地多用户登录、角色权限、中英文切换和深浅主题切换。当前不连接真实设备，不包含实际激光加工控制功能。

## 当前功能

- **加工主页面**：三栏工业布局，单件孔位视图、XYZ 坐标、设备状态、工艺摘要及常驻事件日志。
- **Mock 加工流程**：回零、启动确认、暂停/继续、停止、逐孔进度、完成状态、开门故障、模拟急停及复位。
- **孔位编辑**：管理员可新增、删除和修改孔位 XY、孔径及目标深度；当前孔位仅保留本次登录会话。工艺参数继续保存到本地。

第一版设计和参考产品见 [主页面调研与设计](docs/MainPageDesign.md)。登录后点击“轴回零”并确认，完成后点击“启动加工”并确认，即可观看模拟过程。

- **公司启动页**：展示公司标识与产品名称，默认显示 2 秒后进入登录页，支持配置显示时长。
- **无标题栏登录窗口**：760 × 540 DIP，支持拖动、最小化和关闭。
- **本地多用户登录**：首次运行创建管理员，管理员可新增操作员或管理员账号；退出时清空密码和登录会话。
- **角色权限**：管理员可管理用户、保存工艺参数；操作员仅可查看参数。界面和业务服务双重校验权限。
- **多语言**：中文、英文即时切换，包含登录校验提示。
- **主题切换**：浅色、深色即时切换，使用应用共享颜色资源。
- **可复用标识**：提供 SVG、透明 PNG 和 WPF 矢量资源。

语言与主题选择在本次运行期间保留，重启后默认使用中文和浅色主题。登录成功后显示当前角色、工艺参数以及管理员专用的用户管理面板。

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
| HandyControl **3.5.1** | WPF 控件与主题资源，启动页使用 LoadingLine 加载动画 |
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

### 首次登录与权限

首次启动进入“创建管理员”，填写账号、密码及确认密码后创建并登录。账号为 3～32 位字母、数字、下划线、点或短横线；密码为 8～128 位。没有预置管理员密码，也没有可直接使用的旧演示账号。

管理员在“用户管理”中创建账号，默认角色为操作员，勾选“授予管理员权限”才会创建管理员。操作员不能新增用户或保存工艺参数；服务层也会拒绝这些调用。当前不包含删除用户、密码重置、禁用用户或权限审计。

账号及模拟工艺参数保存在 `%LOCALAPPDATA%\LaserDrilling\accounts.json`，因此同一 Windows 用户下重启会保留数据。密码使用随机盐和 PBKDF2-SHA256（210000 次迭代）保存哈希，不保存明文。写入采用临时文件替换并防止并发写入；账号文件损坏时不会自动重建管理员。

这是应用内的本地角色权限，不是对 Windows 账户或本机文件管理权限的隔离；具备数据文件修改权限的人仍能影响账户数据。部署真实设备前需结合受控 Windows 账户、目录权限、账号恢复和审计方案。工艺参数范围仅用于模拟界面校验，不代表实际设备加工规格，也不会下发硬件。

当前认证入口为 `IAccountService` / `LocalAccountService`。`DemoAuthenticationService.cs` 保留为历史教学示例，未注册到容器，不再参与登录。

### 冒烟检查

在 Windows 图形桌面会话中执行：

```powershell
dotnet run --project work/StartupSmoke/StartupSmoke.csproj
```

检查工具会打开实际 WPF 窗口，验证启动跳转、登录校验、退出登录、窗口尺寸以及语言和主题切换。预览图写入 `work/previews/`；同时从矢量资源重新生成 `Assets/Brand/company-logo.png`。

## 配置与资源复用

### HandyControl 接入

使用官方 NuGet 包 `HandyControl`。`App.xaml` 先加载控件库的 `SkinDefault.xaml` 和 `Theme.xaml`，再加载项目自己的资源以保留现有界面样式。

`AppearanceService` 统一同步控件库与应用的深浅主题、中英文设置。新增控件不需要另建主题管理器；项目自定义控件样式仍优先于库的默认样式。

在页面中添加命名空间即可使用：

```xml
xmlns:hc="https://handyorg.github.io/handycontrol"
```

```xml
<hc:LoadingLine Width="210" Height="8"
                Foreground="{DynamicResource AccentBrush}" />
```

参考：[HandyControl 官方文档](https://handyorg.github.io/handycontrol/quick_start/)。

### 启动配置

编辑 `LaserDrilling.Hmi/Configuration/startup.json`：

```json
{
  "CompanyName": "XX激光",
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

主界面采用独立品牌 / 用户 / 时钟顶栏，主页常驻设备连接、激光器和运动系统状态。工艺库支持按材质与厚度保存独立 JSON、默认模板自动生成、唯一当前工艺快照，以及可编辑的单孔功率曲线。

数据模型与模拟参数位于 `Models`，文件服务和 Mock 执行位于 `Services`；ViewModel 负责页面绑定和命令。参见 [工艺文件、模型分层与曲线说明](docs/RecipeFiles.md)。

本项目是激光打孔设备 HMI 的开发原型，并非设备厂商发布的正式控制软件。公司名称为暂用展示内容；Logo 为临时原创设计，不是该公司的官方商标，可后续替换为正式素材。

仓库目前未指定开源许可证；第三方依赖遵循各自的许可条款。
