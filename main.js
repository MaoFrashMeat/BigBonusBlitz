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
let reelStrips = (typeof CONFIG !== 'undefined' && CONFIG.reelStrips) ? CONFIG.reelStrips : [
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
    BB_A: 1, BB_B: 2, BB_C: 3, BB_D: 4,
    RB_A: 5, RB_B: 6,
    REPLAY_A: 7, REPLAY_B: 8, REPLAY_C: 9,
    BELL_A: 10, BELL_B: 11, BELL_C: 12,
    CHERRY_A: 13, CHERRY_B: 14, CHERRY_C: 15,
    SUICA_A: 16, SUICA_B: 17, SUICA_C: 18,
    CHANCE_A: 19, CHANCE_B: 20, CHANCE_C: 21
};

// サブフラグごとの揃い方定義
// validLines: 0=上段, 1=中段, 2=下段, 3=右下がり斜め, 4=右上がり斜め
const WIN_COMBOS = {
    [FLAGS.BB_A]: { symbols: ['RED7', 'RED7', 'RED7'], validLines: [0, 1, 2, 3, 4] },
    [FLAGS.BB_B]: { symbols: ['BLUE7', 'BLUE7', 'BLUE7'], validLines: [0, 1, 2, 3, 4] },
    [FLAGS.BB_C]: { symbols: ['BAR', 'BAR', 'BAR'], validLines: [1] }, // 中段のみ
    [FLAGS.BB_D]: { symbols: ['BLUE7', 'RED7', 'BLUE7'], validLines: [1] }, // 中段のみ
    [FLAGS.RB_A]: { symbols: ['RED7', 'RED7', 'BAR'], validLines: [0, 1, 2, 3, 4] },
    [FLAGS.RB_B]: { symbols: ['BLUE7', 'BLUE7', 'BAR'], validLines: [0, 1, 2, 3, 4] },
    [FLAGS.REPLAY_A]: { symbols: ['REPLAY', 'REPLAY', 'REPLAY'], validLines: [0] }, // 上段
    [FLAGS.REPLAY_B]: { symbols: ['REPLAY', 'REPLAY', 'REPLAY'], validLines: [3, 4] }, // 斜め
    [FLAGS.REPLAY_C]: { symbols: ['REPLAY', 'REPLAY', 'REPLAY'], validLines: [2] }, // 下段
    [FLAGS.BELL_A]: { symbols: ['STAR', 'STAR', 'STAR'], validLines: [0, 3, 4] }, // 上段・斜め
    [FLAGS.BELL_B]: { symbols: ['STAR', 'STAR', 'STAR'], validLines: [1] }, // 中段
    [FLAGS.BELL_C]: { symbols: ['STAR', 'STAR', 'STAR'], validLines: [2] }, // 下段
    [FLAGS.CHERRY_A]: { symbols: ['CHERRY', null, null], validLines: [0, 2] }, // 角
    [FLAGS.CHERRY_B]: { symbols: ['CHERRY', null, ['RED7', 'BLUE7', 'BAR']], validLines: [0, 2] }, // 右中段ボーナス図柄 (ライン判定は特殊対応)
    [FLAGS.CHERRY_C]: { symbols: ['CHERRY', null, null], validLines: [1] }, // 中段
    [FLAGS.SUICA_A]: { symbols: ['WATERMELON', 'WATERMELON', 'WATERMELON'], validLines: [0, 3, 4] }, // 上段・斜め
    [FLAGS.SUICA_B]: { symbols: ['WATERMELON', 'WATERMELON', 'WATERMELON'], validLines: [1] }, // 中段
    [FLAGS.SUICA_C]: { symbols: ['WATERMELON', 'WATERMELON', 'WATERMELON'], validLines: [2] }, // 下段
    [FLAGS.CHANCE_A]: { isReachMe: true, type: 'SUICA_MISS' }, // スイカハズレ
    [FLAGS.CHANCE_B]: { isReachMe: true, type: 'CHERRY_MISS' }, // チェリー付きリーチ目
    [FLAGS.CHANCE_C]: { isReachMe: true, type: 'REPLAY_V' } // リプレイ小V
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
    mode: 'A', // A, B, C, D
    spinCount: 0, // 現在のゲーム数
    currentLotteryTable: [], // 現在設定の16384配列
    autoPlayTimeoutId: null,
    isReplay: false,
    currentFlag: FLAGS.HAZE,
    currentRNG: 0,
    heldBonusFlag: 0, // ボーナスの持ち越しフラグ (0=なし, 5=BIG, 6=REG)
    stoppedSymbols: [null, null, null],
    slipPixels: [null, null, null],
    currentSetting: 1 // 現在の設定 (1-6)
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
        heldBonusFlag: state.heldBonusFlag,
        currentSetting: state.currentSetting
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
            if (savedData.heldBonusFlag !== undefined) {
                state.heldBonusFlag = savedData.heldBonusFlag;
            }
            if (savedData.currentSetting !== undefined) {
                state.currentSetting = savedData.currentSetting;
            }
        } catch (e) {
            console.error('Save data parse error', e);
        }
    }
}

