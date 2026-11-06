'use strict';
const test=require('node:test'),assert=require('node:assert/strict');
const fs=require('node:fs'),path=require('node:path');
const {pathToFileURL}=require('node:url');
const {chromium}=require(process.env.PLAYWRIGHT_MODULE||'playwright-core');
const root=path.resolve(__dirname,'../../..');
const out=process.env.PLANNER_DESIGN_OUTPUT||path.join(root,'Library/Locus/tmp/workbench-design');
fs.mkdirSync(out,{recursive:true});
for(const [width,height] of [[1920,1080],[1440,900],[1152,720],[960,600],[768,900],[390,844]]){
 test(`workbench remains readable and editable at ${width}x${height}`,async()=>{
  const browser=await chromium.launch({executablePath:process.env.BROWSER_EXECUTABLE||'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});
  try{
   const context=await browser.newContext({viewport:{width,height},acceptDownloads:true,reducedMotion:'reduce'});
   const page=await context.newPage(),errors=[];
   page.on('pageerror',e=>errors.push(e.message));page.on('dialog',d=>d.accept());
   await page.goto(pathToFileURL(path.join(process.env.PLANNER_EDITOR_DIR||path.join(root,'Tools/PlannerEditor'),'index.html')).href);
   assert.match(await page.title(),/关卡工作台/);
   const config=process.env.PLANNER_CONFIG_DIR||path.join(root,'Config');
   await page.locator('#importFiles').setInputFiles(['settings.json','level.json'].map(n=>path.join(config,n)));
   await page.waitForFunction(()=>document.querySelector('#source').textContent.includes('已导入'));
   await page.locator('nav a[href="#workspace"]').click();
   await page.locator('#board .block').first().click();
   assert.match(await page.locator('#selectionTitle').textContent(),/编辑第 1 块/);
   const geometry=await page.evaluate(()=>{
    const rect=e=>{const r=e.getBoundingClientRect();return {left:r.left,right:r.right,top:r.top,bottom:r.bottom,width:r.width};};
    const table=document.querySelector('#blockTable'),board=document.querySelector('.board-panel'),inspector=document.querySelector('.editor-inspector');
    return {width:innerWidth,scroll:document.documentElement.scrollWidth,table:rect(table),values:rect(document.querySelector('#values')),workspace:rect(document.querySelector('#workspace')),board:rect(board),inspector:rect(inspector),insideInspector:inspector.contains(table),nowrap:[...table.querySelectorAll('th,td')].every(e=>getComputedStyle(e).whiteSpace==='nowrap'),inputs:[...table.querySelectorAll('input[type=number]')].map(e=>rect(e).width)};
   });
   assert.ok(geometry.scroll<=width+1,JSON.stringify(geometry));
   assert.equal(geometry.insideInspector,false,'Seven-column table must not be placed in the narrow inspector');
   assert.ok(geometry.values.width>geometry.workspace.width-60,'Numeric editor must span the workbench');
   assert.ok(geometry.nowrap,'Table labels, coordinates, shapes and types must not wrap vertically');
   assert.ok(geometry.inputs.every(n=>n>=120),'Full-width numeric fields must remain usable');
   if(width<=900)assert.ok(geometry.inspector.top>=geometry.board.bottom-1,'Narrow layout must stack inspector below board');
   else assert.ok(geometry.inspector.left>=geometry.board.right,'Wide layout must retain board and inspector side by side');
   await page.locator('nav a[href="#workspace"]').click();
   await page.locator('#board').evaluate(e=>e.scrollTop=0);
   await page.screenshot({path:path.join(out,`${width}-workspace.png`)});
   await page.locator('nav a[href="#values"]').click();
   const health=page.locator('#blockRows input[data-path$=".health"]');
   await health.first().fill('2147483647');await health.first().press('Enter');
   assert.equal(await health.nth(1).evaluate(e=>document.activeElement===e),true);
   assert.equal(JSON.parse(await page.locator('#levelPreview').textContent()).levels[0].initialBlocks[0].health,2147483647);
   await page.locator('#values').screenshot({path:path.join(out,`${width}-values.png`)});
   const reward=page.locator('#blockRows input[data-path$=".rewardBalls"]').first();
   await reward.click();await reward.fill('0');
   const scroll=await page.locator('#values .table-scroll').evaluate(e=>({client:e.clientWidth,scroll:e.scrollWidth,left:e.scrollLeft}));
   if(width<=768){assert.ok(scroll.scroll>scroll.client);assert.ok(scroll.left>0,'Right-side input must remain reachable by local scrolling');}
   await reward.press('Tab');
   await page.locator('#undo').click();
   assert.equal(JSON.parse(await page.locator('#levelPreview').textContent()).levels[0].initialBlocks[0].health,5);
   // Check the real keyboard entry and every anchor target rather than a screenshot alone.
   for(const id of ['workspace','values','settings','delivery']){
    await page.locator(`nav a[href="#${id}"]`).click();assert.ok(await page.locator('#'+id).isVisible());
   }
   const download=page.waitForEvent('download');await page.locator('#exportLevel').click();
   assert.equal((await download).suggestedFilename(),'level.json');
   assert.deepEqual(errors,[]);await context.close();
  }finally{await browser.close();}
 });
}
