'use strict';
// Reproducible seed from the explicitly approved MVP commit, never from a dirty working copy.
const fs=require('node:fs'),cp=require('node:child_process'),path=require('node:path');
const root=path.resolve(__dirname,'..');
const legacy=JSON.parse(cp.execFileSync('git',['show','b0244f9:Config/level.json'],{cwd:root,encoding:'utf8'}));
const clone=v=>JSON.parse(JSON.stringify(v));
const levels=[1,2,3].map(index=>{
  const data=clone(legacy);delete data.schemaVersion;delete data.version;
  for(const b of [...data.initialBlocks,...data.incomingRows.flatMap(r=>r.blocks)]) {b.height=1;b.rewardBalls=0;if(!b.target)b.health+=index-1;if(b.target)b.health=index===1?10:index===2?14:18;}
  if(index===1){const b=data.initialBlocks.find(b=>b.row===2&&b.column===1);b.health=8;b.rewardBalls=3;}
  if(index===2){data.initialBlocks.find(b=>b.row===3&&b.column===0).column=2;const b=data.initialBlocks.find(b=>b.row===2&&b.column===5);b.health=12;b.rewardBalls=5;}
  if(index===3){data.initialBlocks.find(b=>b.row===3&&b.column===0).column=1;data.initialBlocks.find(b=>b.row===3&&b.column===6).column=5;data.initialBlocks.find(b=>b.row===0&&b.column===3).column=2;}
  return {id:`stage-${index}`,name:`第 ${index} 关`,...data};
});
const growth={schemaVersion:2,version:'growth-1',levels};
const dir=path.join(root,'Tests/Fixtures');fs.mkdirSync(dir,{recursive:true});
fs.writeFileSync(path.join(dir,'legacy-level.json'),JSON.stringify(legacy,null,2)+'\n');
fs.writeFileSync(path.join(dir,'growth-level.json'),JSON.stringify(growth,null,2)+'\n');
if(process.argv.includes('--publish')) {
  fs.writeFileSync(path.join(root,'Config/level.json'),JSON.stringify(growth,null,2)+'\n');
  const defaults={settings:JSON.parse(fs.readFileSync(path.join(root,'Config/settings.json'),'utf8')),level:growth};
  fs.writeFileSync(path.join(root,'Tools/PlannerEditor/defaults.js'),`(function(root){const defaults=${JSON.stringify(defaults,null,2)};if(typeof module==='object'&&module.exports)module.exports=defaults;else root.PlannerDefaults=defaults;})(typeof globalThis!=='undefined'?globalThis:this);\n`);
}
console.log('Generated approved legacy and three-stage fixtures'+(process.argv.includes('--publish')?' and published defaults.':'.'));
