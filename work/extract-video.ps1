Add-Type -AssemblyName PresentationCore,WindowsBase
$p = New-Object System.Windows.Media.MediaPlayer
$p.Open([uri]'C:/Users/cheung/Desktop/录屏软件/Videos/20261012170317.mp4')
$p.Volume=0
$p.Play()
function Pump { $f=New-Object Windows.Threading.DispatcherFrame; $t=New-Object Windows.Threading.DispatcherTimer; $t.Interval=[TimeSpan]::FromMilliseconds(500); $t.Add_Tick({$f.Continue=$false}); $t.Start(); [Windows.Threading.Dispatcher]::PushFrame($f); $t.Stop() }
Pump
$p.Pause()
Write-Output "$($p.NaturalVideoWidth)x$($p.NaturalVideoHeight) $($p.NaturalDuration)"
New-Item -ItemType Directory -Force work/video-frames | Out-Null
foreach($s in @(1,3,5,7,9)) {
$p.Position=[TimeSpan]::FromSeconds($s); Pump
$v=New-Object Windows.Media.DrawingVisual
$d=$v.RenderOpen(); $d.DrawVideo($p,[Windows.Rect]::new(0,0,1280,720)); $d.Close()
$b=[Windows.Media.Imaging.RenderTargetBitmap]::new(1280,720,96,96,[Windows.Media.PixelFormats]::Pbgra32); $b.Render($v)
$e=New-Object Windows.Media.Imaging.PngBitmapEncoder; $e.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($b)); $fs=[IO.File]::Create("$pwd/work/video-frames/$s.png"); $e.Save($fs); $fs.Close()
}
$p.Close()
