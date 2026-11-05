(function(root, factory) {
  const api = factory();
  if (typeof module === 'object' && module.exports) module.exports = api;
  else root.PlannerModel = api;
})(typeof globalThis !== 'undefined' ? globalThis : this, function() {
  'use strict';
  const clone = value => JSON.parse(JSON.stringify(value));
  function parse(text) {
    const value=JSON.parse(text), stack=[], tokens=/"(?:\\.|[^"\\])*"|[{}\[\]]/g;let token;
    while((token=tokens.exec(text))!==null){
      const raw=token[0];
      if(raw==='{'||raw==='[')stack.push(raw==='{'?new Set():null);
      else if(raw==='}'||raw===']')stack.pop();
      else if(/^\s*:/.test(text.slice(tokens.lastIndex))){const keys=stack[stack.length-1],key=JSON.parse(raw);if(keys?.has(key))throw new Error(`重复 JSON 字段：${key}`);keys?.add(key);}
    }
    return value;
  }
  const object = value => value !== null && typeof value === 'object' && !Array.isArray(value);
  const integer = value => Number.isInteger(value) && value >= -2147483648 && value <= 2147483647;
  function diagnostics(settings, level) {
    const errors=[];
    const fail=(path,message)=>errors.push({path,message});
    const keys=(obj,allowed,path)=>Object.keys(obj).forEach(k=>{if(!allowed.includes(k))fail(`${path}.${k}`,'未知字段');});
    function header(obj,path,allowed,versions) {
      if(!object(obj)){fail(path,'需要 JSON 对象');return false;}
      keys(obj,allowed,path);
      if(!versions.includes(obj.schemaVersion))fail(`${path}.schemaVersion`,`仅支持 ${versions.join('/')} 版本`);
      if(typeof obj.version!=='string'||!/^[A-Za-z0-9._-]{1,32}$/.test(obj.version))fail(`${path}.version`,'使用 1–32 个英文字母、数字、点、下划线或连字符');
      return true;
    }
    if(header(settings,'settings.json',['schemaVersion','version','ballsPerRound','ballRadius','ballSpeed','launchInterval','maxAimAngle','recallTimeout'],[1])) {
      if(!integer(settings.ballsPerRound)||settings.ballsPerRound<=0)fail('settings.json.ballsPerRound','需要正 Int32 整数');
      for(const key of ['ballRadius','ballSpeed','launchInterval','maxAimAngle','recallTimeout']) {
        const v=settings[key],max=key==='ballRadius'?0.5:key==='maxAimAngle'?90:Infinity;
        if(typeof v!=='number'||!Number.isFinite(Math.fround(v))||Math.fround(v)<=0||Math.fround(v)>=max)fail(`settings.json.${key}`,`需要有限正数${max===Infinity?'':`，且小于 ${max}`}`);
      }
    }
    const v2=level?.schemaVersion===2;
    if(!header(level,'level.json',v2?['schemaVersion','version','levels']:['schemaVersion','version','initialBlocks','incomingRows'],[1,2]))return errors;
    const stages=v2?level.levels:[level];
    if(!Array.isArray(stages)||stages.length===0){fail('level.json.levels','至少需要一关');return errors;}
    const ids=new Set();let possible=0;
    stages.forEach((stage,si)=>{
      const path=v2?`level.json.levels[${si}]`:'level.json';
      if(!object(stage)){fail(path,'需要关卡对象');return;}
      if(v2) {
        keys(stage,['id','name','initialBlocks','incomingRows'],path);
        if(typeof stage.id!=='string'||!/^[A-Za-z0-9._-]{1,32}$/.test(stage.id)||ids.has(stage.id))fail(`${path}.id`,'关卡 id 必须合法且唯一');
        ids.add(stage.id);
        if(typeof stage.name!=='string'||!stage.name.trim()||stage.name.length>40)fail(`${path}.name`,'名称需要 1–40 个字符');
      }
      let targets=0,rewardSum=0;
      function blocks(list,p,incoming,last) {
        if(!Array.isArray(list)){fail(p,'需要方块数组');return;}
        const occupied=new Set();
        list.forEach((b,bi)=>{
          const q=`${p}[${bi}]`;
          if(!object(b)){fail(q,'需要方块对象');return;}
          keys(b,v2?['column','row','width','height','health','target','rewardBalls']:['column','row','width','health','target'],q);
          const c=integer(b.column)&&b.column>=0&&b.column<=7,r=integer(b.row)&&b.row>=0&&b.row<=7&&(!incoming||b.row===0),w=b.width===1||b.width===2,height=v2?b.height:1,h=height===1||height===2;
          if(!c)fail(`${q}.column`,'列号必须为 0–7');
          if(!r)fail(`${q}.row`,incoming?'补行 row 必须为 0':'行号必须为 0–7');
          if(!w)fail(`${q}.width`,'宽度只能为 1 或 2');
          if(c&&w&&b.column+b.width>8)fail(q,'超出右边界');
          if(!h)fail(`${q}.height`,'高度只能为 1 或 2');
          if(height===2&&(incoming||b.width!==1))fail(`${q}.height`,'竖块仅允许初始布局的 1×2；补行高度必须为 1');
          if(r&&h&&b.row+height>8)fail(`${q}.height`,'row + height 超出顶部边界');
          if(!integer(b.health)||b.health<=0)fail(`${q}.health`,'血量必须为正 Int32 整数');
          if(typeof b.target!=='boolean')fail(`${q}.target`,'必须为 true 或 false');
          const bonus=v2?b.rewardBalls:0;
          if(!integer(bonus)||bonus<0)fail(`${q}.rewardBalls`,'需要非负 Int32 整数');
          if(b.target===true){targets++;if(!incoming||!last)fail(`${q}.target`,'目标必须位于最后一条补行');if(bonus!==0)fail(q,'target 与奖励互斥');}
          if(integer(bonus)&&bonus>0){rewardSum+=bonus;if(si===stages.length-1)fail(`${q}.rewardBalls`,'最终关不能有奖励块');}
          if(c&&r&&w&&h&&b.column+b.width<=8&&b.row+height<=8)for(let y=b.row;y<b.row+height;y++)for(let x=b.column;x<b.column+b.width;x++){const cell=`${x},${y}`;if(occupied.has(cell))fail(q,`与其他块重叠，列 ${x}，行 ${y}`);occupied.add(cell);}
        });
      }
      blocks(stage.initialBlocks,`${path}.initialBlocks`,false,false);
      if(!Array.isArray(stage.incomingRows)||!stage.incomingRows.length)fail(`${path}.incomingRows`,'至少需要一条补行');
      else stage.incomingRows.forEach((row,i)=>{const p=`${path}.incomingRows[${i}]`;if(!object(row)){fail(p,'需要 blocks 对象');return;}keys(row,['blocks'],p);blocks(row.blocks,`${p}.blocks`,true,i===stage.incomingRows.length-1);});
      if(targets!==1)fail(path,`每关必须有且仅有一个目标，当前 ${targets} 个`);
      if(rewardSum>2147483647)fail(`${path}.rewardBalls`,'本关奖励合计超出 Int32 最大值');
      possible+=rewardSum;
    });
    if(integer(settings?.ballsPerRound)&&settings.ballsPerRound+possible>2147483647)fail('level.json.levels','基础球数加奖励超出 Int32 最大值');
    return errors;
  }
  const validate=(settings,level)=>diagnostics(settings,level).map(e=>`${e.path}：${e.message}`);
  function normalize(level) {
    if(level.schemaVersion===2)return clone(level);
    const b=value=>({...clone(value),height:1,rewardBalls:0});
    return {schemaVersion:2,version:level.version,levels:[{id:'stage-1',name:'旧单关',initialBlocks:level.initialBlocks.map(b),incomingRows:level.incomingRows.map(row=>({blocks:row.blocks.map(b)}))}]};
  }
  function stageReward(stage) { return [...stage.initialBlocks,...stage.incomingRows.flatMap(r=>r.blocks)].reduce((n,b)=>n+b.rewardBalls,0); }
  function entryOptions(settings,level,index) {
    const campaign=normalize(level);
    const maximum=settings.ballsPerRound+campaign.levels.slice(0,index).reduce((n,s)=>n+stageReward(s),0);
    return maximum===settings.ballsPerRound?[maximum]:[settings.ballsPerRound,maximum];
  }
  function placementError(blocks, block, selectedIndex, incoming) {
    const height=block.height??1;
    if (![1,2].includes(height)) return '高度必须为 1 或 2';
    if (height===2&&(incoming||block.width!==1)) return '竖块仅支持初始布局的 1×2，补行高度必须为 1';
    if (block.rewardBalls!==undefined && (!integer(block.rewardBalls)||block.rewardBalls<0)) return '奖励需要非负 Int32 整数';
    if (block.target && block.rewardBalls>0) return '目标和奖励互斥';
    if (!integer(block.column) || block.column<0 || block.column>7) return '列号必须为 0–7';
    if (!integer(block.row) || block.row<0 || block.row>7 || (incoming && block.row!==0)) return incoming ? '补行必须使用第 0 行' : '行号必须为 0–7';
    if (![1,2].includes(block.width) || block.column+block.width>8) return '宽度必须为 1 或 2，且不能越过第 7 列';
    if (!integer(block.health) || block.health<=0) return '血量必须为正整数';
    if (block.row+height>8) return '方块超出顶部边界';
    if (blocks.some((b,i) => i!==selectedIndex && b.row<block.row+height && b.row+(b.height??1)>block.row && b.column<block.column+block.width && b.column+b.width>block.column)) return '此位置与其他块重叠';
    return null;
  }
  return {clone, parse, integer, validate, diagnostics, normalize, stageReward, entryOptions, placementError};
});
