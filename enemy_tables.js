// エネミーエンゲージ専用テーブル (ENEMY_ENGAGE_TABLES)
// このファイルはエネミーエディターによって自動更新されます

let ENEMY_ENGAGE_TABLES = [
    {
        id: "table_slime_1",
        name: "スライム（基本）",
        enemyType: "slime",
        defeatProbabilities: {
            "BELL": 5,
            "CHERRY": 20,
            "WATERMELON": 40,
            "CHANCE": 100,
            "REPLAY": 1
        },
        tier2Probabilities: {
            "HAZE": 18536,
            "BB_A": 0, "BB_B": 0, "BB_C": 0, "BB_D": 0,
            "RB_A": 0, "RB_B": 0,
            "REPLAY_A": 2000, "REPLAY_B": 2000, "REPLAY_C": 1000,
            "BELL_A": 20000, "BELL_B": 5000, "BELL_C": 5000,
            "CHERRY_A": 2000, "CHERRY_B": 2000, "CHERRY_C": 1000,
            "SUICA_A": 2000, "SUICA_B": 2000, "SUICA_C": 1000,
            "CHANCE_A": 500, "CHANCE_B": 500, "CHANCE_C": 500, "CHANCE_D": 500
        }
    },
    {
        id: "table_goblin_1",
        name: "ゴブリン（強敵）",
        enemyType: "goblin",
        defeatProbabilities: {
            "BELL": 2,
            "CHERRY": 10,
            "WATERMELON": 25,
            "CHANCE": 100,
            "REPLAY": 0
        },
        tier2Probabilities: {
            "HAZE": 18536,
            "BB_A": 0, "BB_B": 0, "BB_C": 0, "BB_D": 0,
            "RB_A": 0, "RB_B": 0,
            "REPLAY_A": 2000, "REPLAY_B": 2000, "REPLAY_C": 1000,
            "BELL_A": 20000, "BELL_B": 5000, "BELL_C": 5000,
            "CHERRY_A": 2000, "CHERRY_B": 2000, "CHERRY_C": 1000,
            "SUICA_A": 2000, "SUICA_B": 2000, "SUICA_C": 1000,
            "CHANCE_A": 500, "CHANCE_B": 500, "CHANCE_C": 500, "CHANCE_D": 500
        }
    },
    {
        id: "table_bat_1",
        name: "コウモリ（ベルチャンス）",
        enemyType: "bat",
        defeatProbabilities: {
            "BELL": 15,
            "CHERRY": 30,
            "WATERMELON": 50,
            "CHANCE": 100,
            "REPLAY": 2
        },
        tier2Probabilities: {
            "HAZE": 18536,
            "BB_A": 0, "BB_B": 0, "BB_C": 0, "BB_D": 0,
            "RB_A": 0, "RB_B": 0,
            "REPLAY_A": 2000, "REPLAY_B": 2000, "REPLAY_C": 1000,
            "BELL_A": 20000, "BELL_B": 5000, "BELL_C": 5000,
            "CHERRY_A": 2000, "CHERRY_B": 2000, "CHERRY_C": 1000,
            "SUICA_A": 2000, "SUICA_B": 2000, "SUICA_C": 1000,
            "CHANCE_A": 500, "CHANCE_B": 500, "CHANCE_C": 500, "CHANCE_D": 500
        }
    }
];
