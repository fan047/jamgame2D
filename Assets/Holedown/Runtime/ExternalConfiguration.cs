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
        public BoardState Board { get; }
        public string Label { get; }
        internal LoadedConfiguration(GameSettings settings, BoardState board, string label)
        { Settings=settings; Board=board; Label=label; }
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
                    return new LoadedConfiguration(settings,level.CreateBoard(),"Built-in");
                }
                if(!hasSettings || !hasLevel) throw new ArgumentException("Config requires both settings.json and level.json. Missing: "+(!hasSettings?settingsPath:levelPath));
                var s=Read(settingsPath); var l=Read(levelPath);
                string sv=Header(s,"settings.json"),lv=Header(l,"level.json");
                Keys(s,"settings.json","schemaVersion","version","ballsPerRound","ballRadius","ballSpeed","launchInterval","maxAimAngle","recallTimeout");
                Keys(l,"level.json","schemaVersion","version","initialBlocks","incomingRows");
                settings.ballsPerRound=Integer(s,"ballsPerRound","settings.json");
                settings.ballRadius=Number(s,"ballRadius","settings.json",0.5f);
                settings.ballSpeed=Number(s,"ballSpeed","settings.json");
                settings.launchInterval=Number(s,"launchInterval","settings.json");
                settings.maxAimAngle=Number(s,"maxAimAngle","settings.json",90f);
                settings.recallTimeout=Number(s,"recallTimeout","settings.json");
                if(settings.ballsPerRound<=0) throw new ArgumentException("settings.json.ballsPerRound: expected positive integer.");
                settings.Validate();
                var initial=Blocks(Array(l,"initialBlocks","level.json"),"level.json.initialBlocks");
                var rowsArray=Array(l,"incomingRows","level.json"); var rows=new List<RowDefinition>();
                for(int i=0;i<rowsArray.Count;i++)
                {
                    string path=$"level.json.incomingRows[{i}]";
                    var row=Object(rowsArray[i],path); Keys(row,path,"blocks");
                    rows.Add(new RowDefinition{blocks=Blocks(Array(row,"blocks",path),path+".blocks")});
                }
                var board=new BoardState(initial,rows);
                return new LoadedConfiguration(settings,board,$"S:{sv} / L:{lv}");
            }
            catch
            {
                LoadedConfiguration.Release(settings);
                throw;
            }
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
        static string Header(JObject obj,string path)
        {
            if(Integer(obj,"schemaVersion",path)!=1) throw new ArgumentException(path+".schemaVersion: only version 1 is supported.");
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
        static List<BlockDefinition> Blocks(JArray array,string path)
        {
            var blocks=new List<BlockDefinition>();
            for(int i=0;i<array.Count;i++)
            {
                string p=$"{path}[{i}]"; var obj=Object(array[i],p);
                Keys(obj,p,"column","row","width","health","target");
                if(obj["target"]?.Type!=JTokenType.Boolean) throw new ArgumentException(p+".target: expected true or false.");
                blocks.Add(new BlockDefinition(Integer(obj,"column",p),Integer(obj,"row",p),Integer(obj,"width",p),Integer(obj,"health",p),(bool)obj["target"]));
            }
            return blocks;
        }
    }
}
