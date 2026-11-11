const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const { execFileSync } = require('node:child_process');
const root = path.resolve(__dirname, '../../Docs/中期版/音效试听包/矿石轻乐音候选');
const original = path.resolve(__dirname, '../../Assets/Holedown/Audio');
const sha = data => crypto.createHash('sha256').update(data).digest('hex');
const save = (name, data) => fs.writeFileSync(path.join(root, name), data, { flag: 'wx' });
const baseline = Object.fromEntries(fs.readdirSync(original).filter(n => n.endsWith('.wav')).map(n => [n, sha(fs.readFileSync(path.join(original, n)))]));
if (Object.keys(baseline).length !== 8) throw Error('Expected eight original WAV files');
if (fs.existsSync(path.join(root, '原音效校验.json'))) {
  if (JSON.stringify(JSON.parse(fs.readFileSync(path.join(root, '原音效校验.json'), 'utf8'))) !== JSON.stringify(baseline)) throw Error('Original audio changed');
} else save('原音效校验.json', JSON.stringify(baseline, null, 2));
const repos = {
  modern: ['iree-gd/iree.gd', '9db7d7069a9ad75c77e9b5657d1ae5bdf615a6bc'],
  ui: ['ShadowBlip/OpenGamepadUI', '12747dfff84cac02ed00528194199f2ad4f63c1c'],
  legacy: ['ismetcabbar9-oss/kenney-audio-assets', 'b7729283e47d38f203d85560cfda5b2c1fd6fc7e']
};
const entries = [];
for (let n = 0; n < 5; n++) entries.push({ id: `mining${n}`, repo: 'modern', folder: 'Kenney-Impact', file: `sample/assets/audio/impact/impactMining_00${n}.ogg`, author: 'Kenney', page: 'https://kenney.nl/assets/impact-sounds' });
for (const n of [10, 11, 16]) entries.push({ id: `jingle${n}`, repo: 'modern', folder: 'Kenney-Jingles', file: `sample/assets/audio/pizzicato_jingles/jingles_PIZZI${n}.ogg`, author: 'Kenney', page: 'https://kenney.nl/assets/music-jingles' });
for (const n of [1, 2]) entries.push({ id: `click${n}`, repo: 'ui', folder: 'Kenney-Interface', file: `assets/audio/interface/click_00${n}.ogg`, author: 'Kenney', page: 'https://kenney.nl/assets/interface-sounds' });
for (const n of [1, 2]) entries.push({ id: `air${n}`, repo: 'legacy', folder: 'Kenney-RPG', file: `Audio (295 files)/RPG sounds (50 sounds)/knifeSlice${n === 1 ? '' : '2'}.ogg`, author: 'Kenney', page: 'https://github.com/ismetcabbar9-oss/kenney-audio-assets' });
const folders = new Set(entries.map(e => e.folder));
for (const folder of folders) {
  const directory = path.join(root, '原始素材', folder);
  if (!fs.existsSync(directory)) fs.mkdirSync(directory);
}
function get(repoKey, file) {
  const [repo, ref] = repos[repoKey];
  const endpoint = `repos/${repo}/contents/${file.split('/').map(encodeURIComponent).join('/')}?ref=${ref}`;
  const content = execFileSync('gh', ['api', endpoint, '--jq', '.content'], { maxBuffer: 16 * 1024 * 1024, encoding: 'utf8' });
  return Buffer.from(content, 'base64');
}
for (const e of entries) {
  const data = get(e.repo, e.file);
  if (data.subarray(0, 4).toString() !== 'OggS') throw Error(`Not Ogg audio: ${e.id}`);
  const name = path.basename(e.file);
  e.local = `原始素材/${e.folder}/${name}`;
  e.sha256 = sha(data);
  e.bytes = data.length;
  const [repo, ref] = repos[e.repo];
  e.mirror = `https://github.com/${repo}/blob/${ref}/${e.file.split('/').map(encodeURIComponent).join('/')}`;
  save(e.local, data);
  console.log(`${e.id}: ${name} (${data.length} bytes)`);
}
const license = get('legacy', 'LICENSE.md');
const notice = get('legacy', 'README.md');
if (!license.toString().includes('CC0 1.0 Universal') || !notice.toString().includes('Copyright (c) 2014 Kenney')) throw Error('Missing Kenney license evidence');
save('原始素材/Kenney-RPG/CC0原文.txt', license);
save('原始素材/Kenney-RPG/镜像来源说明.txt', notice);
save('素材清单.json', JSON.stringify({ created: new Date().toISOString(), entries, licenseEvidence: 'RPG: 镜像保留Kenney作者及CC0原文；其余三包：原作者素材页面公开标注CC0，镜像未附独立素材包许可文件，发布前建议再复核原包。镜像仓库软件许可不替代音频作者许可。', airSubstitution: 'Swishes镜像存在泛化CC-BY-SA声明，未下载未使用；以Kenney RPG knifeSlice/knifeSlice2替代短气流候选。' }, null, 2));
console.log('Sources downloaded; original Unity audio untouched.');
