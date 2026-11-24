using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.CoreAudioApi;

namespace ScreenToGif.QuickCapture;
internal sealed class QuickAudio : IDisposable
{
    readonly WasapiCapture capture;
    readonly WaveFileWriter writer;
    readonly TaskCompletionSource<bool> stopped=new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly Stopwatch clock=new();
    readonly object gate=new();
    bool started, ended;
    Exception? failure;
    long written;
    public string Path {get;}
    public QuickAudio(string mode,string path)
    {
        Path=path;
        capture=mode=="system" ? new WasapiLoopbackCapture() : new WasapiCapture();
        try { writer=new WaveFileWriter(path,capture.WaveFormat); }
        catch { capture.Dispose(); throw; }
        capture.DataAvailable += (_,e)=> { lock(gate) { if(ended) return; try {
            // WASAPI loopback can omit silent packets. Fill observed gaps with silence.
            var format=capture.WaveFormat; var expected=(long)(clock.Elapsed.TotalSeconds*format.AverageBytesPerSecond)-e.BytesRecorded;
            if(expected-written>format.AverageBytesPerSecond/5) Silence(expected-written);
            writer.Write(e.Buffer,0,e.BytesRecorded); written+=e.BytesRecorded;
        } catch(Exception ex) { failure=ex; } } };
        capture.RecordingStopped += (_,e)=> { lock(gate) { if(e.Exception!=null) failure=e.Exception; } stopped.TrySetResult(true); };
    }
    void Silence(long bytes) { var align=capture.WaveFormat.BlockAlign; bytes=Math.Max(0,bytes/align*align); var buffer=new byte[65536/align*align]; while(bytes>0) { var n=(int)Math.Min(bytes,buffer.Length); writer.Write(buffer,0,n); written+=n; bytes-=n; } }
    public void Start() { if(started) return; clock.Start(); capture.StartRecording(); started=true; }
    public async Task Finish()
    {
        if(ended) return;
        clock.Stop();
        if(started) { capture.StopRecording(); await stopped.Task.WaitAsync(TimeSpan.FromSeconds(10)); }
        lock(gate) { if(ended) return; ended=true; var f=capture.WaveFormat; Silence((long)(clock.Elapsed.TotalSeconds*f.AverageBytesPerSecond)-written); writer.Dispose(); }
        capture.Dispose(); if(failure!=null) throw new IOException("Audio capture failed",failure);
    }
    public void Dispose() { if(ended) return; lock(gate) { ended=true; } try { if(started) capture.StopRecording(); } catch {} capture.Dispose(); writer.Dispose(); }
}
