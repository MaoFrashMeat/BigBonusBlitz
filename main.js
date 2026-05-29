/**
 * BigBonusBlitz - Main Logic
 */

// 定数定義
const SYMBOL_SIZE = 100; // 1コマの高さ(px)
const REEL_SYMBOLS = 21; // リール1周のコマ数
const SYMBOLS = {
    'RED7': '<img src="assets/red7.png" alt="RED7">',
    'BLUE7': '<img src="assets/blue7.png" alt="BLUE7">',
    'BAR': '<img src="assets/bar.png" alt="BAR">',
    'STAR': '<img src="assets/star.png" alt="STAR">',
    'WATERMELON': '<img src="assets/watermelon.png" alt="WATERMELON">',
    'CHERRY': '<img src="assets/cherry.png" alt="CHERRY">',
    'REPLAY': '<img src="assets/replay.png" alt="REPLAY">',
    'BLANK': ' '
};

// 画像に基づいた21コマ配列
const reelStrips = [
    // 左リール
    ['WATERMELON', 'RED7', 'REPLAY', 'WATERMELON', 'STAR', 'REPLAY', 'STAR', 'STAR', 'BLUE7', 'WATERMELON', 'STAR', 'REPLAY', 'CHERRY', 'REPLAY', 'STAR', 'BAR', 'WATERMELON', 'REPLAY', 'WATERMELON', 'CHERRY', 'STAR'],
    // 中リール
    ['STAR', 'RED7', 'CHERRY', 'WATERMELON', 'REPLAY', 'STAR', 'CHERRY', 'REPLAY', 'STAR', 'STAR', 'BAR', 'CHERRY', 'REPLAY', 'STAR', 'CHERRY', 'BLUE7', 'WATERMELON', 'REPLAY', 'STAR', 'CHERRY', 'REPLAY'],
    // 右リール
    ['CHERRY', 'RED7', 'WATERMELON', 'STAR', 'REPLAY', 'WATERMELON', 'BLUE7', 'REPLAY', 'STAR', 'BAR', 'WATERMELON', 'CHERRY', 'REPLAY', 'STAR', 'WATERMELON', 'BLUE7', 'REPLAY', 'STAR', 'WATERMELON', 'STAR', 'REPLAY']
];

// フラグ定義
const FLAGS = {
    HAZE: 0,
    REPLAY: 1,
    CHERRY: 2,
    WATERMELON: 3,
    STAR: 4,
    BONUS: 5
};

const WIN_SYMBOLS = {
    [FLAGS.REPLAY]: ['REPLAY'],
    [FLAGS.CHERRY]: ['CHERRY'],
    [FLAGS.WATERMELON]: ['WATERMELON'],
    [FLAGS.STAR]: ['STAR'],
    [FLAGS.BONUS]: ['RED7', 'BLUE7', 'BAR']
};

// 状態管理
let state = {
    credit: 50,
    bet: 0,
    reelsSpinning: [false, false, false],
    reelPositions: [0, 0, 0], 
    reelOffsets: [0, 0, 0], 
    animationIds: [null, null, null],
    isGameActive: false,
    isAutoMode: false,
    autoPlayTimeoutId: null,
    isReplay: false,
    currentFlag: FLAGS.HAZE,
    currentRNG: 0,
    heldBonus: false, // ボーナスの持ち越し
    stoppedSymbols: [null, null, null], // [ [top, center, bottom], ... ]
    slipPixels: [null, null, null]
};

// セーブデータ用キー
const SAVE_KEY = 'BigBonusBlitz_Save';

function saveGameState() {
    const bgmSlider = document.getElementById('bgm-volume-slider');
    const seSlider = document.getElementById('se-volume-slider');
    const saveData = {
        credit: state.credit,
        bgmVolume: bgmSlider ? bgmSlider.value : 0.2,
        seVolume: seSlider ? seSlider.value : 0.5,
        heldBonus: state.heldBonus
    };
    localStorage.setItem(SAVE_KEY, JSON.stringify(saveData));
}

