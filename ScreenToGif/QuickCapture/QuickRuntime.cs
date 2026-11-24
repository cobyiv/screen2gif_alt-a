using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using ScreenToGif.Util.Settings;
using ScreenToGif.Util;
using ScreenToGif.Domain.Enums;
using ScreenToGif.Windows;
using ScreenToGif.Controls;
using Forms=System.Windows.Forms;

namespace ScreenToGif.QuickCapture;
internal static class QuickRuntime
{
    internal static QuickSettings Settings=new();
    internal static Hotkeys? Keys;
    internal static List<string> Registration=new();
    internal static CaptureBinding? Pending;
    static Forms.NotifyIcon? tray;
    static SettingsWindow? window;
    internal static void Log(string text) { try { var dir=Path.Combine(QuickSettings.Root,"Logs"); Directory.CreateDirectory(dir); File.AppendAllText(Path.Combine(dir,"quickcapture.log"),DateTime.Now.ToString("s")+" "+text+Environment.NewLine); } catch {} }
    internal static void Notify(string text,bool error=false) { Log(text); if(error || Settings.Notify) tray?.ShowBalloonTip(3500,"ScreenToGif | Quick Capture",text,error?Forms.ToolTipIcon.Warning:Forms.ToolTipIcon.Info); }
    internal static void InitializeSettings()
    {
        try { Settings=QuickSettings.Load(); Settings.Validate(); if(!File.Exists(QuickSettings.FilePath)) Settings.Save(); }
        catch(Exception ex) { Log(ex.ToString()); MessageBox.Show("Quick Capture settings could not be loaded. Using defaults without overwriting your file.\n"+ex.Message); Settings=new(); }
        UserSettings.All.StartMinimized=true; UserSettings.All.ShowNotificationIcon=true; UserSettings.All.KeepOpen=true;
        UserSettings.All.NewRecorder=true; UserSettings.All.SingleInstance=true;
        UserSettings.All.InstallUpdates=false;
        UserSettings.All.TemporaryFolder=Path.Combine(Path.GetTempPath(),"ScreenToGif-QuickCapture");
        UserSettings.All.DeleteCacheWhenClosing=false;
        UserSettings.All.FfmpegLocation=QuickExport.Ffmpeg;
    }
    internal static void ApplyTheme()
    {
        var theme=Settings.Theme switch { "light"=>AppThemes.Light, "dark"=>AppThemes.Dark, _=>AppThemes.FollowSystem };
        UserSettings.All.MainTheme=theme; ThemeHelper.SelectTheme(theme);
    }
    internal static void Start()
    {
        ApplyTheme();
        tray=new Forms.NotifyIcon { Icon=System.Drawing.SystemIcons.Application,Text="ScreenToGif | Quick Capture",Visible=true };
        var menu=new Forms.ContextMenuStrip();
        void Item(string name,Action action) => menu.Items.Add(name,null,(_,_)=>Application.Current.Dispatcher.BeginInvoke(action));
        Item("Capture region",()=>Begin(new CaptureBinding()));
        Item("Capture region + system audio",()=>Begin(new CaptureBinding {Audio="system"}));
        Item("Capture region + microphone",()=>Begin(new CaptureBinding {Audio="microphone"}));
        Item("Quick Capture settings / test shortcuts",ShowSettings);
        Item("Original ScreenToGif options",()=>App.MainViewModel.OpenOptions.Execute(null));
        Item("Original ScreenToGif editor",()=>App.MainViewModel.OpenEditor.Execute(null));
        Item("Open output folder",()=> { try { var folder=QuickSettings.Expand(Settings.OutputFolder); Directory.CreateDirectory(folder); System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(folder){UseShellExecute=true}); } catch(Exception ex){Notify(ex.Message,true);} });
        Item("Exit",()=> { if(Active()!=null) { Notify("Finish the active capture or export before exiting."); return; } App.MainViewModel.ExitApplication.Execute(null); });
        var exportMenu = new Forms.ToolStripMenuItem("Stop and export");
        menu.Items.Insert(3, exportMenu);
        menu.Opening += (_,_) => {
            exportMenu.DropDownItems.Clear();
            var active=Active(); exportMenu.Enabled=active!=null;
            foreach(var preset in Settings.Presets) exportMenu.DropDownItems.Add(preset.Name,null,(_,_)=>Application.Current.Dispatcher.BeginInvoke(new Action(()=>Active()?.QuickChoose(preset))));
            exportMenu.DropDownItems.Add("Edit",null,(_,_)=>Application.Current.Dispatcher.BeginInvoke(new Action(()=>Active()?.QuickEdit())));
        };
        tray.ContextMenuStrip=menu; tray.DoubleClick+=(_,_)=>Application.Current.Dispatcher.BeginInvoke(new Action(ShowSettings));
        Application.Current.Exit+=(_,_)=> { Keys?.Dispose(); tray.Dispose(); };
        Register(); try { ApplyStartup(); } catch(Exception ex) { Notify("Startup registration failed: "+ex.Message,true); }
    }
    internal static void Register()
    {
        Keys?.Dispose(); Keys=new Hotkeys(); Registration.Clear();
        foreach(var b in Settings.CaptureShortcuts) Registration.Add(Keys.Add("Capture ("+b.Audio+")",b.Shortcut,()=>Begin(b)));
        foreach(var p in Settings.Presets.Where(x=>!string.IsNullOrWhiteSpace(x.Shortcut))) Registration.Add(Keys.Add(p.Name,p.Shortcut,()=>Active()?.QuickChoose(p)));
        if(!string.IsNullOrWhiteSpace(Settings.EditShortcut)) Registration.Add(Keys.Add("Edit",Settings.EditShortcut,()=>Active()?.QuickEdit()));
        foreach(var line in Registration) Log(line);
        if(Registration.Any(x=>x.Contains("FAILED"))) Notify("One or more shortcuts conflict. Open settings to test.",true);
    }
    internal static void ApplyStartup()
    {
        using var key=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
        const string name="ScreenToGifQuickCapture";
        if(Settings.StartWithWindows) key.SetValue(name,"\""+Environment.ProcessPath+"\""); else key.DeleteValue(name,false);
    }
    internal static NewRecorder? Active()=>Application.Current.Windows.OfType<NewRecorder>().FirstOrDefault(r=>r.QuickSession);
    internal static void Begin(CaptureBinding b)
    {
        if(window!=null || Pending!=null || Application.Current.Windows.OfType<BaseRecorder>().Any()) { Notify("Close the active recorder or settings before starting another capture."); return; }
        try { Pending=b; UserSettings.All.NewRecorder=true; App.MainViewModel.OpenRecorder.Execute(null); }
        catch(Exception ex) { Pending=null; Notify(ex.Message,true); }
    }
    internal static void ShowSettings()
    {
        if(Active()!=null) { Notify("Finish the active capture before changing settings."); return; }
        if(window!=null) { window.Activate(); return; }
        window=new SettingsWindow(); window.Closed+=(_,_)=> { if(Keys!=null){Keys.TestMode=false;Keys.Received=null;} window=null; }; window.Show();
    }
}
