using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using LaserDrilling.Hmi.Services;
using Prism.Commands;
using Prism.Mvvm;
namespace LaserDrilling.Hmi.ViewModels
{
    public sealed class RecipeLibraryViewModel : BindableBase, IDisposable
    {
        private readonly IAccountService _accounts;
        private readonly IRecipeFileService _files;
        private readonly MockDrillingMachine _machine;
        private bool _loading, _dirty;
        private int _revision;
        private string _message="";
        public ObservableCollection<ProcessRecipe> Recipes { get; } = new();
        public ObservableCollection<PowerPoint> Points { get; } = new();
        public System.Collections.Generic.IReadOnlyList<string> Gases => ProcessRecipe.AvailableGases;
        public string DirectoryPath => _files.DirectoryPath;
        public bool CanSelect => !_dirty;
        public DelegateCommand DiscardCommand { get; }
        public string ActiveName => _machine.ActiveRecipeName;
        public string LoadWarning { get; private set; } = "";
        public string Message => _message=="" ? "" : App.Appearance.Text(_message);
        public string EditStatus => App.Appearance.Text(_dirty ? "Machine.Unsaved" : "Machine.SavedFile");
        public int Revision => _revision;
        public bool CanEdit => _accounts.CurrentUser?.Role==UserRole.Administrator && !_machine.Busy && !_machine.IsJogging;
        private ProcessRecipe _selected;
        public ProcessRecipe Selected { get=>_selected; set { if(SetProperty(ref _selected,value) && value!=null) Load(value); } }
        private PowerPoint _selectedPoint;
        public PowerPoint SelectedPoint { get=>_selectedPoint; set { SetProperty(ref _selectedPoint,value); if(value!=null) { PointProgress=F(value.Progress); PointPower=F(value.PowerPercent); } RefreshCommands(); } }
        private string _pointProgress="0", _pointPower="0";
        public string PointProgress { get=>_pointProgress; set=>SetProperty(ref _pointProgress,value); }
        public string PointPower { get=>_pointPower; set=>SetProperty(ref _pointPower,value); }
        public DelegateCommand SaveCommand { get; }
        public DelegateCommand ApplyCommand { get; }
        public DelegateCommand NewCommand { get; }
        public DelegateCommand ReloadCommand { get; }
        public DelegateCommand AddPointCommand { get; }
        public DelegateCommand UpdatePointCommand { get; }
        public DelegateCommand DeletePointCommand { get; }
        public RecipeLibraryViewModel(IAccountService accounts, MockDrillingMachine machine)
        {
            _accounts=accounts; _files=accounts.RecipeLibrary; _machine=machine;
            SaveCommand=new(Save,()=>CanEdit);
            ApplyCommand=new(Apply,()=>CanEdit && Selected!=null && !_dirty);
            NewCommand=new(New,()=>CanEdit && !_dirty);
            ReloadCommand=new(()=>Reload(),()=>!_dirty);
            AddPointCommand=new(AddPoint,()=>CanEdit && Points.Count<32);
            UpdatePointCommand=new(UpdatePoint,()=>CanEdit && SelectedPoint!=null);
            DeletePointCommand=new(DeletePoint,()=>CanEdit && SelectedPoint!=null && SelectedPoint.Progress>0 && SelectedPoint.Progress<100);
            DiscardCommand=new(()=>Reload(),()=>_dirty);
            Reload(); _machine.PropertyChanged+=OnState;
        }
        private static string F(double value)=>value.ToString("0.###",CultureInfo.InvariantCulture);
        private static double Parse(string text) { if(!double.TryParse(text,NumberStyles.Float,CultureInfo.InvariantCulture,out var value) || !double.IsFinite(value)) throw new ArgumentException("Machine.InvalidRecipe"); return value; }
        private void Dirty() { if(_loading)return; _dirty=true; Changed(); }
        private void Changed() { _revision++; RaisePropertyChanged(""); RefreshCommands(); }
        private void RefreshCommands() { DiscardCommand?.RaiseCanExecuteChanged(); SaveCommand?.RaiseCanExecuteChanged(); ApplyCommand?.RaiseCanExecuteChanged(); NewCommand?.RaiseCanExecuteChanged(); ReloadCommand?.RaiseCanExecuteChanged(); AddPointCommand?.RaiseCanExecuteChanged(); UpdatePointCommand?.RaiseCanExecuteChanged(); DeletePointCommand?.RaiseCanExecuteChanged(); }
        private void Load(ProcessRecipe r)
        {
            _loading=true;
            Name=r.Name; Material=r.Material; Thickness=F(r.ThicknessMm); Nozzle=r.Nozzle; Gas=r.Gas;
            Pressure=F(r.GasPressureBar); Focus=F(r.FocusOffsetMm); Dwell=F(r.DwellMilliseconds);
            Power=F(r.Parameters.PowerPercent); Frequency=F(r.Parameters.FrequencyKhz); Pulses=r.Parameters.PulseCount.ToString();
            Points.Clear(); foreach(var point in r.PowerCurve) Points.Add(point); SelectedPoint=Points.FirstOrDefault();
            _dirty=false; _loading=false; _message=""; Changed();
        }
        private void Reload(string name=null)
        {
            try { var all=_files.LoadAll(); Selected=null; Recipes.Clear(); foreach(var recipe in all) Recipes.Add(recipe); LoadWarning=_files.LoadErrors.Count==0 ? "" : App.Appearance.Text("Machine.BadFiles")+string.Join(", ",_files.LoadErrors); Selected=Recipes.FirstOrDefault(r=>r.Name==(name??ActiveName))??Recipes.FirstOrDefault(); Changed(); }
            catch(Exception) { Show("StoreError"); }
        }
        private void New() { if(!CanEdit)return; Selected=null; Load(ProcessRecipe.Template("NEW_"+DateTime.Now.ToString("HHmmss"))); Dirty(); }
        private ProcessRecipe Build()
        {
            if(!int.TryParse(Pulses,out var pulses))throw new ArgumentException("Machine.InvalidRecipe");
            var r=new ProcessRecipe(Name.Trim(),new ProcessParameters(Parse(Power),Parse(Frequency),pulses)) { Material=Material.Trim(),ThicknessMm=Parse(Thickness),Nozzle=Nozzle.Trim(),Gas=Gas,GasPressureBar=Parse(Pressure),FocusOffsetMm=Parse(Focus),DwellMilliseconds=Parse(Dwell),PowerCurve=Points.ToArray() };
            r.Validate(); return r;
        }
        private void Show(string key) { _message=key; RaisePropertyChanged(nameof(Message)); }
        public void Save()
        {
            if(!CanEdit) { Show("AccessDenied"); return; }
            try { var recipe=Build(); _files.Save(recipe); _dirty=false; Reload(recipe.Name); Show("Machine.RecipeSaved"); }
            catch(ArgumentException e) { Show(e.Message); } catch(Exception) { Show("StoreError"); }
        }
        public void Apply()
        {
            if(!CanEdit || Selected==null || _dirty) { Show("Machine.SaveFirst"); return; }
            try { _files.Apply(Selected.Name); _machine.RefreshRecipe(); Show("Machine.RecipeApplied"); Changed(); }
            catch(Exception) { Show("StoreError"); }
        }
        private void AddPoint()
        {
            if(!CanEdit || Points.Count>=32)return;
            var i=Enumerable.Range(0,Points.Count-1).OrderByDescending(n=>Points[n+1].Progress-Points[n].Progress).First();
            var a=Points[i]; var b=Points[i+1]; var point=new PowerPoint((a.Progress+b.Progress)/2,(a.PowerPercent+b.PowerPercent)/2);
            Points.Insert(i+1,point); SelectedPoint=point; Dirty();
        }
        private void UpdatePoint()
        {
            if(!CanEdit || SelectedPoint==null)return;
            try
            {
                var x=Parse(PointProgress); var y=Parse(PointPower); var i=Points.IndexOf(SelectedPoint);
                if(y<0 || y>100 || i==0 && x!=0 || i==Points.Count-1 && x!=100 || i>0 && x<=Points[i-1].Progress || i<Points.Count-1 && x>=Points[i+1].Progress) throw new ArgumentException("Machine.InvalidCurve");
                Points[i]=new PowerPoint(x,y); SelectedPoint=Points[i]; Dirty();
            }
            catch(ArgumentException) { Show("Machine.InvalidCurve"); }
        }
        private void DeletePoint() { if(!CanEdit || SelectedPoint==null || SelectedPoint.Progress==0 || SelectedPoint.Progress==100)return; Points.Remove(SelectedPoint); SelectedPoint=Points[0]; Dirty(); }
        public void MovePoint(int index,double progress,double power)
        {
            if(!CanEdit || index<0 || index>=Points.Count)return;
            progress=index==0?0:index==Points.Count-1?100:Math.Clamp(progress,Points[index-1].Progress+0.01,Points[index+1].Progress-0.01);
            Points[index]=new PowerPoint(Math.Round(progress,2),Math.Round(Math.Clamp(power,0,100),2)); SelectedPoint=Points[index]; Dirty();
        }
        private void OnState(object sender,PropertyChangedEventArgs e) { RaisePropertyChanged(nameof(CanEdit)); RaisePropertyChanged(nameof(ActiveName)); RaisePropertyChanged(nameof(Message)); RaisePropertyChanged(nameof(EditStatus)); RefreshCommands(); }
        public void Dispose()=>_machine.PropertyChanged-=OnState;
        private string _name="";
        public string Name { get=>_name; set { if(SetProperty(ref _name,value)) Dirty(); } }
        private string _material="";
        public string Material { get=>_material; set { if(SetProperty(ref _material,value)) Dirty(); } }
        private string _thickness="";
        public string Thickness { get=>_thickness; set { if(SetProperty(ref _thickness,value)) Dirty(); } }
        private string _nozzle="";
        public string Nozzle { get=>_nozzle; set { if(SetProperty(ref _nozzle,value)) Dirty(); } }
        private string _gas="";
        public string Gas { get=>_gas; set { if(SetProperty(ref _gas,value)) Dirty(); } }
        private string _pressure="";
        public string Pressure { get=>_pressure; set { if(SetProperty(ref _pressure,value)) Dirty(); } }
        private string _focus="";
        public string Focus { get=>_focus; set { if(SetProperty(ref _focus,value)) Dirty(); } }
        private string _dwell="";
        public string Dwell { get=>_dwell; set { if(SetProperty(ref _dwell,value)) Dirty(); } }
        private string _power="";
        public string Power { get=>_power; set { if(SetProperty(ref _power,value)) Dirty(); } }
        private string _frequency="";
        public string Frequency { get=>_frequency; set { if(SetProperty(ref _frequency,value)) Dirty(); } }
        private string _pulses="";
        public string Pulses { get=>_pulses; set { if(SetProperty(ref _pulses,value)) Dirty(); } }
    }
}