function loadGameState() {
    const savedDataStr = localStorage.getItem(SAVE_KEY);
    if (savedDataStr) {
        try {
            const savedData = JSON.parse(savedDataStr);
            if (typeof savedData.credit === 'number') {
                state.credit = savedData.credit;
            }
            if (savedData.bgmVolume !== undefined) {
                const slider = document.getElementById('bgm-volume-slider');
                if (slider) slider.value = savedData.bgmVolume;
            }
            if (savedData.seVolume !== undefined) {
                const slider = document.getElementById('se-volume-slider');
                if (slider) slider.value = savedData.seVolume;
            }
            if (savedData.heldBonus !== undefined) {
                state.heldBonus = savedData.heldBonus;
            }
        } catch (e) {
            console.error('Save data parse error', e);
        }
    }
}

// DOM要素
const elCredit = document.getElementById('credit-display');
const elPayout = document.getElementById('payout-display');
const elMessage = document.getElementById('message-display');
const strips = [
    document.getElementById('strip-left'),
    document.getElementById('strip-center'),
    document.getElementById('strip-right')
];
const btnAuto = document.getElementById('btn-auto');
const btnMaxBet = document.getElementById('btn-maxbet');
const btnStops = [
    document.getElementById('btn-stop-left'),
    document.getElementById('btn-stop-center'),
    document.getElementById('btn-stop-right')
];
const btnPaytable = document.getElementById('btn-paytable');
const btnClosePaytable = document.getElementById('btn-close-paytable');
const paytableModal = document.getElementById('paytable-modal');
const btnBgm = document.getElementById('btn-bgm');
const btnOptions = document.getElementById('btn-options');
const btnCloseOptions = document.getElementById('btn-close-options');
const optionsModal = document.getElementById('options-modal');

// 音声システム（Web Audio API）
let audioCtx = null;
let bgmGain = null;
let seGain = null;

function initAudio() {
    if (audioCtx) {
        if (audioCtx.state === 'suspended') audioCtx.resume();
        return;
    }
    const AudioContext = window.AudioContext || window.webkitAudioContext;
    if (!AudioContext) return;
    
    audioCtx = new AudioContext();
    bgmGain = audioCtx.createGain();
    seGain = audioCtx.createGain();
    bgmGain.connect(audioCtx.destination);
    seGain.connect(audioCtx.destination);
    
    const bgmSlider = document.getElementById('bgm-volume-slider');
    const seSlider = document.getElementById('se-volume-slider');
    
    if (bgmSlider) {
        bgmGain.gain.value = bgmSlider.value;
        bgmSlider.addEventListener('input', (e) => {
            if (bgmGain) bgmGain.gain.value = e.target.value;
            saveGameState();
        });
    }
    
    if (seSlider) {
        seGain.gain.value = seSlider.value;
        seSlider.addEventListener('input', (e) => {
            if (seGain) seGain.gain.value = e.target.value;
            saveGameState();
        });
    }
}

// 基本的なビープ音生成（効果音用）
function playTone(freq, type, duration, vol = 1, slideDown = false) {
    if (!audioCtx || !seGain) return;
    if (audioCtx.state === 'suspended') audioCtx.resume();
    
    const osc = audioCtx.createOscillator();
    const gain = audioCtx.createGain();
    
    osc.type = type;
    osc.frequency.setValueAtTime(freq, audioCtx.currentTime);
    
    // スライドダウン（ガツンという打撃感・重量感の演出）
    if (slideDown) {
        osc.frequency.exponentialRampToValueAtTime(freq * 0.1, audioCtx.currentTime + duration);
    }
    
    // クリックノイズを防ぎつつ、アタックを強くするエンベロープ
    gain.gain.setValueAtTime(vol, audioCtx.currentTime);
    gain.gain.exponentialRampToValueAtTime(0.001, audioCtx.currentTime + duration);
    
    osc.connect(gain);
    gain.connect(seGain);
    
    osc.start();
    osc.stop(audioCtx.currentTime + duration);
}

