using System.Diagnostics;
using System.Text.Json;
using Rhmn.Core;

namespace Rhmn.Studio.Services;

internal sealed class RenderService(Panel viewport)
{
    public static string ToolsFolder=>Path.Combine(AppContext.BaseDirectory,"tools","ffmpeg");
    public static string Ffmpeg=>Path.Combine(ToolsFolder,"ffmpeg.exe");
    public static string Ffprobe=>Path.Combine(ToolsFolder,"ffprobe.exe");
    public static void ValidateTools()
    {
        if(!File.Exists(Ffmpeg)||!File.Exists(Ffprobe))throw new FileNotFoundException("FFmpeg/FFprobe belum tersedia. Pasang ulang paket lengkap RHMN Motion Graphic Studio.");
    }
    public async Task<QualityReport> RenderAsync(RenderJob job,IProgress<RenderProgress> progress,CancellationToken ct)
    {
        var s=job.Settings;s.Validate();ValidateTools();
        ProjectFiles.ValidateEntry(job.EntryFile);
        var output=Path.GetFullPath(job.OutputFile);
        if(File.Exists(output))throw new IOException("File tujuan sudah ada. Pilih nama baru agar hasil lama tetap tersimpan.");
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        var drive=new DriveInfo(Path.GetPathRoot(output)!);
        if(drive.AvailableFreeSpace<s.EstimatedBytes*2+256_000_000)throw new IOException("Ruang disk tidak cukup untuk estimasi hasil render.");
        var partial=Path.Combine(Path.GetDirectoryName(output)!,"."+Path.GetFileNameWithoutExtension(output)+"-"+Guid.NewGuid().ToString("N")+".partial.mp4");
        using var browser=new StudioBrowser();viewport.Controls.Add(browser.View);browser.View.BringToFront();
        Process? encoder=null;Task<string>? stderr=null;
        try
        {
            progress.Report(new(0,"Menyiapkan proyek dan aset…"));
            await browser.OpenAsync(job.EntryFile,s,true,ct);
            var ready=await browser.EvaluateAsync("window.__rhmnReady()",ct);
            if(ready.GetProperty("media").GetInt32()>0)throw new InvalidOperationException("Proyek berisi audio, video, atau iframe. Mode render frame saat ini hanya mendukung HTML/CSS/Canvas/SVG/WebGL tanpa media tertanam.");
            if(browser.BlockedResources.Count>0)throw new InvalidOperationException("Ada aset jaringan yang diblokir. Simpan aset di folder proyek atau aktifkan akses internet lalu ulangi.\n"+string.Join("\n",browser.BlockedResources));
            var start=Encoder.StartInfo(Ffmpeg,Encoder.Arguments(s,partial));start.RedirectStandardInput=true;
            encoder=new Process{StartInfo=start};encoder.Start();stderr=encoder.StandardError.ReadToEndAsync();
            var stdout=encoder.StandardOutput.ReadToEndAsync();
            using var kill=ct.Register(()=>{try{if(!encoder.HasExited)encoder.Kill(true);}catch(InvalidOperationException){}});
            byte[]? first=null;
            var watch=Stopwatch.StartNew();
            for(var frame=0;frame<s.FrameCount;frame++)
            {
                ct.ThrowIfCancellationRequested();
                await browser.FrameAsync(frame/s.Fps.Value,s.Mode,ct);
                var png=await browser.CaptureAsync(s,ct);
                first??=png;
                await encoder.StandardInput.BaseStream.WriteAsync(png,ct).AsTask().WaitAsync(TimeSpan.FromSeconds(90),ct);
                if(frame%Math.Max(1,(int)s.Fps.Value/4)==0||frame==s.FrameCount-1)
                {
                    var fps=(frame+1)/Math.Max(0.001,watch.Elapsed.TotalSeconds);
                    var remaining=TimeSpan.FromSeconds((s.FrameCount-frame-1)/fps);
                    progress.Report(new((frame+1d)/s.FrameCount*0.9,$"Frame {frame+1:N0}/{s.FrameCount:N0} • {fps:0.0} frame/detik • sisa {remaining:hh\\:mm\\:ss}"));
                }
            }
            encoder.StandardInput.Close();
            await encoder.WaitForExitAsync(ct).WaitAsync(TimeSpan.FromMinutes(5),ct);
            var log=await stderr;await stdout;
            if(encoder.ExitCode!=0)throw new IOException("FFmpeg gagal: "+log);
            progress.Report(new(0.92,"Memeriksa seluruh frame hasil ekspor…"));
            var info=await Encoder.ProbeAsync(Ffprobe,partial,ct);
            var report=QualityCheck.Inspect(info,s);
            if(info.Width!=s.Width||info.Height!=s.Height||info.FrameCount!=s.FrameCount||Math.Abs(info.Fps-s.Fps.Value)>0.001)throw new IOException("Verifikasi ekspor gagal: ukuran, FPS, atau jumlah frame tidak sesuai.");
            await Encoder.RunAsync(Ffmpeg,["-v","error","-xerror","-i",partial,"-f","null","-"],ct);
            report.Checks.Add(new("LULUS","Dekode menyeluruh","FFmpeg memeriksa video sampai akhir tanpa kesalahan dekode."));
            foreach(var canvas in ready.GetProperty("canvas").EnumerateArray())
            {
                var w=canvas.GetProperty("width").GetInt32();var h=canvas.GetProperty("height").GetInt32();
                var cssW=canvas.GetProperty("cssWidth").GetInt32();var cssH=canvas.GetProperty("cssHeight").GetInt32();
                if(w<cssW||h<cssH)report.Checks.Add(new("TINJAU","Ketajaman Canvas",$"Canvas {w}×{h} ditampilkan {cssW}×{cssH}; tingkatkan resolusi internal di kode proyek."));
            }
            if(s.CheckLoop&&first is not null)
            {
                progress.Report(new(0.97,"Membandingkan batas loop…"));
                await browser.FrameAsync(s.ActualDuration,s.Mode,ct);
                var endpoint=await browser.CaptureAsync(s,ct);
                var diff=ImageDifference(first,endpoint);
                report.Checks.Add(new(diff<0.01?"LULUS":"TINJAU","Kesamaan batas loop",$"Selisih rata-rata {diff:P2} antara t=0 dan t={s.ActualDuration:0.###}. Sampel 64×64; kontinuitas kecepatan gerak tetap perlu ditinjau."));
            }
            ct.ThrowIfCancellationRequested();
            File.Move(partial,output,false);
            try
            {
                JsonFiles.Save(Path.ChangeExtension(output,"qc.json"),report);
                File.WriteAllText(Path.ChangeExtension(output,"qc.txt"),report.Summary+Environment.NewLine+string.Join(Environment.NewLine,report.Checks.Select(c=>$"[{c.Status}] {c.Name}: {c.Detail}"))+Environment.NewLine+report.Source);
            }
            catch(Exception e) when(e is IOException or UnauthorizedAccessException)
            {report.Checks.Add(new("TINJAU","Penyimpanan laporan","Video tersimpan, laporan gagal disimpan: "+e.Message));}
            progress.Report(new(1,"Ekspor selesai. "+report.Summary));
            return report;
        }
        catch(Exception e) when(e is not OperationCanceledException)
        {
            if(encoder is not null) {try{if(!encoder.HasExited)encoder.Kill(true);}catch(InvalidOperationException){}}
            var detail=stderr is null ? "" : await stderr;
            throw new InvalidOperationException(e.Message+(string.IsNullOrWhiteSpace(detail)?"":"\nFFmpeg: "+detail),e);
        }
        finally
        {
            if(encoder is not null){try{if(!encoder.HasExited)encoder.Kill(true);}catch(InvalidOperationException){}encoder.Dispose();}
            if(File.Exists(partial)){try{File.Delete(partial);}catch(IOException){}}
            viewport.Controls.Remove(browser.View);
        }
    }
    static double ImageDifference(byte[] a,byte[] b)
    {
        using var sa=new MemoryStream(a);using var sb=new MemoryStream(b);
        using var ia=Image.FromStream(sa);using var ib=Image.FromStream(sb);
        using var ba=new Bitmap(ia,new Size(64,64));using var bb=new Bitmap(ib,new Size(64,64));
        long sum=0;for(var y=0;y<64;y++)for(var x=0;x<64;x++){var p=ba.GetPixel(x,y);var q=bb.GetPixel(x,y);sum+=Math.Abs(p.R-q.R)+Math.Abs(p.G-q.G)+Math.Abs(p.B-q.B);}
        return sum/(64d*64*3*255);
    }
}
