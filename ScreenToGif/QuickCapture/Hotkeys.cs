using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace ScreenToGif.QuickCapture;
internal sealed class Hotkeys : IDisposable
{
    [DllImport("user32.dll",SetLastError=true)] static extern bool RegisterHotKey(IntPtr h,int id,uint modifiers,uint key);
    [DllImport("user32.dll",SetLastError=true)] static extern bool UnregisterHotKey(IntPtr h,int id);
    readonly HwndSource source = new(new HwndSourceParameters("QuickCaptureHotkeys") { ParentWindow=new IntPtr(-3), WindowStyle=0 });
    readonly Dictionary<int,(string label,Action action)> bindings=new();
    int next=100;
    public Action<string>? Received;
    public bool TestMode;
    public Hotkeys() { source.AddHook(Hook); }
    public static (uint,uint) Parse(string text)
    {
        var converter=new KeyGestureConverter();
        if(converter.ConvertFromInvariantString(text) is not KeyGesture gesture || gesture.Modifiers==ModifierKeys.None || gesture.Key==Key.None) throw new FormatException("Use a modified key, e.g. Ctrl+Alt+R: "+text);
        return ((uint)gesture.Modifiers,(uint)KeyInterop.VirtualKeyFromKey(gesture.Key));
    }
    public string Add(string label,string text,Action action)
    {
        var (mods,key)=Parse(text); var id=next++;
        if(!RegisterHotKey(source.Handle,id,mods|0x4000,key)) return label+" | "+text+" | FAILED ("+new Win32Exception(Marshal.GetLastWin32Error()).Message+")";
        bindings.Add(id,(label+" | "+text,action)); return label+" | "+text+" | Registered";
    }
    IntPtr Hook(IntPtr hwnd,int msg,IntPtr w,IntPtr l,ref bool handled)
    {
        if(msg==0x312 && bindings.TryGetValue(w.ToInt32(),out var b)) { handled=true; Received?.Invoke(b.label); if(!TestMode) b.action(); } return IntPtr.Zero;
    }
    public void Dispose() { foreach(var id in bindings.Keys) UnregisterHotKey(source.Handle,id); bindings.Clear(); source.Dispose(); }
}
