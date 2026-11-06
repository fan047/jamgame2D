'use strict';
const test=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const A=require('../audio-model.js'),defaults=require('../audio-defaults.js');
const root=path.resolve(__dirname,'../../..');
const wav=name=>fs.readFileSync(path.join(root,'Assets/Holedown/Audio',name+'.wav'));
test('defaults cover eight independent cues at 100% unmuted',()=>{
 const doc=A.defaults();assert.deepEqual(A.NAMES,['volley_launch','block_hit','block_break','reward_break','stage_clear','stage_fail','manual_recall','ui_confirm']);
 assert.equal(Object.keys(doc.cues).length,8);
 for(const name of A.NAMES){assert.deepEqual(doc.cues[name],{volumePercent:100,muted:false,wavBase64:null});}
 doc.cues.block_hit.volumePercent=0;assert.equal(doc.cues.block_break.volumePercent,100);
});
test('embedded preview clips stay byte-identical to the shipped WAV files',()=>{
 for(const name of A.NAMES){
  const embedded=Buffer.from(defaults[name],'base64'),file=wav(name);
  assert.equal(crypto.createHash('sha256').update(embedded).digest('hex'),crypto.createHash('sha256').update(file).digest('hex'),name);
  const info=A.inspectWave(new Uint8Array(embedded));assert.ok(info.seconds>0&&info.seconds<=3,name);
 }
});
test('volume boundaries and mute round-trip through build and parse',()=>{
 const doc=A.defaults();doc.version='audio-2';
 for(const value of [0,1,100,150]){doc.cues.stage_clear.volumePercent=value;const text=JSON.stringify(A.build(doc));assert.equal(A.parse(text).cues.stage_clear.volumePercent,value);}
 doc.cues.stage_clear.muted=true;assert.equal(A.parse(JSON.stringify(A.build(doc))).cues.stage_clear.muted,true);
 const sparse=A.parse('{"schemaVersion":1,"version":"audio-3","cues":{"block_break":{"volumePercent":120}}}');
 assert.equal(sparse.cues.block_break.volumePercent,120);assert.equal(sparse.cues.block_break.muted,false);assert.deepEqual(sparse.cues.reward_break,{volumePercent:100,muted:false,wavBase64:null});
});
test('replacement keeps that cue volume and mute and omits untouched clips',()=>{
 const doc=A.defaults();doc.cues.reward_break.volumePercent=140;doc.cues.reward_break.muted=true;doc.cues.reward_break.wavBase64=wav('block_break').toString('base64');
 const exported=A.build(doc);
 assert.deepEqual(Object.keys(exported.cues.reward_break),['volumePercent','muted','wavBase64']);
 assert.deepEqual(Object.keys(exported.cues.block_hit),['volumePercent','muted']);
 assert.equal(exported.cues.reward_break.volumePercent,140);assert.equal(exported.cues.reward_break.muted,true);
 assert.equal(A.parse(JSON.stringify(exported)).cues.reward_break.wavBase64,doc.cues.reward_break.wavBase64);
});
for(const [reason,json] of [
 ['unknown schema','{"schemaVersion":2,"version":"v","cues":{}}'],
 ['missing schema','{"version":"v","cues":{}}'],
 ['bad version','{"schemaVersion":1,"version":"bad version","cues":{}}'],
 ['unknown key','{"schemaVersion":1,"version":"v","cues":{},"extra":1}'],
 ['unknown cue','{"schemaVersion":1,"version":"v","cues":{"explosion":{}}}'],
 ['unknown cue key','{"schemaVersion":1,"version":"v","cues":{"block_hit":{"file":"x.wav"}}}'],
 ['volume too high','{"schemaVersion":1,"version":"v","cues":{"block_hit":{"volumePercent":151}}}'],
 ['negative volume','{"schemaVersion":1,"version":"v","cues":{"block_hit":{"volumePercent":-1}}}'],
 ['fractional volume','{"schemaVersion":1,"version":"v","cues":{"block_hit":{"volumePercent":1.5}}}'],
 ['string volume','{"schemaVersion":1,"version":"v","cues":{"block_hit":{"volumePercent":"100"}}}'],
 ['non-boolean mute','{"schemaVersion":1,"version":"v","cues":{"block_hit":{"muted":1}}}'],
 ['bad base64','{"schemaVersion":1,"version":"v","cues":{"block_hit":{"wavBase64":"!!!!"}}}'],
 ['truncated wav','{"schemaVersion":1,"version":"v","cues":{"block_hit":{"wavBase64":"UklGRg=="}}}'],
 ['broken json','{"schemaVersion":1,']])test(`rejects ${reason}`,()=>{assert.throws(()=>A.parse(json),/./);});
test('wave reader rejects stereo, wrong rate, empty and over three seconds',()=>{
 const base=wav('block_hit');
 assert.equal(A.inspectWave(new Uint8Array(base)).seconds,0.085);
 const stereo=Buffer.from(base);stereo.writeUInt16LE(2,22);assert.throws(()=>A.inspectWave(new Uint8Array(stereo)),/单声道/);
 const rate=Buffer.from(base);rate.writeUInt32LE(44100,24);assert.throws(()=>A.inspectWave(new Uint8Array(rate)),/48 kHz/);
 const bits=Buffer.from(base);bits.writeUInt16LE(8,34);assert.throws(()=>A.inspectWave(new Uint8Array(bits)),/16-bit/);
 const empty=Buffer.from(base);empty.writeUInt32LE(0,40);assert.throws(()=>A.inspectWave(new Uint8Array(empty)),/0–3 秒/);
 assert.throws(()=>A.inspectWave(new Uint8Array(base.subarray(0,30))),/RIFF/);
});
test('encoder emits canonical 48 kHz 16-bit mono PCM that the reader accepts',()=>{
 const samples=new Float32Array(48000);for(let i=0;i<samples.length;i++)samples[i]=Math.sin(i/20)*0.5;
 const bytes=A.encodeWave(samples);assert.equal(bytes.length,44+96000);
 assert.equal(String.fromCharCode(...bytes.subarray(0,4)),'RIFF');assert.equal(String.fromCharCode(...bytes.subarray(8,12)),'WAVE');
 const info=A.inspectWave(bytes);assert.equal(info.seconds,1);
 assert.throws(()=>A.encodeWave(new Float32Array(48001*3+1)),/0–3 秒/);
 assert.throws(()=>A.encodeWave(new Float32Array(0)),/0–3 秒/);
 assert.deepEqual(A.build(Object.assign(A.defaults(),{cues:Object.assign(A.defaults().cues,{block_hit:{volumePercent:100,muted:false,wavBase64:A.encodeBase64(bytes)}})} )).cues.block_hit.wavBase64,A.encodeBase64(bytes));
});
test('invalid version or volume cannot be exported',()=>{
 const doc=A.defaults();doc.version='bad version';assert.throws(()=>A.build(doc),/version/);
 const other=A.defaults();other.cues.block_hit.volumePercent=151;assert.throws(()=>A.build(other),/volumePercent/);
});
