"use strict";
/* RACHMANZO Batch 02 renderer — reproducible, deterministic, 4K MP4 */
const fs=require("node:fs");
const path=require("node:path");
const crypto=require("node:crypto");
const {spawn,execFileSync}=require("node:child_process");
const {once}=require("node:events");
const {pathToFileURL}=require("node:url");
const {chromium}=require("playwright");
const root=__dirname, build=path.join(root,"build");
const catalog=JSON.parse(fs.readFileSync(path.join(root,"manifest.json"),"utf8"));
const ids=(process.env.SCENES||"013,014").split(",").map(s=>s.trim()).filter(Boolean);
const fps=30,frames=300,minMiB=5;
fs.mkdirSync(build,{recursive:true});

const csvEscape=v=>'"'+String(v??"").replace(/"/g,'""')+'"';
function csv(items){
  const data=[["Filename","Title","Keywords","Category","Releases"],...items.map(a=>[a.filename,a.title,a.keywords.join(", "),"",""])];
  fs.writeFileSync(path.join(build,"Adobe_Stock_Metadata.csv"),"\ufeff"+data.map(r=>r.map(csvEscape).join(",")).join("\r\n")+"\r\n");
}
function probe(file){
  const out=execFileSync("ffprobe",["-v","error","-select_streams","v:0","-show_entries","stream=codec_name,width,height,avg_frame_rate,nb_frames,pix_fmt,color_primaries,color_transfer,color_space:format=duration,size","-of","json",file],{encoding:"utf8"});
  return JSON.parse(out);
}
async function produce(page,asset,crf){
  const output=path.join(build,asset.filename);
  const args=["-hide_banner","-loglevel","error","-y","-f","image2pipe","-framerate",String(fps),"-vcodec","png","-i","-","-an","-c:v","libx264","-preset","fast","-crf",String(crf),"-pix_fmt","yuv420p","-r",String(fps),"-frames:v",String(frames),"-color_primaries","bt709","-color_trc","bt709","-colorspace","bt709","-movflags","+faststart",output];
  const ff=spawn("ffmpeg",args,{stdio:["pipe","ignore","pipe"]});
  let err="";
  ff.stderr.on("data",d=>{err+=String(d).slice(-1500);if(err.length>9000)err=err.slice(-9000)});
  ff.stdin.on("error",()=>{}); // errors are reported by ffmpeg exit code
  const exited=new Promise((resolve,reject)=>ff.on("close",code=>code===0?resolve():reject(new Error("ffmpeg exited "+code+" "+err))));
  const duration=asset.duration;
  for(let f=0;f<frames;f++){
    const time=f/fps;
    const b64=await page.evaluate((t)=>{window.renderAtTime(t);return document.getElementById("stage").toDataURL("image/png").split(",")[1];},time);
    const data=Buffer.from(b64,"base64");
    if(!ff.stdin.write(data))await once(ff.stdin,"drain");
    if(f%60===0)console.log(asset.id,"frame",f,"/",frames);
  }
  ff.stdin.end();await exited;
  const result=probe(output);
  const stream=result.streams?.[0]||{}, format=result.format||{};
  const bytes=fs.statSync(output).size, miB=bytes/(1024*1024);
  const ok=stream.codec_name==="h264"&&stream.pix_fmt==="yuv420p"&&stream.width===asset.width&&stream.height===asset.height&&stream.avg_frame_rate==="30/1"&&Math.abs(Number(format.duration)-10)<.05&&Number(stream.nb_frames)===300&&miB>minMiB;
  return {id:asset.id,file:asset.filename,bytes,miB:Number(miB.toFixed(2)),codec:stream.codec_name,pixelFormat:stream.pix_fmt,resolution:stream.width+"x"+stream.height,fps:stream.avg_frame_rate,frames:stream.nb_frames,duration:format.duration,crf,passed:ok};
}
(async()=>{
 const assets=ids.map(id=>{const asset=catalog.assets.find(a=>a.id===id);if(!asset)throw new Error("Invalid id "+id);return asset});
 csv(assets);fs.writeFileSync(path.join(build,"Asset_Manifest.json"),JSON.stringify(assets,null,2));
 const browser=await chromium.launch({headless:true,args:["--no-sandbox","--disable-dev-shm-usage","--use-angle=swiftshader"]});
 const report=[];
 try{
  for(const asset of assets){
   console.log("START",asset.id,asset.title);
   const page=await browser.newPage({viewport:{width:1280,height:720},deviceScaleFactor:1});
   const u=pathToFileURL(path.join(root,"engine.html")).href+"?scene="+asset.id+"&render";
   await page.goto(u,{waitUntil:"load"});await page.waitForFunction(()=>typeof window.renderAtTime==="function");
   const info=await page.evaluate(()=>window.RHMN_RENDER_INFO);
   if(info.width!==asset.width||info.height!==asset.height)throw new Error("Canvas resolution mismatch "+asset.id);
   // Confirm exact periodic boundary BEFORE video encoding. A still at t=10 equals t=0.
   const edge=await page.evaluate(()=>{
     window.renderAtTime(0);const a=document.getElementById("stage").toDataURL("image/png");
     window.renderAtTime(10);const b=document.getElementById("stage").toDataURL("image/png");
     return a===b;
   });
   console.log("Periodic boundary test:",asset.id,edge);
   if(!edge)throw new Error("Unstable start/end boundary for "+asset.id);
   let result=await produce(page,asset,15);
   if(result.miB<=minMiB){console.log("Retesting with higher fidelity CRF 9",asset.id);result=await produce(page,asset,9);}
   result.boundaryEqual=edge;report.push(result);
   await page.close();
   console.log("FINISHED",JSON.stringify(result));
   fs.writeFileSync(path.join(build,"QA_Report.json"),JSON.stringify(report,null,2));
  }
 }finally{await browser.close()}
 const failed=report.filter(r=>!r.passed);
 if(failed.length){console.error("QA FAILED",failed);process.exitCode=2}else console.log("ALL ASSETS TECHNICALLY VALIDATED");
})().catch(err=>{console.error(err.stack||String(err));process.exitCode=1});