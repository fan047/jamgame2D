'use strict';
const assert=require('node:assert/strict');
const fs=require('node:fs');
const path=require('node:path');
const {pathToFileURL}=require('node:url');
const {chromium}=require(process.env.PLAYWRIGHT_MODULE||'playwright-core');
const M=require('../config-model.js');
const root=path.resolve(__dirname,'../../..'),out=path.join(root,'Library/Locus/tmp/planner-browser-test');
fs.mkdirSync(out,{recursive:true});
(async()=>{
  const browser=await chromium.launch({executablePath:process.env.BROWSER_EXECUTABLE||'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--disable-gpu']});
  try {
    const context=await browser.newContext({acceptDownloads:true,viewport:{width:1400,height:1000}});
    const page=await context.newPage();const errors=[];page.on('pageerror',e=>errors.push(e.message));
    page.on('dialog',dialog=>dialog.accept());
    await page.goto(pathToFileURL(path.join(process.env.PLANNER_EDITOR_DIR||path.join(root,'Tools/PlannerEditor'),'index.html')).href);
    assert.match(await page.locator('#validation').innerText(),/校验通过/);
    assert.equal(await page.locator('#board .block').count(),11);
    await page.locator('#ballsPerRound').fill('7');
    await page.getByRole('button',{name:'空格 列4 行4',exact:true}).click();
    await page.locator('#width').selectOption('2');await page.locator('#health').fill('8');await page.locator('#applyBlock').click();
    assert.equal(await page.locator('#board .block').count(),12);
    await page.locator('#health').fill('9');await page.locator('#applyBlock').click();
    await page.getByRole('button',{name:'空格 列0 行7',exact:true}).click();
    await page.locator('#column').fill('0');await page.locator('#row').fill('0');await page.locator('#applyBlock').click();
    assert.match(await page.locator('#message').innerText(),/重叠/);assert.equal(await page.locator('#board .block').count(),12);
    await page.locator('#settingsVersion').fill('bad version');assert.equal(await page.locator('#exportSettings').isDisabled(),true);
    await page.locator('#settingsVersion').fill('planner-test-2');
    for(const [button,name] of [['exportSettings','settings.json'],['exportLevel','level.json']]) {
      const download=page.waitForEvent('download');await page.locator('#'+button).click();const file=await download;
      assert.equal(file.suggestedFilename(),name);await file.saveAs(path.join(out,name));
    }
    const settings=JSON.parse(fs.readFileSync(path.join(out,'settings.json'),'utf8')),level=JSON.parse(fs.readFileSync(path.join(out,'level.json'),'utf8'));
    assert.equal(settings.ballsPerRound,7);assert.equal(settings.version,'planner-test-2');
    assert.ok(level.initialBlocks.some(b=>b.column===4&&b.row===4&&b.health===9&&b.width===2));
    assert.deepEqual(M.validate(settings,level),[]);
    await page.locator('#importFiles').setInputFiles([path.join(out,'settings.json'),path.join(out,'level.json')]);
    await page.waitForFunction(()=>document.querySelector('#source').textContent.includes('已导入'));
    assert.equal(await page.locator('#ballsPerRound').inputValue(),'7');
    await page.locator('#addRow').click();assert.equal(await page.locator('#exportLevel').isDisabled(),true);
    await page.locator('#layout').selectOption('3');await page.locator('#board .block').click();await page.locator('#target').uncheck();await page.locator('#applyBlock').click();
    await page.locator('#layout').selectOption('4');await page.getByRole('button',{name:'空格 列3 行0',exact:true}).click();
    await page.locator('#width').selectOption('2');await page.locator('#health').fill('10');await page.locator('#target').check();await page.locator('#applyBlock').click();
    assert.equal(await page.locator('#exportLevel').isDisabled(),false);
    await page.locator('#board').scrollIntoViewIfNeeded();await page.screenshot({path:path.join(out,'editor-board.png')});
    await page.locator('#removeRow').click();assert.equal(await page.locator('#exportLevel').isDisabled(),true);
    await page.locator('#reset').click();assert.equal(await page.locator('#exportLevel').isDisabled(),false);assert.equal(await page.locator('#ballsPerRound').inputValue(),'12');
    const invalid=path.join(out,'invalid');fs.mkdirSync(invalid,{recursive:true});
    fs.writeFileSync(path.join(invalid,'settings.json'),JSON.stringify({...settings,ballSpeed:-1}));fs.writeFileSync(path.join(invalid,'level.json'),JSON.stringify(level));
    await page.locator('#importFiles').setInputFiles([path.join(invalid,'settings.json'),path.join(invalid,'level.json')]);
    await page.waitForFunction(()=>document.querySelector('#message').textContent.includes('导入失败'));
    assert.equal(await page.locator('#ballsPerRound').inputValue(),'12');
    await page.locator('#layout').selectOption('-1');await page.locator('#board .block').first().click();await page.locator('#deleteBlock').click();assert.equal(await page.locator('#board .block').count(),10);
    assert.deepEqual(errors,[]);
    console.log('Browser UI PASS: offline file URL, parameters, board add/edit/delete, overlap guard, target relocation, import, downloads, validation and invalid-import rollback.');
    console.log('Exported valid fixture:',out);
    await context.close();
  } finally {await browser.close();}
})().catch(error=>{console.error(error);process.exitCode=1;});
