using System.ComponentModel;
using System.Diagnostics;
using Rhmn.Core;
using Rhmn.Studio.Services;

namespace Rhmn.Studio;

internal sealed class MainForm:Form
{
    static readonly Color Background=Color.FromArgb(11,16,26),Card=Color.FromArgb(21,30,46),Field=Color.FromArgb(31,43,62),TextMain=Color.FromArgb(238,244,250),Muted=Color.FromArgb(173,188,207),Accent=Color.FromArgb(104,232,195);
    readonly BindingList<RenderJob> jobs=[];
    readonly ListBox sources=new(){Dock=DockStyle.Fill,IntegralHeight=false};
    readonly ComboBox resolution=new(),fps=new(),preset=new(),mode=new();
    readonly NumericUpDown duration=new(){Minimum=1,Maximum=600,DecimalPlaces=3,Value=12},bitrate=new(){Minimum=1,Maximum=200,Value=60};
    readonly CheckBox network=new(){Text="Izinkan aset dari internet",AutoSize=true},loop=new(){Text="Periksa kesamaan batas loop",Checked=true,AutoSize=true};
    readonly TextBox output=new(){ReadOnly=true};
    readonly Label estimate=new(){AutoSize=true,ForeColor=Muted,MaximumSize=new Size(305,0)};
    readonly Label status=new(){Dock=DockStyle.Fill,Text="Siap • Impor proyek atau buka contoh untuk mulai",TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=true};
    readonly Label previewTitle=new(){Text="PRATINJAU PROYEK",Dock=DockStyle.Fill,ForeColor=Muted,TextAlign=ContentAlignment.MiddleLeft};
    readonly Panel viewport=new(){Dock=DockStyle.Fill,BackColor=Color.FromArgb(7,11,19)};
    readonly TextBox log=new(){Multiline=true,ReadOnly=true,Dock=DockStyle.Fill,ScrollBars=ScrollBars.Vertical};
    readonly TextBox qc=new(){Multiline=true,ReadOnly=true,Dock=DockStyle.Fill,ScrollBars=ScrollBars.Vertical};
    readonly DataGridView queue=new(){Dock=DockStyle.Fill,AutoGenerateColumns=false,AllowUserToAddRows=false,AllowUserToDeleteRows=false,ReadOnly=true,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false,RowHeadersVisible=false};
    readonly ProgressBar progress=new(){Dock=DockStyle.Bottom,Height=8,Maximum=1000};
    readonly List<Control> mutationControls=[];
    readonly Button cancel;
    readonly TabControl tabs=new(){Dock=DockStyle.Fill,DrawMode=TabDrawMode.OwnerDrawFixed,ItemSize=new Size(130,32),SizeMode=TabSizeMode.Fixed};
    StudioBrowser? preview;
    CancellationTokenSource? previewCancellation,renderCancellation;
    bool busy,loading,closeAfterCancel;
    readonly string statePath;
    public MainForm(string? sessionPath=null)
    {
        statePath=sessionPath??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"RHMN","MotionStudio","session.json");
        Text="RHMN Motion Graphic Studio";Width=1440;Height=960;MinimumSize=new Size(1120,780);StartPosition=FormStartPosition.CenterScreen;
        BackColor=Background;ForeColor=TextMain;Font=new Font("Segoe UI",10);
        Icon=Icon.ExtractAssociatedIcon(Environment.ProcessPath!);
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Padding=new Padding(20)};
        root.RowStyles.Add(new(SizeType.Absolute,82));root.RowStyles.Add(new(SizeType.Percent,100));root.RowStyles.Add(new(SizeType.Absolute,44));Controls.Add(root);
        var header=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2};header.ColumnStyles.Add(new(SizeType.Percent,100));header.ColumnStyles.Add(new(SizeType.Absolute,280));
        var brand=new FlowLayoutPanel{FlowDirection=FlowDirection.TopDown,Dock=DockStyle.Fill,WrapContents=false};
        brand.Controls.Add(new Label{Text="RHMN  /  MOTION GRAPHIC STUDIO",AutoSize=true,Font=new Font("Segoe UI",20,FontStyle.Bold),ForeColor=TextMain,Margin=new Padding(0)});
        brand.Controls.Add(new Label{Text="Dari proyek web menjadi video. Kendali penuh, frame demi frame.",AutoSize=true,ForeColor=Muted,Margin=new Padding(0,6,0,0)});
        var headerActions=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft};headerActions.Controls.Add(Button("Panduan",()=>OpenGuide(),false));headerActions.Controls.Add(Button("Buka contoh",()=>LoadSamples()));
        header.Controls.Add(brand,0,0);header.Controls.Add(headerActions,1,0);root.Controls.Add(header,0,0);
        var body=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,Margin=Padding.Empty};body.ColumnStyles.Add(new(SizeType.Absolute,350));body.ColumnStyles.Add(new(SizeType.Percent,100));root.Controls.Add(body,0,1);
        var sidebar=new Panel{Dock=DockStyle.Fill,AutoScroll=true,BackColor=Card,Padding=new Padding(18),Margin=new Padding(0,0,16,0)};
        var form=new TableLayoutPanel{Dock=DockStyle.Top,ColumnCount=1,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Padding=Padding.Empty};form.ColumnStyles.Add(new(SizeType.Percent,100));sidebar.Controls.Add(form);body.Controls.Add(sidebar,0,0);
        void Add(Control c,int height=0){c.Margin=new Padding(0,0,0,9);c.Dock=DockStyle.Fill;var r=form.RowCount++;form.RowStyles.Add(height>0?new RowStyle(SizeType.Absolute,height):new RowStyle(SizeType.AutoSize));form.Controls.Add(c,0,r);}
        Add(Heading("01  PROYEK"));
        var import=new FlowLayoutPanel{AutoSize=true,WrapContents=true};import.Controls.Add(Button("Impor HTML",()=>ImportFiles()));import.Controls.Add(Button("Impor folder",()=>ImportFolder()));Add(import);
        sources.DisplayMember="Name";sources.BackColor=Field;sources.ForeColor=TextMain;sources.BorderStyle=BorderStyle.FixedSingle;sources.SelectedIndexChanged+=async(_,_)=>{if(!busy&&!loading)await GuardAsync(PreviewAsync);};mutationControls.Add(sources);Add(sources,100);
        Add(Heading("02  PENGATURAN EKSPOR"));
        resolution.Items.AddRange(Resolution.Presets);resolution.SelectedIndex=0;
        fps.Items.AddRange(FrameRate.Supported);fps.SelectedIndex=4;
        preset.Items.AddRange(["Cepat","Seimbang","Kualitas tinggi"]);preset.SelectedIndex=1;
        mode.Items.AddRange(["Otomatis (disarankan)","Fungsi renderAtTime(t)","Animasi CSS / RAF / timer"]);mode.SelectedIndex=0;
        Add(Labeled("Resolusi video",resolution));
        var pair=new TableLayoutPanel{ColumnCount=2,AutoSize=true,Dock=DockStyle.Fill};pair.ColumnStyles.Add(new(SizeType.Percent,50));pair.ColumnStyles.Add(new(SizeType.Percent,50));pair.Controls.Add(Labeled("Frame rate",fps),0,0);pair.Controls.Add(Labeled("Durasi (detik)",duration),1,0);Add(pair);
        Add(Labeled("Bitrate target (Mbps)",bitrate));Add(Labeled("Kecepatan enkoder",preset));Add(Labeled("Pengendalian waktu",mode));Add(loop);Add(network);Add(estimate);
        foreach(var c in new Control[]{resolution,fps,duration,bitrate,preset,mode,network,loop})mutationControls.Add(c);
        foreach(var c in new[]{resolution,fps,preset,mode}){c.DropDownStyle=ComboBoxStyle.DropDownList;c.FlatStyle=FlatStyle.Flat;c.BackColor=Field;c.ForeColor=TextMain;}
        resolution.SelectedIndexChanged+=(_,_)=>UpdateEstimate();fps.SelectedIndexChanged+=(_,_)=>UpdateEstimate();duration.ValueChanged+=(_,_)=>UpdateEstimate();bitrate.ValueChanged+=(_,_)=>UpdateEstimate();
        foreach(var n in new[]{duration,bitrate}){n.BackColor=Field;n.ForeColor=TextMain;n.BorderStyle=BorderStyle.FixedSingle;}
        Add(Heading("03  SIMPAN & RENDER"));
        output.Text=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),"RHMN Motion");Add(output,34);Add(Button("Pilih folder hasil",()=>ChooseOutput()));
        Add(Button("+ Tambah ke antrean",()=>EnqueueSelected()));Add(Button("+ Antrekan semua proyek",()=>EnqueueAll()));
        Add(ButtonAsync("MULAI BATCH RENDER",RenderQueueAsync,true),48);
        cancel=Button("Batalkan render",()=>renderCancellation?.Cancel(),false);cancel.Enabled=false;Add(cancel,42);
        var right=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Margin=Padding.Empty};right.RowStyles.Add(new(SizeType.Absolute,40));right.RowStyles.Add(new(SizeType.Percent,58));right.RowStyles.Add(new(SizeType.Absolute,48));right.RowStyles.Add(new(SizeType.Percent,42));body.Controls.Add(right,1,0);
        var previewBar=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2};previewBar.ColumnStyles.Add(new(SizeType.Percent,100));previewBar.ColumnStyles.Add(new(SizeType.Absolute,180));previewBar.Controls.Add(previewTitle,0,0);previewBar.Controls.Add(ButtonAsync("Muat ulang preview",PreviewAsync),1,0);right.Controls.Add(previewBar,0,0);right.Controls.Add(viewport,0,1);
        var empty=new Label{Dock=DockStyle.Fill,Text="RUANG PRATINJAU\n\nImpor file HTML beserta folder asetnya\natau pilih Buka contoh di atas.",TextAlign=ContentAlignment.MiddleCenter,ForeColor=Muted,Font=new Font("Segoe UI",13)};viewport.Controls.Add(empty);
        var queueTools=new FlowLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(0,6,0,0),WrapContents=false};queueTools.Controls.Add(Button("Simpan antrean",()=>SaveQueue()));queueTools.Controls.Add(Button("Buka antrean",()=>OpenQueue()));queueTools.Controls.Add(Button("Hapus terpilih",()=>RemoveJob()));queueTools.Controls.Add(ButtonAsync("Periksa MP4",InspectFileAsync));queueTools.Controls.Add(Button("Folder hasil",()=>OpenOutput(),false));right.Controls.Add(queueTools,0,2);
        foreach(var (name,control) in new (string,Control)[]{("Antrean",queue),("Laporan QC",qc),("Aktivitas",log)}){var page=new TabPage(name){BackColor=Card,ForeColor=TextMain,Padding=new Padding(8)};page.Controls.Add(control);tabs.TabPages.Add(page);}
        tabs.DrawItem+=(_,e)=>{var selected=e.Index==tabs.SelectedIndex;using var b=new SolidBrush(selected?Field:Card);e.Graphics.FillRectangle(b,e.Bounds);TextRenderer.DrawText(e.Graphics,tabs.TabPages[e.Index].Text,Font,e.Bounds,selected?Accent:Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);};right.Controls.Add(tabs,0,3);
        ConfigureGrid();
        foreach(var t in new[]{log,qc,output}){t.BackColor=Field;t.ForeColor=TextMain;t.BorderStyle=BorderStyle.FixedSingle;}
        qc.Text="Pemeriksaan setelah render: resolusi, FPS, durasi, codec, ukuran berkas, jumlah frame, dekode penuh dan batas loop.\r\n\r\nLogo, lisensi, banding dan flicker perlu diperiksa manual. Lulus teknis tidak menjamin diterima Adobe Stock.";
        var footer=new Panel{Dock=DockStyle.Fill};footer.Controls.Add(status);footer.Controls.Add(progress);root.Controls.Add(footer,0,2);
        AllowDrop=true;DragEnter+=(_,e)=>{if(!busy&&e.Data?.GetDataPresent(DataFormats.FileDrop)==true)e.Effect=DragDropEffects.Copy;};DragDrop+=(_,e)=>{if(!busy&&e.Data?.GetData(DataFormats.FileDrop) is string[] paths)Guard(()=>ImportPaths(paths));};
        FormClosing+=OnClosing;
        Shown+=async(_,_)=>{RestoreSession();UpdateEstimate();await GuardAsync(PreviewAsync);};
    }
    Label Heading(string text)=>new(){Text=text,AutoSize=true,ForeColor=Accent,Font=new Font("Segoe UI",10,FontStyle.Bold),Padding=new Padding(0,10,0,3)};
    Control Labeled(string text,Control c)
    {
        var p=new TableLayoutPanel{ColumnCount=1,RowCount=2,AutoSize=true,Dock=DockStyle.Fill,Margin=Padding.Empty};p.RowStyles.Add(new(SizeType.Absolute,24));p.RowStyles.Add(new(SizeType.Absolute,35));p.Controls.Add(new Label{Text=text,Dock=DockStyle.Fill,ForeColor=Muted},0,0);c.Dock=DockStyle.Fill;p.Controls.Add(c,0,1);return p;
    }
    Button Button(string text,Action action,bool mutation=true)
    {
        var b=new Button{Text=text,AutoSize=true,Height=34,MinimumSize=new Size(80,34),FlatStyle=FlatStyle.Flat,BackColor=Field,ForeColor=TextMain,Cursor=Cursors.Hand,Padding=new Padding(8,3,8,3),Margin=new Padding(0,0,7,0)};b.FlatAppearance.BorderColor=Color.FromArgb(60,76,98);b.Click+=(_,_)=>Guard(action);if(mutation)mutationControls.Add(b);return b;
    }
    Button ButtonAsync(string text,Func<Task> action,bool primary=false)
    {
        var b=Button(text,()=>{});b.Click+=async(_,_)=>await GuardAsync(action);if(primary){b.BackColor=Accent;b.ForeColor=Background;b.Font=new Font(Font,FontStyle.Bold);b.FlatAppearance.BorderSize=0;}return b;
    }
    void ConfigureGrid()
    {
        queue.BackgroundColor=Card;queue.BorderStyle=BorderStyle.None;queue.GridColor=Field;queue.EnableHeadersVisualStyles=false;
        queue.ColumnHeadersDefaultCellStyle=new(){BackColor=Field,ForeColor=TextMain,Font=new Font(Font,FontStyle.Bold),Padding=new Padding(4)};
        queue.DefaultCellStyle=new(){BackColor=Card,ForeColor=TextMain,SelectionBackColor=Color.FromArgb(41,76,82),SelectionForeColor=TextMain,Padding=new Padding(4)};
        queue.AlternatingRowsDefaultCellStyle.BackColor=Color.FromArgb(25,35,52);queue.RowTemplate.Height=38;queue.ColumnHeadersHeight=38;
        queue.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName="Name",HeaderText="Proyek",Width=160});
        queue.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName="Status",HeaderText="Status",Width=125});
        queue.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName="OutputFile",HeaderText="File hasil",AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill});
        queue.DataSource=jobs;
        queue.SelectionChanged+=(_,_)=>{if(queue.CurrentRow?.DataBoundItem is RenderJob j)status.Text=$"{j.Settings.Width}×{j.Settings.Height} • {j.Settings.Fps} • {j.Settings.DurationSeconds}s • {j.Detail}";};
    }
    RenderSettings Settings()=>new(){Width=((Resolution)resolution.SelectedItem!).Width,Height=((Resolution)resolution.SelectedItem!).Height,Fps=(FrameRate)fps.SelectedItem!,DurationSeconds=duration.Value,BitrateMbps=(int)bitrate.Value,EncoderPreset=new[]{"fast","medium","slow"}[preset.SelectedIndex],Mode=(ClockMode)mode.SelectedIndex,AllowNetwork=network.Checked,CheckLoop=loop.Checked};
    void UpdateEstimate(){if(resolution.SelectedIndex<0||fps.SelectedIndex<0)return;var s=Settings();estimate.Text=$"{s.FrameCount:N0} frame • estimasi {s.EstimatedBytes/1_000_000d:0} MB\nAdobe Stock: durasi 5–60 detik.";}
    void ImportFiles(){using var dialog=new OpenFileDialog{Title="Pilih HTML utama",Filter="Proyek HTML|*.html;*.htm",Multiselect=true};if(dialog.ShowDialog(this)==DialogResult.OK)ImportPaths(dialog.FileNames);}
    void ImportFolder(){using var dialog=new FolderBrowserDialog{Description="Pilih folder proyek atau folder yang berisi beberapa proyek",UseDescriptionForTitle=true};if(dialog.ShowDialog(this)==DialogResult.OK)ImportPaths([dialog.SelectedPath]);}
    void ImportPaths(IEnumerable<string> paths)
    {
        foreach(var path in paths)
        {
            if(Directory.Exists(path))
            {
                var entries=ProjectFiles.FindEntries(path);
                if(entries.Length==0)entries=Directory.EnumerateDirectories(path).Where(d=>!Path.GetFileName(d).StartsWith('.')&&Path.GetFileName(d)!="node_modules").SelectMany(ProjectFiles.FindEntries).ToArray();
                if(entries.Length==0)throw new IOException("Tidak menemukan HTML pada folder atau subfolder langsung.");
                foreach(var entry in entries)AddSource(entry);
            }
            else AddSource(ProjectFiles.ValidateEntry(path));
        }
        if(sources.SelectedIndex<0&&sources.Items.Count>0)sources.SelectedIndex=0;
        Log($"Daftar proyek: {sources.Items.Count} file.");Persist();
    }
    void AddSource(string path){path=Path.GetFullPath(path);if(!sources.Items.Cast<SourceEntry>().Any(p=>p.Path.Equals(path,StringComparison.OrdinalIgnoreCase)))sources.Items.Add(new SourceEntry(path));}
    void LoadSamples()=>ImportPaths([Path.Combine(AppContext.BaseDirectory,"samples")]);
    async Task PreviewAsync()
    {
        if(busy||sources.SelectedItem is not SourceEntry entry)return;
        previewCancellation?.Cancel();previewCancellation?.Dispose();previewCancellation=new();var ct=previewCancellation.Token;
        preview?.Dispose();preview=new StudioBrowser();var current=preview;viewport.Controls.Add(current.View);current.View.BringToFront();
        previewTitle.Text="PRATINJAU • "+entry.Name;
        try {await current.OpenAsync(entry.Path,Settings(),false,ct);status.Text="Preview langsung siap. Render akan dimulai kembali dari t=0.";}
        catch(OperationCanceledException)when(ct.IsCancellationRequested){}
        catch(ObjectDisposedException)when(ct.IsCancellationRequested){}
    }
    void ChooseOutput(){using var d=new FolderBrowserDialog{Description="Pilih folder untuk video MP4",UseDescriptionForTitle=true};if(d.ShowDialog(this)==DialogResult.OK){output.Text=d.SelectedPath;Persist();}}
    void EnqueueSelected(){if(sources.SelectedItem is not SourceEntry source)throw new InvalidOperationException("Impor dan pilih proyek terlebih dahulu.");AddJob(source);}
    void EnqueueAll(){if(sources.Items.Count==0)throw new InvalidOperationException("Belum ada proyek.");foreach(SourceEntry source in sources.Items)AddJob(source);}
    void AddJob(SourceEntry source)
    {
        var s=Settings();s.Validate();var folder=Path.GetFullPath(output.Text);Directory.CreateDirectory(folder);
        jobs.Add(new(){EntryFile=source.Path,Settings=s,OutputFile=ProjectFiles.UniqueOutput(folder,source.Path,jobs.Select(j=>j.OutputFile))});Persist();status.Text=$"{jobs.Count} pekerjaan dalam antrean. Pengaturan disimpan untuk tiap pekerjaan.";
    }
    async Task RenderQueueAsync()
    {
        if(busy)return;
        var pending=jobs.Where(j=>j.Status is "Menunggu" or "Gagal" or "Dibatalkan").ToArray();
        if(pending.Length==0)throw new InvalidOperationException("Tambahkan proyek ke antrean terlebih dahulu.");
        RenderService.ValidateTools();busy=true;foreach(var c in mutationControls)c.Enabled=false;cancel.Enabled=true;
        previewCancellation?.Cancel();preview?.Dispose();preview=null;renderCancellation=new();var ct=renderCancellation.Token;
        var completed=0;var failed=0;
        try
        {
            var service=new RenderService(viewport);
            foreach(var job in pending)
            {
                if(ct.IsCancellationRequested)break;
                job.Status="Merender";job.Detail="";jobs.ResetBindings();Persist();previewTitle.Text="RENDER • "+job.Name;
                var reportProgress=new Progress<RenderProgress>(p=>{progress.Value=Math.Clamp((int)(p.Fraction*1000),0,1000);status.Text=p.Message;});
                try
                {
                    Log("Mulai: "+job.EntryFile);
                    var report=await service.RenderAsync(job,reportProgress,ct);
                    completed++;job.Status=report.TechnicalPassed?"Selesai":"Tinjau QC";job.Detail=report.Summary;
                    qc.Text=Path.GetFileName(job.OutputFile)+"\r\n\r\n"+report.Summary+"\r\n\r\n"+string.Join("\r\n\r\n",report.Checks.Select(c=>$"[{c.Status}] {c.Name}\r\n{c.Detail}"));Log("Tersimpan: "+job.OutputFile);
                }
                catch(OperationCanceledException){job.Status="Dibatalkan";job.Detail="Render dihentikan; file parsial dibersihkan.";Log(job.Detail);break;}
                catch(Exception e){failed++;job.Status="Gagal";job.Detail=e.Message;Log("GAGAL: "+e.Message);}
                finally {jobs.ResetBindings();Persist();}
            }
        }
        finally
        {
            busy=false;foreach(var c in mutationControls)c.Enabled=true;cancel.Enabled=false;renderCancellation.Dispose();renderCancellation=null;
            status.Text=$"Batch berakhir • {completed} tersimpan • {failed} gagal"+(ct.IsCancellationRequested?" • dibatalkan":"");
            if(closeAfterCancel)Close();else {tabs.SelectedIndex=completed>0?1:2;await PreviewAsync();}
        }
    }
    async Task InspectFileAsync()
    {
        using var d=new OpenFileDialog{Filter="Video MP4|*.mp4",Title="Periksa video MP4"};if(d.ShowDialog(this)!=DialogResult.OK)return;
        RenderService.ValidateTools();using var timeout=new CancellationTokenSource(TimeSpan.FromMinutes(10));status.Text="Memeriksa video…";
        var info=await Encoder.ProbeAsync(RenderService.Ffprobe,d.FileName,timeout.Token);var report=QualityCheck.Inspect(info);
        await Encoder.RunAsync(RenderService.Ffmpeg,["-v","error","-xerror","-i",d.FileName,"-f","null","-"],timeout.Token);
        report.Checks.Add(new("LULUS","Dekode penuh","Tidak ada kesalahan dekode."));
        qc.Text=report.Summary+"\r\n\r\n"+string.Join("\r\n\r\n",report.Checks.Select(c=>$"[{c.Status}] {c.Name}\r\n{c.Detail}"));tabs.SelectedIndex=1;status.Text=report.Summary;
    }
    void RemoveJob(){if(queue.CurrentRow?.DataBoundItem is RenderJob job){jobs.Remove(job);Persist();}}
    void SaveQueue(){using var d=new SaveFileDialog{Filter="Antrean RHMN|*.rhmn.json",FileName="Antrean-RHMN.rhmn.json"};if(d.ShowDialog(this)==DialogResult.OK){JsonFiles.Save(d.FileName,Session());Log("Antrean disimpan.");}}
    void OpenQueue(){using var d=new OpenFileDialog{Filter="Antrean RHMN|*.rhmn.json;*.json"};if(d.ShowDialog(this)==DialogResult.OK){ApplySession(JsonFiles.Load<StudioSession>(d.FileName)??throw new InvalidDataException("Antrean tidak valid."));Persist();}}
    StudioSession Session()=>new(sources.Items.Cast<SourceEntry>().Select(s=>s.Path).ToArray(),jobs.ToList(),output.Text,Settings());
    void Persist(){if(loading)return;try{JsonFiles.Save(statePath,Session());}catch(Exception e){Log("Sesi belum tersimpan: "+e.Message);}}
    void RestoreSession(){if(File.Exists(statePath))try{var state=JsonFiles.Load<StudioSession>(statePath);if(state is not null)ApplySession(state);}catch(Exception e){Log("Sesi sebelumnya tidak dapat dibuka: "+e.Message);}}
    internal async Task VerifyUiAsync(string evidenceFolder)
    {
        loading=true;
        try{LoadSamples();}finally{loading=false;}
        await PreviewAsync();
        if(sources.Items.Count!=3||preview?.View.CoreWebView2 is null)throw new InvalidOperationException("UI import/preview did not initialize.");
        await preview.EvaluateAsync("new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(()=>resolve(true))))",CancellationToken.None);
        output.Text=Path.Combine(evidenceFolder,"ui-output");EnqueueAll();
        if(jobs.Count!=3||jobs.Any(j=>j.Settings.Width!=3840||j.Settings.Height!=2160))throw new InvalidOperationException("UI batch queue is incomplete.");
        Persist();
        var restored=JsonFiles.Load<StudioSession>(statePath)??throw new InvalidDataException("UI session was not saved.");
        if(restored.Jobs.Count!=3)throw new InvalidDataException("UI queue persistence failed.");
        status.Text="Uji Windows lulus • 3 proyek diimpor • preview aktif • antrean tersimpan";
        Refresh();
        using var image=new Bitmap(Width,Height);
        using(var graphics=Graphics.FromImage(image))graphics.CopyFromScreen(Location,Point.Empty,Size);
        image.Save(Path.Combine(evidenceFolder,"studio-windows.png"),System.Drawing.Imaging.ImageFormat.Png);
    }
    void ApplySession(StudioSession s)
    {
        s.Settings.Validate();foreach(var job in s.Jobs){job.Settings.Validate();if(Path.GetExtension(job.OutputFile)!=".mp4")throw new InvalidDataException("Tujuan antrean harus MP4.");}
        loading=true;
        try
        {
            sources.Items.Clear();foreach(var source in s.Sources)if(File.Exists(source))AddSource(ProjectFiles.ValidateEntry(source));
            jobs.Clear();foreach(var job in s.Jobs){if(job.Status=="Merender")job.Status="Menunggu";jobs.Add(job);}
            output.Text=Path.GetFullPath(s.OutputFolder);resolution.SelectedIndex=Array.FindIndex(Resolution.Presets,r=>r.Width==s.Settings.Width&&r.Height==s.Settings.Height);if(resolution.SelectedIndex<0)resolution.SelectedIndex=0;
            fps.SelectedIndex=Array.IndexOf(FrameRate.Supported,s.Settings.Fps);duration.Value=s.Settings.DurationSeconds;bitrate.Value=s.Settings.BitrateMbps;preset.SelectedIndex=Array.IndexOf(new[]{"fast","medium","slow"},s.Settings.EncoderPreset);mode.SelectedIndex=(int)s.Settings.Mode;network.Checked=s.Settings.AllowNetwork;loop.Checked=s.Settings.CheckLoop;
            if(sources.Items.Count>0)sources.SelectedIndex=0;
        }
        finally{loading=false;UpdateEstimate();}
    }
    void OpenOutput(){Directory.CreateDirectory(output.Text);Process.Start(new ProcessStartInfo(Path.GetFullPath(output.Text)){UseShellExecute=true});}
    void OpenGuide()
    {
        var guide=new Form{Text="Panduan • RHMN Motion Graphic Studio",Width=850,Height=680,StartPosition=FormStartPosition.CenterParent,BackColor=Card,ForeColor=TextMain};
        var t=new TextBox{Multiline=true,ReadOnly=true,Dock=DockStyle.Fill,ScrollBars=ScrollBars.Vertical,BackColor=Card,ForeColor=TextMain,Font=new Font("Segoe UI",11),BorderStyle=BorderStyle.None,Text=File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"PANDUAN.md"))};guide.Padding=new Padding(24);guide.Controls.Add(t);guide.Show(this);
    }
    void OnClosing(object? sender,FormClosingEventArgs e)
    {
        if(busy){e.Cancel=true;if(MessageBox.Show(this,"Batalkan render dan tutup aplikasi?","Render masih berjalan",MessageBoxButtons.YesNo,MessageBoxIcon.Question)==DialogResult.Yes){closeAfterCancel=true;renderCancellation?.Cancel();}return;}
        Persist();previewCancellation?.Cancel();preview?.Dispose();previewCancellation?.Dispose();
    }
    void Log(string message)=>log.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
    void Guard(Action action){try{action();}catch(Exception e){ShowError(e);}}
    async Task GuardAsync(Func<Task> action){try{await action();}catch(OperationCanceledException){}catch(Exception e){ShowError(e);}}
    void ShowError(Exception e){Log(e.Message);status.Text="Ada masalah. Lihat tab Aktivitas.";MessageBox.Show(this,e.Message,"RHMN • Periksa kembali",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
    sealed record SourceEntry(string Path){public string Name=>$"{System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(Path))} / {System.IO.Path.GetFileName(Path)}";}
}
internal sealed record StudioSession(string[] Sources,List<RenderJob> Jobs,string OutputFolder,RenderSettings Settings);
