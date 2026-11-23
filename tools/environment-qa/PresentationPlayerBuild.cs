using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class PresentationPlayerBuild
{
    public static void Run()
    {
        if(!Application.isBatchMode || Application.productName!="shop-qa")
            throw new Exception("Isolated player build required");
        var dest=Path.Combine(Environment.GetEnvironmentVariable("PLAYER_QA_OUTPUT"),"shop-qa.exe");
        var scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();
        var report=BuildPipeline.BuildPlayer(scenes,dest,BuildTarget.StandaloneWindows64,BuildOptions.None);
        Debug.Log("PRESENTATION_BUILD "+report.summary.result+" "+report.summary.totalSize+" bytes "+report.summary.totalErrors+" errors");
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Standalone build failed");
    }
}
