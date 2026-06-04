import re

def create_new_editor():
    with open('editor.html', 'r', encoding='utf-8') as f:
        html = f.read()

    # Find the target to replace
    h3_idx = html.find('<h3 class="section-title" style="margin-top:30px;">確率設定（各役の当選数 / 65536）</h3>')
    
    # We want to remove the duplicate tabs right before it.
    start_idx = html.rfind('<div class="tabs"', 0, h3_idx)
    
    end_str = '            </div> <!-- end tab-prob -->'
    end_idx = html.find(end_str) + len(end_str)
    
    new_html = """
            <h3 class="section-title" style="margin-top:30px;">確率設定（各役の当選数 / 65536）</h3>
            
            <style> .desc-span { font-size:12px; color:#aaa; font-family:monospace; text-align:right; width: 110px; } </style>
            
            <!-- ボーナス設定 -->
            <div id="bonus-container" style="background: rgba(0,0,0,0.2); padding: 15px; border-radius: 5px; margin-top: 15px; border-left: 4px solid #e94560;">
                <h4 style="margin:0 0 10px 0; color:#e94560; font-size: 18px;">モード設定高確率 <span id="total-bonus" style="float:right; font-size:14px; color:#ddd; font-weight:normal;"></span></h4>
                
                <div class="tabs" style="border-bottom: 2px solid #e94560; gap:5px; justify-content:flex-start; margin-bottom:5px;">
                    <button class="bonus-state-btn active" data-state="NORMAL_A" style="background:#e94560; color:#fff; border:none; padding:6px 12px; cursor:pointer; border-radius:5px 5px 0 0; font-weight:bold; opacity:1;">通常A</button>
                    <button class="bonus-state-btn" data-state="NORMAL_B" style="background:#e94560; color:#fff; border:none; padding:6px 12px; cursor:pointer; border-radius:5px 5px 0 0; font-weight:bold; opacity:0.6;">通常B</button>
                    <button class="bonus-state-btn" data-state="NORMAL_C" style="background:#e94560; color:#fff; border:none; padding:6px 12px; cursor:pointer; border-radius:5px 5px 0 0; font-weight:bold; opacity:0.6;">通常C</button>
                    <button class="bonus-state-btn" data-state="NORMAL_D" style="background:#e94560; color:#fff; border:none; padding:6px 12px; cursor:pointer; border-radius:5px 5px 0 0; font-weight:bold; opacity:0.6;">通常D</button>
                </div>
                
                <div class="tabs" style="border-bottom:none; gap:5px; justify-content:flex-start; margin-bottom:15px;">
                    <button class="bonus-setting-btn active" data-setting="1" style="background:#0f3460; color:#fff; border:none; padding:6px 12px; cursor:pointer; border-radius:5px 5px 0 0; font-weight:bold; opacity:1;">設定 1</button>
                    <button class="bonus-setting-btn" data-setting="2" style="background:#0f3460; color:#fff; border:none; padding:6px 12px; cursor:pointer; border-radius:5px 5px 0 0; font-weight:bold; opacity:0.6;">設定 2</button>
                    <button class="bonus-setting-btn" data-setting="3" style="background:#0f3460; color:#fff; border:none; padding:6px 12px; cursor:pointer; border-radius:5px 5px 0 0; font-weight:bold; opacity:0.6;">設定 3</button>
                    <button class="bonus-setting-btn" data-setting="4" style="background:#0f3460; color:#fff; border:none; padding:6px 12px; cursor:pointer; border-radius:5px 5px 0 0; font-weight:bold; opacity:0.6;">設定 4</button>
                    <button class="bonus-setting-btn" data-setting="5" style="background:#0f3460; color:#fff; border:none; padding:6px 12px; cursor:pointer; border-radius:5px 5px 0 0; font-weight:bold; opacity:0.6;">設定 5</button>
                    <button class="bonus-setting-btn" data-setting="6" style="background:#0f3460; color:#fff; border:none; padding:6px 12px; cursor:pointer; border-radius:5px 5px 0 0; font-weight:bold; opacity:0.6;">設定 6</button>
                </div>

                <div style="display:grid; grid-template-columns: 1fr 1fr; gap: 15px;">
                    <div class="row"><label>BB_A (赤7揃い)</label><div style="display:flex; align-items:center; gap:10px;"><span id="descBB_A" class="desc-span"></span><input type="number" class="prob-input" id="probBB_A"></div></div>
                    <div class="row"><label>BB_B (青7揃い)</label><div style="display:flex; align-items:center; gap:10px;"><span id="descBB_B" class="desc-span"></span><input type="number" class="prob-input" id="probBB_B"></div></div>
                    <div class="row"><label>BB_C (BAR揃い中)</label><div style="display:flex; align-items:center; gap:10px;"><span id="descBB_C" class="desc-span"></span><input type="number" class="prob-input" id="probBB_C"></div></div>
                    <div class="row"><label>BB_D (青赤青中)</label><div style="display:flex; align-items:center; gap:10px;"><span id="descBB_D" class="desc-span"></span><input type="number" class="prob-input" id="probBB_D"></div></div>
                    <div class="row"><label>RB_A (赤赤BAR)</label><div style="display:flex; align-items:center; gap:10px;"><span id="descRB_A" class="desc-span"></span><input type="number" class="prob-input" id="probRB_A"></div></div>
                    <div class="row"><label>RB_B (青青BAR)</label><div style="display:flex; align-items:center; gap:10px;"><span id="descRB_B" class="desc-span"></span><input type="number" class="prob-input" id="probRB_B"></div></div>
                </div>
            </div>

            <!-- 小役設定 -->
            <div id="small-container" style="background: rgba(0,0,0,0.2); padding: 15px; border-radius: 5px; margin-top: 25px; border-left: 4px solid #00d2ff;">
                <h4 style="margin:0 0 10px 0; color:#00d2ff; font-size: 18px;">小役、レア役確率</h4>
                
                <div class="tabs" style="border-bottom: 2px solid #00d2ff; gap:5px; justify-content:flex-start; margin-bottom:5px;">
                    <button class="small-state-btn active" data-state="NORMAL_A" style="background:#00d2ff; color:#000; border:none; padding:6px 12px; cursor:pointer; border-radius:5px 5px 0 0; font-weight:bold; opacity:1;">通常A</button>
                    <button class="small-state-btn" data-state="NORMAL_B" style="background:#00d2ff; color:#000; border:none; padding:6px 12px; cursor:pointer; border-radius:5px 5px 0 0; font-weight:bold; opacity:0.6;">通常B</button>
                    <button class="small-state-btn" data-state="NORMAL_C" style="background:#00d2ff; color:#000; border:none; padding:6px 12px; cursor:pointer; border-radius:5px 5px 0 0; font-weight:bold; opacity:0.6;">通常C</button>
                    <button class="small-state-btn" data-state="NORMAL_D" style="background:#00d2ff; color:#000; border:none; padding:6px 12px; cursor:pointer; border-radius:5px 5px 0 0; font-weight:bold; opacity:0.6;">通常D</button>
                    <button class="small-state-btn" data-state="BB" style="background:#ffaa00; color:#000; border:none; padding:6px 12px; cursor:pointer; border-radius:5px 5px 0 0; font-weight:bold; opacity:0.6;">BB中</button>
                    <button class="small-state-btn" data-state="RB" style="background:#ffaa00; color:#000; border:none; padding:6px 12px; cursor:pointer; border-radius:5px 5px 0 0; font-weight:bold; opacity:0.6;">RB中</button>
                </div>
                
                <div class="tabs" style="border-bottom:none; gap:5px; justify-content:flex-start; margin-bottom:15px;">
                    <button class="small-setting-btn active" data-setting="1" style="background:#0f3460; color:#fff; border:none; padding:6px 12px; cursor:pointer; border-radius:5px 5px 0 0; font-weight:bold; opacity:1;">設定 1</button>
                    <button class="small-setting-btn" data-setting="2" style="background:#0f3460; color:#fff; border:none; padding:6px 12px; cursor:pointer; border-radius:5px 5px 0 0; font-weight:bold; opacity:0.6;">設定 2</button>
                    <button class="small-setting-btn" data-setting="3" style="background:#0f3460; color:#fff; border:none; padding:6px 12px; cursor:pointer; border-radius:5px 5px 0 0; font-weight:bold; opacity:0.6;">設定 3</button>
                    <button class="small-setting-btn" data-setting="4" style="background:#0f3460; color:#fff; border:none; padding:6px 12px; cursor:pointer; border-radius:5px 5px 0 0; font-weight:bold; opacity:0.6;">設定 4</button>
                    <button class="small-setting-btn" data-setting="5" style="background:#0f3460; color:#fff; border:none; padding:6px 12px; cursor:pointer; border-radius:5px 5px 0 0; font-weight:bold; opacity:0.6;">設定 5</button>
                    <button class="small-setting-btn" data-setting="6" style="background:#0f3460; color:#fff; border:none; padding:6px 12px; cursor:pointer; border-radius:5px 5px 0 0; font-weight:bold; opacity:0.6;">設定 6</button>
                </div>

                <div class="row" style="background: rgba(255,0,0,0.1); margin-bottom: 15px;">
                    <label>ハズレ (※選択中の状態での自動計算)</label>
                    <div style="display:flex; align-items:center; gap:10px;"><span id="descHAZE" class="desc-span"></span><input type="number" class="prob-input" id="probHAZE" readonly style="background:#333; color:#aaa; border-color:#555;"></div>
                </div>

                <div style="display:grid; grid-template-columns: 1fr; gap: 15px;">
                    
                    <!-- 小役 -->
                    <div style="background: rgba(0, 0, 0, 0.2); padding: 10px; border-radius: 5px;">
                        <h4 style="margin:5px 0 10px 0; color:#aaa; font-size: 15px;">【一般小役】<span id="total-koyaku" style="float:right; font-size:14px; color:#ddd; font-weight:normal;"></span></h4>
                        
                        <h5 style="margin:10px 0 5px 0; color:#aaa;">リプレイ<span id="total-replay" style="float:right; font-size:14px; color:#ddd; font-weight:normal;"></span></h5>
                        <div class="row"><label>REPLAY_A (上段)</label><div style="display:flex; align-items:center; gap:10px;"><span id="descREPLAY_A" class="desc-span"></span><input type="number" class="prob-input" id="probREPLAY_A"></div></div>
                        <div class="row"><label>REPLAY_B (斜め)</label><div style="display:flex; align-items:center; gap:10px;"><span id="descREPLAY_B" class="desc-span"></span><input type="number" class="prob-input" id="probREPLAY_B"></div></div>
                        <div class="row"><label>REPLAY_C (下段)</label><div style="display:flex; align-items:center; gap:10px;"><span id="descREPLAY_C" class="desc-span"></span><input type="number" class="prob-input" id="probREPLAY_C"></div></div>

                        <h5 style="margin:10px 0 5px 0; color:#aaa;">ベル (STAR)<span id="total-bell" style="float:right; font-size:14px; color:#ddd; font-weight:normal;"></span></h5>
                        <div class="row"><label>BELL_A (上/斜)</label><div style="display:flex; align-items:center; gap:10px;"><span id="descBELL_A" class="desc-span"></span><input type="number" class="prob-input" id="probBELL_A"></div></div>
                        <div class="row"><label>BELL_B (中段)</label><div style="display:flex; align-items:center; gap:10px;"><span id="descBELL_B" class="desc-span"></span><input type="number" class="prob-input" id="probBELL_B"></div></div>
                        <div class="row"><label>BELL_C (下段)</label><div style="display:flex; align-items:center; gap:10px;"><span id="descBELL_C" class="desc-span"></span><input type="number" class="prob-input" id="probBELL_C"></div></div>
                    </div>
                    
                    <!-- レア役 -->
                    <div style="background: rgba(0, 0, 0, 0.2); padding: 10px; border-radius: 5px;">
                        <h4 style="margin:5px 0 10px 0; color:#ffaa00; font-size: 15px;">【レア役】<span id="total-rare" style="float:right; font-size:14px; color:#ddd; font-weight:normal;"></span></h4>
                        
                        <h5 style="margin:10px 0 5px 0; color:#aaa;">チェリー<span id="total-cherry" style="float:right; font-size:14px; color:#ddd; font-weight:normal;"></span></h5>
                        <div class="row"><label>CHERRY_A (角)</label><div style="display:flex; align-items:center; gap:10px;"><span id="descCHERRY_A" class="desc-span"></span><input type="number" class="prob-input" id="probCHERRY_A"></div></div>
                        <div class="row"><label>CHERRY_B (右中ボ)</label><div style="display:flex; align-items:center; gap:10px;"><span id="descCHERRY_B" class="desc-span"></span><input type="number" class="prob-input" id="probCHERRY_B"></div></div>
                        <div class="row"><label>CHERRY_C (中段)</label><div style="display:flex; align-items:center; gap:10px;"><span id="descCHERRY_C" class="desc-span"></span><input type="number" class="prob-input" id="probCHERRY_C"></div></div>
                        
                        <h5 style="margin:10px 0 5px 0; color:#aaa;">スイカ<span id="total-suica" style="float:right; font-size:14px; color:#ddd; font-weight:normal;"></span></h5>
                        <div class="row"><label>SUICA_A (上/斜)</label><div style="display:flex; align-items:center; gap:10px;"><span id="descSUICA_A" class="desc-span"></span><input type="number" class="prob-input" id="probSUICA_A"></div></div>
                        <div class="row"><label>SUICA_B (中段)</label><div style="display:flex; align-items:center; gap:10px;"><span id="descSUICA_B" class="desc-span"></span><input type="number" class="prob-input" id="probSUICA_B"></div></div>
                        <div class="row"><label>SUICA_C (下段)</label><div style="display:flex; align-items:center; gap:10px;"><span id="descSUICA_C" class="desc-span"></span><input type="number" class="prob-input" id="probSUICA_C"></div></div>
                        
                        <h5 style="margin:10px 0 5px 0; color:#aaa;">チャンス<span id="total-chance" style="float:right; font-size:14px; color:#ddd; font-weight:normal;"></span></h5>
                        <div class="row"><label>CHANCE_A (ス外れ)</label><div style="display:flex; align-items:center; gap:10px;"><span id="descCHANCE_A" class="desc-span"></span><input type="number" class="prob-input" id="probCHANCE_A"></div></div>
                        <div class="row"><label>CHANCE_B (チ外れ)</label><div style="display:flex; align-items:center; gap:10px;"><span id="descCHANCE_B" class="desc-span"></span><input type="number" class="prob-input" id="probCHANCE_B"></div></div>
                        <div class="row"><label>CHANCE_C (リプV)</label><div style="display:flex; align-items:center; gap:10px;"><span id="descCHANCE_C" class="desc-span"></span><input type="number" class="prob-input" id="probCHANCE_C"></div></div>
                    </div>
                </div>
                
                <div class="validation-bar" id="validation-bar">
                    選択中の状態での合計: 0 / 65536
                </div>
            </div>
            </div> <!-- end tab-prob -->
"""

    if start_idx != -1 and end_idx != -1:
        html = html[:start_idx] + new_html + html[end_idx:]

    # Add missing BB / RB probabilities initializations
    target = 'if (!currentConfig.probabilities_D) currentConfig.probabilities_D = JSON.parse(JSON.stringify(currentConfig.probabilities_A));'
    replacement = target + '\n        if (!currentConfig.probabilities_BB) currentConfig.probabilities_BB = JSON.parse(JSON.stringify(currentConfig.probabilities_A));\n        if (!currentConfig.probabilities_RB) currentConfig.probabilities_RB = JSON.parse(JSON.stringify(currentConfig.probabilities_A));'
    html = html.replace(target, replacement)

    # NOW Replace JS logic.
    html = html.replace("let activeSetting = 1;", "let activeSetting = 1;\n        let bonusSetting = 1;\n        let bonusState = 'NORMAL_A';\n        let smallSetting = 1;\n        let smallState = 'NORMAL_A';")
    
    js_start = html.find('function loadCommonUI() {')
    js_end = html.find('function generateCode() {')
    
    new_js = """function loadCommonUI() {
            document.getElementById('timeAutoReel1').value = currentConfig.timings.reel1;
            document.getElementById('timeAutoReel2').value = currentConfig.timings.reel2;
            document.getElementById('timeAutoReel3').value = currentConfig.timings.reel3;
            document.getElementById('timeAutoNext').value = currentConfig.timings.next;
            document.getElementById('timeAutoNextWin').value = currentConfig.timings.nextWin;

            const rl = currentConfig.reelLayout || { translateY: -38, scale: 1.41 };
            document.getElementById('reelLayoutY').value     = rl.translateY;
            document.getElementById('reelLayoutScale').value = rl.scale;
            
            document.getElementById('ceilingA').value = currentConfig.ceilings.A;
            document.getElementById('ceilingB').value = currentConfig.ceilings.B;
            document.getElementById('ceilingC').value = currentConfig.ceilings.C;
            document.getElementById('ceilingD').value = currentConfig.ceilings.D;
        }

        function getProbObj(state, setting) {
            if (state === 'BB') return currentConfig.probabilities_BB[setting];
            if (state === 'RB') return currentConfig.probabilities_RB[setting];
            if (state === 'NORMAL_B') return currentConfig.probabilities_B[setting];
            if (state === 'NORMAL_C') return currentConfig.probabilities_C[setting];
            if (state === 'NORMAL_D') return currentConfig.probabilities_D[setting];
            return currentConfig.probabilities_A[setting];
        }

        function getPayoutsObj(state) {
            if (state === 'BB') return currentConfig.payouts_BB || currentConfig.payouts;
            if (state === 'RB') return currentConfig.payouts_RB || currentConfig.payouts;
            return currentConfig.payouts;
        }

        function loadGeneralSettingUI(settingNum) {
            activeSetting = settingNum;
            let payoutsObj = getPayoutsObj(currentState);
            
            document.getElementById('payoutBig').value = payoutsObj.BIG;
            document.getElementById('payoutReg').value = payoutsObj.REG;
            document.getElementById('payoutStar').value = payoutsObj.STAR;
            document.getElementById('payoutWatermelon').value = payoutsObj.WATERMELON;
            document.getElementById('payoutCherry').value = payoutsObj.CHERRY;
            
            const transInit = currentConfig.modeTransitions.initial[settingNum] || {A:25, B:25, C:25, D:25};
            const transBonus = currentConfig.modeTransitions.bonus[settingNum] || {A:25, B:25, C:25, D:25};
            
            document.getElementById('transInitA').value = transInit.A;
            document.getElementById('transInitB').value = transInit.B;
            document.getElementById('transInitC').value = transInit.C;
            document.getElementById('transInitD').value = transInit.D;
            
            document.getElementById('transBonusA').value = transBonus.A;
            document.getElementById('transBonusB').value = transBonus.B;
            document.getElementById('transBonusC').value = transBonus.C;
            document.getElementById('transBonusD').value = transBonus.D;
        }

        function loadBonusUI() {
            let p = getProbObj(bonusState, bonusSetting);
            
            document.getElementById('probBB_A').value = p.BB_A || 0;
            document.getElementById('probBB_B').value = p.BB_B || 0;
            document.getElementById('probBB_C').value = p.BB_C || 0;
            document.getElementById('probBB_D').value = p.BB_D || 0;
            document.getElementById('probRB_A').value = p.RB_A || 0;
            document.getElementById('probRB_B').value = p.RB_B || 0;
            
            validateTotal();
        }

        function loadSmallUI() {
            let p = getProbObj(smallState, smallSetting);
            
            const smallRoles = [
                'REPLAY_A', 'REPLAY_B', 'REPLAY_C',
                'BELL_A', 'BELL_B', 'BELL_C',
                'CHERRY_A', 'CHERRY_B', 'CHERRY_C',
                'SUICA_A', 'SUICA_B', 'SUICA_C',
                'CHANCE_A', 'CHANCE_B', 'CHANCE_C'
            ];
            for (let f of smallRoles) {
                const el = document.getElementById('prob' + f);
                if (el) el.value = p[f] || 0;
            }
            
            validateTotal();
        }

        function loadSettingUI(settingNum) {
            loadGeneralSettingUI(settingNum);
            loadBonusUI();
            loadSmallUI();
        }

        function saveGeneralInputs() {
            let payoutsObj = {
                BIG: parseInt(document.getElementById('payoutBig').value, 10) || 0,
                REG: parseInt(document.getElementById('payoutReg').value, 10) || 0,
                STAR: parseInt(document.getElementById('payoutStar').value, 10) || 0,
                WATERMELON: parseInt(document.getElementById('payoutWatermelon').value, 10) || 0,
                CHERRY: parseInt(document.getElementById('payoutCherry').value, 10) || 0,
                REPLAY: 3
            };
            
            currentConfig.ceilings = {
                A: parseInt(document.getElementById('ceilingA').value, 10) || 999,
                B: parseInt(document.getElementById('ceilingB').value, 10) || 555,
                C: parseInt(document.getElementById('ceilingC').value, 10) || 333,
                D: parseInt(document.getElementById('ceilingD').value, 10) || 100
            };
            
            currentConfig.modeTransitions.initial[activeSetting] = {
                A: parseInt(document.getElementById('transInitA').value, 10) || 0,
                B: parseInt(document.getElementById('transInitB').value, 10) || 0,
                C: parseInt(document.getElementById('transInitC').value, 10) || 0,
                D: parseInt(document.getElementById('transInitD').value, 10) || 0
            };
            
            currentConfig.modeTransitions.bonus[activeSetting] = {
                A: parseInt(document.getElementById('transBonusA').value, 10) || 0,
                B: parseInt(document.getElementById('transBonusB').value, 10) || 0,
                C: parseInt(document.getElementById('transBonusC').value, 10) || 0,
                D: parseInt(document.getElementById('transBonusD').value, 10) || 0
            };
            
            if (currentState === 'BB') { currentConfig.payouts_BB = payoutsObj; }
            else if (currentState === 'RB') { currentConfig.payouts_RB = payoutsObj; }
            else { currentConfig.payouts = payoutsObj; }
        }

        function saveBonusInputs() {
            let p = getProbObj(bonusState, bonusSetting);
            p.BB_A = parseInt(document.getElementById('probBB_A').value, 10) || 0;
            p.BB_B = parseInt(document.getElementById('probBB_B').value, 10) || 0;
            p.BB_C = parseInt(document.getElementById('probBB_C').value, 10) || 0;
            p.BB_D = parseInt(document.getElementById('probBB_D').value, 10) || 0;
            p.RB_A = parseInt(document.getElementById('probRB_A').value, 10) || 0;
            p.RB_B = parseInt(document.getElementById('probRB_B').value, 10) || 0;
            recalcHazeFor(bonusState, bonusSetting);
        }

        function saveSmallInputs() {
            let p = getProbObj(smallState, smallSetting);
            const smallRoles = [
                'REPLAY_A', 'REPLAY_B', 'REPLAY_C',
                'BELL_A', 'BELL_B', 'BELL_C',
                'CHERRY_A', 'CHERRY_B', 'CHERRY_C',
                'SUICA_A', 'SUICA_B', 'SUICA_C',
                'CHANCE_A', 'CHANCE_B', 'CHANCE_C'
            ];
            for (let f of smallRoles) {
                p[f] = parseInt(document.getElementById('prob' + f).value, 10) || 0;
            }
            recalcHazeFor(smallState, smallSetting);
        }
        
        function saveCurrentSettingInputs() {
            saveGeneralInputs();
            saveBonusInputs();
            saveSmallInputs();
        }
        
        function recalcHazeFor(state, setting) {
            let p = getProbObj(state, setting);
            let sum = 0;
            for (let f of SUB_FLAGS) {
                sum += (p[f] || 0);
            }
            p.HAZE = 65536 - sum;
        }

        function formatProb(val) {
            if (!val || val <= 0) return '0.00% (1/0.0)';
            const pct = ((val / 65536) * 100).toFixed(2);
            const denom = (65536 / val).toFixed(1);
            return `${pct}% (1/${denom})`;
        }

        function validateTotal() {
            let onScreenBonusSum = 0;
            ['BB_A', 'BB_B', 'BB_C', 'BB_D', 'RB_A', 'RB_B'].forEach(f => {
                const val = parseInt(document.getElementById('prob'+f).value, 10) || 0;
                onScreenBonusSum += val;
                document.getElementById('desc'+f).textContent = formatProb(val);
            });
            
            let onScreenSmallSum = 0;
            const smallRoles = [
                'REPLAY_A', 'REPLAY_B', 'REPLAY_C',
                'BELL_A', 'BELL_B', 'BELL_C',
                'CHERRY_A', 'CHERRY_B', 'CHERRY_C',
                'SUICA_A', 'SUICA_B', 'SUICA_C',
                'CHANCE_A', 'CHANCE_B', 'CHANCE_C'
            ];
            
            let totalReplay=0, totalBell=0, totalCherry=0, totalSuica=0, totalChance=0;
            
            smallRoles.forEach(f => {
                const val = parseInt(document.getElementById('prob'+f).value, 10) || 0;
                onScreenSmallSum += val;
                document.getElementById('desc'+f).textContent = formatProb(val);
                
                if (f.startsWith('REPLAY')) totalReplay += val;
                else if (f.startsWith('BELL')) totalBell += val;
                else if (f.startsWith('CHERRY')) totalCherry += val;
                else if (f.startsWith('SUICA')) totalSuica += val;
                else if (f.startsWith('CHANCE')) totalChance += val;
            });
            
            if(document.getElementById('total-bonus')) document.getElementById('total-bonus').textContent = '合算: ' + formatProb(onScreenBonusSum);
            if(document.getElementById('total-replay')) document.getElementById('total-replay').textContent = '合算: ' + formatProb(totalReplay);
            if(document.getElementById('total-bell')) document.getElementById('total-bell').textContent = '合算: ' + formatProb(totalBell);
            if(document.getElementById('total-koyaku')) document.getElementById('total-koyaku').textContent = '合算: ' + formatProb(totalReplay + totalBell);
            if(document.getElementById('total-cherry')) document.getElementById('total-cherry').textContent = '合算: ' + formatProb(totalCherry);
            if(document.getElementById('total-suica')) document.getElementById('total-suica').textContent = '合算: ' + formatProb(totalSuica);
            if(document.getElementById('total-chance')) document.getElementById('total-chance').textContent = '合算: ' + formatProb(totalChance);
            if(document.getElementById('total-rare')) document.getElementById('total-rare').textContent = '合算: ' + formatProb(totalCherry + totalSuica + totalChance);
            
            let pSmallContext = getProbObj(smallState, smallSetting);
            let backgroundBonusSum = 0;
            ['BB_A', 'BB_B', 'BB_C', 'BB_D', 'RB_A', 'RB_B'].forEach(f => {
                backgroundBonusSum += (pSmallContext[f] || 0);
            });
            
            let hazeForSmallContext = 65536 - backgroundBonusSum - onScreenSmallSum;
            
            document.getElementById('probHAZE').value = hazeForSmallContext;
            document.getElementById('descHAZE').textContent = formatProb(hazeForSmallContext);
            
            const payoutBig = parseInt(document.getElementById('payoutBig').value, 10) || 0;
            const payoutReg = parseInt(document.getElementById('payoutReg').value, 10) || 0;
            const payoutStar = parseInt(document.getElementById('payoutStar').value, 10) || 0;
            const payoutWatermelon = parseInt(document.getElementById('payoutWatermelon').value, 10) || 0;
            const payoutCherry = parseInt(document.getElementById('payoutCherry').value, 10) || 0;
            
            let expectedOut = 0;
            smallRoles.forEach(f => {
                const val = parseInt(document.getElementById('prob' + f).value, 10) || 0;
                if (f.startsWith('REPLAY')) expectedOut += val * 3;
                else if (f.startsWith('CHERRY')) expectedOut += val * payoutCherry;
                else if (f.startsWith('SUICA') || f.startsWith('CHANCE')) expectedOut += val * payoutWatermelon;
                else if (f.startsWith('BELL')) expectedOut += val * payoutStar;
            });
            ['BB_A', 'BB_B', 'BB_C', 'BB_D', 'RB_A', 'RB_B'].forEach(f => {
                const val = parseInt(document.getElementById('prob' + f).value, 10) || 0;
                if (f.startsWith('BB')) expectedOut += val * payoutBig;
                else if (f.startsWith('RB')) expectedOut += val * payoutReg;
            });
            
            const rtp = (expectedOut / (65536 * 3)) * 100;
            
            const initTotal = ['A','B','C','D'].reduce((acc, m) => acc + (parseInt(document.getElementById('transInit'+m).value, 10) || 0), 0);
            const bonusTotal = ['A','B','C','D'].reduce((acc, m) => acc + (parseInt(document.getElementById('transBonus'+m).value, 10) || 0), 0);
            
            const elInitTotal = document.getElementById('trans-init-total');
            const elBonusTotal = document.getElementById('trans-bonus-total');
            if (elInitTotal) {
                elInitTotal.textContent = `合計: ${initTotal}%`;
                elInitTotal.style.color = initTotal === 100 ? '#0f0' : '#f55';
            }
            if (elBonusTotal) {
                elBonusTotal.textContent = `合計: ${bonusTotal}%`;
                elBonusTotal.style.color = bonusTotal === 100 ? '#0f0' : '#f55';
            }
            const isModeTransValid = (initTotal === 100) && (bonusTotal === 100);

            if (hazeForSmallContext < 0) {
                validationBar.className = 'validation-bar invalid';
                validationBar.textContent = `エラー: 小役の合計が 65536 を ${Math.abs(hazeForSmallContext)} オーバーしています！ (選択中状態)`;
                btnSave.disabled = true;
            } else if (!isModeTransValid) {
                validationBar.className = 'validation-bar invalid';
                validationBar.textContent = `エラー: モード移行確率の合計が 100% になっていません`;
                btnSave.disabled = true;
            } else {
                validationBar.className = 'validation-bar valid';
                validationBar.textContent = `選択中状態の合計: 65536 / 65536 ｜ 推定機械割: ${rtp.toFixed(2)}%`;
                btnSave.disabled = false;
            }
        }
"""
    if js_start != -1 and js_end != -1:
        html = html[:js_start] + new_js + html[js_end:]

    load_start = html.find("document.querySelectorAll('.state-tab-btn').forEach(btn => {")
    load_end = html.find("});\n        });\n\n        btnOpen.addEventListener")
    
    new_event_listeners = """
            // General State Tabs
            document.querySelectorAll('.state-tab-btn').forEach(btn => {
                btn.addEventListener('click', (e) => {
                    saveGeneralInputs();
                    document.querySelectorAll('.state-tab-btn').forEach(b => {
                        b.style.background = '#0f3460';
                        b.style.opacity = '0.6';
                        b.classList.remove('active');
                    });
                    
                    const clickedState = e.currentTarget.dataset.state;
                    document.querySelectorAll(`.state-tab-btn[data-state="${clickedState}"]`).forEach(b => {
                        b.style.background = '#e94560';
                        b.style.opacity = '1';
                        b.classList.add('active');
                    });
                    
                    currentState = clickedState;
                    loadGeneralSettingUI(activeSetting);
                    validateTotal();
                });
            });

            // Bonus State Tabs
            document.querySelectorAll('.bonus-state-btn').forEach(btn => {
                btn.addEventListener('click', (e) => {
                    saveBonusInputs();
                    document.querySelectorAll('.bonus-state-btn').forEach(b => {
                        b.style.background = '#e94560';
                        b.style.opacity = '0.6';
                        b.classList.remove('active');
                    });
                    e.currentTarget.style.opacity = '1';
                    e.currentTarget.classList.add('active');
                    bonusState = e.currentTarget.dataset.state;
                    loadBonusUI();
                });
            });

            // Bonus Setting Tabs
            document.querySelectorAll('.bonus-setting-btn').forEach(btn => {
                btn.addEventListener('click', (e) => {
                    saveBonusInputs();
                    document.querySelectorAll('.bonus-setting-btn').forEach(b => {
                        b.style.background = '#0f3460';
                        b.style.opacity = '0.6';
                        b.classList.remove('active');
                    });
                    e.currentTarget.style.opacity = '1';
                    e.currentTarget.classList.add('active');
                    bonusSetting = parseInt(e.currentTarget.dataset.setting);
                    loadBonusUI();
                });
            });

            // Small State Tabs
            document.querySelectorAll('.small-state-btn').forEach(btn => {
                btn.addEventListener('click', (e) => {
                    saveSmallInputs();
                    document.querySelectorAll('.small-state-btn').forEach(b => {
                        b.style.opacity = '0.6';
                        b.classList.remove('active');
                    });
                    e.currentTarget.style.opacity = '1';
                    e.currentTarget.classList.add('active');
                    smallState = e.currentTarget.dataset.state;
                    loadSmallUI();
                });
            });

            // Small Setting Tabs
            document.querySelectorAll('.small-setting-btn').forEach(btn => {
                btn.addEventListener('click', (e) => {
                    saveSmallInputs();
                    document.querySelectorAll('.small-setting-btn').forEach(b => {
                        b.style.background = '#0f3460';
                        b.style.opacity = '0.6';
                        b.classList.remove('active');
                    });
                    e.currentTarget.style.opacity = '1';
                    e.currentTarget.classList.add('active');
                    smallSetting = parseInt(e.currentTarget.dataset.setting);
                    loadSmallUI();
                });
            });
"""

    if load_start != -1 and load_end != -1:
        # Avoid the extra brackets!
        html = html[:load_start] + new_event_listeners + html[load_end:].replace('});\n        });\n\n        btnOpen.addEventListener', '});\n        });\n\n        btnOpen.addEventListener')

    with open('editor.html', 'w', encoding='utf-8') as f:
        f.write(html)

if __name__ == '__main__':
    create_new_editor()
