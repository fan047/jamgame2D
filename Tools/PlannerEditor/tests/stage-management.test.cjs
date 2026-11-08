'use strict';
const {test}=require('node:test'),assert=require('node:assert/strict');
const M=require('../config-model.js'),D=require('../defaults.js'),{EditorState}=require('../editor-state.js');
const fresh=()=>new EditorState(D.settings,D.level);
test('copy to nine stages keeps full layouts and unique stable IDs through undo reorder delete',()=>{
 const s=fresh(),original=M.clone(s.stage),shipped=s.level.levels.length;while(s.level.levels.length<9)s.addStage(true);
 assert.equal(new Set(s.level.levels.map(v=>v.id)).size,9);
 assert.deepEqual(s.stage.initialBlocks,original.initialBlocks);assert.deepEqual(s.stage.incomingRows,original.incomingRows);
 s.navigate(4);const id=s.stage.id,index=s.context.stage,prev=M.clone(s.data);s.moveStage(-1);assert.equal(s.stage.id,id);assert.equal(s.context.stage,index-1);
 s.undo();assert.deepEqual(s.data,prev);s.redo();assert.equal(s.stage.id,id);
 s.deleteStage();assert.notEqual(s.stage.id,id);s.undo();assert.equal(s.stage.id,id);assert.equal(s.level.levels.length,9);
 s.navigate(0);const first=s.stage.id;s.moveStage(8);assert.equal(s.stage.id,first);assert.ok(M.validate(s.settings,s.level).some(e=>e.includes('最终关')));
});
test('new incomplete stage errors locate stage and can be removed; rename retains identity and is undoable',()=>{
 const s=fresh(),shipped=s.level.levels.length,id=s.stage.id,path='level.levels[0].name';s.stageInput(path,'新名字');s.commitInput(path);assert.equal(s.stage.id,id);assert.equal(s.stage.name,'新名字');s.undo();assert.equal(s.stage.name,D.level.levels[0].name);
 const next=s.addStage();assert.equal(s.stage.id,next);assert.equal(s.stage.initialBlocks.length,0);assert.ok(M.diagnostics(s.settings,s.level).some(e=>e.path==='level.json.levels[1]'));
 s.deleteStage();assert.equal(s.level.levels.length,shipped);
 s.stageInput('settings.ballSpeed','-2');const before=JSON.stringify(s.data);assert.throws(()=>s.addStage());assert.equal(JSON.stringify(s.data),before);
});
