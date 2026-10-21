using System.Reflection;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Rhmn.Core;

namespace Rhmn.Studio.Services;

internal sealed class StudioBrowser : IDisposable
{
    public WebView2 View {get;} = new() {Dock=DockStyle.Fill,DefaultBackgroundColor=Color.FromArgb(12,17,27)};
    public List<string> BlockedResources {get;}=[];
    readonly string host="project-"+Guid.NewGuid().ToString("N")+".invalid";
    public static string UserDataFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"RHMN","MotionStudio","Browser");
    public async Task OpenAsync(string entry,RenderSettings settings,bool render,CancellationToken ct)
    {
        entry=ProjectFiles.ValidateEntry(entry);
        var options=new CoreWebView2EnvironmentOptions("--disable-background-timer-throttling --disable-renderer-backgrounding");
        var env=await CoreWebView2Environment.CreateAsync(null,UserDataFolder,options).WaitAsync(ct);
        await View.EnsureCoreWebView2Async(env).WaitAsync(ct);
        var core=View.CoreWebView2;
        core.Settings.AreDefaultContextMenusEnabled=false;
        core.Settings.AreDevToolsEnabled=false;
        core.Settings.AreBrowserAcceleratorKeysEnabled=false;
        core.Settings.IsWebMessageEnabled=false;
        core.Settings.AreHostObjectsAllowed=false;
        core.Settings.IsStatusBarEnabled=false;
        core.Settings.IsZoomControlEnabled=false;
        core.Settings.IsPasswordAutosaveEnabled=false;
        core.Settings.IsGeneralAutofillEnabled=false;
        core.PermissionRequested+=(_,e)=>e.State=CoreWebView2PermissionState.Deny;
        core.NewWindowRequested+=(_,e)=>e.Handled=true;
        core.DownloadStarting+=(_,e)=>e.Cancel=true;
        core.NavigationStarting+=(_,e)=>{if(!Uri.TryCreate(e.Uri,UriKind.Absolute,out var u)||u.Host!=host)e.Cancel=true;};
        core.AddWebResourceRequestedFilter("*",CoreWebView2WebResourceContext.All);
        core.WebResourceRequested+=(_,e)=>
        {
            if(Uri.TryCreate(e.Request.Uri,UriKind.Absolute,out var u) && (u.Host==host || u.Scheme is "data" or "blob" || settings.AllowNetwork && u.Scheme is "https" or "http")) return;
            if(BlockedResources.Count<20)BlockedResources.Add(e.Request.Uri);
            e.Response=env.CreateWebResourceResponse(new MemoryStream(),403,"Akses jaringan nonaktif","");
        };
        core.SetVirtualHostNameToFolderMapping(host,Path.GetDirectoryName(entry)!,CoreWebView2HostResourceAccessKind.DenyCors);
        if(render)
        {
            await core.CallDevToolsProtocolMethodAsync("Emulation.setDeviceMetricsOverride",JsonSerializer.Serialize(new {width=settings.Width,height=settings.Height,deviceScaleFactor=1,mobile=false,screenWidth=settings.Width,screenHeight=settings.Height})).WaitAsync(ct);
            using var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("Rhmn.Studio.Assets.render-clock.js") ?? throw new InvalidOperationException("Skrip render tidak ditemukan.");
            using var reader=new StreamReader(stream);
            var clock=(await reader.ReadToEndAsync(ct)).Replace("__RHMN_SEED__",settings.Seed.ToString(System.Globalization.CultureInfo.InvariantCulture));
            clock="window.__nativeClear=window.clearTimeout.bind(window);\n"+clock;
            await core.AddScriptToExecuteOnDocumentCreatedAsync(clock).WaitAsync(ct);
        }
        var done=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void Loaded(object? sender,CoreWebView2NavigationCompletedEventArgs args)
        {if(args.IsSuccess)done.TrySetResult();else done.TrySetException(new IOException("Halaman tidak dapat dibuka: "+args.WebErrorStatus));}
        core.NavigationCompleted+=Loaded;
        try
        {
            core.Navigate("https://"+host+"/"+Uri.EscapeDataString(Path.GetFileName(entry)));
            await done.Task.WaitAsync(TimeSpan.FromSeconds(45),ct);
        }
        finally {core.NavigationCompleted-=Loaded;}
    }
    public async Task<JsonElement> EvaluateAsync(string expression,CancellationToken ct)
    {
        var json=await View.CoreWebView2.CallDevToolsProtocolMethodAsync("Runtime.evaluate",JsonSerializer.Serialize(new {expression,awaitPromise=true,returnByValue=true})).WaitAsync(TimeSpan.FromSeconds(45),ct);
        using var doc=JsonDocument.Parse(json);
        if(doc.RootElement.TryGetProperty("exceptionDetails",out var error)) throw new InvalidOperationException("JavaScript: "+error.ToString());
        return doc.RootElement.GetProperty("result").TryGetProperty("value",out var value) ? value.Clone() : default;
    }
    public Task<JsonElement> FrameAsync(double seconds,ClockMode mode,CancellationToken ct)
    {
        var key=mode switch {ClockMode.RenderAtTime=>"contract",ClockMode.AnimationClock=>"clock",_=>"auto"};
        return EvaluateAsync($"window.__rhmnFrame({seconds.ToString("R",System.Globalization.CultureInfo.InvariantCulture)},'{key}')",ct);
    }
    public async Task<byte[]> CaptureAsync(RenderSettings s,CancellationToken ct)
    {
        var json=await View.CoreWebView2.CallDevToolsProtocolMethodAsync("Page.captureScreenshot",JsonSerializer.Serialize(new {format="png",fromSurface=true,captureBeyondViewport=true,clip=new{x=0,y=0,width=s.Width,height=s.Height,scale=1}})).WaitAsync(TimeSpan.FromSeconds(45),ct);
        using var doc=JsonDocument.Parse(json);
        var bytes=Convert.FromBase64String(doc.RootElement.GetProperty("data").GetString()!);
        if(bytes.Length<24 || System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16,4))!=s.Width || System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20,4))!=s.Height)
            throw new InvalidDataException("WebView2 menghasilkan ukuran frame yang berbeda dari target.");
        return bytes;
    }
    public void Dispose()=>View.Dispose();
}
