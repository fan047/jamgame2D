(function(root,factory){const api=factory();if(typeof module==='object'&&module.exports)module.exports=api;else root.PlannerAudio=api;})(typeof globalThis!=='undefined'?globalThis:this,function(){
  'use strict';
  const CUES=[
    {name:'volley_launch',label:'发射',hint:'每轮首球出手，一声短气流。'},
    {name:'block_hit',label:'受击',hint:'方块受伤且还在，高频轻敲。'},
    {name:'block_break',label:'碎裂',hint:'普通块或目标块击碎，玻璃裂响。'},
    {name:'reward_break',label:'奖励',hint:'击碎奖励块出现 +球 提示，上扬拨弦音。'},
    {name:'stage_clear',label:'过关',hint:'本轮结算为通关，明亮上行乐句。'},
    {name:'stage_fail',label:'失败',hint:'本轮结算为失败，柔和下行乐句。'},
    {name:'manual_recall',label:'召回',hint:'主动召回生效，收束气流。'},
    {name:'ui_confirm',label:'确认',hint:'开始、暂停或重试等操作生效，轻点击声。'}
  ];
  const NAMES=CUES.map(c=>c.name);
  const MAX_SECONDS=3,MAX_BYTES=4*1024*1024,MAX_WAV_BYTES=48000*MAX_SECONDS*2;
  const RATE=48000;
  function defaults(){const cues={};for(const name of NAMES)cues[name]={volumePercent:100,muted:false,wavBase64:null};return {schemaVersion:1,version:'audio-1',cues};}
  function clone(value){return JSON.parse(JSON.stringify(value));}
  function invalid(message){throw new Error(message);}
  function isObject(value){return value!==null&&typeof value==='object'&&!Array.isArray(value);}
  function keys(value,path,allowed){for(const key of Object.keys(value))if(!allowed.includes(key))invalid(`${path}.${key}：未知字段或音效槽位。`);}
  function version(value,path='audio.json.version'){
    if(typeof value!=='string'||!/^[A-Za-z0-9_.-]{1,32}$/.test(value))invalid(`${path}：需为 1–32 位字母、数字、点、下划线或连字符。`);
    return value;
  }
  function volume(value,path){
    if(!Number.isInteger(value)||value<0||value>150)invalid(`${path}：需为 0–150 的整数。`);
    return value;
  }
  // Parses audio.json. Never partially applies: the caller receives a full document or an error.
  function parse(text){
    let doc;try{doc=JSON.parse(text);}catch(e){invalid('audio.json：JSON 格式错误。');}
    if(!isObject(doc))invalid('audio.json：根节点需为对象。');
    keys(doc,'audio.json',['schemaVersion','version','cues']);
    if(doc.schemaVersion!==1)invalid('audio.json.schemaVersion：仅支持整数 1。');
    const result=defaults();result.version=version(doc.version);
    if(doc.cues===undefined)return result;
    if(!isObject(doc.cues))invalid('audio.json.cues：需为对象。');
    keys(doc.cues,'audio.json.cues',NAMES);
    for(const name of Object.keys(doc.cues)){
      const path='audio.json.cues.'+name,value=doc.cues[name];
      if(!isObject(value))invalid(path+'：需为对象。');
      keys(value,path,['volumePercent','muted','wavBase64']);const cue=result.cues[name];
      if(value.volumePercent!==undefined)cue.volumePercent=volume(value.volumePercent,path+'.volumePercent');
      if(value.muted!==undefined){if(typeof value.muted!=='boolean')invalid(path+'.muted：需为布尔值。');cue.muted=value.muted;}
      if(value.wavBase64!==undefined){
        if(typeof value.wavBase64!=='string'||!value.wavBase64)invalid(path+'.wavBase64：需为有效 WAV 的 Base64。');
        const bytes=decodeBase64(value.wavBase64);inspectWave(bytes);cue.wavBase64=value.wavBase64;
      }
    }
    return result;
  }
  // Writes only replaced clips; unchanged cues stay compact.
  function build(state){
    version(state.version);const cues={};
    for(const name of NAMES){
      const cue=state.cues[name]||{volumePercent:100,muted:false,wavBase64:null};
      volume(cue.volumePercent,'audio.json.cues.'+name+'.volumePercent');
      if(typeof cue.muted!=='boolean')invalid('audio.json.cues.'+name+'.muted：需为布尔值。');
      const item={volumePercent:cue.volumePercent,muted:cue.muted};
      if(cue.wavBase64){inspectWave(decodeBase64(cue.wavBase64));item.wavBase64=cue.wavBase64;}
      cues[name]=item;
    }
    return {schemaVersion:1,version:state.version,cues};
  }
  function decodeBase64(text){
    if(typeof atob==='function'&&typeof Uint8Array==='function'){
      let binary;try{binary=atob(text);}catch(e){invalid('WAV 不是有效的 Base64。');}
      const bytes=new Uint8Array(binary.length);for(let i=0;i<binary.length;i++)bytes[i]=binary.charCodeAt(i);return bytes;
    }
    const buffer=Buffer.from(text,'base64');return new Uint8Array(buffer.buffer,buffer.byteOffset,buffer.byteLength);
  }
  function encodeBase64(bytes){
    if(typeof btoa==='function'){let binary='';for(let i=0;i<bytes.length;i+=8192)binary+=String.fromCharCode.apply(null,bytes.subarray(i,i+8192));return btoa(binary);}
    return Buffer.from(bytes).toString('base64');
  }
  function ascii(bytes,offset){return String.fromCharCode(bytes[offset],bytes[offset+1],bytes[offset+2],bytes[offset+3]);}
  function u32(bytes,offset){return (bytes[offset]|(bytes[offset+1]<<8)|(bytes[offset+2]<<16)|(bytes[offset+3]<<24))>>>0;}
  function u16(bytes,offset){return bytes[offset]|(bytes[offset+1]<<8);}
  // Mirrors the runtime reader: RIFF/WAVE, PCM, mono, 48 kHz, 16-bit, 0–3 seconds of data.
  function inspectWave(bytes){
    if(!bytes||bytes.length<44)invalid('不是完整的 RIFF/WAVE 文件。');
    if(bytes.length>MAX_WAV_BYTES+1024)invalid('WAV 超过 3 秒。');
    if(ascii(bytes,0)!=='RIFF'||u32(bytes,4)!==bytes.length-8||ascii(bytes,8)!=='WAVE')invalid('不是完整的 RIFF/WAVE 文件。');
    let offset=12,fmt=false,chunks=0,dataOffset=-1,dataLength=0;
    while(offset<bytes.length){
      if(bytes.length-offset<8)invalid('WAV 分块头不完整。');
      const tag=ascii(bytes,offset),length=u32(bytes,offset+4),start=offset+8,end=start+length;
      if(end>bytes.length)invalid('WAV 分块数据被截断。');
      chunks++;
      if(tag==='fmt '){
        if(fmt||length<16)invalid('WAV 格式块无效。');
        if(u16(bytes,start)!==1||u16(bytes,start+2)!==1)invalid('请用工作台转换为 48 kHz、16-bit、单声道 PCM WAV。');
        if(u32(bytes,start+4)!==RATE||u32(bytes,start+8)!==RATE*2||u16(bytes,start+14)!==16)invalid('请用工作台转换为 48 kHz、16-bit、单声道 PCM WAV。');
        fmt=true;
      }else if(tag==='data'){
        if(dataOffset>=0||length===0||length%2!==0)invalid('WAV 需为 0–3 秒的非空音频。');
        if(length>MAX_WAV_BYTES)invalid('WAV 超过 3 秒。');
        dataOffset=start;dataLength=length;
      }
      offset=end+(length%2);
      if(offset>bytes.length)invalid('WAV 填充字节缺失。');
    }
    if(!fmt||dataOffset<0)invalid('WAV 缺少格式或音频数据。');
    return {seconds:dataLength/2/RATE,bytes:bytes.length,dataOffset,dataLength};
  }
  function encodeWave(samples){
    if(!Array.isArray(samples)&&!(samples instanceof Float32Array))invalid('音频数据缺失。');
    if(samples.length===0||samples.length>RATE*MAX_SECONDS)invalid('音频需为 0–3 秒。');
    const bytes=new Uint8Array(44+samples.length*2),view=new DataView(bytes.buffer);
    const text=(offset,value)=>{for(let i=0;i<4;i++)bytes[offset+i]=value.charCodeAt(i);};
    text(0,'RIFF');view.setUint32(4,bytes.length-8,true);text(8,'WAVE');text(12,'fmt ');
    view.setUint32(16,16,true);view.setUint16(20,1,true);view.setUint16(22,1,true);view.setUint32(24,RATE,true);
    view.setUint32(28,RATE*2,true);view.setUint16(32,2,true);view.setUint16(34,16,true);text(36,'data');
    view.setUint32(40,samples.length*2,true);
    for(let i=0;i<samples.length;i++){const clamped=Math.max(-1,Math.min(1,samples[i]));view.setInt16(44+i*2,Math.round(clamped*32767),true);}
    inspectWave(bytes);return bytes;
  }
  return {CUES,NAMES,MAX_SECONDS,MAX_BYTES,RATE,defaults,clone,parse,build,inspectWave,encodeWave,decodeBase64,encodeBase64,version,volume};
});
