using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Holedown
{
    // Owns an isolated settings object. Never modifies the Inspector assets.
    public sealed class LoadedConfiguration : IDisposable
    {
        public GameSettings Settings { get; }
        public CampaignDefinition Campaign { get; }
        // Compatibility entry point: each access creates a fresh first-stage board.
        public BoardState Board => CreateBoard(0);
        public BoardState CreateBoard(int stageIndex) => Campaign.CreateBoard(stageIndex);
        public string Label { get; }
        internal LoadedConfiguration(GameSettings settings, CampaignDefinition campaign, string label)
        { Settings=settings; Campaign=campaign; Label=label; }
        public void Dispose() { Release(Settings); }
        internal static void Release(GameSettings settings)
        {
            if(!settings) return;
            if(Application.isPlaying) UnityEngine.Object.Destroy(settings);
            else UnityEngine.Object.DestroyImmediate(settings);
        }
    }

    public static class ExternalConfiguration
    {
        public static LoadedConfiguration Load(string directory, GameSettings defaults, LevelDefinition level)
        {
            string settingsPath=directory==null?null:Path.Combine(directory,"settings.json"), levelPath=directory==null?null:Path.Combine(directory,"level.json");
            bool hasSettings=File.Exists(settingsPath),hasLevel=File.Exists(levelPath);
            var settings=UnityEngine.Object.Instantiate(defaults);
            try
            {
                if(!hasSettings && !hasLevel)
                {
                    settings.Validate();
                    return new LoadedConfiguration(settings,new CampaignDefinition(new[]{new StageDefinition("stage-1","Built-in",level.initialBlocks,level.incomingRows)},settings.ballsPerRound),"Built-in");
                }
                if(!hasSettings || !hasLevel) throw new ArgumentException("Config requires both settings.json and level.json. Missing: "+(!hasSettings?settingsPath:levelPath));
                var s=Read(settingsPath); var l=Read(levelPath);
                string sv=Header(s,"settings.json",1),lv=Header(l,"level.json",1,2);
                Keys(s,"settings.json","schemaVersion","version","ballsPerRound","ballRadius","ballSpeed","launchInterval","maxAimAngle","recallTimeout");
                settings.ballsPerRound=Integer(s,"ballsPerRound","settings.json");
                settings.ballRadius=Number(s,"ballRadius","settings.json",0.5f);
                settings.ballSpeed=Number(s,"ballSpeed","settings.json");
                settings.launchInterval=Number(s,"launchInterval","settings.json");
                settings.maxAimAngle=Number(s,"maxAimAngle","settings.json",90f);
                settings.recallTimeout=Number(s,"recallTimeout","settings.json");
                if(settings.ballsPerRound<=0) throw new ArgumentException("settings.json.ballsPerRound: expected positive integer.");
                settings.Validate();
                var stages=new List<StageDefinition>();
                if(Integer(l,"schemaVersion","level.json")==1)
                {
                    Keys(l,"level.json","schemaVersion","version","initialBlocks","incomingRows");
                    stages.Add(ParseStage(l,"level.json",true));
                }
                else
                {
                    Keys(l,"level.json","schemaVersion","version","levels");
                    var levels=Array(l,"levels","level.json");
                    for(int i=0;i<levels.Count;i++)
                    {
                        string path=$"level.json.levels[{i}]";var stage=Object(levels[i],path);
                        Keys(stage,path,"id","name","initialBlocks","incomingRows");
                        stages.Add(ParseStage(stage,path,false));
                    }
                }
                return new LoadedConfiguration(settings,new CampaignDefinition(stages,settings.ballsPerRound),$"S:{sv} / L:{lv}");
            }
            catch
            {
                LoadedConfiguration.Release(settings);
                throw;
            }
        }

        static StageDefinition ParseStage(JObject obj,string path,bool legacy)
        {
            var initial=Blocks(Array(obj,"initialBlocks",path),path+".initialBlocks",legacy);
            var rowsArray=Array(obj,"incomingRows",path);var rows=new List<RowDefinition>();
            for(int i=0;i<rowsArray.Count;i++)
            {
                string p=$"{path}.incomingRows[{i}]";var row=Object(rowsArray[i],p);Keys(row,p,"blocks");
                rows.Add(new RowDefinition{blocks=Blocks(Array(row,"blocks",p),p+".blocks",legacy)});
            }
            string id="stage-1",name="Legacy Stage";
            if(!legacy)
            {
                if(obj["id"]?.Type!=JTokenType.String)throw new ArgumentException(path+".id: expected string.");
                if(obj["name"]?.Type!=JTokenType.String)throw new ArgumentException(path+".name: expected string.");
                id=(string)obj["id"];name=(string)obj["name"];
            }
            try {return new StageDefinition(id,name,initial,rows);}
            catch(ArgumentException ex) {throw new ArgumentException(ex.Message.StartsWith("level.json")?ex.Message.Replace("level.json",path):path+"."+ex.Message,ex);}
        }
        static JObject Read(string path)
        {
            try
            {
                return JObject.Parse(File.ReadAllText(path),new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});
            }
            catch(Exception ex) when(ex is JsonException || ex is IOException || ex is UnauthorizedAccessException)
            { throw new ArgumentException(Path.GetFileName(path)+": "+ex.Message,ex); }
        }
        static string Header(JObject obj,string path,params int[] supported)
        {
            if(!supported.Contains(Integer(obj,"schemaVersion",path))) throw new ArgumentException(path+".schemaVersion: supported versions: "+string.Join(",",supported)+".");
            if(obj["version"]?.Type!=JTokenType.String || !Regex.IsMatch((string)obj["version"],"^[A-Za-z0-9._-]{1,32}$"))
                throw new ArgumentException(path+".version: use 1-32 letters, digits, dots, underscores or hyphens.");
            return (string)obj["version"];
        }
        static void Keys(JObject obj,string path,params string[] allowed)
        {
            foreach(var property in obj.Properties())
                if(!allowed.Contains(property.Name)) throw new ArgumentException(path+"."+property.Name+": unknown field.");
        }
        static JObject Object(JToken token,string path)
            => token as JObject ?? throw new ArgumentException(path+": expected object.");
        static JArray Array(JObject obj,string key,string path)
            => obj[key] as JArray ?? throw new ArgumentException(path+"."+key+": expected array.");
        static int Integer(JObject obj,string key,string path)
        {
            var token=obj[key];
            if(token?.Type!=JTokenType.Integer || !int.TryParse(token.ToString(),out int value))
                throw new ArgumentException(path+"."+key+": expected 32-bit integer.");
            return value;
        }
        static float Number(JObject obj,string key,string path,float exclusiveMaximum=float.PositiveInfinity)
        {
            var token=obj[key];
            if(token==null || (token.Type!=JTokenType.Float && token.Type!=JTokenType.Integer)) throw new ArgumentException(path+"."+key+": expected number.");
            double value=token.Value<double>(); float result=(float)value;
            if(!float.IsFinite(result) || result<=0 || result>=exclusiveMaximum)
                throw new ArgumentException(path+"."+key+": expected a positive finite number"+(float.IsFinite(exclusiveMaximum)?" below "+exclusiveMaximum:"")+".");
            return result;
        }
        static List<BlockDefinition> Blocks(JArray array,string path,bool legacy)
        {
            var blocks=new List<BlockDefinition>();
            for(int i=0;i<array.Count;i++)
            {
                string p=$"{path}[{i}]"; var obj=Object(array[i],p);
                Keys(obj,p,legacy?new[]{"column","row","width","health","target"}:new[]{"column","row","width","height","health","target","rewardBalls"});
                if(obj["target"]?.Type!=JTokenType.Boolean) throw new ArgumentException(p+".target: expected true or false.");
                blocks.Add(new BlockDefinition(Integer(obj,"column",p),Integer(obj,"row",p),Integer(obj,"width",p),Integer(obj,"health",p),(bool)obj["target"],legacy?0:Integer(obj,"rewardBalls",p),legacy?1:Integer(obj,"height",p)));
            }
            return blocks;
        }
    }
}
