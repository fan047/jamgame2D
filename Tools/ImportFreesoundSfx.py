"""Replace the two placeholder block SFX with user-chosen CC0 Freesound sounds.

Sources (both Creative Commons 0):
  break  https://freesound.org/people/discofield/sounds/711657/  "Stone crash"
  hit    https://freesound.org/people/kasparsj/sounds/513693/    "impact-stone.wav"

Freesound serves original WAVs only after login, so this tool downloads the
public high-quality preview MP3, decodes it offline with headless Chrome
(WebAudio, 48 kHz mono) and writes 48 kHz/16-bit mono PCM WAV over the two
existing placeholder files. File names and .meta GUIDs stay unchanged, so the
scene's AudioClip references and Unity import settings are preserved.

Nothing else is touched: no C#, scene, .asset, .meta or other audio file.
Run: python Tools/ImportFreesoundSfx.py [--check-only]
"""
from pathlib import Path
import argparse
import base64
import hashlib
import json
import re
import subprocess
import urllib.request
import wave
import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
AUDIO = ROOT / 'Assets/Holedown/Audio'
SCRATCH = ROOT / 'Library/Locus/tmp/sfx-m3'
DOWNLOADS = SCRATCH / 'downloads'
RATE = 48000

SOURCES = {
    'stone-crash': {
        'page': 'https://freesound.org/people/discofield/sounds/711657/',
        'author': 'discofield', 'title': 'Stone crash', 'license': 'CC0 1.0',
        'file': 'stone-crash-hq.mp3',
        'url': 'https://cdn.freesound.org/previews/711/711657_882799-hq.mp3',
        'sha256': '91cca28a4c4a9eae31cdad60b8e73414b2b3fe261a9779aa719060a2c57a1503',
        'original': '711657__discofield__stone-crash.wav (44.1 kHz 16-bit stereo, 2.500 s)',
    },
    'impact-stone': {
        'page': 'https://freesound.org/people/kasparsj/sounds/513693/',
        'author': 'kasparsj', 'title': 'impact-stone.wav', 'license': 'CC0 1.0',
        'file': 'impact-stone-hq.mp3',
        'url': 'https://cdn.freesound.org/previews/513/513693_914915-hq.mp3',
        'sha256': '07a682838d494480d10d3f8dcafb44b841228c42998a74cc02af865d7840c4a6',
        'original': '513693__kasparsj__impact-stone.wav (44.1 kHz 24-bit mono, 1.002 s)',
    },
}

# Output target: keep existing runtime mix balance (gains 0.3 hit / 0.42 break in
# FeedbackPlayer) instead of making the new clips louder than the old ones.
# Loudness is matched to the measured placeholders (hit RMS -24.7 dBFS, break RMS
# -23.0 dBFS, both at their FeedbackPlayer gains 0.3 / 0.42) instead of matching a
# peak: a 40 ms stone transient would read much quieter at equal peak.
TARGETS = {
    'block_hit.wav': {'source': 'impact-stone', 'max_seconds': 0.15, 'rms_dbfs': -24.7,
                      'peak_cap': 0.60, 'tail_db': -42, 'fade_out': 0.030,
                      'note': 'single short stone tick, one impact, truncated after decay'},
    'block_break.wav': {'source': 'stone-crash', 'max_seconds': 0.70, 'rms_dbfs': -23.0,
                        'peak_cap': 0.85, 'tail_db': -40, 'fade_out': 0.070,
                        'note': 'layered stone crash, truncated after decay'},
}

