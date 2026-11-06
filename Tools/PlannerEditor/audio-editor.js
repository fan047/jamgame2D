/* global PlannerAudio, PlannerAudioDefaults, PlannerState */
(function(){
  'use strict';
  const A=PlannerAudio,$=id=>document.getElementById(id);
  const state=A.defaults();
  let baseline=A.clone(state),exported=null,context=null,buffers=new Map(),playing=null,pending=0;
  const dirty=()=>JSON.stringify(state)!==JSON.stringify(baseline);
  const status=()=>exported&&JSON.stringify(state)===JSON.stringify(exported)?'已请求导出，请核对下载':dirty()?'有未导出修改':'未修改';
  function message(text,error=false){const box=$('message');box.textContent=text;box.classList.toggle('error',error);box.style.display='block';}
  function decoded(name){
    if(buffers.has(name))return buffers.get(name);
    const base64=state.cues[name].wavBase64||(PlannerAudioDefaults&&PlannerAudioDefaults[name]);
    if(!base64)throw new Error(name+'：缺少内置试听素材。');
    const promise=new AudioContext().decodeAudioData(A.decodeBase64(base64).buffer.slice(0)).then(buffer=>{buffers.set(name,buffer);return buffer;},error=>{buffers.delete(name);throw new Error('无法解码 '+name+'：'+error.message);});
    buffers.set(name,promise);return promise;
  }
  function audio(){if(!context)context=new (window.AudioContext||window.webkitAudioContext)({sampleRate:A.RATE});return context;}
  async function preview(name){
    const cue=state.cues[name];if(cue.muted||cue.volumePercent===0){message(cue.muted?'该音效已静音，先取消静音再试听。':'该音效音量为 0%，先调高再试听。',true);return false;}
    if(playing&&playing.source){try{playing.source.stop();}catch(e){}playing=null;}
    const ctx=audio();const buffer=await decoded(name);
    const source=ctx.createBufferSource(),gain=ctx.createGain();
    source.buffer=buffer;gain.gain.value=cue.volumePercent/100;source.connect(gain).connect(ctx.destination);source.start();
    playing={source,name};
    document.dispatchEvent(new CustomEvent('planner-audio-preview',{detail:{name,volumePercent:cue.volumePercent,seconds:buffer.duration}}));
    message(`正在试听 ${cueLabel(name)} · ${cue.volumePercent}%${cue.wavBase64?' · 自定义素材':' · 内置素材'}（共 ${buffer.duration.toFixed(2)} 秒）`);
    return true;
  }
  function cueLabel(name){return (A.CUES.find(c=>c.name===name)||{label:name}).label;}
  // Any decodable WAV is accepted, then normalised to 48 kHz, 16-bit mono PCM for both preview and export.
  async function convert(file){
    if(!file)throw new Error('请选择一个 WAV 文件。');
    if(!/\.wav$/i.test(file.name))throw new Error('仅支持 .wav 文件；其他格式请先转成 WAV。');
    if(file.size>A.MAX_BYTES)throw new Error('WAV 文件过大（最多 4 MiB）。');
    const bytes=new Uint8Array(await file.arrayBuffer());
    let buffer;
    try{buffer=await audio().decodeAudioData(bytes.buffer.slice(0));}
    catch(e){throw new Error('无法解码该 WAV：'+e.message);}
    if(!buffer.length)throw new Error('该 WAV 没有音频数据。');
    if(buffer.duration>A.MAX_SECONDS+1e-6)throw new Error(`音频 ${buffer.duration.toFixed(2)} 秒，超过 ${A.MAX_SECONDS} 秒上限。`);
    const offline=new (window.OfflineAudioContext||window.webkitOfflineAudioContext)(1,Math.max(1,Math.ceil(buffer.duration*A.RATE)),A.RATE);
    const source=offline.createBufferSource();source.buffer=buffer;source.connect(offline.destination);source.start();
    const rendered=await offline.startRendering();
    return A.encodeBase64(A.encodeWave(rendered.getChannelData(0)));
  }
  function row(name){
    const cue=A.CUES.find(c=>c.name===name),value=state.cues[name],tr=document.createElement('tr');
    tr.dataset.cue=name;
    const cell=(className,text)=>{const td=document.createElement('td');if(className)td.className=className;if(text!==undefined)td.textContent=text;tr.append(td);return td;};
    cell('cue-name').append(Object.assign(document.createElement('strong'),{textContent:cue.label}));
    cell().append(Object.assign(document.createElement('code'),{textContent:name}));
    cell('cue-hint').textContent=cue.hint;
    const volumeCell=cell('cue-volume');const input=document.createElement('input');input.type='number';input.min='0';input.max='150';input.step='1';input.value=String(value.volumePercent);input.dataset.volume=name;input.setAttribute('aria-label',cue.label+'音量百分比');
    input.oninput=()=>{const raw=input.value.trim();const parsed=raw===''?NaN:Number(raw);const valid=Number.isInteger(parsed)&&parsed>=0&&parsed<=150;input.classList.toggle('invalid',!valid);input.title=valid?'0–150 的整数':'需为 0–150 的整数';if(valid){state.cues[name].volumePercent=parsed;refresh();}};
    input.onblur=()=>{if(input.classList.contains('invalid')){input.value=String(state.cues[name].volumePercent);input.classList.remove('invalid');message('音量需为 0–150 的整数，已恢复原值。',true);}};
    volumeCell.append(input);
    const muteCell=cell('cue-mute'),label=document.createElement('label'),check=document.createElement('input');check.type='checkbox';check.checked=value.muted;check.dataset.mute=name;check.setAttribute('aria-label',cue.label+'静音');
    check.onchange=()=>{state.cues[name].muted=check.checked;refresh();};label.append(check);muteCell.append(label);
    const clipCell=cell('cue-clip'),badge=document.createElement('span');badge.className='badge'+(value.wavBase64?' custom':'');badge.textContent=value.wavBase64?'自定义 WAV':'内置素材';clipCell.append(badge);
    const actionsCell=cell('cue-actions');
    const previewButton=document.createElement('button');previewButton.type='button';previewButton.textContent='试听';previewButton.dataset.preview=name;previewButton.onclick=()=>preview(name).catch(error=>message(error.message,true));
    const pick=document.createElement('label');pick.className='button small';pick.textContent='换 WAV';
    const file=document.createElement('input');file.type='file';file.accept='.wav,audio/wav';file.hidden=true;file.dataset.file=name;
    file.onchange=async()=>{const chosen=file.files[0];file.value='';if(!chosen)return;pending++;refresh();try{const base64=await convert(chosen);state.cues[name].wavBase64=base64;buffers.delete(name);message(`${cueLabel(name)}已换为 ${chosen.name}，已转换为 48 kHz、16-bit 单声道；音量与静音保持不变。`);}
      catch(error){message(`${cueLabel(name)}未替换：${error.message} 当前仍为${state.cues[name].wavBase64?'自定义':'内置'}素材。`,true);}
      finally{pending--;refresh();}};
    pick.append(file);
    const reset=document.createElement('button');reset.type='button';reset.textContent='恢复默认';reset.dataset.reset=name;reset.disabled=!value.wavBase64&&value.volumePercent===100&&!value.muted;
    reset.onclick=()=>{state.cues[name]={volumePercent:100,muted:false,wavBase64:null};buffers.delete(name);refresh();message(`${cueLabel(name)}已恢复内置素材、100% 音量并取消静音。`);};
    actionsCell.append(previewButton,pick,reset);
    return tr;
  }
  function refresh(){
    $('audioRows').replaceChildren(...A.NAMES.map(row));
    $('audioStatus').textContent=status()+(pending?'（正在转换 WAV…）':'');
    const blocked=pending>0;
    $('exportAudio').disabled=blocked;$('audioVersion').disabled=blocked;
    $('audioPreviewJson').textContent=JSON.stringify(A.build(state),null,2);
  }
  function apply(doc,source){
    state.schemaVersion=1;state.version=doc.version;state.cues=doc.cues;buffers.clear();refresh();
    message(source);
  }
  $('audioVersion').oninput=()=>{const input=$('audioVersion'),valid=/^[A-Za-z0-9_.-]{1,32}$/.test(input.value);input.classList.toggle('invalid',!valid);input.title=valid?'1–32 位字母、数字、点、下划线或连字符':'需为 1–32 位字母、数字、点、下划线或连字符';if(valid){state.version=input.value;refresh();}};
  $('audioVersion').onblur=()=>{const input=$('audioVersion');if(input.classList.contains('invalid')){input.value=state.version;input.classList.remove('invalid');}};
  $('audioStamp').onclick=()=>{state.version=PlannerState.version();$('audioVersion').value=state.version;refresh();};
  $('audioImport').onchange=async()=>{
    const input=$('audioImport'),file=input.files[0];input.value='';if(!file)return;
    try{
      if(!/\.json$/i.test(file.name))throw new Error('请选择 audio.json 文件。');
      const doc=A.parse(await file.text());
      if(dirty()&&!confirm('导入会替换未导出的音效修改，继续？'))return;
      $('audioVersion').value=doc.version;apply(doc,`已导入音效配置 ${doc.version}；请核对音量、静音与自定义素材后导出。`);
    }catch(error){message(`音效配置导入失败，当前设置保持不变：${error.message}`,true);}
  };
  $('exportAudio').onclick=()=>{
    try{
      if(pending){throw new Error('WAV 仍在转换，请稍候。');}
      const doc=A.build(state),blob=new Blob([JSON.stringify(doc,null,2)+'\n'],{type:'application/json;charset=utf-8'}),url=URL.createObjectURL(blob),link=document.createElement('a');
      link.href=url;link.download='audio.json';document.body.append(link);link.click();link.remove();setTimeout(()=>URL.revokeObjectURL(url),10000);
      exported=A.clone(state);refresh();
      message('已请求导出 audio.json，请放入游戏 exe 同目录的 Config，再在策划测试暂停菜单点「重载音效」；不是已保存到游戏。');
    }catch(error){message('不能导出：'+error.message,true);}
  };
  $('audioResetAll').onclick=()=>{if(!confirm('把全部音效恢复为内置素材、100% 音量并取消静音？'))return;state.version=A.defaults().version;state.cues=A.defaults().cues;$('audioVersion').value=state.version;buffers.clear();refresh();message('全部音效已恢复默认。');};
  window.PlannerAudioEditor={isDirty:dirty,state,build:()=>A.build(state),preview};
  window.addEventListener('beforeunload',e=>{if(dirty()){e.preventDefault();e.returnValue='';}});
  refresh();
})();
