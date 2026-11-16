"""Conservative tracked-source hygiene checks; not a substitute for a secret audit."""
from pathlib import Path
import re, subprocess
root=Path(__file__).resolve().parents[1]
files=subprocess.check_output(['git','ls-files','-z'],cwd=root).decode().split('\0')
patterns=[r'-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----',r'gh[pousr]_[A-Za-z0-9]{30,}',r'github_pat_[A-Za-z0-9_]{30,}',r'eyJ[A-Za-z0-9_-]{20,}\.[A-Za-z0-9_-]{20,}\.',r'(?:media\.)?clawiz\.com',r'C:[/\\]Users[/\\]adank',r'192\.168\.1\.191']
errors=[]
for name in filter(None,files):
    path=root/name
    if any(part in ['bin','obj','dist','node_modules'] for part in path.relative_to(root).parts) or path.suffix.lower() in ['.dll','.zip','.pdb','.db','.sqlite'] or path.name.startswith('.env'):
        errors.append(name+': prohibited publication artifact');continue
    if name=='scripts/check_publication.py': continue
    try: text=path.read_text(encoding='utf-8')
    except UnicodeDecodeError: continue
    for pattern in patterns:
        if re.search(pattern,text,re.I): errors.append(name+': sensitive/private-data pattern (value suppressed)');break
if errors: raise SystemExit('\n'.join(errors))
print('Tracked-source publication hygiene passed:',len(list(filter(None,files))),'files')
