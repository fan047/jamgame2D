'use strict';
const {test}=require('node:test');
const assert=require('node:assert/strict');
const fs=require('node:fs');
const path=require('node:path');
const M=require('../config-model.js');
const defaults=require('../defaults.js');
const root=path.resolve(__dirname,'../../..');
test('embedded editor defaults match shipped source configs',()=>{
  assert.deepEqual(defaults.settings,JSON.parse(fs.readFileSync(path.join(root,'Config/settings.json'),'utf8')));
  assert.deepEqual(defaults.level,JSON.parse(fs.readFileSync(path.join(root,'Config/level.json'),'utf8')));
  assert.deepEqual(M.validate(defaults.settings,defaults.level),[]);
});
for(const [key,value] of [['ballsPerRound',1.5],['ballsPerRound',0],['ballRadius',0.5],['ballSpeed',NaN],['ballSpeed',1e-100],['ballSpeed',1e100],['maxAimAngle',90],['version','bad version'],['schemaVersion',2]]) {
  test(`reject invalid settings ${key}=${value}`,()=>{
    const s=M.clone(defaults.settings);s[key]=value;
    assert.ok(M.validate(s,defaults.level).some(e=>e.includes(key)));
  });
}
test('reject missing and unknown fields',()=>{
  const s=M.clone(defaults.settings);delete s.ballSpeed;s.ballSpead=12;
  const errors=M.validate(s,defaults.level);assert.ok(errors.some(e=>e.includes('ballSpeed')));assert.ok(errors.some(e=>e.includes('ballSpead')));
});
test('level errors identify block and overlap',()=>{
  const l=M.clone(defaults.level);l.initialBlocks[1].column=1;
  assert.ok(M.validate(defaults.settings,l).some(e=>e.includes('initialBlocks[1]')&&e.includes('重叠')));
});
test('target uniqueness and last incoming row enforced',()=>{
  const l=M.clone(defaults.level);l.initialBlocks[0].target=true;
  assert.ok(M.validate(defaults.settings,l).length>=2);
  l.initialBlocks[0].target=false;l.incomingRows.push({blocks:[]});
  assert.ok(M.validate(defaults.settings,l).some(e=>e.includes('最后一条')));
});
test('malformed levels fail without crashing',()=>{
  for(const l of [null,{}, {...defaults.level,initialBlocks:null},{...defaults.level,incomingRows:[null]}]) assert.ok(M.validate(defaults.settings,l).length>0);
});
test('placement rejects overlap and bounds while permitting editing self',()=>{
  const list=M.clone(defaults.level.initialBlocks),b={...list[0]};
  assert.equal(M.placementError(list,b,0,false),null);
  assert.ok(M.placementError(list,b,-1,false));
  assert.ok(M.placementError([], {...b,column:7,width:2},-1,false));
  assert.ok(M.placementError([], {...b,row:1},-1,true));
});
test('export and re-import round-trip retains exact values',()=>{
  const edited=M.clone(defaults);edited.settings.ballsPerRound=7;edited.level.initialBlocks[0].health=9;
  const roundtrip=JSON.parse(JSON.stringify(edited));assert.deepEqual(roundtrip,edited);assert.deepEqual(M.validate(roundtrip.settings,roundtrip.level),[]);
});
