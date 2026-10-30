/* global PlannerModel, PlannerDefaults */
(function() {
  'use strict';
  const $ = id => document.getElementById(id);
  const M = PlannerModel;
  let state = M.clone(PlannerDefaults), scope = -1, selected = -1, dirty = false;
  const numeric = ['ballsPerRound','ballRadius','ballSpeed','launchInterval','maxAimAngle','recallTimeout'];
  const current = () => scope < 0 ? state.level.initialBlocks : state.level.incomingRows[scope].blocks;
  function message(text, error=false) { $('message').textContent=text; $('message').classList.toggle('error',error); $('message').style.display='block'; }
  function changed() { dirty=true; validate(); }
  function validate() {
    const errors=M.validate(state.settings,state.level), box=$('validation');
    box.replaceChildren();box.classList.toggle('invalid',errors.length>0);
    if(errors.length) {
      const ul=document.createElement('ul');
      errors.slice(0,12).forEach(e=>{const li=document.createElement('li');li.textContent=e;ul.append(li);});
      box.append(ul);
      if(errors.length>12)box.append(document.createTextNode(`另有 ${errors.length-12} 项错误`));
    } else box.textContent=`校验通过 · 初始 ${state.level.initialBlocks.length} 块 · ${state.level.incomingRows.length} 条补行 · 唯一目标有效。导出后仍需游戏试玩。`;
    $('exportSettings').disabled=$('exportLevel').disabled=errors.length>0;
    $('settingsPreview').textContent=JSON.stringify(state.settings,null,2);
    $('levelPreview').textContent=JSON.stringify(state.level,null,2);
    return errors.length===0;
  }
  function fillSettings() {
    numeric.forEach(key=>$(key).value=state.settings[key]);
    $('settingsVersion').value=state.settings.version;$('levelVersion').value=state.level.version;
  }
  function renderLayouts() {
    $('layout').replaceChildren();
    const entries=[['-1','初始棋盘（8 行）'],...state.level.incomingRows.map((_,i)=>[String(i),`第 ${i+1} 次补行${i===state.level.incomingRows.length-1?'（最后一条，目标应在此）':''}`])];
    entries.forEach(([value,label])=>{const option=document.createElement('option');option.value=value;option.textContent=label;$('layout').append(option);});
    $('layout').value=String(scope);$('removeRow').disabled=scope<0||state.level.incomingRows.length<=1;
    $('layoutHint').textContent=scope<0?'初始布局：下方为 row 0，上方为 row 7，推进到 row 8 才失败。':`第 ${scope+1} 次补行：所有方块 row 必须为 0；本行在对应回合结算时补入底部。`;
  }
  function select(index,column=0,row=0) {
    selected=index;const block=index<0?{column,row,width:1,health:3,target:false}:current()[index];
    ['column','row','width','health'].forEach(key=>$(key).value=block[key]);$('target').checked=block.target;
    $('row').disabled=scope>=0;
    $('selectionTitle').textContent=index<0?'新增方块':`编辑第 ${index+1} 个方块`;
    $('applyBlock').textContent=index<0?'新增方块':'保存方块修改';$('deleteBlock').disabled=index<0;
    renderBoard();
  }
  function renderBoard() {
    const board=$('board'),count=scope<0?8:1;
    board.replaceChildren();board.style.gridTemplateRows=`repeat(${count}, minmax(0, 1fr))`;
    for(let row=count-1;row>=0;row--) for(let col=0;col<8;col++) {
      const cell=document.createElement('button');cell.type='button';cell.className='cell';cell.textContent='+';
      cell.style.gridColumn=col+1;cell.style.gridRow=count-row;cell.title=`列 ${col}，行 ${row}`;
      cell.setAttribute('aria-label',`空格 列${col} 行${row}`);cell.addEventListener('click',()=>select(-1,col,row));board.append(cell);
    }
    current().forEach((block,index)=>{
      const button=document.createElement('button');button.type='button';button.className=`block${block.target?' goal':''}${selected===index?' selected':''}`;
      button.style.gridColumn=`${block.column+1} / span ${block.width}`;button.style.gridRow=count-block.row;
      button.textContent=`${block.target?'★ ':''}${block.health}`;
      button.title=`列 ${block.column}，行 ${block.row}；宽 ${block.width}；血量 ${block.health}${block.target?'；目标':''}`;
      button.setAttribute('aria-label',button.title);button.addEventListener('click',()=>select(index));board.append(button);
    });
  }
  numeric.forEach(key=>$(key).addEventListener('input',()=>{state.settings[key]=$(key).value===''?NaN:Number($(key).value);changed();}));
  $('settingsVersion').addEventListener('input',()=>{state.settings.version=$('settingsVersion').value;changed();});
  $('levelVersion').addEventListener('input',()=>{state.level.version=$('levelVersion').value;changed();});
  $('layout').addEventListener('change',()=>{scope=Number($('layout').value);renderLayouts();select(-1);});
  $('blockForm').addEventListener('submit',event=>{
    event.preventDefault();
    const block={};['column','row','width','health'].forEach(key=>block[key]=$(key).value===''?NaN:Number($(key).value));block.target=$('target').checked;
    const error=M.placementError(current(),block,selected,scope>=0);
    if(error) {message(error,true);return;}
    if(block.target && (scope<0 || scope!==state.level.incomingRows.length-1)) {message('目标只能放在最后一条补行中。',true);return;}
    if(selected<0){current().push(block);selected=current().length-1;}else current()[selected]=block;
    select(selected);changed();message('方块已更新。通过整体校验后再导出。');
  });
  $('newBlock').addEventListener('click',()=>select(-1));
  $('deleteBlock').addEventListener('click',()=>{if(selected<0)return;current().splice(selected,1);select(-1);changed();message('已删除方块。');});
  $('addRow').addEventListener('click',()=>{state.level.incomingRows.push({blocks:[]});scope=state.level.incomingRows.length-1;renderLayouts();select(-1);changed();message('已新增末尾补行。原目标若不在最后一条，请重新放置目标。');});
  $('removeRow').addEventListener('click',()=>{
    if(scope<0||state.level.incomingRows.length<=1)return;
    if(!confirm(`删除第 ${scope+1} 次补行及其中所有块？`))return;
    state.level.incomingRows.splice(scope,1);scope=Math.min(scope,state.level.incomingRows.length-1);renderLayouts();select(-1);changed();
  });
  $('reset').addEventListener('click',()=>{
    if(!confirm('用默认示例替换当前编辑内容？未导出的修改会丢失。'))return;
    state=M.clone(PlannerDefaults);scope=-1;fillSettings();renderLayouts();select(-1);changed();$('source').textContent='当前编辑：已还原内置默认示例。';message('示例已恢复；尚未写回游戏文件。');
  });
  $('importFiles').addEventListener('change',async()=>{
    try {
      const files=Array.from($('importFiles').files),s=files.find(f=>f.name==='settings.json'),l=files.find(f=>f.name==='level.json');
      if(!s||!l||files.length!==2)throw new Error('请同时选择名称为 settings.json 和 level.json 的两份文件。');
      if(dirty&&!confirm('导入会替换尚未导出的编辑内容，继续？'))return;
      const imported={settings:JSON.parse(await s.text()),level:JSON.parse(await l.text())};
      const errors=M.validate(imported.settings,imported.level);
      if(errors.length)throw new Error('导入失败，当前编辑内容保持不变：\n'+errors.slice(0,8).join('\n'));
      state=imported;scope=-1;dirty=false;fillSettings();renderLayouts();select(-1);validate();
      $('source').textContent=`当前编辑：已导入 settings.json（${state.settings.version}）与 level.json（${state.level.version}）。`;
      message('两份配置已导入。现在可以编辑，修改不会自动写回原文件。');
    } catch(error) {message(error.message,true);} finally {$('importFiles').value='';}
  });
  function download(kind) {
    if(!validate())return;
    const name=kind==='settings'?'settings.json':'level.json';
    const blob=new Blob([JSON.stringify(state[kind],null,2)+'\n'],{type:'application/json;charset=utf-8'});
    const url=URL.createObjectURL(blob),link=document.createElement('a');link.href=url;link.download=name;document.body.append(link);link.click();link.remove();setTimeout(()=>URL.revokeObjectURL(url),10000);
    message(`已请求下载 ${name}。请确认下载完成，并用该文件替换游戏 Config 下同名文件，再点击游戏 Reload + Restart。`);
  }
  $('exportSettings').addEventListener('click',()=>download('settings'));$('exportLevel').addEventListener('click',()=>download('level'));
  window.addEventListener('beforeunload',event=>{if(dirty){event.preventDefault();event.returnValue='';}});
  fillSettings();renderLayouts();select(-1);validate();
})();
