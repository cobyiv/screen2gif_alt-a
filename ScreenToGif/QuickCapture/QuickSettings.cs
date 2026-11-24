using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;

namespace ScreenToGif.QuickCapture;

public sealed class ExportPreset
{
    public string Name { get; set; } = "MP4";
    public string Format { get; set; } = "mp4";
    public int Fps { get; set; } = 30;
    public int Crf { get; set; } = 23;
    public int MaxWidth { get; set; } = 0;
    public int GifColors { get; set; } = 256;
    public string Audio { get; set; } = "none"; // none, system, microphone
    public string Shortcut { get; set; } = "";
    public string Folder { get; set; } = "";
    public override string ToString() => Name;
}
public sealed class CaptureBinding
{
    public string Shortcut { get; set; } = "Ctrl+Alt+R";
    public string Audio { get; set; } = "none";
}
public sealed class QuickSettings
{
    public bool StartWithWindows { get; set; } = true;
    public string Theme { get; set; } = "system";
    public string OutputFolder { get; set; } = @"%USERPROFILE%\Videos\Captures";
    public string Filename { get; set; } = "yyyy-MM-dd_HH-mm-ss-fff";
    public int CaptureFps { get; set; } = 30;
    public bool CopyFile { get; set; } = true;
    public bool Notify { get; set; } = true;
    public List<CaptureBinding> CaptureShortcuts { get; set; } = new() { new() };
    public List<ExportPreset> Presets { get; set; } = new() {
        new() { Name="MP4", Shortcut="Ctrl+Alt+M" },
        new() { Name="GIF", Format="gif", Fps=15, Shortcut="Ctrl+Alt+G" }
    };
    public string EditShortcut { get; set; } = "Ctrl+Alt+E";
    public static string Root => AppContext.BaseDirectory;
    public static string FilePath => Path.Combine(Root, "quickcapture.json");
    public static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented=true, PropertyNameCaseInsensitive=true };
    public static QuickSettings Load() => File.Exists(FilePath)
        ? JsonSerializer.Deserialize<QuickSettings>(File.ReadAllText(FilePath), JsonOptions) ?? throw new InvalidDataException("Empty settings") : new();
    public void Save() { Validate(); var temp=FilePath+".tmp"; File.WriteAllText(temp,JsonSerializer.Serialize(this,JsonOptions)); File.Move(temp,FilePath,true); }
    public static string Expand(string path) { var s=Environment.ExpandEnvironmentVariables(path); return Path.GetFullPath(Path.IsPathRooted(s)?s:Path.Combine(Root,s)); }
    public static bool ValidAudio(string s) => s is "none" or "system" or "microphone";
    public void Validate()
    {
        if (CaptureFps < 1 || CaptureFps > 60 || Theme is not ("system" or "light" or "dark")) throw new InvalidDataException("Invalid capture FPS or theme.");
        if (string.IsNullOrWhiteSpace(OutputFolder) || string.IsNullOrWhiteSpace(Filename)) throw new InvalidDataException("Output folder and filename are required.");
        _=Expand(OutputFolder); var name=DateTime.Now.ToString(Filename,System.Globalization.CultureInfo.InvariantCulture);
        if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(Path.GetInvalidFileNameChars())>=0 || name.EndsWith('.') || name.EndsWith(' ')) throw new InvalidDataException("Invalid filename pattern.");
        if (CaptureShortcuts == null || CaptureShortcuts.Count==0 || Presets==null || Presets.Count==0 || Presets.Count>20) throw new InvalidDataException("Provide at least one capture shortcut and 1-20 presets.");
        var keys=new HashSet<(uint,uint)>();
        void Check(string value) { if(string.IsNullOrWhiteSpace(value)) return; var k=Hotkeys.Parse(value); if(!keys.Add(k)) throw new InvalidDataException("Duplicate shortcut: "+value); }
        foreach(var b in CaptureShortcuts) { if(b==null || !ValidAudio(b.Audio) || string.IsNullOrWhiteSpace(b.Shortcut)) throw new InvalidDataException("Invalid capture shortcut."); Check(b.Shortcut); }
        var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach(var p in Presets) {
            if(p==null || string.IsNullOrWhiteSpace(p.Name) || p.Name.Length>35 || !names.Add(p.Name) || p.Format is not ("mp4" or "gif") || p.Fps<1 || p.Fps>60 || p.Crf<0 || p.Crf>51 || p.MaxWidth<0 || p.MaxWidth==1 || p.MaxWidth>16384 || p.GifColors<4 || p.GifColors>256 || !ValidAudio(p.Audio) || (p.Format=="gif" && p.Audio!="none")) throw new InvalidDataException("Invalid or duplicate export preset.");
            if(!string.IsNullOrWhiteSpace(p.Folder)) _=Expand(p.Folder); Check(p.Shortcut);
        }
        Check(EditShortcut);
    }
}
