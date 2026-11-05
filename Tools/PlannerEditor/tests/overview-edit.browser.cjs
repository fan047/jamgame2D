'use strict';
const assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path');
const {pathToFileURL}=require('node:url');
const {chromium}=require(process.env.PLAYWRIGHT_MODULE||'playwright-core');
const M=require('../config-model.js');
const root=path.resolve(__dirname,'../../..'),out=path.join(root,'Library/Locus/tmp/overview-edit-browser-test');fs.mkdirSync(out,{recursive:true});
(async()=>{
 const browser=await chromium.launch({executablePath:process.env.BROWSER_EXECUTABLE||'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});
 try{
  const context=await browser.newContext({acceptDownloads:true,viewport:{width:1500,height:1050}}),page=await context.newPage(),errors=[];
  page.on('pageerror',e=>errors.push(e.message));page.on('dialog',d=>d.accept());
  await page.goto(pathToFileURL(path.join(process.env.PLANNER_EDITOR_DIR||path.join(root,'Tools/PlannerEditor'),'index.html')).href);
  const level=()=>page.locator('#levelPreview').textContent().then(JSON.parse);
  const group=n=>page.locator(`.layout-group[data-layout="${n}"]`);
  const first=()=>group(-1).locator('.block').first(),target=()=>group(6).locator('.goal');
  const reset=async()=>{await page.locator('#reset').click();await page.locator('#stage').selectOption('2');};
  await page.locator('#stage').selectOption('2');const original=await level();
  await first().click();await target().click();assert.equal(await page.locator('#layout').inputValue(),'6');
  assert.match(await page.locator('#selectionTitle').innerText(),/第 3 关.*补行 7.*最后一条/);
  assert.match(await page.locator('#editingContext').innerText(),/stage-3.*补行 7/);
  await target().evaluate(e=>window.savedTarget=e);
  const top=await page.locator('#board').evaluate(e=>e.scrollTop);
  await page.locator('#health').fill('777');await page.locator('#health').press('Enter');
  assert.equal(await page.locator('#health').evaluate(e=>e===document.activeElement),true);
  assert.equal(await page.locator('#board').evaluate(e=>e.scrollTop),top);
  assert.equal(await target().evaluate(e=>e===window.savedTarget),true,'Numeric commit must preserve the pressed/focused board element');
  assert.match(await target().innerText(),/777/);
  // Real blur -> pointer click sequence, without pressing Enter first.
  await page.locator('#health').fill('778');await first().click();
  assert.equal(await page.locator('#layout').inputValue(),'-1','Blur commit must not swallow the cross-group click');
  const expected=structuredClone(original);expected.levels[2].incomingRows[6].blocks[0].health=778;
  assert.deepEqual(await level(),expected);
  for(const file of ['settings','level']){const wait=page.waitForEvent('download');await page.locator(file==='settings'?'#exportSettings':'#exportLevel').click();await (await wait).saveAs(path.join(out,file+'.json'));}
  assert.deepEqual(JSON.parse(fs.readFileSync(path.join(out,'level.json'))),expected);
  assert.deepEqual(M.validate(JSON.parse(fs.readFileSync(path.join(out,'settings.json'))),expected),[]);
  await reset();
  for(const action of ['Cancel','Discard','Apply']){
   await target().click();await page.locator('#column').fill('1');await first().click();
   assert.equal(await page.locator('#draftDialog').isVisible(),true);assert.equal(await page.locator('#layout').inputValue(),'6');
   await page.locator('#draft'+action).click();
   if(action==='Cancel'){
    assert.equal(await page.locator('#layout').inputValue(),'6');assert.equal((await level()).levels[2].incomingRows[6].blocks[0].column,0);
    await first().click();await page.locator('#draftDiscard').click();
   }else{
    assert.equal(await page.locator('#layout').inputValue(),'-1');assert.equal((await level()).levels[2].incomingRows[6].blocks[0].column,action==='Apply'?1:0);
   }
  }
  await reset();await group(0).locator('.cell[data-column="7"][data-row="0"]').click();
  assert.equal(await page.locator('#layout').inputValue(),'0');assert.equal(await page.locator('#column').inputValue(),'7');assert.equal(await page.locator('#row').inputValue(),'0');
  assert.equal(await group(0).locator('.placement-preview').count(),1);assert.equal(await group(-1).locator('.placement-preview').count(),0);
  const count=(await level()).levels[2].incomingRows[0].blocks.length;await page.locator('#applyBlock').click();
  assert.equal((await level()).levels[2].incomingRows[0].blocks.length,count+1);assert.equal((await level()).levels[2].incomingRows[0].blocks[count].row,0);
  await page.locator('#selectOrdinary').click();assert.ok(await page.locator('#blockRows input[type="checkbox"]:checked').count()>0);
  await target().click();assert.equal(await page.locator('#blockRows input[type="checkbox"]:checked').count(),0);assert.match(await page.locator('#batchSummary').innerText(),/作用范围：第 3 关 · 补行 7/);
  // Invalid numeric drafts block cross-group navigation and preserve the model.
  await page.locator('#health').fill('0');await first().click();assert.equal(await page.locator('#layout').inputValue(),'6');assert.equal(await page.locator('#health').inputValue(),'0');
  await page.locator('#health').press('Escape');await first().click();assert.equal(await page.locator('#layout').inputValue(),'-1');
  await page.locator('#layout').selectOption('6');await page.locator('#addRow').click();
  assert.equal(await page.locator('.layout-group').count(),9);assert.equal(await page.locator('#layout').inputValue(),'7');
  await page.locator('#undo').click();assert.equal(await page.locator('.layout-group').count(),8);assert.equal(await page.locator('#layout').inputValue(),'6');
  await page.locator('#redo').click();assert.equal(await page.locator('.layout-group').count(),9);assert.equal(await page.locator('#layout').inputValue(),'7');
  await page.locator('.error-link').filter({hasText:'incomingRows[6]'}).first().click();assert.equal(await page.locator('#layout').inputValue(),'6');assert.ok(await page.locator('#board').evaluate(e=>e.scrollTop>100));
  await page.locator('#layout').selectOption('7');await page.locator('#removeRow').click();assert.equal(await page.locator('.layout-group').count(),8);assert.equal(await page.locator('#layout option').count(),8);
  await page.locator('#undo').click();assert.equal(await page.locator('.layout-group').count(),9);await page.locator('#redo').click();assert.equal(await page.locator('.layout-group').count(),8);
  await page.locator('#stage').selectOption('0');assert.match(await page.locator('#batchSummary').innerText(),/第 1 关 · 初始棋盘/);assert.equal(await page.locator('#blockRows input[type="checkbox"]:checked').count(),0);
  await reset();await target().click();await page.locator('#health').fill('2147483647');await page.locator('#health').press('Enter');assert.match(await target().getAttribute('title'),/2147483647/);
  // Forty extra empty groups exercise long/narrow navigation without changing production configs.
  const long=await level(),rows=long.levels[2].incomingRows,last=rows.pop();for(let i=0;i<40;i++)rows.push({blocks:[]});rows.push(last);
  const fixture=path.join(out,'long');fs.mkdirSync(fixture,{recursive:true});fs.copyFileSync(path.join(out,'settings.json'),path.join(fixture,'settings.json'));fs.writeFileSync(path.join(fixture,'level.json'),JSON.stringify(long));
  await page.locator('#importFiles').setInputFiles([path.join(fixture,'settings.json'),path.join(fixture,'level.json')]);await page.waitForFunction(()=>document.querySelector('#source').textContent.includes('已导入'));
  await page.setViewportSize({width:390,height:844});await page.locator('#stage').selectOption('2');assert.equal(await page.locator('.layout-group').count(),48);
  await page.locator('#layout').selectOption('46');await group(46).locator('.goal').click();assert.equal(await page.locator('#health').inputValue(),'2147483647');
  const overflow=await page.evaluate(()=>({width:window.innerWidth,scroll:document.documentElement.scrollWidth,elements:[...document.querySelectorAll('main *')].filter(e=>e.getBoundingClientRect().right>window.innerWidth+1&&getComputedStyle(e).position!=='fixed').slice(0,15).map(e=>({tag:e.tagName,id:e.id,cls:e.className,right:e.getBoundingClientRect().right}))}));
  assert.equal(overflow.scroll<=overflow.width,true,'Narrow overflow: '+JSON.stringify(overflow));
  await page.locator('#board').scrollIntoViewIfNeeded();await page.screenshot({path:path.join(out,'narrow-long-final.png'),fullPage:false});
  await page.locator('#layout').selectOption('-1');assert.ok(await page.locator('#board').evaluate(e=>e.scrollTop<10));
  assert.deepEqual(errors,[]);console.log('Overview editing PASS: exact target-only export, blur click, focus/scroll/DOM stability, cross-group apply/discard/cancel, local placement, scoped batch, invalid drafts, error jumps, row history, 47-row narrow view and full Int32 text.');
  await context.close();
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
