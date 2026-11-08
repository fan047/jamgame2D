'use strict';
const {test}=require('node:test'),assert=require('node:assert/strict');
const M=require('../config-model.js'),D=require('../defaults.js'),{EditorState}=require('../editor-state.js');
const fresh=()=>new EditorState(D.settings,D.level);
test('reward batches preview and round without mutation, apply once, undo atomically, change ordinary identity',()=>{
 const s=fresh();s.context.checked=[0,1];const before=JSON.stringify(s.data);
 const plan=s.batchPlan('set',11,'rewardBalls');assert.deepEqual(plan.map(p=>p.after),[11,11]);assert.equal(JSON.stringify(s.data),before);
 s.batch('set',11,'rewardBalls');assert.equal(s.past.length,1);assert.equal(s.summary().reward,2);assert.equal(M.stageReward(s.stage),25);assert.deepEqual(M.entryOptions(s.settings,s.level,1),[12,37]);
 s.batch('multiply',.5,'rewardBalls');assert.equal(s.blocks[0].rewardBalls,6);s.undo();assert.equal(s.blocks[0].rewardBalls,11);
 s.batch('add',-11,'rewardBalls');assert.equal(s.summary().ordinary,2);s.undo();assert.equal(s.summary().reward,2);
});
test('batch target conflict, final reward, overflow, zero health and invalid multiplier fail without partial writes',()=>{
 const s=fresh();s.context.checked=[0,1];const before=JSON.stringify(s.data);
 for(const args of [['set',0,'health'],['add',-99,'rewardBalls'],['multiply',-1,'health'],['multiply',Infinity,'health'],['set',2147483647,'rewardBalls']]){assert.throws(()=>s.batch(...args));assert.equal(JSON.stringify(s.data),before);}
 s.navigate(s.level.levels.length-1);s.context.checked=[0];assert.throws(()=>s.batch('set',1,'rewardBalls'),/最终关/);
 s.navigate(0,s.level.levels[0].incomingRows.length-1);s.context.checked=s.blocks.map((_,i)=>i);assert.throws(()=>s.batch('set',1,'rewardBalls'),/目标块/);
});