# Placeholder hashes as committed before this replacement. Overwriting is only
# allowed while the on-disk file still matches, so later manual edits survive.
PREVIOUS = {
    'block_hit.wav': '736f6046afd91994bc7bd01c78ff7ecdf6ff3090653fc7fe7568f0961873712b',
    'block_break.wav': '82cc1dc9d968f069a7a4376540fef1815f78e90b122e6e15c0d482748cc042f5',
}
UNTOUCHED = ['manual_recall.wav', 'reward_break.wav', 'stage_clear.wav', 'stage_fail.wav',
             'ui_confirm.wav', 'volley_launch.wav']


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def fetch():
    DOWNLOADS.mkdir(parents=True, exist_ok=True)
    for name, spec in SOURCES.items():
        path = DOWNLOADS / spec['file']
        if not path.exists() or sha(path) != spec['sha256']:
            request = urllib.request.Request(spec['url'], headers={'User-Agent': 'Mozilla/5.0'})
            with urllib.request.urlopen(request, timeout=120) as response:
                data = response.read()
            assert data[:3] != b'<!d', 'Received HTML instead of audio'
            path.write_bytes(data)
        actual = sha(path)
        assert actual == spec['sha256'], f'{spec["file"]} hash {actual} != recorded {spec["sha256"]}'


def chrome():
    for candidate in ('C:/Program Files/Google/Chrome/Application/chrome.exe',
                      'C:/Program Files (x86)/Google/Chrome/Application/chrome.exe',
                      'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',
                      'C:/Program Files/Microsoft/Edge/Application/msedge.exe'):
        if Path(candidate).exists():
            return candidate
    raise RuntimeError('No Chrome/Edge found for offline WebAudio decoding')


def decode():
    """Decode each preview MP3 to a full-length 48 kHz mono PCM WAV in scratch.

    Headless Chrome dumps the DOM after the page finishes decoding. One browser
    run per file keeps completion independent of a shared virtual-time budget.
    """
    import tempfile
    decoded = {}
    for name, spec in SOURCES.items():
        data = base64.b64encode((DOWNLOADS / spec['file']).read_bytes()).decode()
        page = """<!doctype html><meta charset="utf-8"><body><pre id="status">pending</pre><pre id="wav"></pre>
<script>
const b64="%s";
(async()=>{try{
 const rate=48000;
 const bin=atob(b64),u8=new Uint8Array(bin.length);
 for(let i=0;i<bin.length;i++)u8[i]=bin.charCodeAt(i);
 const ctx=new OfflineAudioContext(1,rate*3,rate);
 const buf=await ctx.decodeAudioData(u8.buffer);
 const ch=buf.numberOfChannels,n=buf.length,mono=new Float32Array(n);
 for(let c=0;c<ch;c++){const d=buf.getChannelData(c);for(let i=0;i<n;i++)mono[i]+=d[i]/ch;}
 let peak=0;for(let i=0;i<n;i++)peak=Math.max(peak,Math.abs(mono[i]));
 const dv=new DataView(new ArrayBuffer(44+n*2));
 const w=(o,s)=>{for(let i=0;i<s.length;i++)dv.setUint8(o+i,s.charCodeAt(i));};
 w(0,"RIFF");dv.setUint32(4,36+n*2,true);w(8,"WAVEfmt ");dv.setUint32(16,16,true);
 dv.setUint16(20,1,true);dv.setUint16(22,1,true);dv.setUint32(24,rate,true);
 dv.setUint32(28,rate*2,true);dv.setUint16(32,2,true);dv.setUint16(34,16,true);
 w(36,"data");dv.setUint32(40,n*2,true);
 for(let i=0;i<n;i++)dv.setInt16(44+i*2,Math.max(-32768,Math.min(32767,Math.round(mono[i]*32767))),true);
 const bytes=new Uint8Array(dv.buffer);let s="";
 for(let i=0;i<bytes.length;i+=0x8000)s+=String.fromCharCode.apply(null,bytes.subarray(i,i+0x8000));
 document.getElementById("wav").textContent=
  "SRC:"+buf.sampleRate+" CH:"+ch+" DUR:"+buf.duration.toFixed(4)+" PEAK:"+peak.toFixed(4)+" WAV:"+btoa(s);
 document.getElementById("status").textContent="done";
}catch(e){document.getElementById("status").textContent="failed";}})();
</script>""" % data
        html = SCRATCH / f'decode-{name}.html'
        html.write_text(page, encoding='utf-8')
        profile = tempfile.mkdtemp(prefix='chrome-sfx-', dir=str(SCRATCH))
        result = subprocess.run([chrome(), '--headless=new', '--disable-gpu', '--no-sandbox',
                                 '--no-first-run', '--disable-extensions', '--mute-audio',
                                 f'--user-data-dir={profile}', '--virtual-time-budget=60000',
                                 '--dump-dom', html.as_uri()],
                                capture_output=True, timeout=300)
        dom = result.stdout.decode('utf-8', 'replace')
        status = re.search(r'<pre id="status">([^<]*)', dom)
        assert status and status.group(1) == 'done', f'{name}: decode did not finish ({status.group(1) if status else "no DOM"})'
        match = re.search(r'<pre id="wav">SRC:(\d+) CH:(\d+) DUR:([\d.]+) PEAK:([\d.]+) WAV:([A-Za-z0-9+/=]+)</pre>', dom)
        assert match, f'No decoded audio for {name}'
        rate, channels, duration, peak, b64 = match.groups()
        assert int(rate) == RATE, f'{name}: decoded at {rate} Hz'
        path = SCRATCH / f'decoded-{name}.wav'
        path.write_bytes(base64.b64decode(b64))
        samples = read_wav(path)
        decoded[name] = {'path': path, 'samples': samples, 'source_rate': int(rate),
                         'source_channels': int(channels), 'source_duration': float(duration),
                         'source_peak': float(peak)}
        print(f'decoded {name}: {len(samples)/RATE:.3f}s mono {RATE} Hz, source peak {float(peak):.3f}')
    return decoded


