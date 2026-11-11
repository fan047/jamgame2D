const fs = require('node:fs');
const path = require('node:path');
const http = require('node:http');
const root = path.resolve(__dirname, '../../Docs/中期版/音效试听包/矿石轻乐音候选');
const manifest = JSON.parse(fs.readFileSync(path.join(root, '素材清单.json'), 'utf8'));
const jobs = [
  { id:'volley_launch', label:'01 发射', file:'01_发射_volley_launch.wav', sources:['air1'], rate:1.5, peak:0.5, max:0.32 },
  { id:'block_hit', label:'02 受击', file:'02_受击_block_hit.wav', sources:['mining0'], rate:1.12, peak:0.26, max:0.18 },
  { id:'block_break', label:'03 碎裂', file:'03_碎裂_block_break.wav', sources:['mining2','mining3'], rate:1, peak:0.6, max:0.5 },
  { id:'reward_break', label:'04 奖励', file:'04_奖励_reward_break.wav', sources:['jingle16'], rate:1.25, peak:0.5, max:1.4 },
  { id:'stage_clear', label:'05 过关', file:'05_过关_stage_clear.wav', sources:['jingle10'], rate:1, peak:0.65, max:2.8 },
  { id:'stage_fail', label:'06 失败', file:'06_失败_stage_fail.wav', sources:['jingle11'], rate:0.95, peak:0.5, max:2.8 },
  { id:'manual_recall', label:'07 召回', file:'07_召回_manual_recall.wav', sources:['air2'], rate:1.2, peak:0.4, max:0.4, reverse:true },
  { id:'ui_confirm', label:'08 确认', file:'08_确认_ui_confirm.wav', sources:['click1'], rate:1, peak:0.24, max:0.12 }
];
const destinations = new Map(jobs.map(j => [j.id, path.join(root,'试听',j.file)]));
for (const e of manifest.entries) destinations.set(`alt-${e.id}`,path.join(root,'备选',`${e.id}_${path.basename(e.local,'.ogg')}.wav`));
const server = http.createServer(async (req,res) => {
  try {
    const url = new URL(req.url,'http://localhost:4178');
    const origin = req.headers.origin;
    if (origin && !['http://localhost:4178','http://127.0.0.1:4178'].includes(origin)) { res.writeHead(403).end(); return; }
    if (req.method === 'GET' && url.pathname === '/') { res.setHeader('Content-Type','text/html; charset=utf-8'); res.end(fs.readFileSync(path.join(__dirname,'convert.html'))); return; }
    if (req.method === 'GET' && url.pathname === '/audition') { res.setHeader('Content-Type','text/html; charset=utf-8'); res.end(fs.readFileSync(path.join(root,'试听入口.html'))); return; }
    if (req.method === 'GET' && url.pathname === '/config') { res.setHeader('Content-Type','application/json'); res.end(JSON.stringify({jobs,entries:manifest.entries})); return; }
    if (req.method === 'GET' && url.pathname.startsWith('/raw/')) {
      const e = manifest.entries.find(e => e.id === url.pathname.slice(5));
      if (!e) {res.writeHead(404).end();return;}
      res.setHeader('Content-Type','audio/ogg');res.end(fs.readFileSync(path.join(root,e.local)));return;
    }
    if (req.method === 'GET' && url.pathname.startsWith('/output/')) {
      const file = destinations.get(url.pathname.slice(8));
      if (!file || !fs.existsSync(file)) {res.writeHead(404).end();return;}
      res.setHeader('Content-Type','audio/wav');res.end(fs.readFileSync(file));return;
    }
    if (req.method === 'POST' && url.pathname.startsWith('/output/')) {
      const file = destinations.get(url.pathname.slice(8));
      if (!file) {res.writeHead(404).end();return;}
      const chunks=[];let size=0;
      for await (const chunk of req) { size+=chunk.length;if(size>300000) throw Error('WAV too large');chunks.push(chunk); }
      const data=Buffer.concat(chunks);
      if(data.length<46 || data.toString('ascii',0,4)!=='RIFF' || data.toString('ascii',8,12)!=='WAVE' || data.readUInt32LE(4)!==data.length-8 || data.readUInt16LE(20)!==1 || data.readUInt16LE(22)!==1 || data.readUInt32LE(24)!==48000 || data.readUInt16LE(34)!==16 || data.toString('ascii',36,40)!=='data' || data.readUInt32LE(40)!==data.length-44 || (data.length-44)%2) throw Error('Invalid PCM WAV');
      fs.writeFileSync(file,data,{flag:'wx'});res.end('saved');console.log(`Saved ${path.basename(file)} (${data.length} bytes)`);return;
    }
    res.writeHead(404).end();
  } catch(error) { console.error(error.message);res.writeHead(400).end(error.message); }
});
server.listen(4178,'127.0.0.1',()=>console.log('Audio review converter ready at http://localhost:4178'));
