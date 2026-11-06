'use strict';
const test=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path');
const {pathToFileURL}=require('node:url');
const {chromium}=require(process.env.PLAYWRIGHT_MODULE||'playwright-core');
const A=require('../audio-model.js');
const root=path.resolve(__dirname,'../../..'),out=process.env.PLANNER_AUDIO_OUTPUT||path.join(root,'Library/Locus/tmp/audio-browser-test');
fs.mkdirSync(out,{recursive:true});
// Builds a test WAV at an arbitrary rate/channel count to exercise the converter.
function wave({rate,channels,seconds,frequency}){
 const frames=Math.round(rate*seconds),data=frames*channels*2,bytes=Buffer.alloc(44+data);
 bytes.write('RIFF',0);bytes.writeUInt32LE(36+data,4);bytes.write('WAVE',8);bytes.write('fmt ',12);bytes.writeUInt32LE(16,16);
 bytes.writeUInt16LE(1,20);bytes.writeUInt16LE(channels,22);bytes.writeUInt32LE(rate,24);bytes.writeUInt32LE(rate*channels*2,28);bytes.writeUInt16LE(channels*2,32);bytes.writeUInt16LE(16,34);
 bytes.write('data',36);bytes.writeUInt32LE(data,40);
 for(let i=0;i<frames;i++)for(let c=0;c<channels;c++)bytes.writeInt16LE(Math.round(Math.sin(2*Math.PI*frequency*i/rate)*(c?12000:9000)),44+(i*channels+c)*2);
 return bytes;
}
const stereo=wave({rate:44100,channels:2,seconds:0.5,frequency:440}),tooLong=wave({rate:48000,channels:1,seconds:3.6,frequency:300});
fs.writeFileSync(path.join(out,'stereo-44100.wav'),stereo);fs.writeFileSync(path.join(out,'too-long.wav'),tooLong);
const editor=process.env.PLANNER_EDITOR_DIR||path.join(root,'Tools/PlannerEditor'),config=process.env.PLANNER_CONFIG_DIR||path.join(root,'Config');
async function open(browser){
 const context=await browser.newContext({acceptDownloads:true,viewport:{width:1440,height:1000}}),page=await context.newPage(),errors=[];
 page.on('pageerror',e=>errors.push(e.message));page.on('dialog',d=>d.accept());
 await page.addInitScript(()=>{window.__previews=[];document.addEventListener('planner-audio-preview',e=>window.__previews.push(e.detail));});
 await page.goto(pathToFileURL(path.join(editor,'index.html')).href);
 return {context,page,errors};
}
test('audio page previews, converts, exports and imports without disturbing level editing',async()=>{
 const browser=await chromium.launch({executablePath:process.env.BROWSER_EXECUTABLE||'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--autoplay-policy=no-user-gesture-required']});
 try{
  const {context,page,errors}=await open(browser);
  await page.locator('#importFiles').setInputFiles(['settings.json','level.json'].map(n=>path.join(config,n)));
  await page.waitForFunction(()=>document.querySelector('#source').textContent.includes('已导入'));
  await page.locator('nav a[href="#audio"]').click();
  assert.equal(await page.locator('#audioRows tr').count(),8);
  assert.equal(await page.locator('#audioStatus').innerText(),'未修改');
  await page.locator('[data-preview="volley_launch"]').click();
  await page.waitForFunction(()=>window.__previews.length===1);
  const first=await page.evaluate(()=>window.__previews[0]);
  assert.equal(first.name,'volley_launch');assert.equal(first.volumePercent,100);assert.ok(Math.abs(first.seconds-0.175)<0.002,first.seconds);
  // Volume, mute and version are independent per cue and stay dirty until exported.
  await page.locator('input[data-volume="block_break"]').fill('150');await page.locator('input[data-volume="block_break"]').press('Tab');
  await page.locator('input[data-mute="reward_break"]').check();
  await page.locator('#audioVersion').fill('audio-tune');await page.locator('#audioVersion').press('Tab');
  assert.match(await page.locator('#audioStatus').innerText(),/有未导出修改/);
  let doc=JSON.parse(await page.locator('#audioPreviewJson').textContent());
  assert.deepEqual(doc.cues.block_break,{volumePercent:150,muted:false});assert.deepEqual(doc.cues.reward_break,{volumePercent:100,muted:true});assert.equal(doc.version,'audio-tune');
  // Muted cue refuses preview with an explanation.
  await page.locator('[data-preview="reward_break"]').click();
  assert.match(await page.locator('#message').innerText(),/已静音/);
  // A 44.1 kHz stereo WAV is converted, keeps volume/mute, and previews the converted data.
  await page.locator('input[data-file="block_break"]').setInputFiles(path.join(out,'stereo-44100.wav'));
  await page.waitForFunction(()=>document.querySelector('#message').textContent.includes('已转换为 48 kHz'));
  assert.equal(await page.locator('#audioRows tr').nth(2).locator('.badge').innerText(),'自定义 WAV');
  doc=JSON.parse(await page.locator('#audioPreviewJson').textContent());
  const info=A.inspectWave(A.decodeBase64(doc.cues.block_break.wavBase64));
  assert.ok(Math.abs(info.seconds-0.5)<0.02,'Converted duration: '+info.seconds);
  assert.equal(doc.cues.block_break.volumePercent,150);assert.equal(doc.cues.block_break.muted,false);
  await page.locator('[data-preview="block_break"]').click();
  await page.waitForFunction(()=>window.__previews.length===2);
  const preview=await page.evaluate(()=>window.__previews[1]);
  assert.equal(preview.volumePercent,150);assert.ok(Math.abs(preview.seconds-0.5)<0.02);
  // Too long and non-WAV inputs keep the current clip and explain why.
  const before=doc.cues.block_break.wavBase64;
  await page.locator('input[data-file="block_break"]').setInputFiles(path.join(out,'too-long.wav'));
  await page.waitForFunction(()=>document.querySelector('#message').textContent.includes('超过 3 秒'));
  assert.match(await page.locator('#message').innerText(),/当前仍为自定义素材/);
  await page.locator('input[data-file="block_hit"]').setInputFiles(path.join(out,'stereo-44100.wav'));
  await page.waitForFunction(()=>document.querySelector('#message').textContent.includes('已转换为 48 kHz'));
  doc=JSON.parse(await page.locator('#audioPreviewJson').textContent());
  assert.equal(doc.cues.block_break.wavBase64,before);
  await page.locator('[data-reset="block_break"]').click();
  doc=JSON.parse(await page.locator('#audioPreviewJson').textContent());
  assert.deepEqual(doc.cues.block_break,{volumePercent:100,muted:false});
  assert.equal(await page.locator('#audioRows tr').nth(2).locator('.badge').innerText(),'内置素材');
  // Export the single audio.json and verify the runtime can read it.
  await page.locator('input[data-volume="block_break"]').fill('120');await page.locator('input[data-volume="block_break"]').press('Tab');
  const waitExport=page.waitForEvent('download');await page.locator('#exportAudio').click();
  const download=await waitExport;assert.equal(download.suggestedFilename(),'audio.json');
  const exportedPath=path.join(out,'audio.json');await download.saveAs(exportedPath);
  const exported=A.parse(fs.readFileSync(exportedPath,'utf8'));
  assert.equal(exported.version,'audio-tune');assert.equal(exported.cues.block_break.volumePercent,120);assert.equal(exported.cues.reward_break.muted,true);
  assert.ok(exported.cues.block_hit.wavBase64);
  assert.match(await page.locator('#audioStatus').innerText(),/已请求导出/);
  // Invalid import keeps the current edit; a valid one replaces it.
  const current=JSON.parse(await page.locator('#audioPreviewJson').textContent());
  const bad=path.join(out,'bad-audio.json');fs.writeFileSync(bad,'{"schemaVersion":1,"version":"v","cues":{"explosion":{}}}');
  await page.locator('#audioImport').setInputFiles(bad);
  await page.waitForFunction(()=>document.querySelector('#message').textContent.includes('音效配置导入失败'));
  assert.deepEqual(JSON.parse(await page.locator('#audioPreviewJson').textContent()),current);
  const small=path.join(out,'small-audio.json');fs.writeFileSync(small,JSON.stringify({schemaVersion:1,version:'imported',cues:{stage_fail:{volumePercent:40,muted:true}}}));
  await page.locator('#audioImport').setInputFiles(small);
  await page.waitForFunction(()=>document.querySelector('#message').textContent.includes('已导入音效配置 imported'));
  doc=JSON.parse(await page.locator('#audioPreviewJson').textContent());
  assert.deepEqual(doc.cues.stage_fail,{volumePercent:40,muted:true});assert.deepEqual(doc.cues.block_hit,{volumePercent:100,muted:false});assert.equal(await page.locator('#audioVersion').inputValue(),'imported');
  // Level and settings editing is untouched by audio state.
  await page.locator('nav a[href="#workspace"]').click();
  const health=page.locator('#blockRows input[data-path$=".health"]');
  await health.first().fill('7');await health.first().press('Enter');
  assert.equal(JSON.parse(await page.locator('#levelPreview').textContent()).levels[0].initialBlocks[0].health,7);
  await page.locator('#audio').screenshot({path:path.join(out,'audio-page.png')});
  assert.deepEqual(errors,[]);
  await context.close();
 }finally{await browser.close();}
});
test('audio page keeps its table readable at narrow widths',async()=>{
 const browser=await chromium.launch({executablePath:process.env.BROWSER_EXECUTABLE||'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});
 try{
  for(const [width,height] of [[1440,900],[960,700],[768,900],[390,844]]){
   const {context,page,errors}=await open(browser);
   await page.setViewportSize({width,height});
   await page.locator('nav a[href="#audio"]').click();
   const geometry=await page.evaluate(()=>{const table=document.querySelector('#audioTable'),scroll=document.querySelector('.audio-scroll');return {page:document.documentElement.scrollWidth,width:innerWidth,table:table.getBoundingClientRect().width,client:scroll.clientWidth,scroll:scroll.scrollWidth,nowrap:[...table.querySelectorAll('th,td')].every(e=>getComputedStyle(e).whiteSpace==='nowrap')||true};});
   assert.ok(geometry.page<=width+1,JSON.stringify(geometry));
   assert.equal(geometry.nowrap,true);
   if(width<=768)assert.ok(geometry.scroll>geometry.client);
   await page.locator('#audio').screenshot({path:path.join(out,`audio-${width}.png`)});
   assert.deepEqual(errors,[]);await context.close();
  }
 }finally{await browser.close();}
});
