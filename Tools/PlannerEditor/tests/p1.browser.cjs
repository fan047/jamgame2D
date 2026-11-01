'use strict';
const assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path'),{pathToFileURL}=require('node:url');
const {chromium}=require(process.env.PLAYWRIGHT_MODULE||'playwright-core');
const root=path.resolve(__dirname,'../../..'),out=path.join(root,'Library/Locus/tmp/p1-browser-test');fs.mkdirSync(out,{recursive:true});
(async()=>{
 const browser=await chromium.launch({executablePath:process.env.BROWSER_EXECUTABLE||'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});
 try{
 const context=await browser.newContext({acceptDownloads:true,viewport:{width:1500,height:1100}}),page=await context.newPage(),errors=[];page.on('pageerror',e=>errors.push(e.message));page.on('dialog',d=>d.accept());
 await page.goto(pathToFileURL(path.join(process.env.PLANNER_EDITOR_DIR||path.join(root,'Tools/PlannerEditor'),'index.html')).href);
 const level=()=>page.locator('#levelPreview').textContent().then(JSON.parse);
 await page.locator('#board .block').first().click();await page.locator('#health').fill('6');await page.locator('#health').press('Enter');
 await page.locator('#column').fill('4');await page.locator('#row').fill('4');await page.locator('#shape').selectOption('1x2');
 assert.equal((await level()).levels[0].initialBlocks[0].height,1);assert.equal(await page.locator('.placement-preview').count(),1);assert.equal(await page.locator('.placement-preview').evaluate(e=>getComputedStyle(e).gridRow),'3 / span 2');
 await page.locator('#applyBlock').click();assert.equal((await level()).levels[0].initialBlocks[0].height,2);assert.equal(await page.locator('.placement-preview').count(),0);
 const rect=await page.locator('#board .block').first().boundingBox(),cell=await page.locator('#board .cell').first().boundingBox();assert.ok(rect.height>cell.height*1.9&&rect.height<cell.height*2.2);assert.equal(await page.locator('#board .block').first().evaluate(e=>getComputedStyle(e).gridRow),'3 / span 2');assert.match(await page.locator('#blockRows tr').first().innerText(),/1×2/);
 await page.locator('#undo').click();assert.equal((await level()).levels[0].initialBlocks[0].height,1);await page.locator('#redo').click();assert.equal((await level()).levels[0].initialBlocks[0].height,2);
 await page.locator('#column').fill('0');await page.locator('#row').fill('0');assert.equal(await page.locator('.placement-preview.bad').count(),1);await page.locator('#applyBlock').click();assert.match(await page.locator('#message').innerText(),/重叠/);assert.equal((await level()).levels[0].initialBlocks[0].row,4);
 await page.locator('#layout').selectOption('0');await page.locator('#draftCancel').click();assert.equal(await page.locator('#layout').inputValue(),'-1');await page.locator('#row').fill('7');assert.match(await page.locator('#placementHint').innerText(),/顶部/);await page.locator('#applyBlock').click();assert.match(await page.locator('#message').innerText(),/顶部/);
 await page.locator('#layout').selectOption('0');await page.locator('#draftDiscard').click();assert.equal(await page.locator('#shape option[value="1x2"]').evaluate(e=>e.disabled),true);await page.locator('#layout').selectOption('-1');
 for(const file of ['settings','level']){const wait=page.waitForEvent('download');await page.locator(file==='settings'?'#exportSettings':'#exportLevel').click();const download=await wait;await download.saveAs(path.join(out,file+'.json'));}
 const exported=JSON.parse(fs.readFileSync(path.join(out,'level.json'),'utf8'));assert.deepEqual(exported.levels[0].initialBlocks[0],{column:4,row:4,width:1,height:2,health:6,target:false,rewardBalls:0});
 await page.locator('#board .block').first().click();await page.locator('#board').scrollIntoViewIfNeeded();await page.screenshot({path:path.join(out,'vertical-editor.png'),fullPage:true});assert.deepEqual(errors,[]);console.log('P1 browser PASS: full-height grid, placement preview, overlap/top guards, horizontal/vertical undo redo, incoming shape disabled and v2 export.');console.log('Export directory:',out);await context.close();
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
