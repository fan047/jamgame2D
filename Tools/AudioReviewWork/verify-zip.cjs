const fs=require('node:fs'),path=require('node:path'),zlib=require('node:zlib'),crypto=require('node:crypto');
const directory=path.resolve(__dirname,'../../Docs/中期版/音效试听包');
const zipPath=path.join(directory,'矿石轻乐音候选.zip');
const b=fs.readFileSync(zipPath);let eocd=-1;
for(let i=b.length-22;i>=Math.max(0,b.length-65557);i--)if(b.readUInt32LE(i)===0x06054b50){eocd=i;break;}
if(eocd<0)throw Error('Missing ZIP directory');
const count=b.readUInt16LE(eocd+10);let offset=b.readUInt32LE(eocd+16),checked=0;
const found=new Set();
for(let i=0;i<count;i++){
 if(b.readUInt32LE(offset)!==0x02014b50)throw Error('Invalid central header');
 const method=b.readUInt16LE(offset+10),crc=b.readUInt32LE(offset+16),size=b.readUInt32LE(offset+20),plainSize=b.readUInt32LE(offset+24),nameLength=b.readUInt16LE(offset+28),extra=b.readUInt16LE(offset+30),comment=b.readUInt16LE(offset+32),local=b.readUInt32LE(offset+42);
 const name=b.toString('utf8',offset+46,offset+46+nameLength).replaceAll('\\','/');offset+=46+nameLength+extra+comment;
 if(name.endsWith('/'))continue;
 if(name.startsWith('/')||name.split('/').some(p=>p==='..'||p.includes(':')))throw Error('Unsafe ZIP path');
 if(b.readUInt32LE(local)!==0x04034b50)throw Error('Invalid local header');
 const at=local+30+b.readUInt16LE(local+26)+b.readUInt16LE(local+28);
 const compressed=b.subarray(at,at+size);const data=method===0?compressed:method===8?zlib.inflateRawSync(compressed):null;
 if(!data||data.length!==plainSize||zlib.crc32(data)!==crc)throw Error('ZIP data integrity failure: '+name);
 const source=path.resolve(directory,name);
 if(!source.startsWith(path.join(directory,'矿石轻乐音候选')+path.sep)||!fs.readFileSync(source).equals(data))throw Error('ZIP differs from source: '+name);
 found.add(source);checked++;
}
function walk(dir){return fs.readdirSync(dir,{withFileTypes:true}).flatMap(e=>e.isDirectory()?walk(path.join(dir,e.name)):[path.join(dir,e.name)]);}
const expected=walk(path.join(directory,'矿石轻乐音候选'));
if(expected.length!==checked||expected.some(f=>!found.has(f)))throw Error('Incomplete ZIP');
const sha=crypto.createHash('sha256').update(b).digest('hex');
fs.writeFileSync(zipPath+'.sha256',`${sha}  矿石轻乐音候选.zip\n`,{flag:'wx'});
console.log(JSON.stringify({files:checked,bytes:b.length,sha256:sha,crcChecked:true,allFilesMatch:true},null,2));
