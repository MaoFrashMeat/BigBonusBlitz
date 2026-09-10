"""PC 間で Unity プロジェクトの状態が揃っているかを調べる。

    py -3 docs/check_project.py

別の PC で開く前と後に走らせる。「違う」と感じたら、まずこれを両方の PC で走らせて
出力を見比べる。同じ出力になれば、Unity 側の差はレイアウトなどの個人設定だけ。
"""
import io, os, subprocess, sys, json

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PROJ = os.path.join(ROOT, 'UnityProject', 'BigBonusBlitz')
ASSETS = os.path.join(PROJ, 'Assets')
NG = []


def sh(cmd):
    try:
        return subprocess.run(cmd, cwd=ROOT, capture_output=True, text=True, shell=True).stdout.strip()
    except Exception:
        return ''


def head(t):
    print()
    print('■ ' + t)


# ---------------------------------------------------------------- Unity 本体
head('Unity のバージョン')
pv = os.path.join(PROJ, 'ProjectSettings', 'ProjectVersion.txt')
ver = ''
if os.path.exists(pv):
    for line in io.open(pv, encoding='utf-8'):
        if line.startswith('m_EditorVersion:'):
            ver = line.split(':', 1)[1].strip()
print('    プロジェクトの指定 : ' + (ver or '不明'))
if not ver:
    NG.append('ProjectVersion.txt が読めない')
else:
    print('    → このバージョンの Unity で開くこと。違うと再インポートで見た目が変わる')

# 入っている Unity を探す（Windows の既定の場所）
hub = r'C:\Program Files\Unity\Hub\Editor'
if os.path.isdir(hub):
    installed = sorted(os.listdir(hub))
    print('    この PC に入っている : ' + (', '.join(installed) if installed else 'なし'))
    if ver and ver not in installed:
        NG.append('指定バージョン %s がこの PC に入っていない' % ver)

# ---------------------------------------------------------------- git
head('git の状態')
branch = sh('git rev-parse --abbrev-ref HEAD')
commit = sh('git rev-parse --short HEAD')
print('    ブランチ : %s   コミット : %s' % (branch, commit))
behind = sh('git rev-list --count HEAD..@{u} 2>NUL') or '0'
ahead = sh('git rev-list --count @{u}..HEAD 2>NUL') or '0'
print('    リモートとの差 : 遅れ %s / 進み %s' % (behind, ahead))
if behind != '0':
    NG.append('リモートより %s コミット遅れている（git pull が要る）' % behind)

dirty = sh('git status --porcelain')
n_dirty = len([x for x in dirty.splitlines() if x.strip()])
print('    未コミットの変更 : %d 件' % n_dirty)

# ---------------------------------------------------------------- LFS
head('LFS（画像・音の実体）')
lfs_files = [x for x in sh('git lfs ls-files').splitlines() if x.strip()]
print('    LFS 管理下 : %d 件' % len(lfs_files))
pointers = []
for rel in sh('git ls-files').splitlines():
    if not rel.lower().endswith(('.png', '.jpg', '.mp3', '.wav', '.ogg', '.ttf', '.otf', '.fbx', '.psd')):
        continue
    p = os.path.join(ROOT, rel.replace('/', os.sep))
    if not os.path.exists(p):
        continue
    if os.path.getsize(p) < 400:
        try:
            with io.open(p, 'rb') as f:
                if f.read(40).startswith(b'version https://git-lfs'):
                    pointers.append(rel)
        except Exception:
            pass
if pointers:
    NG.append('LFS の実体が落ちていないファイルが %d 件（git lfs pull が要る）' % len(pointers))
    print('    実体が無い : %d 件' % len(pointers))
    for x in pointers[:5]:
        print('      ' + x)
else:
    print('    実体は全部そろっている')

# ---------------------------------------------------------------- meta
head('.meta（これがずれると参照が壊れる）')
missing, orphan = [], []
for base, dirs, files in os.walk(ASSETS):
    dirs[:] = [d for d in dirs if d not in ('.git',)]
    for d in dirs:
        p = os.path.join(base, d)
        if not os.path.exists(p + '.meta'):
            missing.append(os.path.relpath(p, PROJ))
    for f in files:
        if f.endswith('.meta'):
            if not os.path.exists(os.path.join(base, f[:-5])):
                orphan.append(os.path.relpath(os.path.join(base, f), PROJ))
            continue
        p = os.path.join(base, f)
        if not os.path.exists(p + '.meta'):
            missing.append(os.path.relpath(p, PROJ))
print('    .meta が無い : %d 件 / 迷子の .meta : %d 件' % (len(missing), len(orphan)))
for x in (missing + orphan)[:5]:
    print('      ' + x)
if missing:
    NG.append('.meta が無いファイルが %d 件（Unity が新しい GUID を振ってしまう）' % len(missing))
if orphan:
    NG.append('迷子の .meta が %d 件' % len(orphan))

tracked_meta = len([x for x in sh('git ls-files UnityProject/BigBonusBlitz/Assets').splitlines() if x.endswith('.meta')])
disk_meta = sum(1 for b, d, fs in os.walk(ASSETS) for f in fs if f.endswith('.meta'))
print('    追跡 %d 件 / ファイル上 %d 件' % (tracked_meta, disk_meta))
if tracked_meta != disk_meta:
    NG.append('.meta の追跡数とファイル数が合わない')

# ---------------------------------------------------------------- パッケージ
head('パッケージ')
lock = os.path.join(PROJ, 'Packages', 'packages-lock.json')
if os.path.exists(lock):
    try:
        d = json.load(io.open(lock, encoding='utf-8'))
        deps = d.get('dependencies', {})
        print('    packages-lock.json : %d 個で固定されている' % len(deps))
        for name in ('com.unity.inputsystem', 'com.unity.nuget.newtonsoft-json', 'com.unity.test-framework'):
            v = deps.get(name, {}).get('version', '(無し)')
            print('      %-38s %s' % (name, v))
    except Exception as e:
        NG.append('packages-lock.json が読めない: %s' % e)
else:
    NG.append('packages-lock.json が無い（PC ごとにパッケージ版が変わる）')

# ---------------------------------------------------------------- データ
head('ゲームのデータ')
for name in ('game_config.json', 'workflow_config.json', 'enemy_tables.json'):
    p = os.path.join(ASSETS, 'Resources', 'Data', name)
    if not os.path.exists(p):
        NG.append('%s が無い' % name)
        continue
    try:
        d = json.load(io.open(p, encoding='utf-8'))
        print('    %-22s キー %d 個' % (name, len(d) if isinstance(d, dict) else len(d.get('tables', []))))
    except Exception as e:
        NG.append('%s が壊れている: %s' % (name, e))

# ---------------------------------------------------------------- 結果
print()
if NG:
    print('ずれているところが %d 件:' % len(NG))
    for x in NG:
        print('  - ' + x)
    print()
    print('直しかた:')
    print('  git pull --rebase        リモートに追いつく')
    print('  git lfs pull             画像と音の実体を落とす')
    print('  Unity Hub で %s を入れて、そのバージョンで開く' % (ver or '指定バージョン'))
    sys.exit(1)
print('この PC の状態は揃っている（違って見えるならレイアウトなどの個人設定）')