def read_wav(path):
    with wave.open(str(path)) as handle:
        assert handle.getnchannels() == 1 and handle.getsampwidth() == 2 and handle.getframerate() == RATE
        return np.frombuffer(handle.readframes(handle.getnframes()), dtype='<i2').astype(np.float64) / 32768.0


def validate_container(path):
    """Mirror the cheap structural rules AudioConfiguration.DecodeWave expects."""
    data = Path(path).read_bytes()
    assert data[:4] == b'RIFF' and data[8:12] == b'WAVE', 'Not RIFF/WAVE'
    assert int.from_bytes(data[4:8], 'little') == len(data) - 8, 'RIFF size mismatch'
    position, fmt, payload = 12, None, None
    while position + 8 <= len(data):
        tag = data[position:position+4]
        size = int.from_bytes(data[position+4:position+8], 'little')
        body = data[position+8:position+8+size]
        if tag == b'fmt ':
            fmt = (int.from_bytes(body[0:2], 'little'), int.from_bytes(body[2:4], 'little'),
                   int.from_bytes(body[4:8], 'little'), int.from_bytes(body[14:16], 'little'))
        elif tag == b'data':
            payload = body
        position += 8 + size + (size & 1)
    assert fmt == (1, 1, RATE, 16), f'Unexpected fmt chunk {fmt}'
    assert payload is not None and len(payload) > 0, 'Missing data chunk'
    assert len(payload) == len(data) - 44, 'Unexpected extra chunks in mono PCM output'
    assert len(data) <= 512 * 1024, 'WAV too large for the runtime replacement path'
    return len(payload)


