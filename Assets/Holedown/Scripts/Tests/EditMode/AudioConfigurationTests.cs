using System;
using System.IO;
using NUnit.Framework;
namespace Holedown.Tests
{
    public sealed class AudioConfigurationTests
    {
        [Test] public void MissingFileAndMissingCuesUseIndependentDefaults()
        {
            using(var missing=AudioConfiguration.Read("Library/Locus/tmp/nonexistent-audio-"+Guid.NewGuid().ToString("N")))Assert.AreEqual("内置",missing.Version);
            using var audio=AudioConfiguration.Parse("{\"schemaVersion\":1,\"version\":\"audio-1\",\"cues\":{}}");
            Assert.AreEqual(8,audio.Cues.Length);foreach(var cue in audio.Cues){Assert.AreEqual(100,cue.VolumePercent);Assert.IsFalse(cue.Muted);Assert.IsNull(cue.Replacement);}
            Assert.AreNotSame(audio.Cues[0],audio.Cues[1]);
        }
        [TestCase(0)][TestCase(100)][TestCase(150)] public void VolumeBoundariesAndMuteAreAccepted(int volume)
        {
            using var audio=AudioConfiguration.Parse("{\"schemaVersion\":1,\"version\":\"audio-2\",\"cues\":{\"block_break\":{\"volumePercent\":"+volume+",\"muted\":true}}}");
            Assert.AreEqual(volume,audio.Cues[2].VolumePercent);Assert.IsTrue(audio.Cues[2].Muted);
        }
        [TestCase("{\"schemaVersion\":2,\"version\":\"v\",\"cues\":{}}")]
        [TestCase("{\"schemaVersion\":1,\"version\":\"invalid version\",\"cues\":{}}")]
        [TestCase("{\"schemaVersion\":1,\"version\":\"v\",\"cues\":{\"unknown\":{}}}")]
        [TestCase("{\"schemaVersion\":1,\"version\":\"v\",\"cues\":{\"block_hit\":{\"volumePercent\":151}}}")]
        [TestCase("{\"schemaVersion\":1,\"version\":\"v\",\"cues\":{\"block_hit\":{\"volumePercent\":-1}}}")]
        [TestCase("{\"schemaVersion\":1,\"version\":\"v\",\"cues\":{\"block_hit\":{\"volumePercent\":1.5}}}")]
        [TestCase("{\"schemaVersion\":1,\"version\":\"v\",\"cues\":{\"block_hit\":{\"muted\":1}}}")]
        [TestCase("{\"schemaVersion\":1,\"version\":\"v\",\"cues\":{\"block_hit\":{\"file\":\"x.wav\"}}}")]
        [TestCase("{\"schemaVersion\":1,\"version\":\"v\",\"cues\":{\"block_hit\":{\"wavBase64\":\"invalid\"}}}")]
        [TestCase("{\"schemaVersion\":1,\"version\":\"v\",\"version\":\"again\",\"cues\":{}}")]
        [TestCase("broken json")]
        public void InvalidConfigurationsFailWithoutPartialSnapshot(string json){Assert.Throws<ArgumentException>(()=>AudioConfiguration.Parse(json));}
        [Test] public void EmbeddedWaveIsDecodedAndOwnedBySnapshot()
        {
            var bytes=File.ReadAllBytes("Assets/Holedown/Audio/block_hit.wav");var samples=AudioConfiguration.DecodeWave(bytes);Assert.AreEqual(4080,samples.Length);
            var audio=AudioConfiguration.Parse("{\"schemaVersion\":1,\"version\":\"v\",\"cues\":{\"block_hit\":{\"wavBase64\":\""+Convert.ToBase64String(bytes)+"\"}}}");
            var clip=audio.Cues[1].Replacement;Assert.AreEqual(48000,clip.frequency);Assert.AreEqual(1,clip.channels);audio.Dispose();Assert.IsFalse(clip);
        }
        [Test] public void WaveRejectsTruncationStereoWrongRateEmptyAndOverThreeSeconds()
        {
            var bytes=File.ReadAllBytes("Assets/Holedown/Audio/block_hit.wav");
            Assert.Throws<ArgumentException>(()=>AudioConfiguration.DecodeWave(new byte[44]));
            var truncated=(byte[])bytes.Clone();truncated[4]=0;Assert.Throws<ArgumentException>(()=>AudioConfiguration.DecodeWave(truncated));
            var stereo=(byte[])bytes.Clone();stereo[22]=2;Assert.Throws<ArgumentException>(()=>AudioConfiguration.DecodeWave(stereo));
            var rate=(byte[])bytes.Clone();rate[24]=0;Assert.Throws<ArgumentException>(()=>AudioConfiguration.DecodeWave(rate));
            var large=new byte[44+288002];Array.Copy(bytes,large,44);BitConverter.GetBytes(large.Length-8).CopyTo(large,4);BitConverter.GetBytes(288002).CopyTo(large,40);Assert.Throws<ArgumentException>(()=>AudioConfiguration.DecodeWave(large));
        }
    }
}
