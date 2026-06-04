<script>
        // IndexedDB wrapper for FileHandle caching
        const idb = {
            async open() {
                return new Promise((resolve) => {
                    const req = indexedDB.open('EditorDB', 1);
                    req.onupgradeneeded = e => e.target.result.createObjectStore('store');
                    req.onsuccess = e => resolve(e.target.result);
                });
            },
            async get(key) {
                const db = await this.open();
                return new Promise((resolve) => {
                    const tx = db.transaction('store', 'readonly');
                    const req = tx.objectStore('store').get(key);
                    req.onsuccess = () => resolve(req.result);
                });
            },
            async set(key, val) {
                const db = await this.open();
                return new Promise((resolve) => {
                    const tx = db.transaction('store', 'readwrite');
                    tx.objectStore('store').put(val, key);
                    tx.oncomplete = resolve;
                });
            }
        };

        const SUB_FLAGS = [
            'BB_A', 'BB_B', 'BB_C', 'BB_D',
            'RB_A', 'RB_B',
            'REPLAY_A', 'REPLAY_B', 'REPLAY_C',
            'BELL_A', 'BELL_B', 'BELL_C',
            'CHERRY_A', 'CHERRY_B', 'CHERRY_C',
            'SUICA_A', 'SUICA_B', 'SUICA_C',
            'CHANCE_A', 'CHANCE_B', 'CHANCE_C'
        ];

        let fileHandle;
        let currentConfig = typeof CONFIG !== 'undefined' ? CONFIG : {
            payouts: { BIG: 300, REG: 90, STAR: 8, WATERMELON: 6, CHERRY: 4 },
            timings: { reel1: 1400, reel2: 300, reel3: 300, next: 800, nextWin: 1000 },
            probabilities: {
                1: { HAZE: 65536 }, 2: { HAZE: 65536 }, 3: { HAZE: 65536 },
                4: { HAZE: 65536 }, 5: { HAZE: 65536 }, 6: { HAZE: 65536 }
            },
            reelStrips: [
                ['WATERMELON', 'RED7', 'REPLAY', 'WATERMELON', 'STAR', 'REPLAY', 'STAR', 'STAR', 'BLUE7', 'WATERMELON', 'STAR', 'REPLAY', 'CHERRY', 'REPLAY', 'STAR', 'BAR', 'WATERMELON', 'REPLAY', 'WATERMELON', 'CHERRY'],
                ['STAR', 'RED7', 'CHERRY', 'WATERMELON', 'REPLAY', 'STAR', 'CHERRY', 'REPLAY', 'STAR', 'STAR', 'BAR', 'CHERRY', 'REPLAY', 'STAR', 'CHERRY', 'BLUE7', 'WATERMELON', 'REPLAY', 'STAR', 'CHERRY'],
                ['CHERRY', 'RED7', 'WATERMELON', 'STAR', 'REPLAY', 'WATERMELON', 'BLUE7', 'REPLAY', 'STAR', 'BAR', 'WATERMELON', 'CHERRY', 'REPLAY', 'STAR', 'WATERMELON', 'BLUE7', 'REPLAY', 'STAR', 'WATERMELON', 'STAR']
            ]
        };
        if (!currentConfig.reelStrips) {
            currentConfig.reelStrips = [
                ['WATERMELON', 'RED7', 'REPLAY', 'WATERMELON', 'STAR', 'REPLAY', 'STAR', 'STAR', 'BLUE7', 'WATERMELON', 'STAR', 'REPLAY', 'CHERRY', 'REPLAY', 'STAR', 'BAR', 'WATERMELON', 'REPLAY', 'WATERMELON', 'CHERRY'],
                ['STAR', 'RED7', 'CHERRY', 'WATERMELON', 'REPLAY', 'STAR', 'CHERRY', 'REPLAY', 'STAR', 'STAR', 'BAR', 'CHERRY', 'REPLAY', 'STAR', 'CHERRY', 'BLUE7', 'WATERMELON', 'REPLAY', 'STAR', 'CHERRY'],
                ['CHERRY', 'RED7', 'WATERMELON', 'STAR', 'REPLAY', 'WATERMELON', 'BLUE7', 'REPLAY', 'STAR', 'BAR', 'WATERMELON', 'CHERRY', 'REPLAY', 'STAR', 'WATERMELON', 'BLUE7', 'REPLAY', 'STAR', 'WATERMELON', 'STAR']
            ];
        }
        
        if (!currentConfig.probabilities_A) currentConfig.probabilities_A = currentConfig.probabilities || {};
        if (!currentConfig.probabilities_B) currentConfig.probabilities_B = JSON.parse(JSON.stringify(currentConfig.probabilities_A));
        if (!currentConfig.probabilities_C) currentConfig.probabilities_C = JSON.parse(JSON.stringify(currentConfig.probabilities_A));
        if (!currentConfig.probabilities_D) currentConfig.probabilities_D = JSON.parse(JSON.stringify(currentConfig.probabilities_A));
        if (!currentConfig.probabilities_BB) currentConfig.probabilities_BB = JSON.parse(JSON.stringify(currentConfig.probabilities_A));
        if (!currentConfig.probabilities_RB) currentConfig.probabilities_RB = JSON.parse(JSON.stringify(currentConfig.probabilities_A));
        
        if (!currentConfig.ceilings) {
            currentConfig.ceilings = { A: 999, B: 555, C: 333, D: 100 };
        }
        
        if (!currentConfig.modeTransitions) {
            currentConfig.modeTransitions = {
                initial: { 1: {A:25, B:25, C:25, D:25}, 2: {A:25, B:25, C:25, D:25}, 3: {A:25, B:25, C:25, D:25}, 4: {A:25, B:25, C:25, D:25}, 5: {A:25, B:25, C:25, D:25}, 6: {A:25, B:25, C:25, D:25} },
                bonus: { 1: {A:25, B:25, C:25, D:25}, 2: {A:25, B:25, C:25, D:25}, 3: {A:25, B:25, C:25, D:25}, 4: {A:25, B:25, C:25, D:25}, 5: {A:25, B:25, C:25, D:25}, 6: {A:25, B:25, C:25, D:25} }
            };
        }
        
        let activeSetting = 1;
        let bonusSetting = 1;
        let bonusState = 'NORMAL_A';
        let smallSetting = 1;
        let smallState = 'NORMAL_A';
        let currentState = 'NORMAL_A';
        let clipboardSetting = null;

        const btnOpen = document.getElementById('btn-open');
        const btnSave = document.getElementById('btn-save');
        const editorUi = document.getElementById('editor-ui');
        const statusEl = document.getElementById('status');
        const validationBar = document.getElementById('validation-bar');
        const toastEl = document.getElementById('toast');
        let toastTimeout;
        
        function showToast(msg, isError = false) {
            toastEl.textContent = msg;
            toastEl.style.borderLeftColor = isError ? '#f00' : '#0f0';
            toastEl.classList.add('show');
            clearTimeout(toastTimeout);
            toastTimeout = setTimeout(() => {
                toastEl.classList.remove('show');
            }, 2000);
        }

        const SYMBOL_OPTIONS = ['RED7', 'BLUE7', 'BAR', 'STAR', 'WATERMELON', 'CHERRY', 'REPLAY', 'BLANK'];
        const SYMBOL_IMAGES = {
            'RED7': 'assets/red7.png',
            'BLUE7': 'assets/blue7.png',
            'BAR': 'assets/bar.png',
            'STAR': 'assets/star.png',
            'WATERMELON': 'assets/watermelon.png',
            'CHERRY': 'assets/cherry.png',
            'REPLAY': 'assets/replay.png',
            'BLANK': 'assets/remix.png'
        };
        const REEL_NAMES = ['リールA (左)', 'リールB (中)', 'リールC (右)'];
        
        function loadReelsUI() {
            const grid = document.getElementById('reel-grid');
            grid.innerHTML = '';
            
            for (let r = 0; r < 3; r++) {
                const col = document.createElement('div');
                col.style.background = 'rgba(255,255,255,0.05)';
                col.style.padding = '10px';
                col.style.borderRadius = '5px';
                col.innerHTML = '<h4 style="margin:0 0 10px 0; color:#e94560; text-align:center;">' + REEL_NAMES[r] + '</h4>';
                
                for (let i = 0; i < 20; i++) {
                    const row = document.createElement('div');
                    row.style.display = 'flex';
                    row.style.justifyContent = 'space-between';
                    row.style.alignItems = 'center';
                    row.style.margin = '4px 0';
                    row.style.fontSize = '14px';
                    row.innerHTML = '<label style="font-family: monospace;">' + String(20 - i).padStart(2, '0') + '</label>';
                    
                    const sel = document.createElement('select');
                    sel.style.background = '#0f3460';
                    sel.style.color = '#fff';
                    sel.style.border = '1px solid #e94560';
                    sel.style.borderRadius = '3px';
                    sel.style.padding = '2px';
                    sel.style.width = '90px'; // shrunk slightly to fit image
                    
                    for (const sym of SYMBOL_OPTIONS) {
                        const opt = document.createElement('option');
                        opt.value = sym;
                        // はみ出し対策で表示のみ略語（SUICA）に変更
                        opt.textContent = sym === 'WATERMELON' ? 'SUICA' : sym;
                        sel.appendChild(opt);
                    }
                    
                    const imgPreview = document.createElement('img');
                    imgPreview.style.width = '24px';
                    imgPreview.style.height = '24px';
                    imgPreview.style.objectFit = 'contain';
                    imgPreview.style.marginLeft = '5px';
                    imgPreview.style.background = '#fff';
                    imgPreview.style.borderRadius = '3px';
                    
                    const updateImg = (val) => {
                        if (SYMBOL_IMAGES[val]) {
                            imgPreview.src = SYMBOL_IMAGES[val];
                            imgPreview.style.display = 'block';
                        } else {
                            imgPreview.src = '';
                            imgPreview.style.display = 'none';
                        }
                    };
                    
                    sel.value = currentConfig.reelStrips[r][i] || 'BLANK';
                    updateImg(sel.value);
                    
                    sel.addEventListener('change', (e) => {
                        currentConfig.reelStrips[r][i] = e.target.value;
                        updateImg(e.target.value);
                        validateReels();
                    });
                    
                    const wrap = document.createElement('div');
                    wrap.style.display = 'flex';
                    wrap.style.alignItems = 'center';
                    wrap.appendChild(sel);
                    wrap.appendChild(imgPreview);
                    
                    row.appendChild(wrap);
                    col.appendChild(row);
                }
                grid.appendChild(col);
            }
            validateReels();
        }
        
        function validateReels() {
            const warningEl = document.getElementById('reel-warning');
            let warnings = [];
            
            // Replay and Bell/Star must be pullable within 4 frames (max distance 5)
            const checkSymbols = ['REPLAY', 'STAR'];
            
            for (let r = 0; r < 3; r++) {
                const strip = currentConfig.reelStrips[r];
                for (const targetSym of checkSymbols) {
                    let indices = [];
                    for (let i = 0; i < 20; i++) {
                        if (strip[i] === targetSym) {
                            indices.push(i);
                        } else if (r === 0 && targetSym === 'REPLAY' && strip[i] === 'RED7') {
                            indices.push(i);
                        } else if (r === 0 && targetSym === 'STAR' && strip[i] === 'BAR') {
                            indices.push(i);
                        }
                    }
                    
                    if (indices.length === 0) {
                        warnings.push(REEL_NAMES[r] + ' に ' + targetSym + ' が一つもありません。');
                        continue;
                    }
                    
                    for (let j = 0; j < indices.length; j++) {
                        const nextIdx = (j + 1) % indices.length;
                        let gap = indices[nextIdx] - indices[j];
                        if (gap <= 0) gap += 20; // wrap around
                        
                        if (gap > 5) {
                            warnings.push(REEL_NAMES[r] + ' の ' + targetSym + ' の間隔が ' + (gap-1) + ' コマ空いている箇所があります。（4コマ滑りでは取りこぼす位置が存在します）');
                            break; 
                        }
                    }
                }
            }
            
            if (warnings.length > 0) {
                warningEl.style.display = 'block';
                warningEl.innerHTML = '<strong>⚠️ 配置警告</strong><br>' + warnings.join('<br>');
            } else {
                warningEl.style.display = 'none';
            }
        }

        window.addEventListener('load', async () => {
            
            document.querySelectorAll('.main-tab-btn').forEach(btn => {
                btn.addEventListener('click', (e) => {
                    document.querySelectorAll('.main-tab-btn').forEach(b => {
                        b.style.background = '#0f3460';
                        b.classList.remove('active');
                    });
                    document.querySelectorAll('.tab-content').forEach(c => {
                        c.style.display = 'none';
                        c.classList.remove('active');
                    });
                    
                    e.target.style.background = '#ff477e';
                    e.target.classList.add('active');
                    
                    const targetId = e.target.dataset.target;
                    const targetEl = document.getElementById(targetId);
                    targetEl.style.display = 'block';
                    targetEl.classList.add('active');
                });
            });

            loadCommonUI();
            loadSettingUI(1);
            loadReelsUI();
            editorUi.style.display = 'block';

            const savedHandle = await idb.get('settingsFileHandle');
            if (savedHandle) {
                fileHandle = savedHandle;
                btnOpen.textContent = '🔄 紐付け済み';
                btnOpen.style.background = '#0a0';
            }
            
            
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
        });

        btnOpen.addEventListener('click', async () => {
            try {
                [fileHandle] = await window.showOpenFilePicker({
                    types: [{ description: 'JavaScript Files', accept: {'text/javascript': ['.js']} }]
                });
                await idb.set('settingsFileHandle', fileHandle);
                statusEl.textContent = 'ファイルが紐付けられました！以後はファイル選択不要です。';
                statusEl.style.color = '#0ff';
                btnOpen.textContent = `🔄 紐付け済み`;
                btnOpen.style.background = '#0a0';
            } catch (e) { console.error(e); }
        });
        
        function loadCommonUI() {
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
function generateCode() {
            saveCurrentSettingInputs();
            const copyConfig = JSON.parse(JSON.stringify(currentConfig));
            let code = '// パラメータ設定 (CONFIG)\n// このファイルはエディターツールによって自動更新されます\n';
            code += 'let CONFIG = ' + JSON.stringify(copyConfig, null, 4) + ';\n';
            return code;
        }

        btnSave.addEventListener('click', async () => {
            if (!fileHandle) {
                statusEl.textContent = '❌ まず「ファイル紐付け設定」ボタンを押して settings.js を選択してください。';
                statusEl.style.color = '#f00';
                return;
            }
            
            try {
                if ((await fileHandle.queryPermission({ mode: 'readwrite' })) !== 'granted') {
                    if ((await fileHandle.requestPermission({ mode: 'readwrite' })) !== 'granted') {
                        throw new Error('Permission denied');
                    }
                }
                
                currentConfig.timings.reel1 = parseInt(document.getElementById('timeAutoReel1').value, 10);
                currentConfig.timings.reel2 = parseInt(document.getElementById('timeAutoReel2').value, 10);
                currentConfig.timings.reel3 = parseInt(document.getElementById('timeAutoReel3').value, 10);
                currentConfig.timings.next = parseInt(document.getElementById('timeAutoNext').value, 10);
                currentConfig.timings.nextWin = parseInt(document.getElementById('timeAutoNextWin').value, 10);

                // リールレイアウト設定を保存
                if (!currentConfig.reelLayout) currentConfig.reelLayout = {};
                currentConfig.reelLayout.translateY = parseFloat(document.getElementById('reelLayoutY').value) || -38;
                currentConfig.reelLayout.scale      = parseFloat(document.getElementById('reelLayoutScale').value) || 1.41;

                const newCode = generateCode();

                // 書き込み
                const writable = await fileHandle.createWritable();
                await writable.write(newCode);
                await writable.close();

                showToast('保存しました');

            } catch (e) {
                console.error(e);
                showToast('保存に失敗しました', true);
            }
        });
    </script>
</body>
</html>
