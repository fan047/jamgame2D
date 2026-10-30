(function(root) {
  const block = (column,row,width,health,target=false) => ({column,row,width,health,target});
  const defaults = {
    settings: {schemaVersion:1,version:'demo-1',ballsPerRound:12,ballRadius:0.12,ballSpeed:12,launchInterval:0.08,maxAimAngle:75,recallTimeout:30},
    level: {
      schemaVersion:1,version:'demo-1',
      initialBlocks:[block(0,0,2,5),block(3,0,2,4),block(6,0,2,5),block(0,1,1,3),block(2,1,1,3),block(5,1,1,3),block(7,1,1,3),block(1,2,2,4),block(5,2,2,4),block(0,3,2,3),block(6,3,2,3)],
      incomingRows:[
        {blocks:[block(1,0,1,3),block(3,0,2,4),block(6,0,1,3)]},
        {blocks:[block(0,0,2,4),block(6,0,2,4)]},
        {blocks:[block(1,0,2,4),block(5,0,2,4)]},
        {blocks:[block(3,0,2,10,true)]}
      ]
    }
  };
  if(typeof module==='object' && module.exports) module.exports=defaults;
  else root.PlannerDefaults=defaults;
})(typeof globalThis!=='undefined'?globalThis:this);
