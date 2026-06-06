import re

with open("d:\\GitHub\\BigBonusBlitz\\main.js", "r", encoding="utf-8") as f:
    content = f.read()

# 1. state definition
content = re.sub(
    r"enemyHP: 100,\s*enemyMaxHP: 100,",
    "activeEnemyTable: null,",
    content
)

# 2. remove dealDamage
content = re.sub(
    r"// ダメージ処理関数\s*function dealDamage\(amount\) \{.*?\n\}\n",
    "",
    content,
    flags=re.DOTALL
)

# 3. spawnEnemy
content = re.sub(
    r"function spawnEnemy\(\) \{\s*const rand = Math\.random\(\);\s*if \(rand < 0\.33\) \{",
    "function spawnEnemy() {\n    if (state.activeEnemyTable) {\n        state.currentEnemyType = state.activeEnemyTable.enemyType;\n    } else {\n        const rand = Math.random();\n        if (rand < 0.33) {",
    content
)
content = re.sub(
    r"    state\.enemyHP = state\.enemyMaxHP;\s*const hpFill = document\.getElementById\('enemy-hp-fill'\);\s*if \(hpFill\) \{\s*hpFill\.style\.width = '100%';\s*hpFill\.style\.backgroundColor = '#0f0';\s*\}",
    "",
    content
)

# 4. drawLottery (Tier2 probs)
content = re.sub(
    r"        let tier2Probs = \(typeof PROBABILITIES_TIER2 !== 'undefined'\) \? PROBABILITIES_TIER2 : CONFIG\.probabilities_Tier2;\n        if \(tier2Probs\) \{",
    "        let tier2Probs = (typeof PROBABILITIES_TIER2 !== 'undefined') ? PROBABILITIES_TIER2 : CONFIG.probabilities_Tier2;\n        if (state.activeEnemyTable && state.activeEnemyTable.tier2Probabilities) {\n            tier2Probs = state.activeEnemyTable.tier2Probabilities;\n        }\n        if (tier2Probs) {",
    content
)

# 5. evaluateWin - add defeat check, remove dealDamage
content = re.sub(
    r"        dealDamage\(totalPayout \* 5\); // 獲得枚数x5をダメージとする",
    "",
    content
)

helper = """
function checkEnemyDefeat(winType) {
    if (!state.isTier2 || !state.activeEnemyTable || !state.enemyActive) return false;
    const prob = state.activeEnemyTable.defeatProbabilities[winType] || 0;
    if (prob > 0 && (Math.random() * 100 < prob)) {
        if (typeof playSoundEnemyDeath === 'function') playSoundEnemyDeath();
        state.enemyActive = false;
        state.isTier2 = false;
        updateTier2UI();
        
        const enemyImgContainer = document.getElementById('enemy-img');
        if (enemyImgContainer) enemyImgContainer.style.visibility = 'hidden';
        if (typeof hideEnemyBanners === 'function') hideEnemyBanners();
        
        if (typeof gainExp === 'function') gainExp(50);
        
        state.heldBonusFlag = Math.random() < 0.5 ? FLAGS.BB_A : FLAGS.RB_A;
        if (typeof updateLamp === 'function') updateLamp();
        
        const elMessage = document.getElementById('message-display');
        if (elMessage) {
            elMessage.textContent = 'ENEMY DEFEATED! BONUS GET!';
            elMessage.classList.add('flash');
            setTimeout(() => elMessage.classList.remove('flash'), 3000);
        }
        return true;
    }
    return false;
}
"""

content = content.replace("function evaluateWin() {", helper + "\nfunction evaluateWin() {")

content = re.sub(
    r"        if \(state\.isAutoMode\) triggerNextAutoAction\(\);\n    \} else if \(isReplay\) \{",
    "        checkEnemyDefeat(winType);\n        if (state.isAutoMode) triggerNextAutoAction();\n    } else if (isReplay) {",
    content
)

content = re.sub(
    r"        setTimeout\(\(\) => elMessage\.classList\.remove\('flash'\), CONFIG\.timings\.nextWin / state\.autoSpeed\);\n        if \(state\.isAutoMode\) triggerNextAutoAction\(\);\n        return; \n    \} else \{",
    "        setTimeout(() => elMessage.classList.remove('flash'), CONFIG.timings.nextWin / state.autoSpeed);\n        checkEnemyDefeat('REPLAY');\n        if (state.isAutoMode) triggerNextAutoAction();\n        return; \n    } else {",
    content
)

# 6. ENEMY category selection
content = re.sub(
    r"    // ENEMYカテゴリが当選→未出現なら右からスライドイン登場\n    if \(state\.lastWflResult\?\.category === 'ENEMY' && !state\.enemyActive\) \{\n        state\.enemyActive = true;\n        state\.pendingTier2 = true; // 次ゲームからTier2\(帯\)開始",
    "    // ENEMYカテゴリが当選→未出現なら右からスライドイン登場\n    if (state.lastWflResult?.category === 'ENEMY' && !state.enemyActive) {\n        state.enemyActive = true;\n        state.pendingTier2 = true; // 次ゲームからTier2(帯)開始\n        if (typeof ENEMY_ENGAGE_TABLES !== 'undefined' && ENEMY_ENGAGE_TABLES.length > 0) {\n            state.activeEnemyTable = ENEMY_ENGAGE_TABLES[Math.floor(Math.random() * ENEMY_ENGAGE_TABLES.length)];\n            state.currentEnemyType = state.activeEnemyTable.enemyType;\n        }\n",
    content
)

with open("d:\\GitHub\\BigBonusBlitz\\main.js", "w", encoding="utf-8") as f:
    f.write(content)

print("Done")