// 各種効果音のプリセット（全体的に低音域・矩形波/ノコギリ波を使用して機械的な重厚感を出す）
function playSoundBet() { 
    // 重みのあるメダル投入音
    playTone(300, 'square', 0.15, 0.7, true); 
}
function playSoundSpinStart() { 
    // 重いギアが回り始めるような音
    playTone(120, 'sawtooth', 0.3, 0.8, true); 
    setTimeout(() => playTone(80, 'sawtooth', 0.4, 0.6, true), 100);
}
function playSoundStop() { 
    // ガツンと重く止まる音
    playTone(100, 'square', 0.15, 1.0, true); 
}
function playSoundWin() {
    // 勝利ファンファーレも太く響く音色で
    playTone(261.63, 'square', 0.15, 0.6);
    setTimeout(() => playTone(329.63, 'square', 0.15, 0.6), 150);
    setTimeout(() => playTone(392.00, 'square', 0.2, 0.6), 300);
    setTimeout(() => playTone(523.25, 'square', 0.6, 0.8), 450);
}
function playSoundReplay() {
    // リプレイは少し機械的な起動音風
    playTone(300, 'sawtooth', 0.15, 0.6);
    setTimeout(() => playTone(450, 'sawtooth', 0.3, 0.6), 150);
}
function playSoundError() { 
    // ブブーという重いエラー音
    playTone(80, 'sawtooth', 0.4, 1.0, true); 
}

// BGMシーケンサー（Web Audio APIによる自動生成BGM）
let isBgmPlaying = false;
let nextNoteTime = 0;
let currentNote = 0;
let bgmInterval = null;

// Cマイナーペンタトニック的な、少しダークで重厚感のあるループ
const bgmNotes = [
    130.81, 130.81, 155.56, 174.61, 
    196.00, 174.61, 155.56, 130.81,
    103.83, 103.83, 130.81, 155.56,
    174.61, 155.56, 130.81, 103.83
];
const noteDuration = 0.15; // 1音の長さ(秒)

function scheduleBGM() {
    if (!isBgmPlaying || !audioCtx) return;
    
    if (audioCtx.state === 'suspended') audioCtx.resume();

    // タブがバックグラウンドに行った時などにスケジュールが遅延して大量に発音されるのを防ぐ
    if (nextNoteTime < audioCtx.currentTime) {
        nextNoteTime = audioCtx.currentTime + 0.05;
    }

    // 現在時刻より少し先までスケジュールする
    while (nextNoteTime < audioCtx.currentTime + 0.1) {
        const freq = bgmNotes[currentNote];
        playBGMNote(freq, nextNoteTime, noteDuration);
        nextNoteTime += noteDuration;
        currentNote = (currentNote + 1) % bgmNotes.length;
    }
}

function playBGMNote(freq, time, duration) {
    const osc = audioCtx.createOscillator();
    const gain = audioCtx.createGain();
    
    osc.type = 'square'; // スマホ等のスピーカーでも聞こえやすいように倍音の多い矩形波に変更
    osc.frequency.value = freq; // 周波数を戻して聞き取りやすく
    
    // ポップノイズを防ぎつつ、軽く減衰するエンベロープ
    gain.gain.setValueAtTime(0.001, time); // 0だとRampでエラーになるブラウザ対策
    gain.gain.linearRampToValueAtTime(0.4, time + 0.02); // 音量をしっかり上げる
    gain.gain.exponentialRampToValueAtTime(0.01, time + duration - 0.01); // リリース
    
    osc.connect(gain);
    gain.connect(bgmGain);
    
    osc.start(time);
    osc.stop(time + duration);
}

function toggleBGM() {
    initAudio();
    if (isBgmPlaying) {
        isBgmPlaying = false;
        clearInterval(bgmInterval);
        btnBgm.textContent = 'BGM OFF';
        btnBgm.classList.remove('btn-auto-active');
    } else {
        isBgmPlaying = true;
        nextNoteTime = audioCtx.currentTime + 0.1;
        bgmInterval = setInterval(scheduleBGM, 50);
        btnBgm.textContent = 'BGM ON';
        btnBgm.classList.add('btn-auto-active');
    }
}

