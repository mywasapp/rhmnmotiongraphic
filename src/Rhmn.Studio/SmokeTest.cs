using System.Security.Cryptography;
using Rhmn.Core;
using Rhmn.Studio.Services;

namespace Rhmn.Studio;

internal static class SmokeTest
{
    public static int Run(string outputFolder)
    {
        outputFolder=Path.GetFullPath(outputFolder);Directory.CreateDirectory(outputFolder);
        var exit=1;var results=new List<object>();
        using var form=new Form{Text="RHMN • Windows integration smoke test",Width=1000,Height=740,StartPosition=FormStartPosition.CenterScreen};
        var panel=new Panel{Dock=DockStyle.Fill};form.Controls.Add(panel);
        form.Shown+=async(_,_)=>
        {
            using var timeout=new CancellationTokenSource(TimeSpan.FromMinutes(18));
            try
            {
                var service=new RenderService(panel);
                var cases=new[]{("01-Aurora",new RenderSettings{Width=1920,Height=1080,DurationSeconds=5,BitrateMbps=16,EncoderPreset="fast"}), ("02-Geometri-CSS",new RenderSettings{Width=1920,Height=1080,Fps=new(30000,1001),DurationSeconds=5,BitrateMbps=16,EncoderPreset="fast"}), ("03-Partikel-RAF",new RenderSettings{Width=3840,Height=2160,DurationSeconds=5,BitrateMbps=40,EncoderPreset="fast"})};
                foreach(var (sample,settings) in cases)
                {
                    var job=new RenderJob{EntryFile=Path.Combine(AppContext.BaseDirectory,"samples",sample,"index.html"),OutputFile=Path.Combine(outputFolder,sample+".mp4"),Settings=settings};
                    var report=await service.RenderAsync(job,new Progress<RenderProgress>(p=>form.Text=$"RHMN test • {sample} • {p.Message}"),timeout.Token);
                    if(!report.TechnicalPassed)throw new InvalidOperationException("Technical QC failed for "+sample);
                    results.Add(new{sample,status="passed",report.Video,sha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(job.OutputFile)))});
                }
                // Cancellation must not publish a partial MP4.
                var cancelPath=Path.Combine(outputFolder,"cancelled.mp4");using var cancelled=new CancellationTokenSource();cancelled.Cancel();
                try{await service.RenderAsync(new(){EntryFile=Path.Combine(AppContext.BaseDirectory,"samples","01-Aurora","index.html"),OutputFile=cancelPath},new Progress<RenderProgress>(),cancelled.Token);throw new Exception("Cancellation did not stop rendering.");}catch(OperationCanceledException){}
                if(File.Exists(cancelPath)||Directory.GetFiles(outputFolder,"*.partial.mp4").Length>0)throw new Exception("Partial output leaked.");
                results.Add(new{sample="cancel-cleanup",status="passed"});exit=0;
            }
            catch(Exception e){results.Add(new{status="failed",error=e.ToString()});File.WriteAllText(Path.Combine(outputFolder,"failure.txt"),e.ToString());}
            finally{JsonFiles.Save(Path.Combine(outputFolder,"smoke-results.json"),new{exitCode=exit,platform=Environment.OSVersion.ToString(),architecture=System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),results});form.Close();}
        };
        Application.Run(form);return exit;
    }
}