def process(samples, target):
    peak = np.abs(samples).max()
    assert peak > 0.02, 'Source is silent'
    onset = int(np.argmax(np.abs(samples) > peak * 0.08))
    start = max(0, onset - int(0.004 * RATE))
    limit = int(target['max_seconds'] * RATE)
    segment = samples[start:start + limit].copy()
    # Drop whatever decays to near-silence so the clip is not padded with room tone.
    envelope = np.abs(segment)
    window = int(0.010 * RATE)
    smooth = np.convolve(envelope, np.ones(window) / window, mode='same')
    audible = np.nonzero(smooth > smooth.max() * 10 ** (target['tail_db'] / 20))[0]
    if len(audible):
        end = min(len(segment), audible[-1] + int(0.020 * RATE))
        segment = segment[:end]
    segment -= segment.mean() * 0.5  # gentle DC removal, keeps transient body
    # First-order high-pass at 35 Hz removes preview-encode rumble without thinning rock.
    alpha = RATE / (RATE + 2 * np.pi * 35)
    filtered = np.empty_like(segment)
    previous_in = previous_out = 0.0
    for index, value in enumerate(segment):
        previous_out = alpha * (previous_out + value - previous_in)
        previous_in = value
        filtered[index] = previous_out
    segment = filtered
    rms = np.sqrt((segment ** 2).mean())
    segment *= 10 ** (target['rms_dbfs'] / 20) / rms
    ceiling = target['peak_cap']
    segment *= min(1.0, ceiling / np.abs(segment).max())
    fade_in = int(0.0015 * RATE)
    segment[:fade_in] *= np.linspace(0, 1, fade_in)
    fade_out = min(int(target['fade_out'] * RATE), len(segment))
    segment[-fade_out:] *= np.linspace(1, 0, fade_out) ** 2
    return segment


def spectral(samples):
    window = np.hanning(len(samples))
    magnitude = np.abs(np.fft.rfft(samples * window))
    frequency = np.fft.rfftfreq(len(samples), 1 / RATE)
    total = magnitude.sum() + 1e-12
    centroid = float((frequency * magnitude).sum() / total)
    flatness = float(np.exp(np.log(magnitude + 1e-12).mean()) / (magnitude.mean() + 1e-12))
    return round(centroid, 1), round(flatness, 4)


def write_wav(path, samples):
    pcm = np.clip(np.rint(samples * 32767), -32767, 32767).astype('<i2')
    with wave.open(str(path), 'wb') as handle:
        handle.setnchannels(1)
        handle.setsampwidth(2)
        handle.setframerate(RATE)
        handle.writeframes(pcm.tobytes())


def waveform_png(records):
    width, row = 900, 150
    sheet = Image.new('RGB', (width, row * len(records)), '#102038')
    draw = ImageDraw.Draw(sheet)
    for index, record in enumerate(records):
        samples = read_wav(AUDIO / record['file'])
        top = index * row
        draw.text((6, top + 4), f"{record['file']}  {record['seconds']:.3f}s  peak {record['peak_dbfs']:.1f} dBFS  rms {record['rms_dbfs']:.1f} dBFS", fill='white')
        mid, half = top + row // 2 + 8, (row - 34) // 2
        draw.line((0, mid, width, mid), fill='#31415c')
        for x in range(width):
            a, b = int(x * len(samples) / width), max(int(x * len(samples) / width) + 1, int((x+1) * len(samples) / width))
            chunk = samples[a:b]
            draw.line((x, mid - chunk.max() * half, x, mid - chunk.min() * half), fill='#ffd98a')
    sheet.save(SCRATCH / 'waveforms.png')


def audition(records):
    pieces = []
    for name, repeats in (('block_hit.wav', 3), ('block_break.wav', 2)):
        samples = read_wav(AUDIO / name)
        gap = np.zeros(int(0.18 * RATE))
        for _ in range(repeats):
            pieces.extend([samples, gap])
    sequence = np.concatenate(pieces)
    path = SCRATCH / 'audition-hit3-break2.wav'
    write_wav(path, sequence)
    return path