// 16384配列の動的生成
function ensureLotteryTables() {
    const p = CONFIG.probabilities[state.currentSetting];
    if (!p) return;
    let arr = [];
    
    // 全フラグを走査
    const keys = Object.keys(FLAGS);
    for (let k of keys) {
        if (k !== 'HAZE' && p[k]) {
            for (let i = 0; i < p[k]; i++) arr.push(FLAGS[k]);
        }
    }
    
    const hazeCount = p.HAZE !== undefined ? p.HAZE : (16384 - arr.length);
    for (let i = 0; i < hazeCount; i++) arr.push(FLAGS.HAZE);
    
    // シャッフル
    for (let i = arr.length - 1; i > 0; i--) {
        const j = Math.floor(Math.random() * (i + 1));
        [arr[i], arr[j]] = [arr[j], arr[i]];
    }
    state.currentLotteryTable = arr;
}

// DOM要素
const elSlipDisplays = [
    document.getElementById('slip-display-0'),
    document.getElementById('slip-display-1'),
    document.getElementById('slip-display-2')
];
const elCredit = document.getElementById('credit-display');
const elPayout = document.getElementById('payout-display');
const elHeaderCredit = document.getElementById('header-credit');
const elHeaderPayout = document.getElementById('header-payout');
const elMessage = document.getElementById('g-display');
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
const settingSelect = document.getElementById('setting-select');
const btnOptions = document.getElementById('btn-options');
const btnCloseOptions = document.getElementById('btn-close-options');
const optionsModal = document.getElementById('options-modal');
const btnResetState = document.getElementById('btn-reset-state');
const btnAddCredit = document.getElementById('btn-add-credit');

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
    ensureLotteryTables(); // 16384配列を保証
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

    // Setting Selector
    if (settingSelect) {
        settingSelect.value = state.currentSetting || 1;
        settingSelect.addEventListener('change', (e) => {
            initAudio();
            state.currentSetting = parseInt(e.target.value, 10);
            saveGameState();
            ensureLotteryTables(); // 設定変更時にテーブル再生成
        });
    }

    // OPTIONS Modal
    btnOptions.addEventListener('click', () => {
        initAudio();
        optionsModal.classList.remove('hidden');
    });
    btnCloseOptions.addEventListener('click', () => {
        initAudio();
        optionsModal.classList.add('hidden');
    });
    

    // Reset Game State
    if (btnResetState) {
        btnResetState.addEventListener('click', () => {
            if (confirm("クレジットやボーナスなどの状態をすべて初期化しますか？")) {
                localStorage.removeItem(SAVE_KEY);
                state.credit = 50;
                state.bet = 0;
                state.isGameActive = false;
                state.isAutoMode = false;
                if (state.autoPlayTimeoutId) clearTimeout(state.autoPlayTimeoutId);
                state.autoPlayTimeoutId = null;
                state.autoStopTarget = null;
                state.isReplay = false;
                state.currentFlag = FLAGS.HAZE;
                state.heldBonusFlag = 0;
                
                for (let i = 0; i < 3; i++) {
                    if (state.animationIds[i]) {
                        cancelAnimationFrame(state.animationIds[i]);
                        state.animationIds[i] = null;
                    }
                    state.reelsSpinning[i] = false;
                    state.slipPixels[i] = null;
                    btnStops[i].disabled = true;
                    
                    state.reelOffsets[i] = -(REEL_SYMBOLS * SYMBOL_SIZE);
                    updateReelPosition(i);
                    state.stoppedSymbols[i] = null;
                    if (elSlipDisplays[i]) elSlipDisplays[i].textContent = '-';
                }
                
                elMessage.textContent = 'GAME RESET';
                btnMaxBet.disabled = false;
                btnMaxBet.textContent = 'MAX BET (3)';
                btnAuto.classList.remove('btn-auto-active');
                btnAuto.textContent = 'AUTO';
                
                updateLamp();
                updateUI();
                optionsModal.classList.add('hidden');
            }
        });
    }

    // Add Credit
    if (btnAddCredit) {
        btnAddCredit.addEventListener('click', () => {
            initAudio();
            state.credit += 50;
            playSoundBet(); // コイン追加音
            updateUI();
        });
    }

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
function updateUI() {
    elCredit.textContent = state.credit;
    if (elHeaderCredit) elHeaderCredit.textContent = state.credit;
    if (state.isReplay) {
        btnMaxBet.textContent = 'SPIN (REPLAY)';
    } else {
        btnMaxBet.textContent = state.bet === 3 ? 'SPIN (MAX)' : 'MAX BET (3)';
    }
    btnMaxBet.disabled = (state.credit < 3 && !state.isReplay) || state.isGameActive;
    
    // オートモード中のボタン表示
    btnAuto.textContent = state.isAutoMode ? 'AUTO: ON' : 'AUTO';
    
    // G数表示更新
    const gDisp = document.getElementById('g-display');
    if (gDisp) {
        gDisp.textContent = `${state.mode} - ${state.spinCount}G`;
    }
}

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
    lamp.classList.remove('lamp-big', 'lamp-reg');
    if (state.heldBonusFlag >= FLAGS.BB_A && state.heldBonusFlag <= FLAGS.BB_D) {
        lamp.classList.add('lamp-big');
    } else if (state.heldBonusFlag >= FLAGS.RB_A && state.heldBonusFlag <= FLAGS.RB_B) {
        lamp.classList.add('lamp-reg');
    }
}