// 初期化
function init() {
    loadGameState();
    setupReels();
    updateUI();
    updateLamp();
    
    // イベントリスナー
    btnAuto.addEventListener('click', () => { initAudio(); onAutoToggle(); });
    btnMaxBet.addEventListener('click', () => { initAudio(); onMaxBet(); });
    btnStops.forEach((btn, index) => {
        btn.addEventListener('click', () => { initAudio(); onStop(index); });
    });
    
    // PAYTABLE Modal
    btnPaytable.addEventListener('click', () => {
        initAudio();
        paytableModal.classList.remove('hidden');
    });
    btnClosePaytable.addEventListener('click', () => {
        initAudio();
        paytableModal.classList.add('hidden');
    });

    // BGM Toggle
    btnBgm.addEventListener('click', toggleBGM);

    // OPTIONS Modal
    btnOptions.addEventListener('click', () => {
        initAudio();
        optionsModal.classList.remove('hidden');
    });
    btnCloseOptions.addEventListener('click', () => {
        initAudio();
        optionsModal.classList.add('hidden');
    });

    // キーボード操作対応
    window.addEventListener('keydown', (e) => {
        // Space または テンキーの0
        if (e.code === 'Space' || e.code === 'Numpad0') {
            e.preventDefault(); // Spaceキーでの画面スクロールを防止
            initAudio(); // 初回操作対応
            if (!btnMaxBet.disabled) {
                btnMaxBet.click();
            }
        }
        // テンキーの1 (左リール停止)
        if (e.code === 'Numpad1') {
            if (!btnStops[0].disabled) btnStops[0].click();
        }
        // テンキーの2 (中リール停止)
        if (e.code === 'Numpad2') {
            if (!btnStops[1].disabled) btnStops[1].click();
        }
        // テンキーの3 (右リール停止)
        if (e.code === 'Numpad3') {
            if (!btnStops[2].disabled) btnStops[2].click();
        }
    });
}

// リールDOMの生成 (前後に余分に配置してループさせる)
function setupReels() {
    for (let i = 0; i < 3; i++) {
        const strip = strips[i];
        strip.innerHTML = '';
        
        // 描画用に配列を3周分くらい繋げておく（無限スクロール用）
        // 実際は停止位置で調整する
        const arr = reelStrips[i];
        const displayArr = [...arr, ...arr, ...arr]; 
        
        displayArr.forEach(symKey => {
            const div = document.createElement('div');
            div.className = 'symbol';
            div.innerHTML = SYMBOLS[symKey] || symKey; // textContentからinnerHTMLに変更（タグ対応）
            strip.appendChild(div);
        });
        
        // 初期位置（真ん中の配列の先頭を表示）
        state.reelOffsets[i] = -(REEL_SYMBOLS * SYMBOL_SIZE);
        updateReelPosition(i);
    }
}

function updateReelPosition(reelIndex) {
    strips[reelIndex].style.transform = `translateY(${state.reelOffsets[reelIndex]}px)`;
}

// MAX BET処理
function onMaxBet() {
    if (state.isGameActive) return;
    
    if (state.isReplay) {
        state.isReplay = false;
        btnMaxBet.textContent = 'MAX BET (3)';
        btnMaxBet.disabled = true;
        onLever();
        return;
    }

    if (state.credit >= 3) {
        state.bet = 3;
        state.credit -= 3;
        updateUI();
        btnMaxBet.disabled = true;
        playSoundBet();
        onLever();
    } else {
        elMessage.textContent = 'CREDIT NOT ENOUGH';
        playSoundError();
    }
}

// ボーナスランプ更新
function updateLamp() {
    const lamp = document.getElementById('bonus-lamp');
    if (!lamp) return;
    if (state.heldBonus) {
        lamp.classList.add('lamp-on');
    } else {
        lamp.classList.remove('lamp-on');
    }
}

// 内部抽選
function drawLottery() {
    // デバッグ用の強制フラグ
    const debugForce = document.getElementById('debug-force-flag');
    if (debugForce && debugForce.value !== "-1") {
        state.currentFlag = parseInt(debugForce.value, 10);
        if (state.currentFlag === FLAGS.BONUS) state.heldBonus = true;
        updateLamp();
        return;
    }

    // ボーナス持ち越し中
    if (state.heldBonus) {
        state.currentFlag = FLAGS.BONUS;
        // 実機ではリプレイ等の小役と重複するが、ここではボーナス最優先とする
        updateLamp();
        return;
    }

    const rng = Math.floor(Math.random() * 16384) + 1;
    state.currentRNG = rng;
    
    // 確率テーブル
    // REPLAY: 約1/7.3 (2244)
    // CHERRY: 約1/32 (512)
    // WATERMELON: 約1/64 (256)
    // STAR: 約1/128 (128)
    // BONUS: 約1/256 (64)
    
    if (rng <= 2244) state.currentFlag = FLAGS.REPLAY;
    else if (rng <= 2756) state.currentFlag = FLAGS.CHERRY;
    else if (rng <= 3012) state.currentFlag = FLAGS.WATERMELON;
    else if (rng <= 3140) state.currentFlag = FLAGS.STAR;
    else if (rng <= 3204) {
        state.currentFlag = FLAGS.BONUS;
        state.heldBonus = true;
        // 告知音（キュイン等）を鳴らすならここ
    }
    else state.currentFlag = FLAGS.HAZE;

    updateLamp();
}

