namespace Rhmn.Core;

public sealed record QualityItem(string Status,string Name,string Detail);
public sealed record QualityReport(DateTimeOffset CreatedAt,VideoInfo Video,List<QualityItem> Checks)
{
    public string Source {get;init;}="https://helpx.adobe.com/stock/contributor/submit-your-content/submit-videos/technical-requirements-for-video-submissions.html";
    public string RulesCheckedOn {get;init;}="2026-10-21";
    public bool TechnicalPassed=>Checks.All(c=>c.Status!="GAGAL");
    public string Summary=>TechnicalPassed ? "Pemeriksaan teknis lulus; tinjau visual dan hak penggunaan sebelum mengirim." : "Ada pemeriksaan teknis yang gagal; perbaiki sebelum mengirim.";
}
public static class QualityCheck
{
    public static QualityReport Inspect(VideoInfo v,RenderSettings? expected=null)
    {
        var result = new List<QualityItem>();
        void Check(bool good,string name,string info)=>result.Add(new(good?"LULUS":"GAGAL",name,info));
        Check(v.Duration>=4.999 && v.Duration<=60.001,"Durasi Adobe Stock",$"{v.Duration:0.###} detik; rentang 5–60 detik.");
        Check(v.Bytes>0 && v.Bytes<=3_900_000_000L,"Ukuran berkas",$"{v.Bytes/1_000_000d:0.0} MB; maksimum 3.900 MB.");
        Check(FrameRate.Supported.Any(f=>Math.Abs(f.Value-v.Fps)<0.015),"Frame rate",$"{v.Fps:0.###} fps.");
        var sizes=new (int,int)[]{(1920,1080),(2048,1080),(3840,2160),(4096,2160),(4096,2304),(2304,4096),(2160,4096),(2160,3840),(1080,1920),(1080,1080),(2160,2160)};
        result.Add(new(sizes.Contains((v.Width,v.Height))?"LULUS":"TINJAU","Resolusi",$"{v.Width} × {v.Height}. Ukuran nonstandar perlu ditinjau terhadap ketentuan Adobe."));
        Check(v.Codec is "h264" or "prores" or "mpeg2video" or "mpeg4" or "mjpeg" or "png","Codec",v.Codec);
        Check(v.Container.Split(',').Any(x=>x is "mov" or "mp4" or "mpeg"),"Kontainer",v.Container);
        result.Add(new(v.PixelFormat=="yuv420p"?"LULUS":"TINJAU","Kompatibilitas piksel",v.PixelFormat+"; yuv420p dipakai aplikasi untuk MP4 H.264."));
        Check(v.FrameCount>0,"Dekode frame",$"{v.FrameCount} frame berhasil dihitung oleh FFprobe.");
        if(expected is not null)
        {
            Check(v.Width==expected.Width&&v.Height==expected.Height,"Ukuran hasil sesuai pengaturan",$"Target {expected.Width} × {expected.Height}.");
            Check(v.FrameCount==expected.FrameCount,"Jumlah frame sesuai pengaturan",$"Target {expected.FrameCount}; hasil {v.FrameCount}.");
            Check(Math.Abs(v.Fps-expected.Fps.Value)<0.001,"FPS sesuai pengaturan",$"Target {expected.Fps}.");
            Check(Math.Abs(v.Duration-expected.ActualDuration)<=Math.Max(0.002,1/expected.Fps.Value),"Durasi hasil sesuai pengaturan",$"Target efektif {expected.ActualDuration:0.###} detik.");
        }
        result.Add(new("MANUAL","Kualitas visual dan hak penggunaan","Periksa banding, flicker, ketajaman, gerak, logo, merek, lisensi aset dan release. Pemeriksaan ini tidak menjamin persetujuan Adobe Stock."));
        return new(DateTimeOffset.UtcNow,v,result);
    }
}
