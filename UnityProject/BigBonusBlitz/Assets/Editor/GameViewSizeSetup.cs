using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace BBB.EditorTools
{
    /// <summary>
    /// Game ビューの解像度を、狙っている端末（iPhone 横持ち）に固定する。
    ///
    /// Game ビューの一覧は `Library/` に置かれ、git に載らない。
    /// そのままだと PC ごとに違う比率で見ることになり、
    /// 「こちらでは収まっているのに、あちらでは切れる」が起きる。
    /// この一手間で、どの PC でも同じ枠で見られるようにする。
    ///
    /// 使い方: メニューの BBB > 画面サイズ > iPhone 横持ちに固定
    /// </summary>
    public static class GameViewSizeSetup
    {
        /// <summary>狙っている端末。iPhone 15 / 16 Pro の横持ち。</summary>
        public const int TargetW = 2556, TargetH = 1179;
        private const string SizeName = "BBB iPhone 横 2556x1179";

        [MenuItem("BBB/画面サイズ/iPhone 横持ちに固定 (2556x1179)")]
        public static void Fix()
        {
            if (!EnsureSize(SizeName, TargetW, TargetH, out int index))
            {
                Debug.LogWarning("Game ビューの一覧を触れなかった。Unity の版が変わった可能性がある。" +
                                 "Game ビュー左上の一覧から手で 2556x1179 を足してほしい");
                return;
            }
            if (!Select(index))
            {
                Debug.LogWarning("Game ビューが開いていないので、一覧に足すだけにした。" +
                                 "Game ビューを開いてもう一度実行すると選ばれる");
                return;
            }
            Debug.Log($"Game ビューを {SizeName} にした");
        }

        [MenuItem("BBB/画面サイズ/いまの設定を表示")]
        public static void Show()
        {
            Debug.Log($"狙い: {TargetW} x {TargetH}（比 {(float)TargetW / TargetH:F3}）\n" +
                      $"ビルドの既定: {PlayerSettings.defaultScreenWidth} x {PlayerSettings.defaultScreenHeight}\n" +
                      $"仮想の舞台: {BBB.Runtime.SafeStage.StageW} x {BBB.Runtime.SafeStage.StageH}" +
                      $"（比 {BBB.Runtime.SafeStage.StageW / BBB.Runtime.SafeStage.StageH:F3}）\n" +
                      "舞台は端末に合わせて縮小され、外側は背景だけが見える");
        }

        // ------------------------------------------------------------------
        // Game ビューの一覧は公開 API が無いので、リフレクションで触る。
        // Unity の版が変わって届かなくなっても、落ちずに警告で済むようにしてある。
        // ------------------------------------------------------------------
        private static bool EnsureSize(string name, int w, int h, out int index)
        {
            index = -1;
            try
            {
                var asm = typeof(Editor).Assembly;
                var sizesType = asm.GetType("UnityEditor.GameViewSizes");
                var groupType = asm.GetType("UnityEditor.GameViewSizeGroup");
                var sizeType = asm.GetType("UnityEditor.GameViewSize");
                var sizeTypeEnum = asm.GetType("UnityEditor.GameViewSizeType");
                if (sizesType == null || groupType == null || sizeType == null || sizeTypeEnum == null) return false;

                var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
                var instance = singleton.GetProperty("instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                if (instance == null) return false;
                var group = sizesType.GetProperty("currentGroup", BindingFlags.Public | BindingFlags.Instance)?.GetValue(instance);
                if (group == null) return false;

                // すでに同じ名前があればそれを使う
                var displayTexts = (string[])groupType.GetMethod("GetDisplayTexts")?.Invoke(group, null);
                if (displayTexts != null)
                {
                    for (int i = 0; i < displayTexts.Length; i++)
                        if (displayTexts[i] != null && displayTexts[i].StartsWith(name, StringComparison.Ordinal))
                        {
                            index = i;
                            return true;
                        }
                }

                var ctor = sizeType.GetConstructors()
                    .FirstOrDefault(c => c.GetParameters().Length == 4);
                if (ctor == null) return false;
                var fixedRes = Enum.Parse(sizeTypeEnum, "FixedResolution");
                var newSize = ctor.Invoke(new object[] { fixedRes, w, h, name });
                groupType.GetMethod("AddCustomSize")?.Invoke(group, new[] { newSize });

                displayTexts = (string[])groupType.GetMethod("GetDisplayTexts")?.Invoke(group, null);
                if (displayTexts == null) return false;
                for (int i = 0; i < displayTexts.Length; i++)
                    if (displayTexts[i] != null && displayTexts[i].StartsWith(name, StringComparison.Ordinal))
                    {
                        index = i;
                        return true;
                    }
                return false;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Game ビューの一覧を触れなかった: " + e.Message);
                return false;
            }
        }

        private static bool Select(int index)
        {
            try
            {
                var gameViewType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
                if (gameViewType == null) return false;
                var views = Resources.FindObjectsOfTypeAll(gameViewType);
                if (views == null || views.Length == 0) return false;
                var prop = gameViewType.GetProperty("selectedSizeIndex",
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (prop == null) return false;
                foreach (var v in views)
                {
                    prop.SetValue(v, index);
                    ((EditorWindow)v).Repaint();
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Game ビューを選べなかった: " + e.Message);
                return false;
            }
        }
    }
}