function updateDebugUI() {
    const elRng = document.getElementById('debug-rng');
    const elFlag = document.getElementById('debug-flag');
    if (elRng && elFlag) {
        elRng.textContent = state.currentRNG;
        const flagNames = ['HAZE', 'REPLAY', 'CHERRY', 'WATERMELON', 'STAR', 'BONUS'];
        elFlag.textContent = flagNames[state.currentFlag] || 'UNKNOWN';
    }
}

// レバーオン（回転開始）
function onLever() {
    if (state.bet === 0) return;
    state.isGameActive = true;
    elMessage.textContent = 'SPINNING...';
    playSoundSpinStart();
    
    drawLottery();
    updateDebugUI();
    
    state.stoppedSymbols = [null, null, null];
    state.slipPixels = [null, null, null];
    
    // 全リール回転
    for (let i = 0; i < 3; i++) {
        state.reelsSpinning[i] = true;
        btnStops[i].disabled = false;
        startSpinning(i);
    }
    
    if (state.isAutoMode) {
        triggerNextAutoAction();
    }
}

// リール回転アニメーション
function startSpinning(reelIndex) {
    const speed = 30; // 1フレームあたりの移動ピクセル
    
    function spin() {
        if (!state.reelsSpinning[reelIndex]) return; 
        
        let move = speed;
        if (state.slipPixels[reelIndex] !== null) {
            if (state.slipPixels[reelIndex] <= speed) {
                move = state.slipPixels[reelIndex];
                state.slipPixels[reelIndex] = 0;
            } else {
                state.slipPixels[reelIndex] -= speed;
            }
        }
        
        state.reelOffsets[reelIndex] += move;
        if (state.reelOffsets[reelIndex] >= 0) {
            state.reelOffsets[reelIndex] -= (REEL_SYMBOLS * SYMBOL_SIZE);
        }
        updateReelPosition(reelIndex);
        
        if (state.slipPixels[reelIndex] === 0) {
            // 完全停止
            state.reelsSpinning[reelIndex] = false;
            state.slipPixels[reelIndex] = null;
            cancelAnimationFrame(state.animationIds[reelIndex]);
            playSoundStop();
            checkAllStopped();
            if (state.isAutoMode && state.reelsSpinning.some(s => s === true)) {
                triggerNextAutoAction();
            }
            return;
        }
        
        state.animationIds[reelIndex] = requestAnimationFrame(spin);
    }
    spin();
}

function checkSlipValidity(reelIndex, testSymbols, flag, stoppedState) {
    let st = [];
    for(let i=0; i<3; i++) {
        if (i === reelIndex) st.push(testSymbols);
        else st.push(stoppedState[i]);
    }
    
    let hasCherry = testSymbols.includes('CHERRY');
    if (reelIndex === 0) {
        if (flag === FLAGS.CHERRY && !hasCherry) return false;
        if (flag !== FLAGS.CHERRY && hasCherry) return false;
    }

    const lines = [
        [st[0]?.[1], st[1]?.[1], st[2]?.[1]],
        [st[0]?.[0], st[1]?.[0], st[2]?.[0]],
        [st[0]?.[2], st[1]?.[2], st[2]?.[2]],
        [st[0]?.[0], st[1]?.[1], st[2]?.[2]],
        [st[0]?.[2], st[1]?.[1], st[0]?.[0]] // Diag Up (fixed) -> [st[0]?.[2], st[1]?.[1], st[2]?.[0]]
    ];
    lines[4] = [st[0]?.[2], st[1]?.[1], st[2]?.[0]];

    let completedWins = [];
    for (let line of lines) {
        if (line[0] && line[1] && line[2]) {
            if (line[0] === line[1] && line[1] === line[2]) {
                completedWins.push(line[0]);
            }
        }
    }
    
    if (flag === FLAGS.HAZE && completedWins.length > 0) return false;
    
    let targetSyms = WIN_SYMBOLS[flag] || [];
    for (let win of completedWins) {
        if (!targetSyms.includes(win)) return false;
    }
    
    return true;
}

