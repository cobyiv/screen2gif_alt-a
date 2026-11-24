using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ScreenToGif.Model;

namespace ScreenToGif.QuickCapture;
internal static class QuickExport
{
    static readonly CultureInfo Invariant=CultureInfo.InvariantCulture;
    public static string Ffmpeg => Path.Combine(QuickSettings.Root,"FFmpeg","ffmpeg.exe");
    public static async Task<string> Encode(ProjectInfo project,ExportPreset p,QuickSettings s,string? audio)
    {
        if(!File.Exists(Ffmpeg)) throw new FileNotFoundException("Bundled FFmpeg is missing",Ffmpeg);
        if(p.Audio!="none" && (audio==null || !File.Exists(audio))) throw new InvalidOperationException("This audio source was not armed when capture began.");
        var folder=QuickSettings.Expand(string.IsNullOrWhiteSpace(p.Folder)?s.OutputFolder:p.Folder);
        Directory.CreateDirectory(folder);
        var stem=DateTime.Now.ToString(s.Filename,Invariant); var dest=Path.Combine(folder,stem+"."+p.Format);
        if(File.Exists(dest)) dest=Path.Combine(folder,stem+"_"+Guid.NewGuid().ToString("N")[..8]+"."+p.Format);
        var work=Path.Combine(project.FullPath,"QuickExport-"+Guid.NewGuid().ToString("N"));
        var partial=Path.Combine(folder,"."+Guid.NewGuid().ToString("N")+".partial."+p.Format);
        Directory.CreateDirectory(work);
        try {
            var frames=project.Frames.Select(f=>(f.Path,Delay:Math.Max(1,f.Delay))).ToArray();
            if(frames.Length==0) throw new InvalidOperationException("No captured frames.");
            double duration=frames.Sum(f=>(double)f.Delay)/1000;
            // Numbered local copies avoid ffconcat path escaping and protocol injection.
            await Task.Run(()=> {
                var text=new StringBuilder("ffconcat version 1.0\n");
                for(int i=0;i<frames.Length;i++) { var name=i.ToString("D8",Invariant)+".png"; File.Copy(frames[i].Path,Path.Combine(work,name)); text.Append("file ").Append(name).Append("\noption framerate 1000\n").Append("duration ").Append((frames[i].Delay/1000d).ToString("0.000",Invariant)).Append('\n'); }
                text.Append("file ").Append((frames.Length-1).ToString("D8",Invariant)).Append(".png\noption framerate 1000\n"); File.WriteAllText(Path.Combine(work,"frames.ffconcat"),text.ToString(),new UTF8Encoding(false));
            });
            var psi=new ProcessStartInfo(Ffmpeg) { UseShellExecute=false,CreateNoWindow=true,RedirectStandardError=true, WorkingDirectory=work };
            void Add(params string[] a) { foreach(var x in a) psi.ArgumentList.Add(x); }
            Add("-hide_banner","-loglevel","error","-nostdin","-f","concat","-safe","0","-i","frames.ffconcat");
            if(p.Audio!="none") Add("-i",audio!);
            var vf="fps="+p.Fps.ToString(Invariant);
            if(p.MaxWidth>0) vf+=",scale=w='min(iw,"+p.MaxWidth.ToString(Invariant)+")':h=-1";
            if(p.Format=="mp4") {
                vf+=",pad=ceil(iw/2)*2:ceil(ih/2)*2";
                Add("-map","0:v:0","-vf",vf,"-c:v","libx264","-preset","medium","-crf",p.Crf.ToString(Invariant),"-pix_fmt","yuv420p","-movflags","+faststart");
                if(p.Audio=="none") Add("-an"); else Add("-map","1:a:0","-c:a","aac","-b:a","160k","-af","apad");
            } else Add("-filter_complex","[0:v]"+vf+",split[a][b];[a]palettegen=max_colors="+p.GifColors+"[p];[b][p]paletteuse=dither=sierra2_4a","-loop","0","-an");
            Add("-t",Math.Max(1d/p.Fps,duration).ToString("0.000000",Invariant),"-n",partial);
            using var process=Process.Start(psi)??throw new IOException("Cannot start FFmpeg.");
            var stderr=process.StandardError.ReadToEndAsync(); await process.WaitForExitAsync(); var error=await stderr;
            if(process.ExitCode!=0 || !File.Exists(partial) || new FileInfo(partial).Length==0) throw new IOException("FFmpeg failed: "+error);
            File.Move(partial,dest,false); return dest;
        } finally { try { if(File.Exists(partial)) File.Delete(partial); } catch {} try { Directory.Delete(work,true); } catch {} }
    }
    public static async Task Copy(string path)
    {
        for(int i=0;i<8;i++) { try { var files=new StringCollection {path}; Clipboard.SetFileDropList(files); return; } catch(System.Runtime.InteropServices.COMException) when(i<7) { await Task.Delay(125); } }
    }
}