// モードと天井の定義
const CEILINGS = { 'A': 999, 'B': 555, 'C': 333, 'D': 100 };

// 内部抽選
function drawLottery() {
    // デバッグ用の強制フラグ
    const debugForce = document.getElementById('debug-force-flag');
    if (debugForce && debugForce.value !== "-1") {
        state.currentFlag = parseInt(debugForce.value, 10);
        if ((state.currentFlag >= FLAGS.BB_A && state.currentFlag <= FLAGS.BB_D) || 
            (state.currentFlag >= FLAGS.RB_A && state.currentFlag <= FLAGS.RB_B)) {
            state.heldBonusFlag = state.currentFlag;
        }
        updateLamp();
        return;
    }

    // ボーナス持ち越し中
    if (state.heldBonusFlag) {
        state.currentFlag = state.heldBonusFlag;
        updateLamp();
        return;
    }

    // ゲーム数加算と天井判定
    state.spinCount++;
    const ceiling = CEILINGS[state.mode] || 999;
    if (state.spinCount >= ceiling) {
        // 天井恩恵：BIG:REG = 1:1 （とりあえず BB_A と RB_A をセット）
        let forced = Math.random() < 0.5 ? FLAGS.BB_A : FLAGS.RB_A;
        state.currentFlag = forced;
        state.heldBonusFlag = forced;
        updateLamp();
        return;
    }

    const rng = Math.floor(Math.random() * 16384);
    state.currentRNG = rng + 1; // UI表示用に1~16384とする
    
    if (!state.currentLotteryTable || state.currentLotteryTable.length !== 16384) {
        ensureLotteryTables();
    }
    
    // テーブルからフラグを取得
    state.currentFlag = state.currentLotteryTable[rng];
    
    if (state.currentFlag >= FLAGS.BB_A && state.currentFlag <= FLAGS.BB_D) {
        state.heldBonusFlag = state.currentFlag;
    } else if (state.currentFlag >= FLAGS.RB_A && state.currentFlag <= FLAGS.RB_B) {
        state.heldBonusFlag = state.currentFlag;
    }
    
    updateLamp();
}

