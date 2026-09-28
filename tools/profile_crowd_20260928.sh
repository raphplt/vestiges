#!/usr/bin/env bash
# Profil managé EventPipe ; les FPS instrumentés ne sont PAS des résultats de débit.
# Nécessite dotnet-trace ; DOTNET_TRACE peut désigner une installation temporaire.
set -euo pipefail
cd "$(dirname "$0")/.."
python3 - "${1:?dossier neuf}" <<'PY'
import hashlib,json,os,pathlib,shutil,subprocess,sys,tempfile,time
out=pathlib.Path(sys.argv[1]).resolve();out.mkdir(parents=True,exist_ok=False)
profile=tempfile.mkdtemp(prefix='vestiges-crowd-profile-')
env=dict(os.environ,XDG_DATA_HOME=profile+'/data',XDG_CONFIG_HOME=profile+'/config',XDG_CACHE_HOME=profile+'/cache')
trace=os.environ.get('DOTNET_TRACE','dotnet-trace');godot=os.environ.get('GODOT_BIN','godot-mono')
rows=[]
game=None
try:
 for trial,n in enumerate([60,240,240,60]):
  name=f'{trial}-{n}';prefix=out/name
  cmd=[godot,'--path','.', '--windowed','--resolution','1280x720','--rendering-method','gl_compatibility','--disable-vsync','--max-fps','0','--audio-driver','Dummy','res://tools/tests/MovementDenseBenchmark.tscn','--','--dev','--enemies',str(n),'--audit-observer-period','60','--seconds','15','--warmup','4','--output',str(prefix)]
  row={'trial':trial,'enemies':n,'uptime_before':subprocess.check_output(['uptime'],text=True).strip(),'game_command':cmd};rows.append(row)
  with open(str(prefix)+'.log','w') as log:
   game=subprocess.Popen(cmd,env=env,stdout=log,stderr=subprocess.STDOUT)
   deadline=time.monotonic()+90
   while 'Chauffe puis mesure' not in pathlib.Path(str(prefix)+'.log').read_text():
    if game.poll() is not None or time.monotonic()>deadline:raise RuntimeError('initialisation échouée')
    time.sleep(.2)
   time.sleep(4)
   tc=[trace,'collect','--process-id',str(game.pid),'--profile','dotnet-sampled-thread-time,gc-verbose','--duration','00:00:10','--format','Speedscope','--output',str(prefix)+'.nettrace']
   row['trace_command']=tc
   with open(str(prefix)+'.trace.log','w') as trace_log:
    row['trace_exit']=subprocess.run(tc,stdout=trace_log,stderr=subprocess.STDOUT,timeout=40).returncode
   row['game_exit']=game.wait(timeout=40)
  row['uptime_after']=subprocess.check_output(['uptime'],text=True).strip()
  if row['trace_exit'] or row['game_exit']:raise RuntimeError('échec profil')
finally:
 if game is not None and game.poll() is None:
  game.terminate()
  try: game.wait(timeout=5)
  except subprocess.TimeoutExpired: game.kill(); game.wait()
 manifest={'evidence':'échantillonnage des piles managées ; temps mural échantillonné, pas profil natif ni temps CPU exclusif ; FPS exclus',
 'dll_sha256':hashlib.sha256(pathlib.Path('.godot/mono/temp/bin/Debug/Vestiges.dll').read_bytes()).hexdigest(),'trials':rows}
 (out/'manifest.json').write_text(json.dumps(manifest,indent=2,ensure_ascii=False))
 shutil.rmtree(profile)
PY
