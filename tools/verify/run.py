# -*- coding: utf-8 -*-
"""
commit の前の検証を 1 本で回す（どの PC・どのセッションでも同じ手順になるように）。

    py -3 tools/verify/run.py              # コンパイル 3 つ（Core / Runtime / Tests）→ ハーネス（100 万G）
    py -3 tools/verify/run.py --tests      # 上に加えて Unity バッチで EditMode テスト（本体を複製して回す。数分）
    py -3 tools/verify/run.py --compile    # コンパイルだけ（速い）

前提:
- dotnet SDK 8 以上（無い PC は「NG」で止まり、--tests の Unity だけ回る）。初回の restore は NuGet に繋がる
- Unity Hub の既定の場所に ProjectVersion.txt と同じ版の Unity。違う場所なら環境変数 UNITY_EDITOR_DIR（…/Editor まで）
- UnityProject/BigBonusBlitz/Library があること（clone 直後は無い。Unity で本体を一度開くと出来る。
  ここに入っている InputSystem / uGUI / Newtonsoft / NUnit の DLL を参照する）
結果は最後に 1 行ずつ「OK / NG」。NG があれば終了コード 1。
"""
import glob, io, os, re, shutil, subprocess, sys, tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
PROJ = os.path.join(ROOT, "UnityProject", "BigBonusBlitz")
HERE = os.path.join(ROOT, "tools", "verify")


def unity_editor_dir():
    env = os.environ.get("UNITY_EDITOR_DIR")
    if env: return env
    ver = "6000.3.15f1"
    try:
        with io.open(os.path.join(PROJ, "ProjectSettings", "ProjectVersion.txt"), encoding="utf-8") as f:
            m = re.search(r"m_EditorVersion:\s*(\S+)", f.read())
            if m: ver = m.group(1)
    except OSError:
        pass
    for base in (r"C:/Program Files/Unity/Hub/Editor", os.path.expanduser("~/Unity/Hub/Editor"), "/Applications/Unity/Hub/Editor"):
        d = os.path.join(base, ver, "Editor")
        if os.path.isdir(d): return d.replace("\\", "/")
        d = os.path.join(base, ver, "Unity.app", "Contents")
        if os.path.isdir(d): return d.replace("\\", "/")
    return None


def library_missing():
    """csproj が参照する Library の DLL が無ければ、その名前を返す（あれば空）。"""
    lib = os.path.join(PROJ, "Library")
    need = [
        ("Unity.InputSystem.dll", os.path.join(lib, "ScriptAssemblies", "Unity.InputSystem.dll")),
        ("UnityEngine.UI.dll", os.path.join(lib, "ScriptAssemblies", "UnityEngine.UI.dll")),
    ]
    missing = [name for name, path in need if not os.path.exists(path)]
    if not glob.glob(os.path.join(lib, "PackageCache", "com.unity.nuget.newtonsoft-json@*", "Runtime", "Newtonsoft.Json.dll")): missing.append("Newtonsoft.Json.dll")
    if not glob.glob(os.path.join(lib, "PackageCache", "com.unity.ext.nunit@*", "net40", "unity-custom", "nunit.framework.dll")): missing.append("nunit.framework.dll")
    return missing


def run(cmd, cwd=None):
    try:
        p = subprocess.run(cmd, cwd=cwd, capture_output=True, text=True, encoding="utf-8", errors="replace")
    except OSError as e:
        return 127, str(e)
    return p.returncode, (p.stdout or "") + (p.stderr or "")


def build(name, editor):
    code, out = run(["dotnet", "build", os.path.join(HERE, name, name + ".csproj"), "-nologo", "-v", "q", f"-p:UnityEditorDir={editor}"])
    errs = sorted(set(l.strip() for l in out.splitlines() if " error " in l or ": error" in l))
    return code == 0 and not errs, errs[:6]


def harness():
    code, out = run(["dotnet", "run", "--project", os.path.join(HERE, "harness", "harness.csproj"), "-c", "Release", "--roll-forward", "LatestMajor"])
    lines = out.splitlines()
    ok = code == 0 and any("不変条件の違反なし" in l for l in lines)
    keep = [l.strip() for l in lines if ("機械割" in l and "20万G" in l) or "違反" in l or l.strip().startswith("NG") or "error" in l.lower()]
    return ok, keep[:6]


def unity_tests(editor):
    """本体を複製して EditMode テストを回す（本体の Library を触らない。EditMode のコンパイルが C# 全体の確認を兼ねる）。"""
    exe = os.path.join(editor, "Unity.exe") if os.name == "nt" else os.path.join(editor, "MacOS", "Unity")
    if not os.path.exists(exe): return False, ["Unity が無い: " + exe]
    tmp = os.path.join(tempfile.gettempdir(), "bbb_verify_proj")
    for sub in ("Assets", "Packages", "ProjectSettings"):
        dst = os.path.join(tmp, sub)
        if os.path.isdir(dst): shutil.rmtree(dst)
        shutil.copytree(os.path.join(PROJ, sub), dst)
    xml = os.path.join(tmp, "results.xml"); log = os.path.join(tmp, "unity.log")
    if os.path.exists(xml): os.remove(xml)
    code, _ = run([exe, "-batchmode", "-projectPath", tmp, "-runTests", "-testPlatform", "EditMode", "-testResults", xml, "-logFile", log])
    if not os.path.exists(xml):
        errs = []
        try:
            with io.open(log, encoding="utf-8", errors="replace") as f:
                errs = sorted(set(l.strip() for l in f if "error CS" in l))[:6]
        except OSError: pass
        return False, errs or ["結果の xml が無い（ログ: %s）" % log]
    with io.open(xml, encoding="utf-8") as f: s = f.read()
    m = re.search(r'total="(\d+)" passed="(\d+)" failed="(\d+)"', s)
    if not m: return False, ["結果を読めない"]
    total, passed, failed = m.groups()
    names = re.findall(r'<test-case[^>]*\sname="([^"]*)"[^>]*result="Failed"', s)[:6]
    return failed == "0", ["%s/%s" % (passed, total)] + names


def main():
    args = set(sys.argv[1:])
    editor = unity_editor_dir()
    results = []
    if editor is None:
        print("NG  Unity の場所が分からない。UNITY_EDITOR_DIR（…/Editor まで）を設定する"); sys.exit(1)
    have_dotnet = shutil.which("dotnet") is not None
    missing = library_missing()
    if not have_dotnet:
        results.append(("dotnet", False, ["dotnet SDK 8 以上が無い（winget install Microsoft.DotNet.SDK.8）。コンパイルとハーネスは飛ばす"]))
    elif missing:
        results.append(("Library", False, ["UnityProject/BigBonusBlitz/Library に無い: " + ", ".join(missing) + "。Unity で本体を一度開くと出来る。コンパイルとハーネスは飛ばす"]))
    else:
        for name in ("core", "runtime", "tests"):
            ok, errs = build(name, editor)
            results.append(("コンパイル " + name, ok, errs))
        if "--compile" not in args:
            ok, keep = harness(); results.append(("ハーネス 100 万G", ok, keep))
    if "--tests" in args:
        ok, info = unity_tests(editor); results.append(("EditMode テスト", ok, info))
    bad = 0
    for name, ok, info in results:
        print(("OK  " if ok else "NG  ") + name + ("" if not info else "  " + " / ".join(info)))
        if not ok: bad += 1
    sys.exit(1 if bad else 0)


if __name__ == "__main__":
    main()
