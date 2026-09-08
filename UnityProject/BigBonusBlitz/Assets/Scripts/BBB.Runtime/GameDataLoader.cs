using System.Collections.Generic;
using BBB.Core;
using Newtonsoft.Json;
using UnityEngine;

namespace BBB.Runtime
{
    /// <summary>Resources/Data/*.json を読んで Core のデータ型に変換する。</summary>
    public static class GameDataLoader
    {
        public const string GameConfigPath = "Data/game_config";
        public const string WorkflowPath = "Data/workflow_config";
        public const string EnemyTablesPath = "Data/enemy_tables";

        public static GameConfig LoadGameConfig() =>
            JsonConvert.DeserializeObject<GameConfig>(LoadText(GameConfigPath));

        public static Dictionary<string, WorkflowRole> LoadWorkflow() =>
            JsonConvert.DeserializeObject<Dictionary<string, WorkflowRole>>(LoadText(WorkflowPath));

        public static List<EnemyTable> LoadEnemyTables() =>
            JsonConvert.DeserializeObject<EnemyTableSet>(LoadText(EnemyTablesPath)).tables;

        public static SlotMachine CreateMachine(IRandom rng = null, int setting = 1) =>
            new SlotMachine(LoadGameConfig(), LoadWorkflow(), LoadEnemyTables(), rng, setting);

        private static string LoadText(string path)
        {
            var ta = Resources.Load<TextAsset>(path);
            if (ta == null) throw new System.IO.FileNotFoundException("Resources に無い: " + path);
            return ta.text;
        }
    }
}
