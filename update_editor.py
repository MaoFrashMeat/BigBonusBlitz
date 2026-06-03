import sys

def modify_editor():
    with open('workflow_editor.html', 'r', encoding='utf-8') as f:
        content = f.read()
    
    init_prob = """let tier2Probabilities = {
    "HAZE": 18536,
    "BB_A": 0, "BB_B": 0, "BB_C": 0, "BB_D": 0,
    "RB_A": 0, "RB_B": 0,
    "REPLAY_A": 2000, "REPLAY_B": 2000, "REPLAY_C": 1000,
    "BELL_A": 20000, "BELL_B": 5000, "BELL_C": 5000,
    "CHERRY_A": 2000, "CHERRY_B": 2000, "CHERRY_C": 1000,
    "SUICA_A": 2000, "SUICA_B": 2000, "SUICA_C": 1000,
    "CHANCE_A": 500, "CHANCE_B": 500, "CHANCE_C": 500, "CHANCE_D": 500
};"""
    content = content.replace('let currentRole = ROLES[0].key;', f'let currentRole = ROLES[0].key;\n{init_prob}')
    
    old_load = 'const fn = new Function(text + \'; return typeof WORKFLOW_CONFIG!=="undefined"?WORKFLOW_CONFIG:null;\');\n        const loaded = fn();\n        if (loaded) mergeLoaded(loaded);'
    new_load = 'const fn = new Function(text + \'; return { w: typeof WORKFLOW_CONFIG!=="undefined"?WORKFLOW_CONFIG:null, p: typeof PROBABILITIES_TIER2!=="undefined"?PROBABILITIES_TIER2:null };\');\n        const loaded = fn();\n        if (loaded && loaded.w) mergeLoaded(loaded.w);\n        if (loaded && loaded.p) { for (let k in loaded.p) tier2Probabilities[k] = loaded.p[k]; }'
    content = content.replace(old_load, new_load)
    
    old_gen = 'let WORKFLOW_CONFIG = ${JSON.stringify(workflow, null, 4)};\\n`;'
    new_gen = 'let WORKFLOW_CONFIG = ${JSON.stringify(workflow, null, 4)};\\n\\n// Tier2 滞在中のスロット小役確率 (分母 65536)\\nlet PROBABILITIES_TIER2 = ${JSON.stringify(tier2Probabilities, null, 4)};\\n`;'
    content = content.replace(old_gen, new_gen)
    
    old_rsb = 'sb.appendChild(tab);\n    }'
    new_rsb = 'sb.appendChild(tab);\n    }\n    \n    // Tier2 Probs Tab\n    sb.innerHTML += \'<div class="sidebar-lbl" style="margin-top:10px;">スロット設定</div>\';\n    const t2Tab = document.createElement(\'div\');\n    t2Tab.className = `role-tab ${currentRole === \'TIER2_PROBS\' ? \'active\' : \'\'}`;\n    t2Tab.innerHTML = `<span class="rn" style="color:#f59e0b">🎰 Tier2小役確率</span><span class="rt">確率調整</span>`;\n    t2Tab.addEventListener(\'click\', () => { currentRole = \'TIER2_PROBS\'; renderAll(); });\n    sb.appendChild(t2Tab);'
    content = content.replace(old_rsb, new_rsb)
    
    old_rmain = 'function renderMain() {\n    const role    = ROLES.find(r => r.key === currentRole);'
    new_rmain = """function renderMain() {
    if (currentRole === 'TIER2_PROBS') {
        renderTier2Probs();
        return;
    }
    const role    = ROLES.find(r => r.key === currentRole);"""
    content = content.replace(old_rmain, new_rmain)
    
    tier2_func = """
function renderTier2Probs() {
    const main = document.getElementById('main');
    
    let totalAssigned = 0;
    const keys = ['REPLAY_A', 'REPLAY_B', 'REPLAY_C', 'BELL_A', 'BELL_B', 'BELL_C', 'CHERRY_A', 'CHERRY_B', 'CHERRY_C', 'SUICA_A', 'SUICA_B', 'SUICA_C', 'CHANCE_A', 'CHANCE_B', 'CHANCE_C', 'CHANCE_D'];
    keys.forEach(k => totalAssigned += (tier2Probabilities[k] || 0));
    const bonusKeys = ['BB_A', 'BB_B', 'BB_C', 'BB_D', 'RB_A', 'RB_B'];
    bonusKeys.forEach(k => totalAssigned += (tier2Probabilities[k] || 0));

    tier2Probabilities.HAZE = Math.max(0, 65536 - totalAssigned);
    let html = `<div class="role-hdr">
        <div class="role-badge" style="color:#f59e0b;border-color:#f59e0b30;background:#f59e0b12;">
            🎰 Tier2小役確率設定
        </div>
        <div class="role-sub">Tier2滞在中のスロット役の成立確率を設定します（分母: 65536）</div>
    </div>`;

    html += `<div class="total-wrap">
        <div style="font-size:13px;color:var(--muted);min-width:120px;">全役合計: ${totalAssigned} / 65536</div>
        <div class="total-track">
            <div class="total-fill" style="width:${Math.min((totalAssigned/65536)*100, 100)}%;background:#f59e0b;"></div>
        </div>
        <div class="total-lbl" style="color:#10b981;font-size:12px;">ハズレ確率: ${tier2Probabilities.HAZE} (${(tier2Probabilities.HAZE/65536*100).toFixed(1)}%)</div>
    </div>`;

    const groups = [
        { label: 'リプレイ (REPLAY)', keys: ['REPLAY_A', 'REPLAY_B', 'REPLAY_C'], color: '#60a5fa' },
        { label: 'ベル (BELL)', keys: ['BELL_A', 'BELL_B', 'BELL_C'], color: '#fbbf24' },
        { label: 'チェリー (CHERRY)', keys: ['CHERRY_A', 'CHERRY_B', 'CHERRY_C'], color: '#f87171' },
        { label: 'スイカ (SUICA)', keys: ['SUICA_A', 'SUICA_B', 'SUICA_C'], color: '#34d399' },
        { label: 'チャンス (CHANCE)', keys: ['CHANCE_A', 'CHANCE_B', 'CHANCE_C', 'CHANCE_D'], color: '#c084fc' },
    ];

    html += `<div style="display:flex; flex-direction:column; gap:10px;">`;
    for (const g of groups) {
        html += `<div class="section" style="padding:10px 14px;">
            <div style="font-weight:700; color:${g.color}; margin-bottom:8px;">${g.label}</div>
            <div style="display:grid; grid-template-columns:repeat(auto-fit, minmax(150px, 1fr)); gap:10px;">`;
        for (const k of g.keys) {
            html += `
                <div style="display:flex; flex-direction:column; gap:4px;">
                    <div style="font-size:11px; color:var(--muted);">${k}</div>
                    <div class="v-input-box">
                        <input type="number" class="t2p-input" data-key="${k}" value="${tier2Probabilities[k] || 0}" min="0" max="65536" step="1"
                            style="width:100%; padding:5px 8px; background:var(--bg-input); border:1px solid var(--border); border-radius:5px; color:var(--text); font-size:13px; text-align:right;">
                    </div>
                </div>`;
        }
        html += `</div></div>`;
    }
    html += `</div>`;
    main.innerHTML = html;

    main.querySelectorAll('.t2p-input').forEach(el => {
        el.addEventListener('input', () => {
            let v = parseInt(el.value, 10);
            if (isNaN(v) || v < 0) v = 0;
            tier2Probabilities[el.dataset.key] = v;
        });
        el.addEventListener('blur', () => renderAll());
        el.addEventListener('focus', () => el.select());
    });
}
"""
    content = content.replace('/* =====================================================\n   合計表示だけを軽量更新（入力欄はそのまま）', tier2_func + '\n/* =====================================================\n   合計表示だけを軽量更新（入力欄はそのまま）')

    with open('workflow_editor.html', 'w', encoding='utf-8') as f:
        f.write(content)

modify_editor()
