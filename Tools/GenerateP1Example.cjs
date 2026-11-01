'use strict';
// Keep the approved three-stage balance seed untouched; publish an opt-in P1 shape sample.
const fs=require('node:fs'),path=require('node:path');
const root=path.resolve(__dirname,'..');
const level=JSON.parse(fs.readFileSync(path.join(root,'Config/level.json'),'utf8'));
level.version='growth-p1-example';
level.levels[0].initialBlocks[0]={column:4,row:1,width:1,height:2,health:6,target:false,rewardBalls:0};
const directory=path.join(root,'Config/Examples');fs.mkdirSync(directory,{recursive:true});
fs.writeFileSync(path.join(directory,'level-vertical.json'),JSON.stringify(level,null,2)+'\n');
console.log('Generated opt-in vertical example; default Config/level.json is unchanged.');
