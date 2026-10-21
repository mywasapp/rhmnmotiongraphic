using Rhmn.Core;

var passed=0;
void Check(bool condition,string message){if(!condition)throw new Exception("FAIL: "+message);passed++;Console.WriteLine("PASS: "+message);}
void Reject(Action action,string message){try{action();}catch(ArgumentException){passed++;Console.WriteLine("PASS: "+message);return;}throw new Exception("FAIL: "+message);}
var s=new RenderSettings();s.Validate();Check(s.FrameCount==360,"12 seconds at 30 fps produces exactly 360 frames");
foreach(var rate in FrameRate.Supported){var f=s with{Fps=rate,DurationSeconds=5};f.Validate();Check(f.ActualDuration>=5&&f.ActualDuration<5+1/rate.Value,"Fractional frame rate rounding "+rate);}
Reject(()=>(s with{Width=3839}).Validate(),"Odd width rejected");Reject(()=>(s with{Fps=new(0,0)}).Validate(),"Invalid rate rejected");Reject(()=>(s with{BitrateMbps=0}).Validate(),"Zero bitrate rejected");Reject(()=>(s with{DurationSeconds=0}).Validate(),"Zero duration rejected");Reject(()=>(s with{EncoderPreset="bad; command"}).Validate(),"Invalid preset rejected");
var encoderArgs=Encoder.Arguments(s,"C:\\folder with space\\hasil.mp4");Check(encoderArgs[^1]=="C:\\folder with space\\hasil.mp4","Output path stays one argument");Check(encoderArgs.Contains("-n")&&!encoderArgs.Contains("-y"),"Existing outputs are never overwritten");Check(encoderArgs.Contains("yuv420p")&&encoderArgs.Contains("libx264"),"Portable H.264 MP4 encoding");
var v=new VideoInfo(3840,2160,30,30,12,90_000_000,"h264","yuv420p","mov,mp4,m4a,3gp,3g2,mj2",360,0);
Check(QualityCheck.Inspect(v,s).TechnicalPassed,"Valid 4K video passes technical checks");Check(!QualityCheck.Inspect(v with{Duration=2}).TechnicalPassed,"Under-five-second Adobe Stock video fails");Check(!QualityCheck.Inspect(v with{Bytes=3_900_000_001}).TechnicalPassed,"Oversize video fails");Check(!QualityCheck.Inspect(v with{FrameCount=359},s).TechnicalPassed,"Dropped frame detected");Check(!QualityCheck.Inspect(v with{Fps=120}).TechnicalPassed,"Nonstandard FPS detected");
Check(ProjectFiles.SafeName("CON")=="_CON","Windows reserved file name handled");Check(ProjectFiles.SafeName("a/b:c*")=="a_b_c_","Windows illegal characters sanitized");
var root=Path.Combine(Path.GetTempPath(),"rhmn-tests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
try{
 var entry=Path.Combine(root,"index.html");File.WriteAllText(entry,"<html></html>");Check(ProjectFiles.ValidateEntry(entry)==entry,"HTML import validates existing file");
 var output=ProjectFiles.UniqueOutput(root,entry,[]);File.WriteAllText(output,"existing");Check(ProjectFiles.UniqueOutput(root,entry,[])!=output,"Existing output collision gets new name");Check(ProjectFiles.UniqueOutput(root,entry,[Path.Combine(root,new DirectoryInfo(root).Name+"-2.mp4")]).EndsWith("-3.mp4"),"Queued filename collision gets new name");
 var path=Path.Combine(root,"session.json");JsonFiles.Save(path,new RenderJob{EntryFile=entry,Settings=s});Check(JsonFiles.Load<RenderJob>(path)?.Settings==s,"Queue settings round-trip through JSON");
 if(argsFromEnvironment() is string ffmpeg){
   var video=Path.Combine(root,"integration.mp4");await Encoder.RunAsync(ffmpeg,["-v","error","-f","lavfi","-i","testsrc2=size=1920x1080:rate=30000/1001","-frames:v","150","-c:v","libx264","-preset","ultrafast","-pix_fmt","yuv420p",video],CancellationToken.None);
   var probe=Environment.GetEnvironmentVariable("RHMN_TEST_FFPROBE")??"ffprobe";var actual=await Encoder.ProbeAsync(probe,video,CancellationToken.None);Check(actual.Width==1920&&actual.FrameCount==150,"Real FFprobe decodes 150 frames");Check(Math.Abs(actual.Fps-30000d/1001)<0.001,"Real FFprobe preserves fractional FPS");Check(QualityCheck.Inspect(actual).TechnicalPassed,"Real encoded video passes QC");
 }
}finally{Directory.Delete(root,true);}
Console.WriteLine($"{passed} checks passed.");
static string? argsFromEnvironment()=>Environment.GetEnvironmentVariable("RHMN_TEST_FFMPEG");
