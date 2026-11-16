"""Build-independent release packaging. Run the Release build and regression tests first."""
from pathlib import Path
from datetime import datetime, timezone
import argparse, hashlib, json, re, subprocess, zipfile

ROOT = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument('--tag', required=True)
args = parser.parse_args()
version = re.search(r'<Version>([^<]+)</Version>', (ROOT/'src/Strimarr/Gelato.csproj').read_text()).group(1)
if not re.fullmatch(r'\d+\.\d+\.\d+', version): raise SystemExit('Expected three-part project version')
if not re.fullmatch(re.escape('v'+version)+r'(?:-rc\.\d+)?', args.tag): raise SystemExit('Tag does not match project version')
numeric = version+'.0'
out=ROOT/'dist';out.mkdir(exist_ok=True)
dll=out/'build/Strimarr.dll'
if not dll.is_file(): raise SystemExit('Build dist/build/Strimarr.dll first')
meta={'name':'Strimarr','guid':'b137092c-cc78-4c6e-b1a5-c3a1aa587a1d','version':numeric,'targetAbi':'12.1.0.0','framework':'net10.0','overview':'Metadata-first AIOStreams integration for Jellyfin','description':'A GPL-3.0 Gelato derivative with background discovery and bounded HTTP relay.','owner':'Ankitd013','status':'Active','autoUpdate':True}
archive=out/f'Strimarr-{numeric}.zip'
with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED) as z:
    z.write(dll,'Strimarr.dll')
    z.writestr('meta.json',json.dumps(meta,indent=2)+'\n')
    for name in ['LICENSE','NOTICE.md','README.md']: z.write(ROOT/name,name)
source=out/f'Strimarr-{args.tag}-source.zip'
tracked=subprocess.check_output(['git','ls-files','-z'],cwd=ROOT).decode().split('\0')
if not any(tracked): raise SystemExit('Stage audited source files before packaging')
with zipfile.ZipFile(source,'w',zipfile.ZIP_DEFLATED) as z:
    for name in sorted(filter(None,tracked)):
        path=ROOT/name
        if path.is_file() and not name.startswith('dist/'): z.write(path,'strimarr/'+name)
url=f'https://github.com/Ankitd013/strimarr/releases/download/{args.tag}/{archive.name}'
entry={k:meta[k] for k in ['guid','name','overview','description','owner']}
entry['category']='General'
entry['versions']=[{'version':numeric,'targetAbi':'12.1.0.0','sourceUrl':url,'checksum':hashlib.md5(archive.read_bytes()).hexdigest(),'timestamp':datetime.now(timezone.utc).isoformat(),'changelog':f'{args.tag}: public candidate; see CHANGELOG.md and validation limits.'}]
manifest=out/'release-manifest.json';manifest.write_text(json.dumps([entry],indent=2)+'\n')
assets=[archive,source,manifest]
(out/'SHA256SUMS').write_text(''.join(hashlib.sha256(p.read_bytes()).hexdigest()+'  '+p.name+'\n' for p in assets))
print('Packaged:',*[p.name for p in assets],'SHA256SUMS',sep='\n')
