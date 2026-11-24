using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ScreenToGif.Domain.Enums;
using ScreenToGif.QuickCapture;
using ScreenToGif.Util.Settings;

namespace ScreenToGif.Windows;
public partial class NewRecorder
{
    internal bool QuickSession {get;private set;}
    bool quickStopping, quickAudioInvalid, quickAllowClose;
    string quickAudioMode="none";
    QuickAudio? quickAudio;
    string? quickAudioPath;
    ExportPreset? quickPreset;
    internal async Task<bool> QuickBeginIfPending()
    {
        var request=QuickRuntime.Pending;
        if(request==null)return false;
        QuickRuntime.Pending=null; QuickSession=true; quickAudioMode=request.Audio;
        try {
            UserSettings.All.CaptureFrequency=CaptureFrequencies.PerSecond;UserSettings.All.LatestFps=QuickRuntime.Settings.CaptureFps;
            UserSettings.All.UsePreStart=false;UserSettings.All.UseMemoryCache=false;UserSettings.All.UseDesktopDuplication=false;UserSettings.All.CursorFollowing=false;
            DetectCaptureFrequency();
            QuickButtons.Visibility=Visibility.Visible;
            foreach(var p in QuickRuntime.Settings.Presets) {
                var b=QuickButton(p.Name,()=>QuickChoose(p));b.ToolTip=p.Format.ToUpperInvariant()+" | "+p.Audio+" | "+p.Shortcut;
                b.IsEnabled=p.Audio=="none" || p.Audio==quickAudioMode; QuickButtons.Children.Add(b);
            }
            QuickButtons.Children.Add(QuickButton("Edit",QuickEdit));
            var label=new TextBlock{Text=quickAudioMode=="none"?"Silent":"Audio: "+quickAudioMode,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(5,0,5,0)};
            QuickButtons.Children.Add(label);
            _viewModel.Region=Rect.Empty;
            UpdateLayout();
            await PickRegion(ModeType.Region,true,true);
            if(_viewModel.Region.IsEmpty){Close();return true;}
            UpdateLayout();
            await Record();
        }catch(Exception ex){QuickRuntime.Notify("Capture failed: "+ex.Message,true);QuickRuntime.Log(ex.ToString());if(IsLoaded)Close();}
        return true;
    }
    Button QuickButton(string label,Action click)
    {
        var b=new Button{Content=label,Padding=new Thickness(9,4,9,4),Margin=new Thickness(2),MinWidth=46};
        var theme=QuickRuntime.Settings.Theme;
        if(theme=="dark") { b.Background=new SolidColorBrush(Color.FromRgb(45,45,49));b.Foreground=Brushes.White;b.BorderBrush=Brushes.Gray; }
        else if(theme=="light"){b.Background=Brushes.WhiteSmoke;b.Foreground=Brushes.Black;}
        b.Click+=(_,_)=>click();return b;
    }
    internal async void QuickChoose(ExportPreset p)
    {
        if(!QuickSession || quickStopping || Stage is not (RecorderStages.Recording or RecorderStages.Paused))return;
        if(p.Audio!="none" && (quickAudioInvalid || p.Audio!=quickAudioMode)){QuickRuntime.Notify("Audio was not captured continuously from the selected source. Use a silent preset or start a new audio-armed capture.",true);return;}
        quickPreset=p;await Stop();
    }
    internal async void QuickEdit(){if(QuickSession && !quickStopping && Stage is (RecorderStages.Recording or RecorderStages.Paused)) {quickPreset=null;await Stop();}}
    void QuickStartAudio()
    {
        if(!QuickSession || quickAudioMode=="none" || quickAudioInvalid || quickAudio!=null || Project==null)return;
        try {quickAudioPath=Path.Combine(Project.FullPath,"QuickCaptureAudio.wav");quickAudio=new QuickAudio(quickAudioMode,quickAudioPath);quickAudio.Start();}
        catch(Exception ex){quickAudio?.Dispose();quickAudio=null;quickAudioInvalid=true;QuickRuntime.Notify("Audio could not start; recording video only. "+ex.Message,true);}
    }
    async Task QuickFinishAudio()
    {
        if(quickAudio==null)return;
        var a=quickAudio;quickAudio=null;
        try {await a.Finish();}catch(Exception ex){quickAudioInvalid=true;a.Dispose();QuickRuntime.Notify("Audio finalization failed: "+ex.Message,true);}
    }
    void QuickPauseAudio(){if(QuickSession && quickAudioMode!="none"){quickAudioInvalid=true;quickAudio?.Dispose();quickAudio=null;QuickRuntime.Notify("Audio export disabled after a pause; silent video and Edit remain available.");}}
    async Task QuickBeforeClose()
    {
        if(!QuickSession || quickPreset==null || Project==null)return;
        var p=quickPreset;quickPreset=null;
        if(p.Audio!="none" && quickAudioInvalid){QuickRuntime.Notify("Audio was unavailable. Opening the original editor; source frames retained.",true);return;}
        var project=Project;
        string path;
        try { Title="Encoding "+p.Name+"..."; QuickButtons.IsEnabled=false; path=await QuickExport.Encode(project,p,QuickRuntime.Settings,p.Audio=="none"?null:quickAudioPath); }
        catch(Exception ex){QuickRuntime.Log(ex.ToString());QuickRuntime.Notify("Export failed. Opening the original editor: "+ex.Message,true);return;}
        // A saved export is valid even if the clipboard is locked. Do not delete project on error.
        Project=null;project.ReleaseMutex();
        try {if(QuickRuntime.Settings.CopyFile)await QuickExport.Copy(path);QuickRuntime.Notify("Saved "+path);}
        catch(Exception ex){QuickRuntime.Notify("Saved "+path+"; clipboard copy failed: "+ex.Message,true);}
        try {Directory.Delete(project.FullPath,true);}catch(Exception ex){QuickRuntime.Log("Temporary project cleanup: "+ex.Message);}
    }
}
