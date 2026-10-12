using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LaserDrilling.Hmi;
using LaserDrilling.Hmi.Views;
using LaserDrilling.Hmi.Controls;
using LaserDrilling.Hmi.Components;
using LaserDrilling.Hmi.Navigation;
using LaserDrilling.Hmi.ViewModels;
using LaserDrilling.Hmi.Services;
using LaserDrilling.Hmi.Models;
using Prism.Ioc;

/// <summary>
/// 执行启动、登录及主要界面交互的自动化冒烟和回归检查。
/// </summary>
internal static class Program
{
    static System.Windows.Controls.Button FindButtonByTag(DependencyObject root, string tag)
    {
        if(root is System.Windows.Controls.Button button && button.Tag?.ToString()==tag)return button;
        for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++) { var found=FindButtonByTag(VisualTreeHelper.GetChild(root,i),tag); if(found!=null)return found; }
        return null;
    }
    [STAThread]
    static void Main()
    {
        var app = new TestApp();
        app.DispatcherUnhandledException += (_, e) => { File.WriteAllText("work/main-ui-error.txt", e.Exception.ToString()); };
        foreach (var uri in new[] { "HandyControl;component/Themes/SkinDefault.xaml", "HandyControl;component/Themes/Theme.xaml", "LaserDrilling.Hmi;component/Resources/Branding.xaml", "LaserDrilling.Hmi;component/Resources/Theme.xaml", "LaserDrilling.Hmi;component/Resources/HmiIcons.xaml" })
            app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/" + uri) });
        app.Startup += (_, _) => app.Dispatcher.BeginInvoke(new Action(async () =>
        {
            try
            {
                var window = app.MainWindow;
                var timer = Stopwatch.StartNew();
                await Task.Delay(100);
                Require(Find<SplashView>(window) != null, "Startup view visible");
                Require(Find<HandyControl.Controls.LoadingLine>(window)?.IsRunning == true, "HandyControl loading animation active");
                Capture(window, "startup-preview.png");
                while (Find<LoginView>(window) == null && timer.Elapsed.TotalSeconds < 5) await Task.Delay(20);
                var login = Find<LoginView>(window);
                Require(login != null, "Navigated to login");
                Require(timer.Elapsed.TotalSeconds >= 1.8 && timer.Elapsed.TotalSeconds < 4, "Default splash duration near 2 seconds: " + timer.Elapsed.TotalSeconds.ToString("F2"));
                await Task.Delay(100);
                Capture(window, "login-preview.png");
                Require(window.WindowStyle == WindowStyle.None && window.Width == 760 && window.Height == 540, "Compact borderless login window");
                var setup = (LoginViewModel)login.DataContext;
                Require(setup.IsSetup, "First run requires administrator creation");
                Require(app.Accounts.LastSignedInUserName == "", "No prior login leaves remembered account empty");
                Capture(window, "account-setup.png");
                setup.UpdatePassword("AdminTest123");
                setup.UpdateConfirmation("mismatch");
                await setup.SignInAsync();
                Require(setup.HasError && app.Accounts.NeedsSetup, "Confirmation mismatch does not create administrator");
                setup.UpdateConfirmation("AdminTest123");
                await setup.SignInAsync();
                await Task.Delay(100);
                var adminWelcome = (WelcomeViewModel)Find<WelcomeView>(window).DataContext;
                Require(adminWelcome.Panel.CanAdmin, "First account is administrator");
                Require(new LocalAccountService(app.StorePath).LastSignedInUserName == "admin", "Successful account survives service restart");
                adminWelcome.SettingsCommand.Execute(); await Task.Delay(50);
                var accountWindow=app.Windows.Cast<Window>().Single(w=>w!=window);
                Require(!Find<ControlConsole>(window).IsEnabled, "Console disabled while settings window is open");
                var accountView=Find<AccessPanel>(accountWindow);
                var passwordInput=(System.Windows.Controls.PasswordBox)accountView.FindName("NewPasswordInput");
                adminWelcome.Panel.NewName="shortpassuser"; passwordInput.Password="123456";
                Click(accountView,App.Appearance.Text("CreateUser")); await Task.Delay(50);
                Require(adminWelcome.Panel.HasError && adminWelcome.Panel.Message==App.Appearance.Text("InvalidPassword"),"123456 gives visible password validation instead of throwing");
                Require(passwordInput.Password=="123456" && app.Accounts.GetUsers().Count==1 && adminWelcome.Panel.CanEdit,"Failed creation keeps input, creates no user and remains usable");
                Capture(accountWindow,"user-password-validation.png");
                App.Appearance.SetLanguage(true); adminWelcome.Panel.RefreshLanguage();
                Require(adminWelcome.Panel.Message==App.Appearance.Text("InvalidPassword") && adminWelcome.Panel.Message.StartsWith("The password"),"User creation validation translates");
                App.Appearance.SetLanguage(false); adminWelcome.Panel.RefreshLanguage(); accountWindow.Close();
                Require(Find<ControlConsole>(window).IsEnabled, "Console restores on workspace after settings closes");
                adminWelcome.Panel.NewName = "operator";
                adminWelcome.Panel.UpdatePassword("Demo@123");
                await adminWelcome.Panel.CreateUserAsync();
                Require(app.Accounts.GetUsers().Count == 2, "Administrator creates operator");
                adminWelcome.Panel.NewName="OPERATOR"; adminWelcome.Panel.UpdatePassword("Another123");
                Require(!await adminWelcome.Panel.CreateUserAsync() && adminWelcome.Panel.Message==App.Appearance.Text("DuplicateUser") && adminWelcome.Panel.CanEdit,"Duplicate user is caught and shown without crashing");
                try { await app.Accounts.CreateUserAsync("OPERATOR", "Another123", UserRole.Operator); throw new Exception("Duplicate accepted"); }
                catch (InvalidOperationException) { Console.WriteLine("PASS: duplicate account rejected"); }
                adminWelcome.Panel.Power = "35";
                adminWelcome.Panel.SaveParameters();
                var reopened = new LocalAccountService(app.StorePath);
                Require(await reopened.SignInAsync("ADMIN", "AdminTest123", default), "Account persists and case-insensitive login works");
                Require(reopened.GetParameters().PowerPercent == 35, "Parameters survive service restart");
                Require(!File.ReadAllText(app.StorePath).Contains("AdminTest123") && !File.ReadAllText(app.StorePath).Contains("Demo@123"), "No plaintext passwords stored");
                try { await reopened.InitializeAdministratorAsync("second", "Another123", default); throw new Exception("Setup reopened"); }
                catch (InvalidOperationException) { Console.WriteLine("PASS: first-run setup cannot be repeated"); }
                try { reopened.SaveParameters(new ProcessParameters(double.NaN, 50, 100)); throw new Exception("NaN accepted"); }
                catch (ArgumentException) { Console.WriteLine("PASS: invalid process parameters rejected"); }
                await Task.Delay(100);
                Capture(window, "administrator-panel.png");
                var drilling = adminWelcome.Operation;
                var machine = drilling.Machine;
                Require(!machine.CanStart, "Cycle cannot start before homing");
                drilling.HomeCommand.Execute();
                Require(drilling.HasConfirmation && machine.State == MachineState.Idle, "Homing requires confirmation");
                drilling.ConfirmCommand.Execute();
                for (int step = 0; step < 20; step++) machine.Advance();
                Require(machine.Homed && machine.CanStart, "Homing enables cycle start");
                // 提示文字变化时，按住鼠标下方的轮盘位置和尺寸必须保持不变。
                foreach (var english in new[] { false, true })
                {
                    App.Appearance.SetLanguage(english);
                    foreach (var size in new[] { (1440d, 920d), (1180d, 740d) })
                    {
                        window.Width=size.Item1; window.Height=size.Item2; window.UpdateLayout();
                        var wheel=Find<JogWheel>(window);
                        Rect WheelBounds() => wheel.TransformToAncestor(window).TransformBounds(new Rect(wheel.RenderSize));
                        var idleBounds=WheelBounds();
                        machine.BeginJog(1,0); machine.Advance(); window.UpdateLayout();
                        Require(WheelBounds()==idleBounds,"Jogging preserves wheel position and scale: " + english + " / " + size);
                        machine.EndJog(); window.UpdateLayout();
                        Require(WheelBounds()==idleBounds,"Jog release preserves wheel position and scale");
                        machine.Start(); machine.Advance(); window.UpdateLayout();
                        Require(WheelBounds()==idleBounds,"Cycle start preserves console layout");
                        machine.Stop(); window.UpdateLayout();
                    }
                }
                App.Appearance.SetLanguage(false); window.Width=1440; window.Height=920;
                machine.Home(); for(int step=0;step<20;step++) machine.Advance();
                machine.RefreshRecipe(); var originalSetpoints=machine.ConfiguredParameters;
                drilling.Setpoints.Power=30; drilling.Setpoints.Frequency=40; drilling.Setpoints.Pulses=120;
                Require(machine.ConfiguredParameters==originalSetpoints,"Editing setpoints does not immediately change running configuration");
                drilling.ApplySetpointsCommand.Execute();
                Require(machine.ConfiguredParameters.PowerPercent==30 && machine.ConfiguredParameters.PulseCount==120,"Apply uses edited session setpoints");
                Require(app.Accounts.RecipeLibrary.GetActive().Parameters==originalSetpoints,"Session setpoints do not overwrite recipe file");
                machine.Start(); for(int i=0;i<8;i++)machine.Advance();
                Require(machine.ConfiguredParameters.FrequencyKhz==40 && machine.ActualFrequencyKhz==40 && !drilling.ApplySetpointsCommand.CanExecute(),"Start retains applied setpoints and locks editing");
                machine.Stop(); Require(machine.ActualFrequencyKhz==0 && machine.CurrentPowerPercent==0,"Stopped readbacks are zero");
                machine.ApplySetpoints(originalSetpoints);

                drilling.Setpoints.Cycles=2; drilling.ApplySetpointsCommand.Execute();
                Require(machine.ConfiguredCycles==2,"Apply saves target cycles");
                machine.Start();
                for(int i=0;i<10000 && machine.Cycles.Completed==0;i++) machine.Advance();
                Require(machine.Cycles.Completed==1 && machine.State==MachineState.Running && machine.Completed==0 && machine.Progress==50,"First full hole list increments count and starts second cycle");
                machine.PauseResume(); machine.Advance();
                Require(machine.Cycles.Completed==1 && machine.State==MachineState.Paused,"Pause preserves cycle count");
                machine.PauseResume();
                for(int i=0;i<10000 && machine.State==MachineState.Running;i++) machine.Advance();
                Require(machine.Cycles.Completed==2 && machine.Progress==100 && machine.State==MachineState.Completed && !machine.HasOutputs,"Target cycles finishes automatically with outputs closed");
                machine.Start(); machine.Advance(); machine.Stop();
                Require(machine.Cycles.Completed==0,"Interrupted partial cycle does not increment count and restart resets count");
                machine.StartDryRun();
                for(int i=0;i<10000 && machine.State==MachineState.Running;i++) { machine.Advance(); if(machine.LaserOn || machine.AirOn) throw new Exception("Repeated preview enabled output"); }
                Require(machine.Cycles.IsPreview && machine.Cycles.Completed==2,"Dry run counts are labelled preview");
                try { machine.ApplySetpoints(originalSetpoints,0); throw new Exception("Invalid cycle count accepted"); } catch(ArgumentException) { }
                drilling.Setpoints.Cycles=1; drilling.ApplySetpointsCommand.Execute();
                machine.Home(); for(int i=0;i<20;i++) machine.Advance();
                machine.BeginJog(1, 1);
                for (int step = 0; step < 15; step++) machine.Advance();
                Require(machine.X > 5 && machine.Y > 5 && Math.Abs(machine.X-machine.Y)<0.001, "Wheel diagonal moves both axes with normalized velocity");
                Find<JogWheel>(window).Release(); var jogX=machine.X; machine.Advance();
                Require(machine.X==jogX && !machine.IsJogging, "Release stops jog");
                Capture(window, "hmi-jog.png");
                machine.BeginZJog(1); for(int i=0;i<200;i++) machine.Advance(); machine.EndJog();
                Require(machine.Z==machine.Simulation.ZMaximum,"Z+ clamps to upper travel limit");
                machine.BeginZJog(-1); for(int i=0;i<300;i++) machine.Advance(); machine.EndJog();
                Require(machine.Z==machine.Simulation.ZMinimum,"Z- clamps to lower travel limit");
                var console=Find<ControlConsole>(window);
                var zButton=FindButtonByTag(console,"+");
                zButton.RaiseEvent(new System.Windows.Input.MouseButtonEventArgs(System.Windows.Input.Mouse.PrimaryDevice,0,System.Windows.Input.MouseButton.Left) { RoutedEvent=System.Windows.UIElement.PreviewMouseLeftButtonDownEvent });
                machine.Advance(); Require(machine.IsJogging && machine.Z>machine.Simulation.ZMinimum,"Z button starts held motion");
                zButton.RaiseEvent(new System.Windows.Input.MouseButtonEventArgs(System.Windows.Input.Mouse.PrimaryDevice,0,System.Windows.Input.MouseButton.Left) { RoutedEvent=System.Windows.UIElement.PreviewMouseLeftButtonUpEvent });
                Require(!machine.IsJogging,"Z button release stops motion");
                var stoppedZ=machine.Z; machine.Advance(); Require(machine.Z==stoppedZ,"Z release stops motion");
                machine.ToggleFollow(); Require(machine.FollowEnabled && !machine.CanZJog,"Follow locks manual Z"); machine.ToggleFollow();
                machine.TestEmission(); Require(!machine.LaserOn,"Closed shutter rejects emission");
                machine.ToggleShutter(); machine.ToggleRedLight(); Require(!machine.CanTest,"Red aiming light inhibits test beam"); machine.ToggleRedLight();
                drilling.EmitCommand.Execute(); Require(drilling.HasConfirmation && !machine.LaserOn,"Emission requires confirmation"); drilling.ConfirmCommand.Execute();
                Require(machine.IsTesting && machine.CurrentPowerPercent==5 && !machine.CanEdit,"Timed test uses modeled power and locks edits");
                for(int i=0;i<machine.Simulation.TestEmissionTicks;i++)machine.Advance();
                Require(!machine.LaserOn && !machine.ShutterOpen && !machine.Busy,"Timed test closes beam and shutter automatically");
                machine.ToggleAir(); machine.ToggleRedLight(); machine.Stop(); Require(!machine.HasOutputs,"Stop clears idle auxiliary outputs");
                machine.ToggleShutter(); machine.TestEmission(); machine.ToggleDoor(); Require(!machine.HasOutputs && !machine.IsTesting,"Door interlock aborts test"); machine.ToggleDoor(); machine.Reset(); machine.Home(); for(int i=0;i<20;i++)machine.Advance();
                var frameCount=machine.Completed; machine.Frame(); Require(machine.IsFraming && machine.RedLightOn && !machine.CanEdit,"Frame mode starts and locks geometry");
                Capture(window,"hmi-frame.png");
                for(int i=0;i<200 && machine.IsFraming;i++) { machine.Advance(); Require(!machine.LaserOn && !machine.AirOn,"Frame never enables laser or air"); }
                Require(!machine.IsFraming && !machine.RedLightOn && machine.Completed==frameCount,"Frame completes without changing machining count");
                machine.Frame(); machine.Stop(); Require(!machine.IsFraming && !machine.HasOutputs,"Stop cancels frame motion");

                drilling.DryRunCommand.Execute(); drilling.ConfirmCommand.Execute();
                for(int step=0;step<20;step++) machine.Advance();
                machine.PauseResume(); var dryX=machine.X; machine.Advance();
                Require(machine.X==dryX && !machine.LaserOn && !machine.AirOn, "Dry run pause holds position with outputs off");
                await Task.Delay(100); Capture(window,"hmi-dry-run.png"); machine.PauseResume();
                for(int step=0;step<machine.Holes.Count*16;step++) { machine.Advance(); if(machine.LaserOn || machine.AirOn) throw new Exception("Dry run activated output"); }
                Require(!machine.LaserOn && !machine.AirOn,"Dry run outputs OFF for entire cycle");
                Require(machine.State==MachineState.Completed && machine.Completed==machine.Holes.Count, "Dry run visits every hole");
                foreach(var hole in machine.Holes) Require(hole.Visited && !hole.Done, "Dry run does not mark hole machined");
                machine.Home(); for(int step=0;step<20;step++) machine.Advance();
                drilling.StartCommand.Execute(); drilling.CancelCommand.Execute();
                Require(machine.State == MachineState.Ready, "Cancel start leaves machine ready");
                drilling.StartCommand.Execute(); drilling.ConfirmCommand.Execute();
                for (int step = 0; step < 8; step++) machine.Advance();
                Require(machine.LaserOn && !machine.CanEdit, "Cycle simulates beam and locks geometry");
                Require(!adminWelcome.SettingsCommand.CanExecute() && !adminWelcome.SignOutCommand.CanExecute(), "Settings and logout locked during cycle");
                await Task.Delay(120);
                Capture(window, "main-running-light.png");
                App.Appearance.SetTheme(true);
                await Task.Delay(100);
                Capture(window, "main-running-dark.png");
                App.Appearance.SetTheme(false);
                machine.PauseResume();
                var pausedX = machine.X; var pausedProgress = machine.Completed;
                App.Appearance.SetLanguage(true); machine.Changed();
                window.Width = 1180; window.Height = 740;
                await Task.Delay(100);
                Capture(window, "main-minimum-english.png");
                window.Width = 1440; window.Height = 920;
                App.Appearance.SetLanguage(false); machine.Changed();
                for (int step = 0; step < 10; step++) machine.Advance();
                Require(!machine.LaserOn && machine.X == pausedX && machine.Completed == pausedProgress, "Pause stops beam and motion");
                machine.PauseResume(); machine.ToggleDoor();
                Require(machine.State == MachineState.Fault && !machine.LaserOn && !machine.Homed, "Door fault stops cycle and invalidates reference");
                machine.Reset(); Require(machine.State == MachineState.Fault, "Open door blocks fault reset");
                machine.ToggleDoor(); machine.Reset();
                Require(!machine.CanStart, "Reset does not auto restart or home");
                machine.Home(); for (int step = 0; step < 20; step++) machine.Advance();
                machine.Start(); machine.Emergency();
                Require(machine.EmergencyLatched && machine.State == MachineState.Fault && !machine.LaserOn, "Simulated E-stop latches fault");
                Capture(window, "main-fault.png");
                Require(Find<HandyControl.Controls.Badge>(window)?.ShowBadge==true,"HandyControl alarm badge follows fault latch");
                Require(machine.LaserText==App.Appearance.Text("Machine.EmissionInhibited"),"Fault status explicitly reports emission inhibited");
                machine.Reset(); machine.Home(); for (int step = 0; step < 20; step++) machine.Advance();
                machine.Start(); for (int step = 0; step < machine.Holes.Count * 16; step++) machine.Advance();
                Require(machine.State == MachineState.Completed && machine.Progress == 100 && !machine.LaserOn, "All holes complete at 100 percent");
                await Task.Delay(100);
                Capture(window, "main-completed.png");
                int originalCount = machine.Holes.Count;
                machine.EditHole(null, 50, 40, 0.8, 1);
                Require(machine.Holes.Count == originalCount + 1 && machine.Completed == 0, "Administrator can change hole count");
                machine.DeleteHole(machine.Holes[machine.Holes.Count-1]);
                machine.Start(); machine.Stop(); Require(machine.State == MachineState.Stopped && !machine.LaserOn, "Immediate stop closes beam");
                adminWelcome.SettingsCommand.Execute();
                await Task.Delay(100);
                foreach(Window popup in app.Windows) if(popup!=window) Capture(popup,"settings-panel.png");
                var popupCount=app.Windows.Count; adminWelcome.SettingsCommand.Execute();
                Require(app.Windows.Count==popupCount,"Settings reuses existing window");
                foreach (var popup in app.Windows.Cast<Window>().Where(w => w != window).ToArray()) popup.Close();
                adminWelcome.DeviceCommand.Execute(); await Task.Delay(100);
                Require(!Find<ControlConsole>(window).IsEnabled && !Find<JogWheel>(window).IsEnabled, "Device page disables console including jog controls");
                Require(app.Windows.Count == 1 && ((FrameworkElement)Find<WorkspaceContentHost>(window).Content).DataContext == drilling, "Device shares central region and live machine"); Capture(window,"hmi-device.png");
                Require(machine.DeviceStatus.X == machine.X && machine.DeviceStatus.Z == machine.Z, "Device telemetry mirrors axis coordinates");
                machine.ToggleAir(); Require(machine.DeviceStatus.GasPressureBar > 0 && machine.DeviceStatus.ActiveOutputs > 0, "Device telemetry follows gas output"); machine.Stop();
                Require(machine.DeviceStatus.GasPressureBar == 0 && machine.DeviceStatus.SimulatedPowerW == 0, "Stopped telemetry has zero gas pressure and beam power");
                App.Appearance.SetTheme(true); App.Appearance.SetLanguage(true); machine.Changed(); await Task.Delay(100); Capture(window,"hmi-device-dark.png");
                window.Width=1180; window.Height=740; await Task.Delay(100); Capture(window,"hmi-device-minimum.png");
                window.Width=1440; window.Height=920; App.Appearance.SetTheme(false); App.Appearance.SetLanguage(false); machine.Changed();
                adminWelcome.MaintenanceCommand.Execute(); await Task.Delay(100);
                Require(app.Windows.Count == 1 && adminWelcome.SelectedPage == "Maintenance", "Maintenance uses central view");
                var maintenance = machine.Maintenance;
                var beforePower=maintenance.Data.PowerSeconds; var beforeBeam=maintenance.Data.EmissionSeconds;
                maintenance.Accumulate(2,false,true,false);
                Require(maintenance.Data.PowerSeconds==beforePower+2 && maintenance.Data.EmissionSeconds==beforeBeam,"Power and emission meters are separate");
                var recordsBefore=maintenance.Data.Records.Count;
                adminWelcome.Maintenance.Note=""; adminWelcome.Maintenance.RecordCommand.Execute();
                Require(maintenance.Data.Records.Count==recordsBefore,"Empty maintenance note does not create record");
                adminWelcome.Maintenance.Note="Cleaned lens and checked cooling / MOCK";
                adminWelcome.Maintenance.SelectedItem="LensService"; adminWelcome.Maintenance.RecordCommand.Execute();
                Require(maintenance.Data.Records.Count==recordsBefore+1 && maintenance.Data.PowerSeconds>=beforePower+2,"Service record retains lifetime meters");
                var reloadedMaintenance=new MaintenanceService(app.Accounts);
                Require(reloadedMaintenance.Data.Records.Count==maintenance.Data.Records.Count && reloadedMaintenance.Data.EmissionSeconds==maintenance.Data.EmissionSeconds,"Maintenance history and meters persist");
                Capture(window,"hmi-maintenance.png");
                machine.Start(); Require(!adminWelcome.Maintenance.RecordCommand.CanExecute(),"Maintenance entry locked during operation"); machine.Stop();
                adminWelcome.LogsCommand.Execute(); await Task.Delay(100); Require(app.Windows.Count == 1 && adminWelcome.SelectedPage == "Logs", "Logs use central region"); Capture(window,"hmi-logs.png");
                {
                    var logGrid=Find<System.Windows.Controls.DataGrid>(Find<WorkspaceContentHost>(window));
                    logGrid.SelectedItem=machine.Events.First(e=>e.Level=="ALARM"); logGrid.ScrollIntoView(logGrid.SelectedItem); logGrid.UpdateLayout();
                    var selectedRow=(System.Windows.Controls.DataGridRow)logGrid.ItemContainerGenerator.ContainerFromItem(logGrid.SelectedItem);
                    selectedRow.Focus(); await Task.Delay(60);
                    foreach(var dark in new[] { false, true })
                    {
                        App.Appearance.SetTheme(dark); await Task.Delay(60);
                        var cell=Find<System.Windows.Controls.DataGridCell>(selectedRow);
                        Require(((SolidColorBrush)selectedRow.Background).Color==(Color)ColorConverter.ConvertFromString("#075A88") && ((SolidColorBrush)cell.Foreground).Color==Colors.White,"Selected log row/cell has readable colors in both themes");
                        Capture(window,dark ? "hmi-logs-selected-dark.png" : "hmi-logs-selected-light.png");
                        Find<JogWheel>(window).Focus(); await Task.Delay(60);
                        Require(((SolidColorBrush)cell.Foreground).Color==Colors.White,"Log text remains white after focus moves away");
                        Capture(window,dark ? "hmi-logs-inactive-dark.png" : "hmi-logs-inactive-light.png");
                    }
                    App.Appearance.SetTheme(false);
                }

                app.Accounts.SaveRecipe("Titanium test",new ProcessParameters(35,50,100)); app.Accounts.ApplyRecipe("Titanium test");
                Require(app.Accounts.GetRecipes().Count>=2,"Named recipe persists alongside default template");
                var library=app.Accounts.RecipeLibrary;
                var curveRecipe=ProcessRecipe.Template("SUS304_1mm_DEMO") with { Material="SUS304", ThicknessMm=1 };
                library.Save(curveRecipe);
                Require(library.GetActive().Name=="Titanium test","Saving a file does not switch active recipe");
                library.Apply(curveRecipe.Name); machine.RefreshRecipe();
                var newService=new LocalAccountService(app.StorePath);
                Require(await newService.SignInAsync("ADMIN","AdminTest123",default),"Recipe test reopens account");
                Require(newService.RecipeLibrary.GetActive().Name==curveRecipe.Name && newService.RecipeLibrary.LoadAll().Any(r=>r.Material=="SUS304" && r.PowerCurve.Length==4),"File metadata, curve and active selection survive restart");
                library.Save(curveRecipe with { Material="Changed library only" });
                Require(library.GetActive().Material=="SUS304","Applied snapshot survives overwrite of library file");
                library.Save(curveRecipe);
                machine.Start(); for(int step=0;step<7;step++)machine.Advance(); var power1=machine.CurrentPowerPercent;
                for(int step=0;step<3;step++)machine.Advance(); var power2=machine.CurrentPowerPercent;
                Require(power2>power1 && power2<=35,"Simulation interpolates varying per-hole power");
                machine.PauseResume(); Require(machine.CurrentPowerPercent==0,"Paused power is zero"); machine.Stop();
                machine.StartDryRun(); for(int step=0;step<10;step++)machine.Advance(); Require(machine.CurrentPowerPercent==0 && !machine.AirOn,"Dry run ignores power curve and keeps outputs off"); machine.Stop();
                var fixtureDir=Path.Combine(Path.GetDirectoryName(app.StorePath),"empty-library-test");
                var emptyFiles=new RecipeFileService(fixtureDir,()=>app.Accounts.CurrentUser,null,null);
                Require(emptyFiles.LoadAll().Count==1 && Directory.GetFiles(emptyFiles.DirectoryPath,"*.json").Length==1,"Empty library generates actual default JSON file");
                File.WriteAllText(Path.Combine(emptyFiles.DirectoryPath,"broken.json"),"{broken");
                Require(emptyFiles.LoadAll().Count==1 && emptyFiles.LoadErrors.Count==1,"Invalid files are reported while valid files load");
                try { emptyFiles.Save(curveRecipe with { PowerCurve=new[] { new PowerPoint(0,20),new PowerPoint(0,30) } }); throw new Exception("Invalid curve accepted"); } catch(ArgumentException) { Console.WriteLine("PASS: duplicate/missing curve endpoints rejected"); }
                using(var editor=new RecipeLibraryViewModel(app.Accounts,machine))
                {
                    editor.NewCommand.Execute(); editor.Name="Aluminum_2mm_DEMO"; editor.Material="Aluminum"; editor.Thickness="2";
                    editor.MovePoint(1,30,30); editor.Save();
                    Require(library.LoadAll().Any(r=>r.Name=="Aluminum_2mm_DEMO" && r.ThicknessMm==2 && r.PowerCurve[1].Progress==30),"Editor saves independent material file and curve nodes");
                    editor.Power="20"; editor.MovePoint(1,30,90.89);
                    Require(editor.CurveWarning.Contains("90.89") && editor.CurveWarning.Contains("20"),"Curve warns immediately with actual peak and limit");
                    editor.Save();
                    Require(editor.Message==editor.CurveWarning && !editor.CanSelect && editor.Points[1].PowerPercent==90.89,"Rejected save explains power limit and preserves draft");
                    Require(library.LoadAll().Single(r=>r.Name=="Aluminum_2mm_DEMO").PowerCurve[1].PowerPercent==30,"Rejected curve does not overwrite file");
                    editor.Power="95";
                    Require(editor.CurveWarning=="","Corrected limit clears live warning");
                    editor.Save(); Require(editor.CanSelect && library.LoadAll().Single(r=>r.Name=="Aluminum_2mm_DEMO").PowerCurve[1].PowerPercent==90.89,"Corrected curve saves successfully");
                    editor.Apply(); Require(machine.ActiveRecipeName=="Aluminum_2mm_DEMO","Apply explicitly sets one active recipe");
                    editor.Power="40"; Require(!editor.ApplyCommand.CanExecute(),"Unsaved edits cannot be applied");
                }
                library.Apply(curveRecipe.Name); machine.RefreshRecipe();
                adminWelcome.RecipeCommand.Execute(); await Task.Delay(100);
                {
                    var popup = window;
                    Require(app.Windows.Count == 1, "Recipes use central region without opening windows");
                    var number=Find<HandyControl.Controls.NumericUpDown>(popup);
                    Require(number!=null,"Recipe uses HandyControl numeric input");
                    var editor=(RecipeLibraryViewModel)number.DataContext;
                    number.SetCurrentValue(HandyControl.Controls.NumericUpDown.ValueProperty,2.5);
                    Require(double.Parse(editor.Thickness,System.Globalization.CultureInfo.InvariantCulture)==2.5 && !editor.ApplyCommand.CanExecute(),"Numeric editor updates model draft and blocks unsaved apply");
                    number.SetCurrentValue(HandyControl.Controls.NumericUpDown.ValueProperty,-1.0);
                    Require(number.Value==number.Minimum,"Numeric editor enforces lower bound");
                    adminWelcome.OperationCommand.Execute(); adminWelcome.RecipeCommand.Execute();
                    Require(ReferenceEquals(editor, adminWelcome.Recipes) && !editor.CanSelect, "Recipe draft survives page switching");
                    editor.DiscardCommand.Execute();
                    Capture(popup,"hmi-recipes.png");
                }
                App.Appearance.SetTheme(true); App.Appearance.SetLanguage(true); machine.Changed(); await Task.Delay(100);
                Capture(window,"hmi-recipes-dark-english.png");
                window.Width=1180; window.Height=740; await Task.Delay(100); Capture(window,"hmi-recipes-minimum.png"); window.Width=1440; window.Height=920;
                App.Appearance.SetTheme(false); App.Appearance.SetLanguage(false); machine.Changed();
                var clockBefore=adminWelcome.ClockText; await Task.Delay(1100); Require(adminWelcome.ClockText!=clockBefore,"Top clock advances");
                adminWelcome.OperationCommand.Execute();
                Require(Find<ControlConsole>(window).IsEnabled, "Workspace restores console navigation gate");
                adminWelcome.SignOutCommand.Execute();
                await Task.Delay(100);
                login = Find<LoginView>(window);
                var vm = (LoginViewModel)login.DataContext;
                Require(vm.UserName == "admin", "Logout prefills last successful administrator");
                vm.UserName = "operator";
                await vm.SignInAsync();
                Require(vm.HasError, "Empty password rejected");
                Click(login, "中文 / EN");
                Require(vm.ErrorMessage == "Enter your password." && vm.LoginCaption == "Sign in", "Existing validation and command captions translate immediately");
                Require(HandyControl.Tools.ConfigHelper.Instance.Lang.IetfLanguageTag == "en-us", "HandyControl language synchronized");
                Click(login, "Dark");
                await Task.Delay(100);
                Require(((SolidColorBrush)app.FindResource("SurfaceBrush")).Color == (Color)ColorConverter.ConvertFromString("#203246"), "Dark palette applied");
                Require(app.Resources.MergedDictionaries[0].Source.OriginalString.EndsWith("SkinDark.xaml"), "HandyControl dark skin synchronized");
                Capture(window, "login-dark-english.png");
                App.Appearance.SetTheme(false);
                Require(app.Resources.MergedDictionaries[0].Source.OriginalString.EndsWith("SkinDefault.xaml"), "HandyControl light skin restored");
                await Task.Delay(50);
                Capture(window, "login-light-english.png");
                App.Appearance.SetLanguage(false);
                App.Appearance.SetTheme(true);
                await Task.Delay(50);
                Capture(window, "login-dark-chinese.png");
                App.Appearance.SetTheme(false);
                vm.UpdatePassword("wrong");
                await vm.SignInAsync();
                Require(vm.HasError && Find<LoginView>(window) != null, "Wrong password rejected");
                Require(new LocalAccountService(app.StorePath).LastSignedInUserName == "admin", "Failed operator login does not overwrite remembered administrator");
                vm.UpdatePassword("Demo@123");
                await vm.SignInAsync();
                await Task.Delay(100);
                var welcome = Find<WelcomeView>(window);
                Require(welcome != null, "Correct demo login enters welcome");
                Require(window.WindowStyle == WindowStyle.SingleBorderWindow && window.Width >= 1180, "Main system restores normal window");
                var welcomeVm = (WelcomeViewModel)welcome.DataContext;
                Require(welcomeVm.UserName == "operator", "Username passed through Prism navigation");
                Require(new LocalAccountService(app.StorePath).LastSignedInUserName == "operator", "Next successful account replaces remembered account");
                Require(!welcomeVm.Panel.CanAdmin && !welcomeVm.Panel.SaveParametersCommand.CanExecute(), "Operator parameter editing disabled");
                try { app.Accounts.SaveRecipe("Unauthorized",new ProcessParameters(50,50,100)); throw new Exception("Operator saved recipe"); } catch(UnauthorizedAccessException) { Console.WriteLine("PASS: operator recipe save rejected"); }
                try { app.Accounts.ApplyRecipe("Titanium test"); throw new Exception("Operator applied recipe"); } catch(UnauthorizedAccessException) { Console.WriteLine("PASS: operator recipe apply rejected"); }
                try { welcomeVm.Operation.Machine.EditHole(null, 50, 40, 1, 1); throw new Exception("Operator edited geometry"); }
                catch (UnauthorizedAccessException) { Console.WriteLine("PASS: operator geometry edit rejected"); }
                try { await app.Accounts.CreateUserAsync("intruder", "Another123", UserRole.Administrator); throw new Exception("Privilege escalation allowed"); }
                catch (UnauthorizedAccessException) { Console.WriteLine("PASS: service rejects operator user creation"); }
                try { app.Accounts.SaveParameters(new ProcessParameters(90, 50, 100)); throw new Exception("Unauthorized save allowed"); }
                catch (UnauthorizedAccessException) { Console.WriteLine("PASS: service rejects operator parameter changes"); }
                Require(app.Accounts.GetParameters().PowerPercent == 35, "Unauthorized change did not alter stored parameters");
                Capture(window, "operator-panel.png");
                welcomeVm.SignOutCommand.Execute();
                await Task.Delay(100);
                login = Find<LoginView>(window);
                Require(login != null, "Logout returns to login");
                Require(app.Accounts.CurrentUser == null, "Logout clears authenticated session");
                Require(window.WindowStyle == WindowStyle.None && window.Width == 760, "Logout restores compact borderless window");
                await ((LoginViewModel)login.DataContext).SignInAsync();
                Require(((LoginViewModel)login.DataContext).HasError, "Logout clears password");
                await Task.Delay(100);
                Capture(window, "login-minimum-size.png");
                var drawing = new DrawingVisual();
                using (var dc = drawing.RenderOpen()) dc.DrawImage((ImageSource)app.FindResource("CompanyLogo"), new Rect(0, 0, 256, 256));
                Save(drawing, 256, 256, "LaserDrilling.Hmi/Assets/Brand/company-logo.png");
                Console.WriteLine("PASS: all startup/login smoke checks");
                app.Shutdown(0);
            }
            catch (Exception ex) { File.WriteAllText("work/main-test-error.txt", ex.ToString()); Console.Error.WriteLine(ex); app.Shutdown(1); }
        }));
        Environment.ExitCode = app.Run();
    }
    /// <summary>
    /// 为界面测试提供隔离的账户存储和服务注册环境。
    /// </summary>
    private sealed class TestApp : App
    {
        public string StorePath { get; } = Path.Combine(Path.GetTempPath(), "LaserDrillingSmoke", Guid.NewGuid().ToString("N"), "accounts.json");
        public LocalAccountService Accounts { get; private set; }
        protected override void RegisterTypes(IContainerRegistry registry)
        {
            base.RegisterTypes(registry);
            Accounts = new LocalAccountService(StorePath);
            registry.RegisterInstance<IAccountService>(Accounts);
        }
    }
    static void Click(DependencyObject parent, string content)
    {
        var button = FindButton(parent, content) ?? throw new Exception("Button not found: " + content);
        button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
    }
    static System.Windows.Controls.Button FindButton(DependencyObject parent, string content)
    {
        if (parent is System.Windows.Controls.Button button && button.Content?.ToString() == content) return button;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) { var found = FindButton(VisualTreeHelper.GetChild(parent, i), content); if (found != null) return found; }
        return null;
    }
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); Console.WriteLine("PASS: " + message); }
    static T Find<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent is T match) return match;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) { var found = Find<T>(VisualTreeHelper.GetChild(parent, i)); if (found != null) return found; }
        return null;
    }
    static void Capture(Window window, string name)
    {
        var content = (FrameworkElement)window.Content;
        content.UpdateLayout();
        Directory.CreateDirectory("work/previews");
        Save(content, (int)content.ActualWidth, (int)content.ActualHeight, "work/previews/" + name);
    }
    static void Save(Visual visual, int width, int height, string path)
    {
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path); encoder.Save(stream);
    }
}
