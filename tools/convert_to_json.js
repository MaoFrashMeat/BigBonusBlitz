// settings.js / workflow.js / enemy_tables.js → Unity 用 JSON 変換
// 使い方: node tools/convert_to_json.js
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const root = path.resolve(__dirname, '..');
const outDir = path.join(root, 'UnityProject/BigBonusBlitz/Assets/Resources/Data');

function loadGlobals(file) {
    const src = fs.readFileSync(path.join(root, file), 'utf8');
    const ctx = {};
    vm.createContext(ctx);
    // let/const をグローバルに出すため var に置換
    vm.runInContext(src.replace(/^\s*(let|const)\s+/gm, 'var '), ctx, { filename: file });
    return ctx;
}

const s = loadGlobals('settings.js');
const w = loadGlobals('workflow.js');
const e = loadGlobals('enemy_tables.js');

const C = s.CONFIG;
const gameConfig = {
    payouts: C.payouts,
    payouts_BB: C.payouts_BB,
    payouts_RB: C.payouts_RB,
    timings: C.timings,
    reelStrips: C.reelStrips,
    probabilities_A: C.probabilities_A,
    probabilities_B: C.probabilities_B,
    probabilities_C: C.probabilities_C,
    probabilities_D: C.probabilities_D,
    probabilities_BB: C.probabilities_BB,
    probabilities_RB: C.probabilities_RB,
    probabilities_Tier2: w.PROBABILITIES_TIER2,
    ceilings: C.ceilings,
    modeTransitions: C.modeTransitions,
    // main.js: 完全告知は3G（UI表示のフォールバックは20だが決着ロジックは3）
    tier2MaxSpins: C.tier2MaxSpins || 3,
    expPerDefeat: 50,
    defaultHintConfig: { appearanceRate: 70, distribution: { redGlow: 40, textOnly: 30, both: 30 } }
};

fs.mkdirSync(outDir, { recursive: true });
const write = (name, obj) => {
    fs.writeFileSync(path.join(outDir, name), JSON.stringify(obj, null, 2) + '\n', 'utf8');
    console.log('wrote', name);
};
write('game_config.json', gameConfig);
write('workflow_config.json', w.WORKFLOW_CONFIG);
write('enemy_tables.json', { tables: e.ENEMY_ENGAGE_TABLES });

// 分母チェック
const check = (label, t) => {
    const sum = Object.values(t).reduce((a, b) => a + b, 0);
    if (sum !== 65536) console.warn(`WARN ${label}: sum=${sum} (≠65536)`);
};
for (const k of ['A', 'B', 'C', 'D', 'BB', 'RB']) {
    const p = gameConfig['probabilities_' + k];
    for (const st of Object.keys(p)) check(`${k}[${st}]`, p[st]);
}
check('Tier2', gameConfig.probabilities_Tier2);
for (const t of e.ENEMY_ENGAGE_TABLES) check(`enemy ${t.id}`, t.tier2Probabilities);
