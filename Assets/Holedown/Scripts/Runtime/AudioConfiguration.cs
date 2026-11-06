using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Holedown
{
    public enum AudioCue { VolleyLaunch, BlockHit, BlockBreak, RewardBreak, StageClear, StageFail, ManualRecall, UiConfirm }
    public sealed class CueSettings
    {
        public int VolumePercent { get; internal set; }=100;
        public bool Muted { get; internal set; }
        public AudioClip Replacement { get; internal set; }
    }
    // Owns only runtime-created replacement clips; scene defaults are never owned here.
    public sealed class AudioSnapshot : IDisposable
    {
        public string Version { get; internal set; }="内置";
        public CueSettings[] Cues { get; }=Enumerable.Range(0,8).Select(_=>new CueSettings()).ToArray();
        public void Dispose(){foreach(var cue in Cues)if(cue.Replacement){if(Application.isPlaying)UnityEngine.Object.Destroy(cue.Replacement);else UnityEngine.Object.DestroyImmediate(cue.Replacement);cue.Replacement=null;}}
    }
    public static class AudioConfiguration
    {
        public static readonly string[] Names={"volley_launch","block_hit","block_break","reward_break","stage_clear","stage_fail","manual_recall","ui_confirm"};
        public const int MaxFileBytes=4*1024*1024;
        public static AudioSnapshot Read(string directory)
        {
            string file=Path.Combine(directory,"audio.json");if(!File.Exists(file))return new AudioSnapshot();
            if(new FileInfo(file).Length>MaxFileBytes)throw new ArgumentException("audio.json：文件过大（最多 4 MiB）。");
            return Parse(File.ReadAllText(file,Encoding.UTF8));
        }
        public static AudioSnapshot Parse(string json)
        {
            var candidate=new AudioSnapshot();
            try
            {
                if(Encoding.UTF8.GetByteCount(json)>MaxFileBytes)throw new ArgumentException("audio.json：文件过大。");
                JObject root;
                using(var reader=new JsonTextReader(new StringReader(json)){DateParseHandling=DateParseHandling.None,MaxDepth=12})
                {
                    root=JObject.Load(reader,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});
                    if(reader.Read())throw new ArgumentException("audio.json：根对象后有多余内容。");
                }
                Keys(root,"audio.json","schemaVersion","version","cues");
                if(root["schemaVersion"]?.Type!=JTokenType.Integer || (long)root["schemaVersion"]!=1)throw new ArgumentException("audio.json.schemaVersion：仅支持整数 1。");
                if(root["version"]?.Type!=JTokenType.String || !Regex.IsMatch((string)root["version"],@"\A[A-Za-z0-9_.-]{1,32}\z"))throw new ArgumentException("audio.json.version：需为 1–32 位字母、数字、点、下划线或连字符。");
                candidate.Version=(string)root["version"];
                var cues=root["cues"] as JObject ?? throw new ArgumentException("audio.json.cues：需为对象。");
                Keys(cues,"audio.json.cues",Names);
                foreach(var entry in cues.Properties())
                {
                    int index=Array.IndexOf(Names,entry.Name);string path="audio.json.cues."+entry.Name;
                    var value=entry.Value as JObject ?? throw new ArgumentException(path+"：需为对象。");
                    Keys(value,path,"volumePercent","muted","wavBase64");var cue=candidate.Cues[index];
                    if(value.TryGetValue("volumePercent",out var volume))
                    {
                        if(volume.Type!=JTokenType.Integer || !long.TryParse(volume.ToString(),out long percent) || percent<0 || percent>150)throw new ArgumentException(path+".volumePercent：需为 0–150 的整数。");
                        cue.VolumePercent=(int)percent;
                    }
                    if(value.TryGetValue("muted",out var muted)){if(muted.Type!=JTokenType.Boolean)throw new ArgumentException(path+".muted：需为布尔值。");cue.Muted=(bool)muted;}
                    if(value.TryGetValue("wavBase64",out var wav))
                    {
                        if(wav.Type!=JTokenType.String || ((string)wav).Length>700000)throw new ArgumentException(path+".wavBase64：需为有效 WAV 的 Base64。");
                        try
                        {
                            var samples=DecodeWave(Convert.FromBase64String((string)wav));
                            var clip=AudioClip.Create(entry.Name+" (audio.json)",samples.Length,1,48000,false);cue.Replacement=clip;
                            if(!clip.SetData(samples,0))throw new ArgumentException("无法创建音频片段。");
                        }
                        catch(Exception error){throw new ArgumentException(path+".wavBase64："+error.Message,error);}
                    }
                }
                return candidate;
            }
            catch(Exception error)
            {
                candidate.Dispose();
                if(error is JsonReaderException reader)
                {
                    string where=reader.LineNumber>0?$"第 {reader.LineNumber+1} 行，第 {reader.LinePosition+1} 列" : "请检查逗号、引号与重复字段";
                    throw new ArgumentException("audio.json：JSON 格式错误（"+where+"）。",error);
                }
                if(error is JsonException)throw new ArgumentException("audio.json：JSON 格式错误，请检查逗号、引号与重复字段。",error);
                throw;
            }
        }
        static void Keys(JObject value,string path,params string[] allowed){foreach(var property in value.Properties())if(!allowed.Contains(property.Name))throw new ArgumentException(path+"."+property.Name+"：未知字段或音效槽位。");}
        public static float[] DecodeWave(byte[] bytes)
        {
            if(bytes.Length<44 || bytes.Length>512*1024)throw new ArgumentException("WAV 大小无效。");
            using var stream=new MemoryStream(bytes,false);using var reader=new BinaryReader(stream,Encoding.ASCII);
            string Tag()=>Encoding.ASCII.GetString(reader.ReadBytes(4));
            if(Tag()!="RIFF" || reader.ReadUInt32()!=bytes.Length-8 || Tag()!="WAVE")throw new ArgumentException("不是完整的 RIFF/WAVE 文件。");
            bool format=false;int dataOffset=-1,dataLength=0;
            while(stream.Position<stream.Length)
            {
                if(stream.Length-stream.Position<8)throw new ArgumentException("WAV 分块头不完整。");
                string tag=Tag();uint length=reader.ReadUInt32();long end=stream.Position+length;
                if(end>stream.Length)throw new ArgumentException("WAV 分块数据被截断。");
                if(tag=="fmt ")
                {
                    if(format || length<16)throw new ArgumentException("WAV 格式块无效。");
                    if(reader.ReadUInt16()!=1 || reader.ReadUInt16()!=1 || reader.ReadUInt32()!=48000 || reader.ReadUInt32()!=96000 || reader.ReadUInt16()!=2 || reader.ReadUInt16()!=16)throw new ArgumentException("请用工作台转换为 48 kHz、16-bit、单声道 PCM WAV。");
                    format=true;
                }
                else if(tag=="data")
                {
                    if(dataOffset>=0 || length==0 || length%2!=0 || length>48000*3*2)throw new ArgumentException("WAV 需为 0–3 秒的非空音频。");
                    dataOffset=(int)stream.Position;dataLength=(int)length;
                }
                stream.Position=end+(length%2);
                if(stream.Position>stream.Length)throw new ArgumentException("WAV 填充字节缺失。");
            }
            if(!format || dataOffset<0)throw new ArgumentException("WAV 缺少格式或音频数据。");
            var samples=new float[dataLength/2];stream.Position=dataOffset;
            for(int i=0;i<samples.Length;i++)samples[i]=reader.ReadInt16()/32768f;
            return samples;
        }
    }
}