function updateDebugUI() {
    const elRng = document.getElementById('debug-rng');
    const elFlag = document.getElementById('debug-flag');
    if (elRng && elFlag) {
        elRng.textContent = state.currentRNG;
        let foundKey = 'UNKNOWN';
        for (const [key, value] of Object.entries(FLAGS)) {
            if (value === state.currentFlag) {
                foundKey = key;
                break;
            }
        }
        elFlag.textContent = foundKey;
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
    elSlipDisplays.forEach(el => { if (el) el.textContent = '-'; });
    
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
    const pixelsPerRotation = REEL_SYMBOLS * SYMBOL_SIZE; // 21コマ * 100px = 2100px
    const durationPerRotation = 780; // 1回転0.78秒 (780ミリ秒)
    const speedPerMs = pixelsPerRotation / durationPerRotation; // 1ミリ秒あたりの移動ピクセル数
    
    let lastTime = performance.now();
    let spinStartTime = lastTime;
    
    function spin(timestamp) {
        if (!state.reelsSpinning[reelIndex]) return; 
        
        let deltaTime = timestamp - lastTime;
        lastTime = timestamp;
        
        // タブ切り替え等で時間が空いた場合の異常なジャンプを防ぐ
        if (deltaTime > 100) deltaTime = 16.67; 
        
        let move = speedPerMs * deltaTime;
        
        // オートプレイ時の目押し（指定時間経過後にセットされたターゲットが近づいたらストップ）
        if (state.isAutoMode && state.autoStopTarget && state.autoStopTarget[reelIndex] !== undefined) {
            const tIdx = state.autoStopTarget[reelIndex];
            
            let idealOffset = -(tIdx - 1) * SYMBOL_SIZE;
            idealOffset = idealOffset % pixelsPerRotation;
            if (idealOffset > 0) idealOffset -= pixelsPerRotation;
            
            let currentOffset = state.reelOffsets[reelIndex] % pixelsPerRotation;
            if (currentOffset > 0) currentOffset -= pixelsPerRotation;
            
            let diff = Math.abs(currentOffset - idealOffset);
            if (diff > pixelsPerRotation / 2) diff = pixelsPerRotation - diff;
            
            // 許容誤差範囲に到達したらストップ（最大4コマ滑るので判定は広めでOK）
            if (diff <= move * 1.5 + 20) {
                delete state.autoStopTarget[reelIndex];
                onStop(reelIndex);
            }
        }
        
        if (state.slipPixels[reelIndex] !== null) {
            if (state.slipPixels[reelIndex] <= move) {
                move = state.slipPixels[reelIndex];
                state.slipPixels[reelIndex] = 0;
            } else {
                state.slipPixels[reelIndex] -= move;
            }
        }
        
        state.reelOffsets[reelIndex] += move;
        if (state.reelOffsets[reelIndex] >= 0) {
            state.reelOffsets[reelIndex] -= pixelsPerRotation;
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
    state.animationIds[reelIndex] = requestAnimationFrame(spin);
}

function checkSlipValidity(reelIndex, testSymbols, flag, stoppedState) {
    let st = [];
    for(let i=0; i<3; i++) {
        if (i === reelIndex) st.push(testSymbols);
        else st.push(stoppedState[i]);
    }

    let isCherryFlag = (flag >= FLAGS.CHERRY_A && flag <= FLAGS.CHERRY_C);
    let hasCherry = testSymbols.includes('CHERRY');

    if (reelIndex === 0) {
        if (!isCherryFlag && hasCherry) return false; // チェリー以外のフラグ時に左リールにチェリーを止めない
    }

    const lines = [
        [st[0]?.[1], st[1]?.[1], st[2]?.[1]], // 0: 中段
        [st[0]?.[0], st[1]?.[0], st[2]?.[0]], // 1: 上段
        [st[0]?.[2], st[1]?.[2], st[2]?.[2]], // 2: 下段
        [st[0]?.[0], st[1]?.[1], st[2]?.[2]], // 3: 右下がり
        [st[0]?.[2], st[1]?.[1], st[2]?.[0]]  // 4: 右上がり
    ];

    let completedWins = [];
    for (let l = 0; l < 5; l++) {
        let line = lines[l];
        if (line[0] && line[1] && line[2]) {
            for (let f in WIN_COMBOS) {
                let combo = WIN_COMBOS[f];
                if (combo.isReachMe) continue;
                
                let match = true;
                for (let r = 0; r < 3; r++) {
                    if (combo.symbols[r] === null) continue;
                    if (Array.isArray(combo.symbols[r])) {
                        if (!combo.symbols[r].includes(line[r])) match = false;
                    } else {
                        if (line[r] !== combo.symbols[r]) match = false;
                    }
                }
                if (match) {
                    if (combo.validLines.includes(l)) {
                        completedWins.push(parseInt(f, 10));
                    }
                }
            }
        }
    }
    
    // ハズレ・リーチ目時に、小役が揃うのはNG（ただし、持ち越しボーナスなら揃ってOK）
    if ((flag === FLAGS.HAZE || (flag >= FLAGS.CHANCE_A && flag <= FLAGS.CHANCE_C)) && completedWins.length > 0) {
        let onlyHeldBonus = true;
        for (let winFlag of completedWins) {
            if (winFlag !== state.heldBonusFlag) {
                onlyHeldBonus = false;
                break;
            }
        }
        if (!onlyHeldBonus) return false;
    }
    
    // 指定フラグ以外が揃うのはNG（ただし、持ち越しボーナスならOK）
    for (let winFlag of completedWins) {
        if (winFlag !== flag && winFlag !== state.heldBonusFlag) return false;
    }
    
    return true;
}

function scoreSlip(reelIndex, testSymbols, flag, stoppedState) {
    if (!checkSlipValidity(reelIndex, testSymbols, flag, stoppedState)) return -1;
    if (flag === FLAGS.HAZE) return 0;
    
    let isCherryFlag = (flag >= FLAGS.CHERRY_A && flag <= FLAGS.CHERRY_C);
    if (isCherryFlag && reelIndex === 0) {
        return testSymbols.includes('CHERRY') ? 100 : 0;
    }
    
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
        [st[0]?.[2], st[1]?.[1], st[0]?.[0]]
    ];

    let combo = WIN_COMBOS[flag];
    if (!combo) return 0;
    
    if (combo.isReachMe) {
        // リーチ目は適当に止める（後で調整可能）
        return 0;
    }

    let score = 0;
    for (let l = 0; l < 5; l++) {
        if (!combo.validLines.includes(l)) continue;
        
        let line = lines[l];
        let targetCount = 0;
        let isPossible = true;
        
        for (let r = 0; r < 3; r++) {
            let target = combo.symbols[r];
            if (target === null) {
                targetCount++;
                continue;
            }
            if (line[r]) {
                if (Array.isArray(target)) {
                    if (target.includes(line[r])) targetCount++;
                    else isPossible = false;
                } else {
                    if (line[r] === target) targetCount++;
                    else isPossible = false;
                }
            }
        }
        
        if (isPossible && targetCount > 0) {
            score += Math.pow(10, targetCount);
        }
    }
    
    if (reelIndex === 0 && !isCherryFlag) {
        let symsToPull = [];
        if (combo.symbols[0]) {
            if (Array.isArray(combo.symbols[0])) symsToPull = combo.symbols[0];
            else symsToPull = [combo.symbols[0]];
        }
        for (let sym of symsToPull) {
            if (testSymbols.includes(sym)) {
                score += 5;
                break;
            }
        }
    }
    
    return score;
}

// ストップボタン処理
function onStop(reelIndex) {
    try {
        if (!state.reelsSpinning[reelIndex]) return;
        btnStops[reelIndex].disabled = true;
        
        let baseIdx = Math.floor(Math.abs(state.reelOffsets[reelIndex]) / SYMBOL_SIZE);
        
        let bestSlip = 0;
        let maxScore = -999;
        
        // スベリ限界は絶対に4コマ（ルール厳守）
        let maxSlip = 4;
        
        for (let k = 0; k <= maxSlip; k++) {
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
        
        if (elSlipDisplays[reelIndex]) {
            elSlipDisplays[reelIndex].textContent = bestSlip;
        }
        
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
    } catch (e) {
        console.error("FATAL ERROR in onStop:", e);
        // フォールバック: 即座に止める
        state.slipPixels[reelIndex] = 0;
        if (elSlipDisplays[reelIndex]) {
            elSlipDisplays[reelIndex].textContent = 0;
        }
        let fallbackIdx = Math.floor(Math.abs(state.reelOffsets[reelIndex]) / SYMBOL_SIZE) % REEL_SYMBOLS;
        state.stoppedSymbols[reelIndex] = [
            reelStrips[reelIndex][fallbackIdx],
            reelStrips[reelIndex][(fallbackIdx + 1) % REEL_SYMBOLS],
            reelStrips[reelIndex][(fallbackIdx + 2) % REEL_SYMBOLS]
        ];
    }
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
    let bonusWon = false;
    
    // チェリー判定 (左リール枠内にチェリーがあれば払い出し)
    let cherryWin = false;
    if (indices[0][0] === 'CHERRY' || indices[0][1] === 'CHERRY' || indices[0][2] === 'CHERRY') {
        cherryWin = true;
    }
    
    for (let l = 0; l < 5; l++) {
        let line = lines[l];
        if (!line[0] || !line[1] || !line[2]) continue;
        
        for (let f in WIN_COMBOS) {
            let combo = WIN_COMBOS[f];
            if (combo.isReachMe) continue;
            if (!combo.validLines.includes(l)) continue;
            
            let match = true;
            for (let r = 0; r < 3; r++) {
                if (combo.symbols[r] === null) continue;
                if (Array.isArray(combo.symbols[r])) {
                    if (!combo.symbols[r].includes(line[r])) match = false;
                } else {
                    if (line[r] !== combo.symbols[r]) match = false;
                }
            }
            
            if (match) {
                let flagId = parseInt(f, 10);
                if (flagId >= FLAGS.BB_A && flagId <= FLAGS.BB_D) {
                    totalPayout += CONFIG.payouts.BIG;
                    bonusWon = true;
                } else if (flagId >= FLAGS.RB_A && flagId <= FLAGS.RB_B) {
                    totalPayout += CONFIG.payouts.REG;
                    bonusWon = true;
                } else if (flagId >= FLAGS.REPLAY_A && flagId <= FLAGS.REPLAY_C) {
                    isReplay = true;
                } else if (flagId >= FLAGS.BELL_A && flagId <= FLAGS.BELL_C) {
                    totalPayout += CONFIG.payouts.STAR;
                } else if (flagId >= FLAGS.SUICA_A && flagId <= FLAGS.SUICA_C) {
                    totalPayout += CONFIG.payouts.WATERMELON;
                }
                // チェリーは個別で判定済みなのでライン判定での加算はしない
            }
        }
    }
    
    if (cherryWin) {
        totalPayout += CONFIG.payouts.CHERRY;
    }
    
    if (bonusWon) {
        state.heldBonusFlag = 0;
        state.spinCount = 0; // ボーナス終了でG数リセット
        
        // モード移行（均等にランダムでA〜Dへ）
        const rnd = Math.random();
        if (rnd < 0.25) state.mode = 'A';
        else if (rnd < 0.5) state.mode = 'B';
        else if (rnd < 0.75) state.mode = 'C';
        else state.mode = 'D';
        
        updateLamp();
    }
    
    if (totalPayout > 0) {
        playSoundWin();
        state.credit += totalPayout;
        elPayout.textContent = totalPayout;
        if (elHeaderPayout) elHeaderPayout.textContent = totalPayout;
        elMessage.textContent = `WIN! +${totalPayout}`;
        elMessage.classList.add('flash');
        setTimeout(() => elMessage.classList.remove('flash'), CONFIG.timings.nextWin);
        if (state.isAutoMode) triggerNextAutoAction();
    } else if (isReplay) {
        playSoundReplay();
        elPayout.textContent = 0;
        if (elHeaderPayout) elHeaderPayout.textContent = 0;
        elMessage.textContent = 'REPLAY!';
        elMessage.classList.add('flash');
        // リプレイはクレジットを減らさずに再度回せる（自動MAX BET状態）
        state.bet = 3;
        state.isReplay = true;
        btnMaxBet.disabled = false;
        btnMaxBet.textContent = 'SPIN (REPLAY)';
        updateUI();
        setTimeout(() => elMessage.classList.remove('flash'), CONFIG.timings.nextWin);
        if (state.isAutoMode) triggerNextAutoAction();
        return; // リプレイ時はここで終了
    } else {
        elPayout.textContent = 0;
        if (elHeaderPayout) elHeaderPayout.textContent = 0;
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
        state.autoStopTarget = null;
    }
}

function triggerNextAutoAction() {
    if (!state.isAutoMode) return;
    
    if (state.autoPlayTimeoutId) {
        clearTimeout(state.autoPlayTimeoutId);
    }
    
    if (state.isGameActive) {
        // 回転中のリールを止める（左から順に1つだけ）
        const spinningIndex = state.reelsSpinning.findIndex((s, idx) => s === true && state.slipPixels[idx] === null);
        if (spinningIndex !== -1) {
            // 回転開始後（第1リール）は設定値1、それ以降は設定値2, 3の間隔でアクションを起こす
            let delay = CONFIG.timings.reel1;
            if (spinningIndex === 1) delay = CONFIG.timings.reel2;
            else if (spinningIndex === 2) delay = CONFIG.timings.reel3;
            
            state.autoPlayTimeoutId = setTimeout(() => {
                if (!state.isAutoMode || !state.reelsSpinning[spinningIndex]) return;
                
                const flag = state.currentFlag;
                const needsMeoshi = (
                    (flag >= FLAGS.BB_A && flag <= FLAGS.BB_D) ||
                    (flag >= FLAGS.RB_A && flag <= FLAGS.RB_B) ||
                    (flag >= FLAGS.CHERRY_A && flag <= FLAGS.CHERRY_C) ||
                    (flag >= FLAGS.SUICA_A && flag <= FLAGS.SUICA_C) ||
                    (flag >= FLAGS.BELL_A && flag <= FLAGS.BELL_C) ||
                    (flag >= FLAGS.REPLAY_A && flag <= FLAGS.REPLAY_C)
                );
                
                if (needsMeoshi) {
                    try {
                        if (!state.autoStopTarget) state.autoStopTarget = {};
                        
                        let combo = WIN_COMBOS[flag];
                        let targets = [];
                        
                        if (combo && combo.symbols && combo.symbols[spinningIndex]) {
                            let sym = combo.symbols[spinningIndex];
                            if (Array.isArray(sym)) {
                                targets = [...sym];
                            } else {
                                targets.push(sym);
                            }
                        }
                        
                        // 既に停止しているリールから絞り込む必要は基本的に無い（WIN_COMBOSでリール毎に指定済みのため）

                        
                        const strip = reelStrips[spinningIndex];
                        let tIdx = -1;
                        if (targets.length > 0) {
                            for (let j = 0; j < strip.length; j++) {
                                if (targets.includes(strip[j])) {
                                    tIdx = j;
                                    break;
                                }
                            }
                        }
                        
                        if (tIdx !== -1) {
                            // 目押しが必要な場合はターゲットをセットし、startSpinningに任せる
                            state.autoStopTarget[spinningIndex] = tIdx;
                            return;
                        }
                    } catch (e) { console.error(e); }
                }
                
                // ハズレや目押し不要な小役（リプレイ、ベル等）は即ストップ
                try { onStop(spinningIndex); } catch(e) { console.error(e); }
                
            }, delay);
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
        }, CONFIG.timings.next);
    }
}

function updateUI() {
    elCredit.textContent = state.credit;
    saveGameState();
}

// ウィンドウサイズに応じたスケール調整
function resizeGame() {
    const gameContainer = document.getElementById('game-container');
    if (!gameContainer) return;
    
    // ベースとなる解像度（元々のCSSの想定サイズ）
    const baseWidth = 770; 
    const baseHeight = 850; 
    
    const windowWidth = window.innerWidth;
    const windowHeight = window.innerHeight;
    
    const scaleX = windowWidth / baseWidth;
    const scaleY = windowHeight / baseHeight;
    // 画面内に収めるためのスケール比率
    const scale = Math.min(scaleX, scaleY) * 0.98; 
    
    gameContainer.style.transform = `scale(${scale})`;
}

window.addEventListener('resize', resizeGame);
window.addEventListener('DOMContentLoaded', resizeGame);
// 初期実行
resizeGame();

// 起動
init();
