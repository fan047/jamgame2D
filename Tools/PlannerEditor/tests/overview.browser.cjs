'use strict';
const assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path');
const {pathToFileURL}=require('node:url');
const {chromium}=require(process.env.PLAYWRIGHT_MODULE||'playwright-core');
const root=path.resolve(__dirname,'../../..'),out=path.join(root,'Library/Locus/tmp/overview-browser-test');fs.mkdirSync(out,{recursive:true});
(async()=>{
 const browser=await chromium.launch({executablePath:process.env.BROWSER_EXECUTABLE||'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});
 try{
  const context=await browser.newContext({acceptDownloads:true,viewport:{width:1500,height:1050}}),page=await context.newPage(),errors=[];
  page.on('pageerror',e=>errors.push(e.message));page.on('dialog',d=>d.accept());
  await page.goto(pathToFileURL(path.join(process.env.PLANNER_EDITOR_DIR||path.join(root,'Tools/PlannerEditor'),'index.html')).href);
  await page.locator('#stage').selectOption('2');
  const source=JSON.parse(await page.locator('#levelPreview').textContent()).levels[2];
  assert.equal(source.incomingRows.length,7,'Long-stage acceptance must use the verified seven-row baseline');
  assert.equal(await page.locator('.layout-group').count(),8);
  assert.equal(await page.locator('.layout-group[data-layout="-1"] .cell').count(),64);
  for(let i=0;i<7;i++){
   assert.equal(await page.locator(`.layout-group[data-layout="${i}"] .cell`).count(),8);
   assert.equal(await page.locator(`.layout-group[data-layout="${i}"] .block`).count(),source.incomingRows[i].blocks.length);
  }
  const grids=await page.locator('.layout-grid').evaluateAll(es=>es.map(e=>({left:e.getBoundingClientRect().left,width:e.getBoundingClientRect().width})));
  for(const grid of grids)assert.deepEqual(grid,grids[0],'Every group must share eight-column alignment');
  const vertical=source.initialBlocks.findIndex(b=>b.height===2);
  assert.ok(vertical>=0);const group=page.locator('.layout-group[data-layout="-1"]');
  const block=await group.locator(`[data-block="${vertical}"]`).boundingBox(),cell=await group.locator('.cell').first().boundingBox();
  assert.ok(block.height>cell.height*1.9&&block.height<cell.height*2.3);
  assert.equal(await page.locator('.layout-group[data-layout="6"] .goal').count(),1);
  await page.locator('#layout').selectOption('6');
  assert.equal(await page.locator('.layout-group.active').getAttribute('data-layout'),'6');
  assert.ok(await page.locator('#board').evaluate(e=>e.scrollTop>100));
  assert.equal(await page.locator('.layout-group').count(),8,'Jumping never hides other groups');
  await page.locator('#board').evaluate(e=>e.scrollTop=0);assert.equal(await group.isVisible(),true);
  await page.locator('#addRow').click();assert.equal(await page.locator('.layout-group').count(),9);assert.equal(await page.locator('.layout-group[data-layout="7"] .block').count(),0);assert.match(await page.locator('.layout-group[data-layout="7"] h3').innerText(),/最后一条/);
  await page.locator('#undo').click();assert.equal(await page.locator('.layout-group').count(),8);
  await page.locator('#board').scrollIntoViewIfNeeded();await page.screenshot({path:path.join(out,'stage3-overview.png'),fullPage:false});
  assert.deepEqual(errors,[]);console.log('Overview browser PASS: verified stage3 initial + seven rows, eight-column alignment, vertical blocks, final target, jump/scroll and empty row undo.');
  await context.close();
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
