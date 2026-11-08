'use strict';
const {test}=require('node:test'),assert=require('node:assert/strict');
const M=require('../config-model.js'),D=require('../defaults.js');
test('four custom rewards and values above ten validate and preserve on JSON roundtrip',()=>{
 const l=M.clone(D.level);const blocks=l.levels[0].initialBlocks;
 blocks.forEach(b=>b.rewardBalls=0);[3,5,8,11].forEach((v,i)=>blocks[i].rewardBalls=v);
 assert.deepEqual(M.validate(D.settings,l),[]);
 assert.deepEqual(M.parse(JSON.stringify(l)),l);
 for(const bad of [-1,1.5,2147483648]){const c=M.clone(l);c.levels[0].initialBlocks[0].rewardBalls=bad;assert.ok(M.validate(D.settings,c).some(e=>e.includes('rewardBalls')));}
 l.levels[0].initialBlocks[0].rewardBalls=2147483647;
 assert.ok(M.validate(D.settings,l).some(e=>e.includes('本关奖励合计')));
 const c=M.clone(D.level);c.levels[0].initialBlocks[0].rewardBalls=2147483640;
 assert.ok(M.validate(D.settings,c).some(e=>e.includes('基础球数')));
 c.levels[c.levels.length-1].initialBlocks[0].rewardBalls=11;assert.ok(M.validate(D.settings,c).some(e=>e.includes('最终关')));
});
