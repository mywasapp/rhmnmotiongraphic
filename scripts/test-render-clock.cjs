// Optional developer check of the JS clock in Chromium. Does not replace WebView2/Windows tests.
const {chromium}=require('playwright');
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'..'),output=path.join(root,'artifacts','clock-test');fs.mkdirSync(output,{recursive:true});
const script="window.__nativeClear=window.clearTimeout.bind(window);\n"+fs.readFileSync(path.join(root,'src/Rhmn.Studio/Assets/render-clock.js'),'utf8').replace('__RHMN_SEED__','12345');
(async()=>{
 const browser=await chromium.launch({headless:true,executablePath:process.env.RHMN_TEST_CHROME||undefined});const results=[];
 try{
  for(const [sample,duration,mode] of [['01-Aurora',12,'auto'],['02-Geometri-CSS',8,'clock'],['03-Partikel-RAF',10,'clock']]){
   const page=await browser.newPage({viewport:{width:3840,height:2160},deviceScaleFactor:1});await page.addInitScript(script);
   await page.goto('file://'+path.join(root,'samples',sample,'index.html'));const ready=await page.evaluate(()=>window.__rhmnReady());
   await page.evaluate(([t,m])=>window.__rhmnFrame(t,m),[0,mode]);const first=await page.screenshot();
   assert.equal(first.readUInt32BE(16),3840);assert.equal(first.readUInt32BE(20),2160);
   await page.evaluate(([t,m])=>window.__rhmnFrame(t,m),[duration/4,mode]);const middle=await page.screenshot();
   assert.notDeepEqual(first,middle,'Motion must change the image');
   await page.evaluate(([t,m])=>window.__rhmnFrame(t,m),[duration,mode]);const last=await page.screenshot();
   // PNG encoders may retain distinct metadata; compare the actual pixels in Python if needed.
   fs.writeFileSync(path.join(output,sample+'-first.png'),first);fs.writeFileSync(path.join(output,sample+'-middle.png'),middle);fs.writeFileSync(path.join(output,sample+'-last.png'),last);
   const h=b=>crypto.createHash('sha256').update(b).digest('hex');results.push({sample,width:3840,height:2160,motionChanged:true,endpointPngEqual:h(first)===h(last),ready});await page.close();
  }
  const page=await browser.newPage();await page.addInitScript(script);await page.goto('data:text/html,<div>timer test</div>');await page.evaluate(()=>{window.ticks=[];setTimeout(()=>ticks.push(Date.now()),500);setInterval(()=>ticks.push(performance.now()),250);requestAnimationFrame(t=>ticks.push(t));});
  await page.evaluate(()=>window.__rhmnFrame(.5,'clock'));const ticks=await page.evaluate(()=>ticks);assert.equal(ticks.length,4);assert(ticks.includes(250)&&ticks.includes(500));results.push({timerCheck:'passed',ticks});await page.close();
  fs.writeFileSync(path.join(output,'results.json'),JSON.stringify({engine:'Chromium on Linux; NOT Windows WebView2',results},null,2));console.log(JSON.stringify(results,null,2));
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