function scoreSlip(reelIndex, testSymbols, flag, stoppedState) {
    if (!checkSlipValidity(reelIndex, testSymbols, flag, stoppedState)) return -1; 
    if (flag === FLAGS.HAZE) return 0;
    if (flag === FLAGS.CHERRY && reelIndex === 0) return testSymbols.includes('CHERRY') ? 100 : 0;
    
    let st = [];
    for(let i=0; i<3; i++) {
        if (i === reelIndex) st.push(testSymbols);
        else st.push(stoppedState[i]);
    }
    
    const lines = [
        [st[0]?.[1], st[1]?.[1], st[2]?.[1]],
        [st[0]?.[0], st[1]?.[0], st[2]?.[0]],
        [st[0]?.[2], st[1]?.[2], st[2]?.[2]],
        [st[0]?.[0], st[1]?.[1], st[2]?.[2]],
        [st[0]?.[2], st[1]?.[1], st[2]?.[0]]
    ];

    let targetSyms = WIN_SYMBOLS[flag] || [];
    let score = 0;
    
    for (let line of lines) {
        let targetCount = 0;
        let isPossible = true;
        for (let i=0; i<3; i++) {
            if (line[i]) {
                if (targetSyms.includes(line[i])) targetCount++;
                else isPossible = false;
            }
        }
        if (isPossible && targetCount > 0) {
            score += Math.pow(10, targetCount); 
        }
    }
    return score;
}

// ストップボタン処理
function onStop(reelIndex) {
    if (!state.reelsSpinning[reelIndex]) return;
    btnStops[reelIndex].disabled = true;
    
    let baseIdx = Math.floor(Math.abs(state.reelOffsets[reelIndex]) / SYMBOL_SIZE);
    
    let bestSlip = 0;
    let maxScore = -999;
    
    for (let k = 0; k <= 4; k++) {
        let testIdx = (baseIdx - k + REEL_SYMBOLS) % REEL_SYMBOLS;
        let testSymbols = [
            reelStrips[reelIndex][testIdx],
            reelStrips[reelIndex][(testIdx + 1) % REEL_SYMBOLS],
            reelStrips[reelIndex][(testIdx + 2) % REEL_SYMBOLS]
        ];
        
        let score = scoreSlip(reelIndex, testSymbols, state.currentFlag, state.stoppedSymbols);
        
        if (score > maxScore) {
            maxScore = score;
            bestSlip = k;
        }
    }
    
    // スリップを適用
    let finalIdx = (baseIdx - bestSlip + REEL_SYMBOLS) % REEL_SYMBOLS;
    state.stoppedSymbols[reelIndex] = [
        reelStrips[reelIndex][finalIdx],
        reelStrips[reelIndex][(finalIdx + 1) % REEL_SYMBOLS],
        reelStrips[reelIndex][(finalIdx + 2) % REEL_SYMBOLS]
    ];
    
    let currentAbsOffset = Math.abs(state.reelOffsets[reelIndex]);
    let targetAbsOffset = finalIdx * SYMBOL_SIZE;
    let distance = currentAbsOffset - targetAbsOffset;
    if (distance < 0) {
        distance += REEL_SYMBOLS * SYMBOL_SIZE;
    }
    
    state.slipPixels[reelIndex] = distance;
}

// 全リール停止判定
function checkAllStopped() {
    if (state.reelsSpinning.every(s => s === false)) {
        // 全停止
        evaluateWin();
    }
}

