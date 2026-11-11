const fs=require('node:fs');
const path=require('node:path');
const crypto=require('node:crypto');
const root=path.resolve(__dirname,'../../Docs/中期版/音效试听包/矿石轻乐音候选');
const original=path.resolve(__dirname,'../../Assets/Holedown/Audio');
const hash=data=>crypto.createHash('sha256').update(data).digest('hex');
const write=(name,data)=>fs.writeFileSync(path.join(root,name),data,{flag:'wx'});
const manifest=JSON.parse(fs.readFileSync(path.join(root,'素材清单.json'),'utf8'));
const baseline=JSON.parse(fs.readFileSync(path.join(root,'原音效校验.json'),'utf8'));
for(const [name,expected] of Object.entries(baseline))if(hash(fs.readFileSync(path.join(original,name)))!==expected)throw Error('Original changed: '+name);
const names=fs.readdirSync(path.join(root,'试听')).filter(n=>n.endsWith('.wav')).sort();
if(names.length!==8)throw Error('Expected eight main clips, got '+names.length);
function wave(file){
 const b=fs.readFileSync(file);
 if(b.toString('ascii',0,4)!=='RIFF'||b.toString('ascii',8,12)!=='WAVE'||b.readUInt32LE(4)!==b.length-8||b.readUInt32LE(24)!==48000||b.readUInt16LE(22)!==1||b.readUInt16LE(34)!==16||b.readUInt16LE(20)!==1||b.toString('ascii',36,40)!=='data'||b.readUInt32LE(40)!==b.length-44)throw Error('Invalid WAV: '+file);
 const samples=b.subarray(44);let peak=0,total=0,nonzero=0;
 for(let i=0;i<samples.length;i+=2){const v=samples.readInt16LE(i)/32768;peak=Math.max(peak,Math.abs(v));total+=v*v;if(v!==0)nonzero++;}
 const seconds=samples.length/96000,rms=Math.sqrt(total/(samples.length/2));
 if(!(seconds>0&&seconds<=3&&peak>0&&peak<0.99&&rms>0.00001&&nonzero>0))throw Error('Invalid duration or samples: '+file);
 if(samples.readInt16LE(0)!==0||samples.readInt16LE(samples.length-2)!==0)throw Error('Unfaded endpoints: '+file);
 return {file:path.relative(root,file).replaceAll('\\','/'),seconds,peak,rms,bytes:b.length,sha256:hash(b),samples};
}
const main=names.map(n=>wave(path.join(root,'试听',n)));
const alternatives=fs.readdirSync(path.join(root,'备选')).filter(n=>n.endsWith('.wav')).map(n=>wave(path.join(root,'备选',n)));
if(alternatives.length!==12)throw Error('Expected twelve alternatives');
const report={date:new Date().toISOString(),format:'48kHz, PCM16, mono',main:main.map(({samples,...m})=>m),alternatives:alternatives.map(({samples,...m})=>m),originalAudioUnchanged:true,decodeChecked:true,subjectiveListeningChecked:false,licenseNote:manifest.licenseEvidence,airSubstitution:manifest.airSubstitution};
write('技术校验.json',JSON.stringify(report,null,2));
const chunks=[];for(const item of main){chunks.push(item.samples,Buffer.alloc(43200));}const pcm=Buffer.concat(chunks);const header=Buffer.alloc(44);header.write('RIFF');header.writeUInt32LE(pcm.length+36,4);header.write('WAVEfmt ',8);header.writeUInt32LE(16,16);header.writeUInt16LE(1,20);header.writeUInt16LE(1,22);header.writeUInt32LE(48000,24);header.writeUInt32LE(96000,28);header.writeUInt16LE(2,32);header.writeUInt16LE(16,34);header.write('data',36);header.writeUInt32LE(pcm.length,40);write('00_八类顺序试听.wav',Buffer.concat([header,pcm]));
const recipes=[
 '01 发射：knifeSlice.ogg；1.5倍速，最长0.32秒，峰值约0.50。替代原计划Swishes气流素材。',
 '02 受击：impactMining_000.ogg；1.12倍速，最长0.18秒，峰值约0.26，保留短起音。',
 '03 碎裂：impactMining_002.ogg＋003（延迟35毫秒、0.35相对增益）；最长0.50秒，峰值约0.60。',
 '04 奖励：jingles_PIZZI16.ogg；1.25倍速，最长1.40秒，峰值约0.50。',
 '05 过关：jingles_PIZZI10.ogg；原速，最长2.80秒，峰值约0.65。',
 '06 失败：jingles_PIZZI11.ogg；0.95倍速，最长2.80秒，峰值约0.50。',
 '07 召回：knifeSlice2.ogg；倒放、1.2倍速，最长0.40秒，峰值约0.40。替代原计划Swishes素材。',
 '08 确认：click_001.ogg；原速，最长0.12秒，峰值约0.24。'
];
write('试听说明.txt','矿石质感＋轻乐音｜独立候选试听包\n\n状态：未接入Unity，未修改原有八条音效。仅完成原生音频解码、格式、时长、峰值、非静音及校验和检查；没有主观试听定稿。请用较低音量开始试听，尤其检查高频重复受击、碎裂与奖励的区分。\n\n使用方式：双击“试听入口.html”逐条播放；或播放“00_八类顺序试听.wav”（顺序01～08，每条间隔0.45秒）。八个独立文件在“试听”目录，备用对照在“备选”。原始OGG未改动，见“原始素材”。\n\n规格：独立试听文件为48 kHz、16-bit、单声道PCM WAV，均不超过3秒；顺序试听总文件超过3秒，不用于游戏槽位。所有单曲去掉低于峰值0.1%的首尾空白、保留约5/10毫秒余量，施加1毫秒起音保护与最多12毫秒尾部淡出，峰值按各事件分工设置；备用WAV统一峰值0.70、最长3秒，完整原件仍保留。\n\n'+recipes.join('\n')+'\n\n授权与来源：作者均为Kenney。RPG替代素材的镜像明确保留作者说明和CC0原文，已一并保存；碰撞、界面、乐句三包的原作者公开页面标注CC0，但当前镜像未附其独立素材包许可证，因此保留官方页地址及固定版本镜像路径作为溯源，并建议正式商用交付前再次取得原包许可。不能将镜像仓库的代码BSD/GPL等许可误当成音频许可。未使用SummoningWars的swish素材，该镜像存在CC-BY-SA总声明，未将其冒称CC0。\n\n所有原件的作者页面、固定提交链接、SHA-256和实际下载文件名见“素材清单.json”。处理数据见“技术校验.json”；原游戏音效的校验记录见“原音效校验.json”。包内没有替换版audio.json，不会自动导入或改写Unity。\n\n音效类型、旋律情绪和整体风格仍需由用户实际试听确认。这是候选，不是音效定稿。\n');
const escape=s=>s.replaceAll('&','&amp;').replaceAll('<','&lt;').replaceAll('"','&quot;');
const cards=main.map((item,i)=>`<section><h2>${escape(names[i].replace(/\.wav$/,''))}</h2><p>${item.seconds.toFixed(3)} 秒 · 48 kHz / 16-bit / 单声道 · 候选</p><audio controls preload="metadata" src="data:audio/wav;base64,${fs.readFileSync(path.join(root,item.file)).toString('base64')}"></audio></section>`).join('');
write('试听入口.html',`<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>矿石轻乐音 · 八类候选试听</title><style>body{margin:0;background:#111b2c;color:#eee9d5;font:16px/1.7 system-ui,sans-serif}main{max-width:820px;margin:auto;padding:32px 20px}h1{font-size:30px;color:#a4ddd0}h2{font-size:19px;margin:0 0 6px}p{color:#aab8c7;margin:0 0 12px}section{padding:22px 0;border-bottom:1px solid #2c3a4e}audio{width:100%}.note{padding:16px;border-left:3px solid #d2aa69;background:#182338}</style><main><h1>矿石质感＋轻乐音</h1><p>独立音效候选 · 八类试听 · 2026-11-09</p><div class="note">请先调低音量，再逐条试听。未接入游戏、未主观定稿；发射与召回使用已注明的替代素材。页面完全离线，音频已内嵌，不需要服务器。</div>${cards}<section><h2>来源和授权</h2><p>Kenney。原始音频、许可证据、镜像版本与加工记录随ZIP保存。部分镜像未附独立包许可，正式商用前请复核原作者包内授权。此页面仅供候选比较，不自动替换游戏文件。</p></section></main></html>`);
function walk(directory){return fs.readdirSync(directory,{withFileTypes:true}).flatMap(e=>e.isDirectory()?walk(path.join(directory,e.name)):[path.join(directory,e.name)]);}
write('SHA256SUMS.txt',walk(root).map(file=>`${hash(fs.readFileSync(file))}  ${path.relative(root,file).replaceAll('\\','/')}`).sort().join('\n')+'\n');
console.log(JSON.stringify({main:report.main,alternatives:alternatives.length,sequenceSeconds:pcm.length/96000,originalAudioUnchanged:true},null,2));
