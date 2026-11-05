(function(root,factory){const api=factory(typeof module==='object'&&module.exports?require('./config-model.js'):root.PlannerModel);if(typeof module==='object'&&module.exports)module.exports=api;else root.PlannerState=api;})(typeof globalThis!=='undefined'?globalThis:this,function(M){
  'use strict';
  const equal=(a,b)=>JSON.stringify(a)===JSON.stringify(b);
  const parts=path=>path.replace(/\[/g,'.').replace(/\]/g,'').split('.');
  const get=(obj,path)=>parts(path).reduce((o,k)=>o[k],obj);
  function put(obj,path,value){const p=parts(path),key=p.pop();get(obj,p.join('.'))[key]=value;}
  class EditorState {
    constructor(settings,level){this.replace(settings,level);}
    replace(settings,level){
      const errors=M.validate(settings,level);if(errors.length)throw new Error(errors.join('\n'));
      this.data={settings:M.clone(settings),level:M.normalize(level),context:{stage:0,layout:-1,selected:-1,checked:[]}};
      this.baseline={settings:M.clone(this.data.settings),level:M.clone(this.data.level)};this.exported={settings:null,level:null};this.past=[];this.future=[];this.drafts={};
    }
    get settings(){return this.data.settings;} get level(){return this.data.level;} get context(){return this.data.context;}
    get stage(){return this.level.levels[this.context.stage];}
    get blocks(){return this.context.layout<0?this.stage.initialBlocks:this.stage.incomingRows[this.context.layout].blocks;}
    get blockPath(){return `level.levels[${this.context.stage}].${this.context.layout<0?'initialBlocks':`incomingRows[${this.context.layout}].blocks`}`;}
    read(path){return get(this.data,path);}
    perform(label,mutator){
      const before=M.clone(this.data),next=M.clone(this.data);mutator(next);
      this.data=next;
      if(equal(before.settings,next.settings)&&equal(before.level,next.level))return false;
      this.past.push({data:before,label});if(this.past.length>50)this.past.shift();this.future=[];return true;
    }
    undo(){if(this.hasDrafts||!this.past.length)return false;const entry=this.past.pop();this.future.push({data:M.clone(this.data),label:entry.label});this.data=entry.data;return true;}
    redo(){if(this.hasDrafts||!this.future.length)return false;const entry=this.future.pop();this.past.push({data:M.clone(this.data),label:entry.label});this.data=entry.data;return true;}
    get hasDrafts(){return Object.keys(this.drafts).length>0;}
    get firstInvalid(){return Object.entries(this.drafts).find(([,d])=>d.error);}
    get hasUnexported(){return ['settings','level'].some(f=>this.status(f)==='有未导出修改')||this.hasDrafts;}
    status(file){if(this.exported[file]&&equal(this.data[file],this.exported[file]))return '已请求导出，请核对下载';if(equal(this.data[file],this.baseline[file]))return '未修改';return '有未导出修改';}
    markExport(file){if(this.hasDrafts||M.validate(this.settings,this.level).length)throw new Error('有错误或未提交输入，不能导出');this.exported[file]=M.clone(this.data[file]);}
    stageInput(path,raw){
      const value=path.endsWith('.version')||path.endsWith('.name')?raw:raw.trim()===''?NaN:Number(raw);
      const candidate=M.clone(this.data);put(candidate,path,value);
      const jsonPath=path.replace(/^settings/,'settings.json').replace(/^level/,'level.json');
      const error=M.diagnostics(candidate.settings,candidate.level).find(e=>e.path===jsonPath);
      this.drafts[path]={raw,value,error:error?.message||null};return this.drafts[path];
    }
    commitInput(path){const draft=this.drafts[path];if(!draft)return true;if(draft.error)return false;delete this.drafts[path];this.perform('数值修改',data=>put(data,path,draft.value));return true;}
    flush(){for(const p of Object.keys(this.drafts))if(!this.commitInput(p))return false;return true;}
    cancelInput(path){delete this.drafts[path];return this.read(path);}
    navigate(stage,layout=-1,selected=-1){if(!this.flush())return false;this.context.stage=stage;this.context.layout=layout;this.context.selected=selected;this.context.checked=[];return true;}
    stageOperation(label,mutator){if(!this.flush())throw new Error('请先修正或取消未提交输入');return this.perform(label,mutator);}
    uniqueStageId(prefix='stage'){
      const ids=new Set(this.level.levels.map(s=>s.id));let n=1;
      while(ids.has(`${prefix.slice(0,22)}-${n}`))n++;
      return `${prefix.slice(0,22)}-${n}`;
    }
    addStage(copy=false){
      if(!this.flush())throw new Error('请先修正或取消未提交输入');
      const id=this.uniqueStageId(copy?this.stage.id+'-copy':'stage');
      const stage=copy?M.clone(this.stage):{id,name:'新关卡',initialBlocks:[],incomingRows:[{blocks:[]}]};
      stage.id=id;if(copy)stage.name=(stage.name+' 副本').slice(0,40);
      this.stageOperation(copy?'复制关卡':'新增关卡',data=>{const index=data.context.stage+1;data.level.levels.splice(index,0,stage);data.context={stage:index,layout:-1,selected:-1,checked:[]};});
      return id;
    }
    deleteStage(){
      if(this.level.levels.length<=1)throw new Error('至少保留一关');
      this.stageOperation('删除关卡',data=>{data.level.levels.splice(data.context.stage,1);data.context={stage:Math.min(data.context.stage,data.level.levels.length-1),layout:-1,selected:-1,checked:[]};});
    }
    moveStage(offset){
      const index=this.context.stage,target=index+offset;if(target<0||target>=this.level.levels.length)return false;
      return this.stageOperation('重排关卡',data=>{const [stage]=data.level.levels.splice(index,1);data.level.levels.splice(target,0,stage);data.context.stage=target;});
    }
    applyBlock(block){
      const error=M.placementError(this.blocks,block,this.context.selected,this.context.layout>=0);if(error)throw new Error(error);
      if(block.rewardBalls>0&&this.context.stage===this.level.levels.length-1)throw new Error('最终关不能设置奖励块');
      if(block.target&&(this.context.layout<0||this.context.layout!==this.stage.incomingRows.length-1))throw new Error('目标必须位于最后一条补行');
      const path=this.blockPath,index=this.context.selected;
      this.perform(index<0?'新增块':'属性修改',data=>{const list=get(data,path);if(index<0){list.push(M.clone(block));data.context.selected=list.length-1;}else list[index]=M.clone(block);});
    }
    deleteBlock(){const index=this.context.selected;if(index<0)return;const path=this.blockPath;this.perform('删除块',data=>{get(data,path).splice(index,1);data.context.selected=-1;data.context.checked=[];});}
    addRow(){const index=this.context.stage;this.perform('新增补行',data=>{data.level.levels[index].incomingRows.push({blocks:[]});data.context.layout=data.level.levels[index].incomingRows.length-1;data.context.selected=-1;data.context.checked=[];});}
    removeRow(){if(this.context.layout<0||this.stage.incomingRows.length<=1)return;const si=this.context.stage,li=this.context.layout;this.perform('删除补行',data=>{const rows=data.level.levels[si].incomingRows;rows.splice(li,1);data.context.layout=Math.min(li,rows.length-1);data.context.selected=-1;data.context.checked=[];});}
    selectRole(role){this.context.checked=this.blocks.flatMap((b,i)=>(role==='target'?b.target:role==='reward'?!b.target&&b.rewardBalls>0:!b.target&&!b.rewardBalls)?[i]:[]);}
    selectOrdinary(){this.selectRole('ordinary');}
    summary(indices=this.context.checked){const result={ordinary:0,reward:0,target:0,total:indices.length};indices.forEach(i=>{const b=this.blocks[i];result[b.target?'target':b.rewardBalls?'reward':'ordinary']++;});return result;}
    batchPlan(mode,value,field='health'){
      if(!['health','rewardBalls'].includes(field)||!['set','add','multiply'].includes(mode))throw new Error('未知批量操作');
      if(mode==='multiply'?(!Number.isFinite(value)||value<0):!M.integer(value))throw new Error('设值／加减需要 Int32 整数；倍率需要非负有限数');
      const indices=[...new Set(this.context.checked)],path=this.blockPath;
      if(!indices.length)throw new Error('请先勾选方块');
      const plan=indices.map(i=>{
        const block=this.blocks[i];if(!block)throw new Error('选中块已失效');
        const result=mode==='set'?value:mode==='add'?block[field]+value:Math.round(block[field]*value);
        let reason=!M.integer(result)||(field==='health'?result<=0:result<0)?'结果超出合法整数范围':null;
        if(field==='rewardBalls'&&result>0&&(block.target||this.context.stage===this.level.levels.length-1))reason=block.target?'目标块不能有奖励':'最终关不能有奖励';
        if(reason){const error=new Error(`第 ${i+1} 块 ${field}：${reason}，整批未应用`);error.path=`${path}[${i}].${field}`;throw error;}
        return {index:i,before:block[field],after:result};
      });
      const candidate=M.clone(this.data);plan.forEach(p=>get(candidate,path)[p.index][field]=p.after);
      const overflow=M.diagnostics(candidate.settings,candidate.level).find(e=>e.message.includes('Int32 最大值'));
      if(overflow){const error=new Error(`${overflow.path}：${overflow.message}，整批未应用`);error.path=overflow.path;throw error;}
      return plan;
    }
    batch(mode,value,field='health'){
      if(!this.flush())throw new Error('请先修正或取消未提交输入');
      const plan=this.batchPlan(mode,value,field),path=this.blockPath;
      this.perform(field==='health'?'批量血量':'批量奖励',data=>plan.forEach(p=>get(data,path)[p.index][field]=p.after));
    }
  }
  function version(now=new Date()) {const pad=(n,w=2)=>String(n).padStart(w,'0');return `${now.getFullYear()}${pad(now.getMonth()+1)}${pad(now.getDate())}-${pad(now.getHours())}${pad(now.getMinutes())}${pad(now.getSeconds())}-${pad(now.getMilliseconds(),3)}`;}
  return {EditorState,version};
});
