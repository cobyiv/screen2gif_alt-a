using System;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Collections.ObjectModel;
using ScreenToGif.Util.Settings;

namespace ScreenToGif.QuickCapture;
internal sealed class SettingsWindow : Window
{
    readonly QuickSettings draft=JsonSerializer.Deserialize<QuickSettings>(JsonSerializer.Serialize(QuickRuntime.Settings))!;
    readonly TextBox folder=new(), filename=new(), fps=new(), edit=new();
    readonly CheckBox startup=new(){Content="Launch at Windows sign-in"}, clipboard=new(){Content="Copy exported file to clipboard"}, notify=new(){Content="Notify when encoding completes"};
    readonly ComboBox theme=new(){ItemsSource=new[]{"system","light","dark"}};
    readonly TextBlock status=new(){TextWrapping=TextWrapping.Wrap};
    readonly DataGrid presets=new(){AutoGenerateColumns=false,CanUserAddRows=false,Height=230,HorizontalScrollBarVisibility=ScrollBarVisibility.Auto};
    readonly DataGrid captures=new(){AutoGenerateColumns=false,CanUserAddRows=false,Height=120};
    readonly ObservableCollection<ExportPreset> ps;
    readonly ObservableCollection<CaptureBinding> cs;
    public SettingsWindow()
    {
        Title="ScreenToGif | Quick Capture settings"; Width=970; Height=800; MinWidth=680; MinHeight=500; WindowStartupLocation=WindowStartupLocation.CenterScreen;
        ps=new(draft.Presets); cs=new(draft.CaptureShortcuts); folder.Text=draft.OutputFolder;filename.Text=draft.Filename;fps.Text=draft.CaptureFps.ToString();edit.Text=draft.EditShortcut;
        startup.IsChecked=draft.StartWithWindows;clipboard.IsChecked=draft.CopyFile;notify.IsChecked=draft.Notify;theme.SelectedItem=draft.Theme;
        var panel=new StackPanel{Margin=new Thickness(20)}; Content=new ScrollViewer{Content=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
        void Header(string s)=>panel.Children.Add(new TextBlock{Text=s,FontSize=17,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,16,0,6)});
        void Field(string label,FrameworkElement control) { var line=new DockPanel{Margin=new Thickness(0,3,0,3)}; var t=new TextBlock{Text=label,Width=190,VerticalAlignment=VerticalAlignment.Center}; DockPanel.SetDock(t,Dock.Left);line.Children.Add(t);line.Children.Add(control);panel.Children.Add(line); }
        Button Button(string name,Action a){var b=new Button{Content=name,Padding=new Thickness(10,5,10,5),Margin=new Thickness(0,4,6,4)};b.Click+=(_,_)=>a();return b;}
        void TextColumn(DataGrid g,string label,string field,double width)=>g.Columns.Add(new DataGridTextColumn{Header=label,Binding=new Binding(field){UpdateSourceTrigger=UpdateSourceTrigger.PropertyChanged},Width=new DataGridLength(width)});
        Header("GENERAL"); Field("Output folder",folder);panel.Children.Add(Button("Browse output folder",()=> { using var picker=new System.Windows.Forms.FolderBrowserDialog(); if(picker.ShowDialog()==System.Windows.Forms.DialogResult.OK) folder.Text=picker.SelectedPath;}));Field("Filename (.NET date pattern)",filename);Field("Capture FPS",fps);Field("Theme",theme);panel.Children.Add(startup);panel.Children.Add(clipboard);panel.Children.Add(notify);
        Header("CAPTURE SHORTCUTS");TextColumn(captures,"Shortcut","Shortcut",250);TextColumn(captures,"Audio: none / system / microphone","Audio",300);captures.ItemsSource=cs;panel.Children.Add(captures);
        var cr=new StackPanel{Orientation=Orientation.Horizontal};cr.Children.Add(Button("Add capture shortcut",()=>cs.Add(new CaptureBinding{Shortcut="Ctrl+Shift+F9"})));cr.Children.Add(Button("Remove selected",()=>{if(captures.SelectedItem is CaptureBinding c)cs.Remove(c);}));panel.Children.Add(cr);
        Header("EXPORT BUTTONS / SHORTCUTS");TextColumn(presets,"Label","Name",110);TextColumn(presets,"Format","Format",70);TextColumn(presets,"FPS","Fps",55);TextColumn(presets,"CRF","Crf",55);TextColumn(presets,"Max width","MaxWidth",85);TextColumn(presets,"GIF colors","GifColors",85);TextColumn(presets,"Audio","Audio",100);TextColumn(presets,"Shortcut","Shortcut",125);TextColumn(presets,"Folder override","Folder",180);presets.ItemsSource=ps;panel.Children.Add(presets);
        var pr=new StackPanel{Orientation=Orientation.Horizontal};pr.Children.Add(Button("Add preset",()=>ps.Add(new ExportPreset{Name="MP4 "+(ps.Count+1),Shortcut=""})));pr.Children.Add(Button("Remove selected",()=>{if(presets.SelectedItem is ExportPreset p)ps.Remove(p);}));pr.Children.Add(Button("Move up",()=>{var i=presets.SelectedIndex;if(i>0)ps.Move(i,i-1);}));pr.Children.Add(Button("Move down",()=>{var i=presets.SelectedIndex;if(i>=0&&i<ps.Count-1)ps.Move(i,i+1);}));panel.Children.Add(pr);Field("Edit shortcut",edit);
        Header("SHORTCUT VERIFICATION");panel.Children.Add(new TextBlock{Text="Save changes, then enable test mode and press a shortcut. Test mode does not record.",TextWrapping=TextWrapping.Wrap});
        var buttons=new StackPanel{Orientation=Orientation.Horizontal};buttons.Children.Add(Button("Save & register",Save));buttons.Children.Add(Button("Test shortcuts",()=>{if(QuickRuntime.Keys!=null){QuickRuntime.Keys.TestMode=true;QuickRuntime.Keys.Received=s=>status.Text="RECEIVED: "+s+" at "+DateTime.Now.ToString("HH:mm:ss")+"\n"+string.Join("\n",QuickRuntime.Registration);status.Text="Test mode: press any registered shortcut.\n"+string.Join("\n",QuickRuntime.Registration);}}));buttons.Children.Add(Button("End test",()=>{if(QuickRuntime.Keys!=null){QuickRuntime.Keys.TestMode=false;QuickRuntime.Keys.Received=null;}status.Text=string.Join("\n",QuickRuntime.Registration);}));panel.Children.Add(buttons);panel.Children.Add(status);status.Text=string.Join("\n",QuickRuntime.Registration);
        ApplyTheme(); theme.SelectionChanged+=(_,_)=>ApplyTheme();
    }
    void ApplyTheme() { var dark=(string?)theme.SelectedItem=="dark" || ((string?)theme.SelectedItem=="system" && SystemColors.WindowColor.R<128); Background=dark?new SolidColorBrush(Color.FromRgb(35,35,38)):SystemColors.WindowBrush; Foreground=dark?Brushes.White:SystemColors.WindowTextBrush; }
    void Save()
    {
        try {
            presets.CommitEdit(DataGridEditingUnit.Cell,true);presets.CommitEdit(DataGridEditingUnit.Row,true);captures.CommitEdit(DataGridEditingUnit.Cell,true);captures.CommitEdit(DataGridEditingUnit.Row,true);
            draft.OutputFolder=folder.Text;draft.Filename=filename.Text;draft.CaptureFps=int.Parse(fps.Text);draft.EditShortcut=edit.Text;draft.Theme=(string)theme.SelectedItem;draft.StartWithWindows=startup.IsChecked==true;draft.CopyFile=clipboard.IsChecked==true;draft.Notify=notify.IsChecked==true;draft.Presets=ps.ToList();draft.CaptureShortcuts=cs.ToList();draft.Save();
            QuickRuntime.Settings=JsonSerializer.Deserialize<QuickSettings>(JsonSerializer.Serialize(draft))!;QuickRuntime.ApplyTheme();QuickRuntime.Register();QuickRuntime.ApplyStartup();status.Text="Saved.\n"+string.Join("\n",QuickRuntime.Registration);
        } catch(Exception ex){status.Text="Not fully applied: "+ex.Message;QuickRuntime.Log(ex.ToString());}
    }
}
