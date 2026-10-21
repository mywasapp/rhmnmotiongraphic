using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace Rhmn.Core;

public static class Encoder
{
    public static string[] Arguments(RenderSettings s, string output)
    {
        s.Validate();
        return ["-hide_banner","-loglevel","warning","-n","-f","image2pipe","-vcodec","png","-framerate",s.Fps.Argument,"-i","pipe:0","-an","-c:v","libx264","-preset",s.EncoderPreset,"-pix_fmt","yuv420p","-b:v",$"{s.BitrateMbps}M","-maxrate",$"{s.BitrateMbps*2}M","-bufsize",$"{s.BitrateMbps*4}M","-vf","scale=in_range=full:out_range=tv:out_color_matrix=bt709","-color_primaries","bt709","-color_trc","bt709","-colorspace","bt709","-color_range","tv","-fps_mode","cfr","-frames:v",s.FrameCount.ToString(CultureInfo.InvariantCulture),"-movflags","+faststart","-f","mp4",output];
    }
    public static ProcessStartInfo StartInfo(string executable, IEnumerable<string> args)
    {
        var start = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true, RedirectStandardOutput = true };
        foreach (var arg in args) start.ArgumentList.Add(arg);
        return start;
    }
    public static async Task<(string Output,string Error)> RunAsync(string executable,IEnumerable<string> args,CancellationToken ct)
    {
        using var process = new Process { StartInfo = StartInfo(executable,args) };
        if(!process.Start()) throw new InvalidOperationException("Tidak dapat menjalankan " + Path.GetFileName(executable));
        var stdout = process.StandardOutput.ReadToEndAsync(ct);
        var stderr = process.StandardError.ReadToEndAsync(ct);
        using var registration = ct.Register(() => { try { if(!process.HasExited) process.Kill(true); } catch(InvalidOperationException) {} });
        await process.WaitForExitAsync(ct);
        var result = (Output:await stdout,Error:await stderr);
        if(process.ExitCode != 0) throw new InvalidOperationException($"{Path.GetFileName(executable)} gagal ({process.ExitCode}): {result.Error}");
        return result;
    }
    public static async Task<VideoInfo> ProbeAsync(string ffprobe,string video,CancellationToken ct)
    {
        var (output,_) = await RunAsync(ffprobe,["-v","error","-count_frames","-show_streams","-show_format","-of","json",video],ct);
        return ParseProbe(output);
    }
    public static VideoInfo ParseProbe(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root=doc.RootElement;
        var streams=root.GetProperty("streams").EnumerateArray().ToArray();
        var v=streams.FirstOrDefault(x=>GetText(x,"codec_type")=="video");
        if(v.ValueKind==JsonValueKind.Undefined) throw new InvalidDataException("Tidak ada stream video.");
        var f=root.GetProperty("format");
        var frames = long.TryParse(GetText(v,"nb_read_frames"),out var count) ? count : -1;
        return new VideoInfo(v.GetProperty("width").GetInt32(),v.GetProperty("height").GetInt32(),Rate(GetText(v,"avg_frame_rate")),Rate(GetText(v,"r_frame_rate")),Number(GetText(f,"duration")),(long)Number(GetText(f,"size")),GetText(v,"codec_name"),GetText(v,"pix_fmt"),GetText(f,"format_name"),frames,streams.Count(x=>GetText(x,"codec_type")=="audio"));
    }
    static string GetText(JsonElement e,string name)=>e.TryGetProperty(name,out var p) ? p.ToString() : "";
    static double Number(string s)=>double.TryParse(s,NumberStyles.Float,CultureInfo.InvariantCulture,out var d) ? d : 0;
    static double Rate(string s) {var parts=s.Split('/');return parts.Length==2 && Number(parts[1])!=0 ? Number(parts[0])/Number(parts[1]) : Number(s);}
}
public sealed record VideoInfo(int Width,int Height,double Fps,double RawFps,double Duration,long Bytes,string Codec,string PixelFormat,string Container,long FrameCount,int AudioStreams);