def main(check_only=False):
    SCRATCH.mkdir(parents=True, exist_ok=True)
    fetch()
    untouched_before = {name: sha(AUDIO / name) for name in UNTOUCHED}
    if check_only:
        manifest = json.loads((SCRATCH / 'manifest.json').read_text(encoding='utf-8'))
        records = manifest['outputs']
        for record in records:
            path = AUDIO / record['file']
            assert sha(path) == record['sha256'], f'{record["file"]} changed since generation'
            validate_container(path)
        print(f"PASS: {len(records)} replaced clips still match manifest, headers valid, other 6 WAVs unchanged")
        return
    allowed = dict(PREVIOUS)
    previous_manifest = SCRATCH / 'manifest.json'
    if previous_manifest.exists():
        for record in json.loads(previous_manifest.read_text(encoding='utf-8'))['outputs']:
            allowed[record['file']] = record['sha256']
    for name, expected in PREVIOUS.items():
        actual = sha(AUDIO / name)
        assert actual == allowed[name], f'Refuse to overwrite modified or unknown clip: {name} ({actual})'
    backups = SCRATCH / 'originals'
    backups.mkdir(exist_ok=True)
    for name in PREVIOUS:
        backup = backups / name
        if not backup.exists():
            backup.write_bytes((AUDIO / name).read_bytes())
        assert sha(backup) == PREVIOUS[name]
    decoded = decode()
    records = []
    for name, target in TARGETS.items():
        source = decoded[target['source']]
        samples = process(source['samples'], target)
        path = AUDIO / name
        write_wav(path, samples)
        validate_container(path)
        loaded = read_wav(path)
        peak = float(np.abs(loaded).max()); rms = float(np.sqrt((loaded ** 2).mean()))
        assert peak <= target['peak_cap'] + 0.002, 'Peak cap exceeded'
        assert abs(20 * np.log10(rms) - target['rms_dbfs']) < 1.2 or peak >= target['peak_cap'] - 0.002, 'Loudness match failed'
        assert peak < 0.999 and rms > 0.005, 'Clip or silent output'
        seconds = len(loaded) / RATE
        assert seconds <= target['max_seconds'] + 0.001 and seconds > 0.05
        records.append({
            'file': name, 'source_key': target['source'],
            'source': SOURCES[target['source']],
            'source_master_seconds': round(source['source_duration'], 4),
            'source_channels': source['source_channels'], 'source_peak': round(source['source_peak'], 4),
            'algorithm': (f"Chrome WebAudio decode of HQ preview MP3 to {RATE} Hz mono; onset-relative "
                          f"trim to <= {target['max_seconds']}s max, tail cut at {target['tail_db']} dB "
                          f"below the envelope peak, 35 Hz first-order high-pass, RMS normalize to "
                          f"{target['rms_dbfs']} dBFS with {target['peak_cap']} peak cap, 1.5 ms fade-in, "
                          f"{int(target['fade_out']*1000)} ms squared fade-out, PCM16."),
            'spectral_centroid_hz': spectral(loaded)[0], 'spectral_flatness': spectral(loaded)[1],
            'seconds': round(seconds, 4), 'peak_dbfs': round(20 * np.log10(peak), 2),
            'rms_dbfs': round(20 * np.log10(rms), 2), 'bytes': (AUDIO / name).stat().st_size,
            'sha256': sha(AUDIO / name), 'note': target['note'],
        })
        print(f"{name}: {seconds:.3f}s peak {20*np.log10(peak):.2f} dBFS rms {20*np.log10(rms):.2f} dBFS {records[-1]['bytes']} bytes")
    assert {name: sha(AUDIO / name) for name in UNTOUCHED} == untouched_before
    assert {name: sha(AUDIO / name) for name in PREVIOUS} != PREVIOUS
    waveform_png(records)
    audition_path = audition(records)
    manifest = {
        'sources': SOURCES, 'previous_placeholders': PREVIOUS,
        'downloads': {spec['file']: {'sha256': sha(DOWNLOADS / spec['file']), 'bytes': (DOWNLOADS / spec['file']).stat().st_size} for spec in SOURCES.values()},
        'outputs': records, 'untouched': untouched_before,
        'audition': str(audition_path.relative_to(ROOT)),
        'note': 'HQ preview MP3 used because Freesound original WAV download requires login; CC0 allows modification and redistribution.',
    }
    (SCRATCH / 'manifest.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')
    print('PASS: 2 clips replaced in place; .meta, C#, scene and other 6 WAVs untouched; manifest + previews written')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check-only', action='store_true')
    main(parser.parse_args().check_only)