// 役判定
function evaluateWin() {
    state.isGameActive = false;
    btnMaxBet.disabled = false;
    
    // 各リールの表示シンボルインデックスを取得 [top, center, bottom]
    const indices = [];
    for (let i = 0; i < 3; i++) {
        // 浮動小数点誤差を避けるため Math.round を使用
        const topIdx = Math.round(Math.abs(state.reelOffsets[i]) / SYMBOL_SIZE) % REEL_SYMBOLS;
        indices.push([
            reelStrips[i][topIdx],
            reelStrips[i][(topIdx + 1) % REEL_SYMBOLS],
            reelStrips[i][(topIdx + 2) % REEL_SYMBOLS]
        ]);
    }
    
    // ペイライン判定 (3BETなので5ライン有効)
    const lines = [
        [indices[0][1], indices[1][1], indices[2][1]], // Center
        [indices[0][0], indices[1][0], indices[2][0]], // Top
        [indices[0][2], indices[1][2], indices[2][2]], // Bottom
        [indices[0][0], indices[1][1], indices[2][2]], // Diag Down
        [indices[0][2], indices[1][1], indices[2][0]]  // Diag Up
    ];
    
    let totalPayout = 0;
    let isReplay = false;
    
    lines.forEach(line => {
        if (line[0] === line[1] && line[1] === line[2]) {
            const sym = line[0];
            if (sym === 'RED7' || sym === 'BLUE7' || sym === 'BAR') {
                totalPayout += 15; 
                state.heldBonus = false; // ボーナス消化
                updateLamp();
            }
            else if (sym === 'STAR') totalPayout += 10;
            else if (sym === 'WATERMELON') totalPayout += 9; // ディスクはスイカ9枚
            else if (sym === 'CHERRY') totalPayout += 4;
            else if (sym === 'REPLAY') isReplay = true;
        }
    });

    // チェリー判定 (左リールにチェリーが出現すれば払い出し)
    if (indices[0][0] === 'CHERRY' || indices[0][1] === 'CHERRY' || indices[0][2] === 'CHERRY') {
        // パチスロの角チェリー/中段チェリーとして4枚払い出し
        totalPayout += 4;
    }
    
    if (totalPayout > 0) {
        playSoundWin();
        state.credit += totalPayout;
        elPayout.textContent = totalPayout;
        elMessage.textContent = `WIN! +${totalPayout}`;
        elMessage.classList.add('flash');
        setTimeout(() => {
            elMessage.classList.remove('flash');
            if (state.isAutoMode) triggerNextAutoAction();
        }, 2000);
    } else if (isReplay) {
        playSoundReplay();
        elPayout.textContent = 0;
        elMessage.textContent = 'REPLAY!';
        elMessage.classList.add('flash');
        // リプレイはクレジットを減らさずに再度回せる（自動MAX BET状態）
        state.bet = 3;
        state.isReplay = true;
        btnMaxBet.disabled = false;
        btnMaxBet.textContent = 'SPIN (REPLAY)';
        updateUI();
        setTimeout(() => {
            elMessage.classList.remove('flash');
            if (state.isAutoMode) triggerNextAutoAction();
        }, 2000);
        return; // リプレイ時はここで終了
    } else {
        elPayout.textContent = 0;
        elMessage.textContent = 'GAME OVER';
        if (state.isAutoMode) triggerNextAutoAction();
    }
    
    state.bet = 0;
    updateUI();
}

// オートプレイ機能
function onAutoToggle() {
    state.isAutoMode = !state.isAutoMode;
    if (state.isAutoMode) {
        btnAuto.classList.add('btn-auto-active');
        btnAuto.textContent = 'AUTO (ON)';
        if (!state.isGameActive || state.reelsSpinning.some(s => s === true)) {
            triggerNextAutoAction();
        }
    } else {
        btnAuto.classList.remove('btn-auto-active');
        btnAuto.textContent = 'AUTO';
        if (state.autoPlayTimeoutId) {
            clearTimeout(state.autoPlayTimeoutId);
            state.autoPlayTimeoutId = null;
        }
    }
}

function triggerNextAutoAction() {
    if (!state.isAutoMode) return;
    
    if (state.autoPlayTimeoutId) {
        clearTimeout(state.autoPlayTimeoutId);
    }
    
    if (state.isGameActive) {
        // 回転中のリールを止める
        const spinningIndex = state.reelsSpinning.findIndex(s => s === true);
        if (spinningIndex !== -1) {
            state.autoPlayTimeoutId = setTimeout(() => {
                if (state.isAutoMode && state.reelsSpinning[spinningIndex]) {
                    onStop(spinningIndex);
                }
            }, 300); // 各リールの停止間隔
        }
    } else {
        // 次のゲーム開始
        state.autoPlayTimeoutId = setTimeout(() => {
            if (!state.isAutoMode) return;
            
            if (!btnMaxBet.disabled) {
                if (state.isReplay || state.credit >= 3) {
                    onMaxBet();
                } else {
                    // クレジット不足でオート解除
                    onAutoToggle();
                }
            }
        }, 800); // 次のゲームまでの間隔
    }
}

function updateUI() {
    elCredit.textContent = state.credit;
    saveGameState();
}

// 起動
init();
