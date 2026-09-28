#!/usr/bin/env bash
# Diagnostic uniquement. Utiliser une copie figée de l'arbre pour comparer le même build.
# Usage : tools/audit_performance_20260928.sh <dossier neuf> [systems|cycles|shaders|physics|crowd|crowd-density]
set -euo pipefail
cd "$(dirname "$0")/.."
OUTPUT=${1:?dossier de résultats requis}
MODE=${2:-systems}
mkdir -p "$OUTPUT"
OUTPUT=$(realpath "$OUTPUT")
if [[ -e "$OUTPUT/manifest-$MODE.json" ]]; then
    echo "Résultats déjà présents pour $MODE" >&2
    exit 1
fi
if [[ ${AUDIT_SKIP_BUILD:-0} != 1 ]]; then
    dotnet build --nologo > "$OUTPUT/build-$MODE.log"
    "${GODOT_BIN:-godot-mono}" --headless --editor --import --path . > "$OUTPUT/import-$MODE.log" 2>&1
fi
python3 - "$OUTPUT" "$MODE" <<'PY'
import datetime, hashlib, json, os, pathlib, shutil, subprocess, sys, tempfile, time
root=pathlib.Path.cwd(); output=pathlib.Path(sys.argv[1]); mode=sys.argv[2]
threshold=int(subprocess.check_output(['getconf','_NPROCESSORS_ONLN']))//4
profile=tempfile.mkdtemp(prefix='vestiges-audit-profile-')
env=dict(os.environ, XDG_DATA_HOME=profile+'/data', XDG_CONFIG_HOME=profile+'/config', XDG_CACHE_HOME=profile+'/cache')
godot=os.environ.get('GODOT_BIN','godot-mono')
manifest={'utc':datetime.datetime.now(datetime.timezone.utc).isoformat(), 'mode':mode,'seed':221092026,'threshold_load1':threshold,
 'head':subprocess.check_output(['git','rev-parse','HEAD'],text=True).strip(), 'status':subprocess.check_output(['git','status','--short'],text=True),
 'dll_sha256':hashlib.sha256((root/'.godot/mono/temp/bin/Debug/Vestiges.dll').read_bytes()).hexdigest(),
 'source_sha256':{str(p):hashlib.sha256(p.read_bytes()).hexdigest() for directory in ['scripts','tools/tests'] for p in sorted(pathlib.Path(directory).rglob('*.cs'))}, 'runs':[]}
process=None
try:
 if mode=='systems': cases=[('systems-a','PerformanceAudit20260928',[],True),('systems-b','PerformanceAudit20260928',[],True)]
 elif mode=='cycles': cases=[('cycles','PerformanceAudit20260928',['--cycles'],True)]
 elif mode=='shaders': cases=[('shaders','ShaderWarmupAudit',[],False)]
 elif mode=='physics': cases=[('physics','PhysicsBodyAudit',[],True)]
 elif mode=='crowd-density':
  cases=[(f'density-{r}-{n}','MovementDenseBenchmark',['--enemies',str(n),'--audit-observer-period','60','--seconds','10','--warmup','3','--width','1280','--height','720'],False) for r,n in enumerate([60,240,240,60])]
 elif mode=='crowd':
  # Les modes sont ceux du même banc instrumenté ; paires AB/BA, nombre d'ennemis constant par paire.
  cases=[(f'crowd-{n}-{r}-{period}','MovementDenseBenchmark',['--enemies',str(n),'--audit-observer-period',str(period),'--seconds','10','--warmup','3','--width','1280','--height','720'],False) for n in [60,240] for r in [0,1] for period in ([1,60] if r==0 else [60,1])]
 else: raise ValueError(mode)
 for name,scene,extra,headless in cases:
  load=os.getloadavg()[0]
  if mode in ['crowd','crowd-density'] and load>threshold:
   manifest['blocked']={'load1':load,'reason':'charge supérieure au seuil bench_ab.sh ; aucune mesure FPS'}
   break
  command=[godot,'--path','.', '--audio-driver','Dummy']
  command+=['--headless','--fixed-fps','60'] if headless else ['--windowed','--resolution','1280x720','--rendering-method','gl_compatibility','--disable-vsync','--max-fps','0']
  suffix='' if mode in ['crowd','crowd-density'] else '.json'
  command+=['res://tools/tests/'+scene+'.tscn','--','--dev','--output',str(output/(name+suffix))]+extra
  row={'name':name,'command':command,'load_samples':[], 'uptime_before':subprocess.check_output(['uptime'],text=True).strip()}
  manifest['runs'].append(row)
  start=time.monotonic()
  with open(output/(name+'.log'),'w') as log:
   process=subprocess.Popen(command,env=env,stdout=log,stderr=subprocess.STDOUT)
   while process.poll() is None:
    row['load_samples'].append({'wall_s':round(time.monotonic()-start,3),'load1':os.getloadavg()[0]})
    if time.monotonic()-start>360:
     process.terminate();process.wait(timeout=10);raise RuntimeError('timeout '+name)
    time.sleep(1)
  row['exit_code']=process.returncode
  row['uptime_after']=subprocess.check_output(['uptime'],text=True).strip()
  row['fps_eligible']=mode in ['crowd','crowd-density'] and all(s['load1']<=threshold for s in row['load_samples'])
  text=(output/(name+'.log')).read_text()
  errors=[line for line in text.splitlines() if line.startswith(('ERROR:','SCRIPT ERROR:')) and not any(x in line for x in ['steam_api','ObjectDB instances were leaked','resources still in use'])]
  row['errors']=errors
  if process.returncode or errors or 'RESULT' not in text: raise RuntimeError('diagnostic invalide '+name)
finally:
 if process is not None and process.poll() is None:
  process.terminate()
  try: process.wait(timeout=5)
  except subprocess.TimeoutExpired: process.kill(); process.wait()
 (output/('manifest-'+mode+'.json')).write_text(json.dumps(manifest,indent=2,ensure_ascii=False)+'\n')
 shutil.rmtree(profile)
PY
