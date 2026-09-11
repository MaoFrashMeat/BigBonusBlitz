using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Play とビルドの前に git のコミット数と短いハッシュを Resources/Data/build_info.json へ書く。
/// タイトルの隅に「dev 106 · 7a498f1」のように出す。手で番号を上げなくても、コミットするたびに進む。
/// まだ α でもない開発中なので、1.0 のような版番号は出さない（ProjectSettings の bundleVersion は 0.0.0）。
/// ファイルは .gitignore で外してある（コミットごとに変わるので追跡しない）。
/// </summary>
[InitializeOnLoad]
public sealed class BuildInfoWriter : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    private const string OutPath = "Assets/Resources/Data/build_info.json";

    static BuildInfoWriter()
    {
        EditorApplication.playModeStateChanged += st => { if (st == PlayModeStateChange.ExitingEditMode) Write(); };
        Write();
    }

    public void OnPreprocessBuild(BuildReport report) => Write();

    private static string Git(string args)
    {
        try
        {
            var psi = new ProcessStartInfo("git", args)
            {
                WorkingDirectory = Path.GetDirectoryName(Application.dataPath),
                RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true,
            };
            using (var p = Process.Start(psi))
            {
                string s = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit(3000);
                return p.ExitCode == 0 ? s : "";
            }
        }
        catch { return ""; }
    }

    [MenuItem("BBB/ビルド情報を書き出す")]
    public static void Write()
    {
        string count = Git("rev-list --count HEAD");
        string hash = Git("rev-parse --short HEAD");
        string dirty = string.IsNullOrEmpty(Git("status --porcelain --untracked-files=no")) ? "" : "+";
        if (string.IsNullOrEmpty(count)) return;      // git が無い環境では触らない
        string json = "{\n  \"count\": " + count + ",\n  \"hash\": \"" + hash + dirty + "\",\n  \"date\": \"" + System.DateTime.Now.ToString("yyyy-MM-dd") + "\"\n}\n";
        Directory.CreateDirectory(Path.GetDirectoryName(OutPath));
        if (File.Exists(OutPath) && File.ReadAllText(OutPath) == json) return;
        File.WriteAllText(OutPath, json);
        AssetDatabase.ImportAsset(OutPath);
    }
}
