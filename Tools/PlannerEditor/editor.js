/* global PlannerModel, PlannerDefaults, PlannerState */
(function(){
  'use strict';
  const $=id=>document.getElementById(id), M=PlannerModel;
  const state=new PlannerState.EditorState(PlannerDefaults.settings,PlannerDefaults.level);
  const numeric=['ballsPerRound','ballRadius','ballSpeed','launchInterval','maxAimAngle','recallTimeout'];
  let propertyPending=false, continuation=null;
  const role=b=>b.target?'target':b.rewardBalls?'reward':'ordinary';
  const roleName=b=>b.target?'目标':b.rewardBalls?`奖励 +${b.rewardBalls}`:'普通';
  function message(text,error=false){$('message').textContent=text;$('message').classList.toggle('error',error);$('message').style.display='block';}
  function attempt(fn){try{fn();}catch(e){message(e.message,true);if(e.path)locate(e.path);}}
  function inputFor(path){return [...document.querySelectorAll('[data-path]')].find(e=>e.dataset.path===path);}
  function guard(action){
    if(!state.flush()){const [path,draft]=state.firstInvalid;message(`未提交输入：${draft.error}。请修正或取消。`,true);inputFor(path)?.focus();refresh();return false;}
    if(propertyPending){continuation=action;$('draftDialog').showModal();return false;}
    attempt(action);return true;
  }
  function markInput(input,path){
    input.dataset.path=path;
    input.addEventListener('input',()=>{const draft=state.stageInput(path,input.value);input.classList.toggle('invalid',!!draft.error);input.title=draft.error||'失焦或 Enter 提交';refresh();});
    input.addEventListener('blur',()=>commit(input,path));
    input.addEventListener('keydown',e=>{if(e.key==='Enter'){e.preventDefault();if(!commit(input,path))return;const inputs=[...document.querySelectorAll('#blockRows input[data-path]')];const i=inputs.indexOf(input);if(i>=0&&inputs[i+1])inputs[i+1].focus();else input.blur();}if(e.key==='Escape'){state.cancelInput(path);input.value=state.read(path);input.classList.remove('invalid');refresh();}});
  }
  function commit(input,path){if(!state.commitInput(path)){message('输入无效，请修正或按 Esc 取消。',true);return false;}input.classList.remove('invalid');renderBoard();syncHealth();refresh();return true;}
  function syncHealth(){document.querySelectorAll('[data-path]').forEach(input=>{if(document.activeElement!==input&&!state.drafts[input.dataset.path])input.value=state.read(input.dataset.path);});}
  numeric.forEach(key=>markInput($(key),`settings.${key}`));markInput($('settingsVersion'),'settings.version');markInput($('levelVersion'),'level.version');
  function refresh(){
    const errors=M.diagnostics(state.settings,state.level),drafts=Object.entries(state.drafts);
    $('settingsStatus').textContent=state.status('settings')+(drafts.some(([p])=>p.startsWith('settings.'))?'（有未提交输入）':'');
    $('levelStatus').textContent=state.status('level')+(propertyPending||drafts.some(([p])=>p.startsWith('level.'))?'（有未提交输入 / 属性）':'');
    $('undo').disabled=!state.past.length;$('redo').disabled=!state.future.length;
    const box=$('validation');box.replaceChildren();box.classList.toggle('invalid',errors.length>0||!!state.firstInvalid);
    $('exportSettings').disabled=$('exportLevel').disabled=errors.length>0||!!state.firstInvalid;
    if(!errors.length&&!state.firstInvalid)box.textContent=`校验通过 · ${state.level.levels.length} 关 · 形状 1×1 / 2×1 / 1×2 · 导出后仍需游戏实射验证。`;
    errors.forEach(error=>{const button=document.createElement('button');button.type='button';button.className='error-link';button.textContent=`${error.path}：${error.message}`;button.onclick=()=>guard(()=>locate(error.path));box.append(button);});
    drafts.filter(([,d])=>d.error).forEach(([path,d])=>{const p=document.createElement('p');p.textContent=`${path}：${d.error}（未提交）`;box.append(p);});
    const sum=state.summary();$('batchSummary').textContent=`已选 ${sum.total}：普通 ${sum.ordinary} / 奖励 ${sum.reward} / 目标 ${sum.target}。整批成功才生效，一次撤销。`;
    const all=[...state.stage.initialBlocks,...state.stage.incomingRows.flatMap(r=>r.blocks)];
    $('statistics').textContent=`本布局 ${state.blocks.length} 块 / 总血量 ${state.blocks.reduce((n,b)=>n+b.health,0)}；本关 ${all.length} 块 / 总血量 ${all.reduce((n,b)=>n+b.health,0)}`;
    const options=state.level.levels.map((_,i)=>M.entryOptions(state.settings,state.level,i));$('entryOptions').textContent='可能入关球数（不是连续区间）：'+options.map((v,i)=>`第 ${i+1} 关 [${v.join(', ')}]`).join('；');
    $('settingsPreview').textContent=JSON.stringify(state.settings,null,2);$('levelPreview').textContent=JSON.stringify(state.level,null,2);renderPreview();
  }
  function renderSelectors(){
    $('stage').replaceChildren();state.level.levels.forEach((s,i)=>$('stage').add(new Option(`${i+1}. ${s.name} (${s.id})`,i)));$('stage').value=state.context.stage;
    $('layout').replaceChildren();$('layout').add(new Option('初始棋盘（8 行）',-1));state.stage.incomingRows.forEach((_,i)=>$('layout').add(new Option(`第 ${i+1} 次补行${i===state.stage.incomingRows.length-1?'（最后一条）':''}`,i)));$('layout').value=state.context.layout;
    $('removeRow').disabled=state.context.layout<0||state.stage.incomingRows.length<=1;
    $('layoutHint').textContent=state.context.layout<0?'初始棋盘下方 row 0，上方 row 7；推进至 row 8 失败。':'补行全部位于 row 0；唯一目标必须放在最后一条补行。';
  }
  function fillProperty(column=0,row=0){
    const selected=state.context.selected,b=selected<0?{column,row,width:1,height:1,health:3,target:false,rewardBalls:0}:state.blocks[selected];
    ['column','row','health'].forEach(key=>$(key).value=b[key]);$('shape').value=`${b.width}x${b.height}`;$('shape').querySelector('[value="1x2"]').disabled=state.context.layout>=0;$('role').value=role(b);$('rewardBalls').value=b.rewardBalls||3;
    $('role').querySelector('[value="reward"]').disabled=state.context.stage===state.level.levels.length-1;
    $('rewardBalls').disabled=role(b)!=='reward';$('row').disabled=state.context.layout>=0;
    $('selectionTitle').textContent=selected<0?'新增方块':`编辑第 ${selected+1} 块`;$('applyBlock').textContent=selected<0?'新增方块':'应用属性修改';$('deleteBlock').disabled=selected<0;propertyPending=false;
    $('health').removeAttribute('data-path');if(selected>=0)$('health').dataset.path=`${state.blockPath}[${selected}].health`;
  }
  function renderBoard(){
    const board=$('board'),count=state.context.layout<0?8:1;board.replaceChildren();board.style.gridTemplateRows=`repeat(${count},minmax(0,1fr))`;
    for(let row=count-1;row>=0;row--)for(let col=0;col<8;col++){const cell=document.createElement('button');cell.className='cell';cell.textContent='+';cell.style.gridColumn=col+1;cell.style.gridRow=count-row;cell.setAttribute('aria-label',`空格 列${col} 行${row}`);cell.onclick=()=>guard(()=>select(-1,col,row));board.append(cell);}
    state.blocks.forEach((b,i)=>{const button=document.createElement('button');button.className=`block${b.target?' goal':''}${b.rewardBalls?' reward':''}${state.context.selected===i?' selected':''}`;button.style.gridColumn=`${b.column+1} / span ${b.width}`;button.style.gridRow=`${count-b.row-b.height+1} / span ${b.height}`;button.textContent=`${b.target?'★ ':''}${b.health}${b.rewardBalls?` +${b.rewardBalls}`:''}`;button.title=`第 ${i+1} 块，列 ${b.column} 行 ${b.row}，形状 ${b.width}×${b.height}，${roleName(b)}`;button.onclick=()=>guard(()=>select(i));board.append(button);});
  }
  function renderTable(){
    $('blockRows').replaceChildren();state.blocks.forEach((b,i)=>{
      const tr=document.createElement('tr'),check=document.createElement('input');check.type='checkbox';check.checked=state.context.checked.includes(i);check.setAttribute('aria-label',`勾选第 ${i+1} 块`);
      check.onchange=()=>{state.context.checked=check.checked?[...new Set([...state.context.checked,i])]:state.context.checked.filter(v=>v!==i);refresh();};
      const td=document.createElement('td');td.append(check);tr.append(td);const label=document.createElement('button');label.textContent=String(i+1);label.onclick=()=>guard(()=>select(i));const c=document.createElement('td');c.append(label);tr.append(c);
      [`${b.column} / ${b.row}`,`${b.width}×${b.height}`,roleName(b)].forEach(text=>{const cell=document.createElement('td');cell.textContent=text;tr.append(cell);});
      const input=document.createElement('input');input.type='number';input.step='1';input.min='1';input.value=b.health;input.setAttribute('aria-label',`第 ${i+1} 块血量`);markInput(input,`${state.blockPath}[${i}].health`);const cell=document.createElement('td');cell.append(input);tr.append(cell);$('blockRows').append(tr);
    });
  }
  function render(){syncHealth();renderSelectors();fillProperty();renderBoard();renderTable();refresh();}
  function select(index,column=null,row=0){state.context.selected=index;fillProperty(column??0,row);if(index<0&&column!==null)propertyPending=true;renderBoard();refresh();}
  function propertyBlock(){
    const [width,height]=$('shape').value.split('x').map(Number);
    return {column:$('column').value.trim()===''?NaN:Number($('column').value),row:$('row').value.trim()===''?NaN:Number($('row').value),width,height,health:$('health').value.trim()===''?NaN:Number($('health').value),target:$('role').value==='target',rewardBalls:$('role').value==='reward'?Number($('rewardBalls').value):0};
  }
  function renderPreview(){
    $('board').querySelector('.placement-preview')?.remove();$('placementHint').textContent='';if(!propertyPending)return;
    const b=propertyBlock(),error=M.placementError(state.blocks,b,state.context.selected,state.context.layout>=0);
    $('placementHint').textContent=error?`未应用：${error}`:'虚线为未应用放置预览；点击应用才写入配置。';
    const count=state.context.layout<0?8:1;
    if(!Number.isInteger(b.column)||!Number.isInteger(b.row)||b.column<0||b.row<0||b.column+b.width>8||b.row+b.height>count)return;
    const preview=document.createElement('div');preview.className='placement-preview'+(error?' bad':'');preview.style.gridColumn=`${b.column+1} / span ${b.width}`;preview.style.gridRow=`${count-b.row-b.height+1} / span ${b.height}`;preview.textContent=`${b.width}×${b.height} 待应用`;$('board').append(preview);
  }
  function applyProperty(){
    const block=propertyBlock();
    if(['column','row','health'].some(k=>$(k).value.trim()===''))throw new Error('位置和血量不能为空');
    state.applyBlock(block);propertyPending=false;render();
  }
  ['column','row','shape','role','rewardBalls'].forEach(id=>$(id).addEventListener('input',()=>{propertyPending=true;$('rewardBalls').disabled=$('role').value!=='reward';refresh();}));
  $('health').addEventListener('input',()=>{if(state.context.selected<0){propertyPending=true;refresh();return;}const d=state.stageInput($('health').dataset.path,$('health').value);$('health').classList.toggle('invalid',!!d.error);refresh();});
  $('health').addEventListener('blur',()=>{if(state.context.selected>=0)commit($('health'),$('health').dataset.path);});
  $('health').addEventListener('keydown',e=>{if(e.key==='Enter'){e.preventDefault();if(state.context.selected>=0)commit($('health'),$('health').dataset.path);}if(e.key==='Escape'&&state.context.selected>=0){state.cancelInput($('health').dataset.path);$('health').value=state.blocks[state.context.selected].health;$('health').classList.remove('invalid');refresh();}});
  $('blockForm').onsubmit=e=>{e.preventDefault();if(state.flush())attempt(applyProperty);else message('请先修正或取消数值输入。',true);};
  $('draftApply').onclick=()=>{try{applyProperty();const next=continuation;continuation=null;$('draftDialog').close();attempt(next);}catch(e){message(e.message,true);}};
  $('draftDiscard').onclick=()=>{propertyPending=false;fillProperty();const next=continuation;continuation=null;$('draftDialog').close();attempt(next);};
  $('draftCancel').onclick=()=>{continuation=null;$('draftDialog').close();renderSelectors();};
  $('draftDialog').addEventListener('cancel',()=>{continuation=null;renderSelectors();});
  $('stage').onchange=()=>{const target=Number($('stage').value);guard(()=>{state.navigate(target);render();});renderSelectors();};
  $('layout').onchange=()=>{const target=Number($('layout').value);guard(()=>{state.navigate(state.context.stage,target);render();});renderSelectors();};
  $('newBlock').onclick=()=>guard(()=>select(-1));$('deleteBlock').onclick=()=>guard(()=>{state.deleteBlock();render();});
  $('addRow').onclick=()=>guard(()=>{state.addRow();render();});$('removeRow').onclick=()=>guard(()=>{if(confirm('删除当前补行及其中全部方块？')){state.removeRow();render();}});
  $('selectOrdinary').onclick=()=>guard(()=>{state.selectOrdinary();renderTable();refresh();});$('clearChecks').onclick=()=>{state.context.checked=[];renderTable();refresh();};
  $('batchApply').onclick=()=>guard(()=>{const summary=state.summary();if(!summary.total)throw new Error('请先勾选方块');if(!confirm(`修改 ${summary.total} 块（普通 ${summary.ordinary} / 奖励 ${summary.reward} / 目标 ${summary.target}）？整批一次撤销。`))return;const raw=$('batchValue').value;if(raw.trim()==='')throw new Error('批量血量不能为空');state.batch($('batchMode').value,Number(raw));render();message('整批已应用。');});
  function history(which){guard(()=>{state[which]();render();});}
  $('undo').onclick=()=>history('undo');$('redo').onclick=()=>history('redo');
  document.addEventListener('keydown',e=>{if(e.target.closest('input,textarea,select')||$('draftDialog').open)return;if((e.ctrlKey||e.metaKey)&&['z','y'].includes(e.key.toLowerCase())){e.preventDefault();history(e.key.toLowerCase()==='y'||e.shiftKey?'redo':'undo');}});
  $('cancelInputs').onclick=()=>{Object.keys(state.drafts).forEach(p=>state.cancelInput(p));document.querySelectorAll('.invalid[data-path]').forEach(e=>e.classList.remove('invalid'));syncHealth();refresh();message('未提交数值已恢复；位置、形状和类型草稿仍需应用或丢弃。');};
  ['settings','level'].forEach(file=>$(file+'Stamp').onclick=()=>guard(()=>{state.perform('生成版本',d=>d[file].version=PlannerState.version());syncHealth();refresh();}));
  function locate(path){
    const clean=path.replace('settings.json','settings').replace('level.json','level');const si=/levels\[(\d+)\]/.exec(clean),li=/incomingRows\[(\d+)\]/.exec(clean),bi=/(?:initialBlocks|\.blocks)\[(\d+)\]/.exec(clean);
    if(si){state.navigate(Number(si[1]),li?Number(li[1]):-1,bi?Number(bi[1]):-1);render();}
    const input=inputFor(clean);if(input){input.focus();input.scrollIntoView({block:'center'});}else $('board').scrollIntoView({block:'center'});
  }
  $('reset').onclick=()=>guard(()=>{if(!confirm('用默认示例替换当前内容？未导出修改会丢失。'))return;state.replace(PlannerDefaults.settings,PlannerDefaults.level);render();$('source').textContent='当前编辑：成长版内置默认示例，不是游戏当前文件。';});
  $('importFiles').onchange=async()=>{
    const files=Array.from($('importFiles').files);$('importFiles').value='';
    try{const s=files.find(f=>f.name==='settings.json'),l=files.find(f=>f.name==='level.json');if(files.length!==2||!s||!l)throw new Error('请同时选择 settings.json 和 level.json 两份文件。');const settings=M.parse(await s.text()),level=M.parse(await l.text());const errors=M.validate(settings,level);if(errors.length)throw new Error('导入失败，当前内容保持不变：\n'+errors.slice(0,8).join('\n'));guard(()=>{if(state.hasUnexported&&!confirm('导入会替换未导出的修改，继续？'))return;state.replace(settings,level);render();$('source').textContent=`当前编辑：已导入两份配置，${state.level.levels.length} 关。旧 v1 将导出为 v2。`;message('导入完成；不会自动写回原文件。');});}catch(e){message(e.message,true);}
  };
  function download(file){guard(()=>{state.markExport(file);const blob=new Blob([JSON.stringify(state[file],null,2)+'\n'],{type:'application/json;charset=utf-8'}),url=URL.createObjectURL(blob),a=document.createElement('a');a.href=url;a.download=file+'.json';document.body.append(a);a.click();a.remove();setTimeout(()=>URL.revokeObjectURL(url),10000);refresh();message(`已请求导出 ${file}.json，请核对下载并替换游戏 Config 文件，再 Reload + Restart；不是已保存到游戏。`);});}
  $('exportSettings').onclick=()=>download('settings');$('exportLevel').onclick=()=>download('level');
  window.addEventListener('beforeunload',e=>{if(state.hasUnexported||propertyPending){e.preventDefault();e.returnValue='';}});
  render();
})();


