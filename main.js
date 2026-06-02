/**
 * BigBonusBlitz - Main Logic
 */

// 定数定義
const SYMBOL_SIZE = 100; // 1コマの高さ(px)
const REEL_SYMBOLS = 20; // リール1周のコマ数
const SYMBOLS = {
    'RED7': '<img src="assets/red7.png" alt="RED7">',
    'BLUE7': '<img src="assets/blue7.png" alt="BLUE7">',
    'BAR': '<img src="assets/bar.png" alt="BAR">',
    'STAR': '<img src="assets/star.png" alt="STAR">',
    'WATERMELON': '<img src="assets/watermelon.png" alt="WATERMELON">',
    'CHERRY': '<img src="assets/cherry.png" alt="CHERRY">',
    'REPLAY': '<img src="assets/replay.png" alt="REPLAY">',
    'BLANK': '<img src="assets/remix.png" alt="BLANK">'
};

// 画像に基づいた20コマ配列
let reelStrips = (typeof CONFIG !== 'undefined' && CONFIG.reelStrips) ? CONFIG.reelStrips : [
    // 左リール
    ['WATERMELON', 'RED7', 'REPLAY', 'WATERMELON', 'STAR', 'REPLAY', 'STAR', 'STAR', 'BLUE7', 'WATERMELON', 'STAR', 'REPLAY', 'CHERRY', 'REPLAY', 'STAR', 'BAR', 'WATERMELON', 'REPLAY', 'WATERMELON', 'CHERRY'],
    // 中リール
    ['STAR', 'RED7', 'CHERRY', 'WATERMELON', 'REPLAY', 'STAR', 'CHERRY', 'REPLAY', 'STAR', 'STAR', 'BAR', 'CHERRY', 'REPLAY', 'STAR', 'CHERRY', 'BLUE7', 'WATERMELON', 'REPLAY', 'STAR', 'CHERRY'],
    // 右リール
    ['CHERRY', 'RED7', 'WATERMELON', 'STAR', 'REPLAY', 'WATERMELON', 'BLUE7', 'REPLAY', 'STAR', 'BAR', 'WATERMELON', 'CHERRY', 'REPLAY', 'STAR', 'WATERMELON', 'BLUE7', 'REPLAY', 'STAR', 'WATERMELON', 'STAR']
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

// 役のグループ化定義
const ROLE_GROUPS = {
    SMALL: [FLAGS.REPLAY_A, FLAGS.REPLAY_B, FLAGS.REPLAY_C, FLAGS.BELL_A, FLAGS.BELL_B, FLAGS.BELL_C], // 小役（リプレイ・ベル）
    RARE: [FLAGS.CHERRY_A, FLAGS.CHERRY_B, FLAGS.CHERRY_C, FLAGS.SUICA_A, FLAGS.SUICA_B, FLAGS.SUICA_C, FLAGS.CHANCE_A, FLAGS.CHANCE_B, FLAGS.CHANCE_C], // レア役（チェリー・スイカ・チャンス目）
    BONUS: [FLAGS.BB_A, FLAGS.BB_B, FLAGS.BB_C, FLAGS.BB_D, FLAGS.RB_A, FLAGS.RB_B] // ボーナス
};

// 該当する役のグループを取得する関数
function getRoleGroup(flag) {
    if (ROLE_GROUPS.SMALL.includes(flag)) return 'SMALL';
    if (ROLE_GROUPS.RARE.includes(flag)) return 'RARE';
    if (ROLE_GROUPS.BONUS.includes(flag)) return 'BONUS';
    return 'HAZE';
}

// サブフラグごとの揃い方定義
// validLines: 0=上段, 1=中段, 2=下段, 3=右下がり斜め, 4=右上がり斜め
const WIN_COMBOS = {
    [FLAGS.BB_A]: { symbols: ['RED7', 'RED7', 'RED7'], validLines: [0, 1, 2, 3, 4] },
    [FLAGS.BB_B]: { symbols: ['BLUE7', 'BLUE7', 'BLUE7'], validLines: [0, 1, 2, 3, 4] },
    [FLAGS.BB_C]: { symbols: ['BAR', 'BAR', 'BAR'], validLines: [1] }, // 中段のみ
    [FLAGS.BB_D]: { symbols: ['BLUE7', 'RED7', 'BLUE7'], validLines: [1] }, // 中段のみ
    [FLAGS.RB_A]: { symbols: ['RED7', 'RED7', 'BAR'], validLines: [0, 1, 2, 3, 4] },
    [FLAGS.RB_B]: { symbols: ['BLUE7', 'BLUE7', 'BAR'], validLines: [0, 1, 2, 3, 4] },
    [FLAGS.REPLAY_A]: { symbols: [['REPLAY', 'RED7'], 'REPLAY', 'REPLAY'], validLines: [0] }, // 上段
    [FLAGS.REPLAY_B]: { symbols: [['REPLAY', 'RED7'], 'REPLAY', 'REPLAY'], validLines: [3, 4] }, // 斜め
    [FLAGS.REPLAY_C]: { symbols: [['REPLAY', 'RED7'], 'REPLAY', 'REPLAY'], validLines: [2] }, // 下段
    [FLAGS.BELL_A]: { symbols: [['STAR', 'BAR'], 'STAR', 'STAR'], validLines: [0, 3, 4] }, // 上段・斜め
    [FLAGS.BELL_B]: { symbols: [['STAR', 'BAR'], 'STAR', 'STAR'], validLines: [1] }, // 中段
    [FLAGS.BELL_C]: { symbols: [['STAR', 'BAR'], 'STAR', 'STAR'], validLines: [2] }, // 下段
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
    currentLotteryTable: [], // 現在設定の65536配列
    autoPlayTimeoutId: null,
    isReplay: false,
    currentFlag: FLAGS.HAZE,
    currentRNG: 0,
    heldBonusFlag: 0, // ボーナスの持ち越しフラグ (0=なし, 5=BIG, 6=REG)
    stoppedSymbols: [null, null, null],
    slipPixels: [null, null, null],     // 停止後のスベリピクセル数
    
    // キャラクターアニメーション管理
    characterAnimInterval: null,
    characterAnimFrame: 1,
    currentSetting: 1, // 現在の設定 (1-6)
    isDebugMode: false,
    bonusMode: 'NORMAL', // 'NORMAL', 'BB', 'RB'
    bonusPayoutTarget: 0,
    bonusEarned: 0,
    stockCount: 0 // 1G連ストックなどの保持数
};

// キャラクターアニメーション再生関数
function playCharacterAnimation(type, onComplete = null) {
    if (state.characterAnimInterval) clearInterval(state.characterAnimInterval);
    state.characterAnimFrame = 1;
    const charEl = document.getElementById('character-sprite');
    if (!charEl) return;

    // 定義されているアニメーションタイプに基づく連番画像切り替え
    const animConfig = {
        'idle': { frames: 9, speed: 150, loop: true },
        'attack': { frames: 6, speed: 100, loop: false }
    };
    const config = animConfig[type] || animConfig['idle'];
    
    charEl.src = `assets/popora_${type}_1.png`;
    
    state.characterAnimInterval = setInterval(() => {
        state.characterAnimFrame++;
        if (state.characterAnimFrame > config.frames) {
            if (config.loop) {
                state.characterAnimFrame = 1;
            } else {
                clearInterval(state.characterAnimInterval);
                state.characterAnimInterval = null;
                if (onComplete) onComplete();
                return;
            }
        }
        charEl.src = `assets/popora_${type}_${state.characterAnimFrame}.png`;
    }, config.speed);
}

function stopCharacterAnimation() {
    if (state.characterAnimInterval) {
        clearInterval(state.characterAnimInterval);
        state.characterAnimInterval = null;
    }
}

// セーブデータ用キー
const SAVE_KEY = 'BigBonusBlitz_Save';

function saveGameState() {
    const bgmSlider = document.getElementById('bgm-volume-slider');
    const seSlider = document.getElementById('se-volume-slider');
    const saveData = {
        credit: state.credit,
        bgmVolume: bgmSlider ? bgmSlider.value : 1.0,
        seVolume: seSlider ? seSlider.value : 1.0,
        heldBonusFlag: state.heldBonusFlag,
        currentSetting: state.currentSetting,
        isDebugMode: state.isDebugMode,
        bonusMode: state.bonusMode,
        bonusPayoutTarget: state.bonusPayoutTarget,
        bonusEarned: state.bonusEarned,
        stockCount: state.stockCount,
        debugPosX: document.getElementById('debug-popup') ? document.getElementById('debug-popup').style.left : null,
        debugPosY: document.getElementById('debug-popup') ? document.getElementById('debug-popup').style.top : null
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
            if (savedData.isDebugMode !== undefined) {
                state.isDebugMode = savedData.isDebugMode;
                const toggle = document.getElementById('debug-mode-toggle');
                if (toggle) toggle.checked = state.isDebugMode;
                const popup = document.getElementById('debug-popup');
                if (popup) {
                    if (state.isDebugMode) popup.classList.remove('hidden');
                    else popup.classList.add('hidden');
                    
                    if (savedData.debugPosX) popup.style.left = savedData.debugPosX;
                    if (savedData.debugPosY) popup.style.top = savedData.debugPosY;
                }
            }
            if (savedData.bonusMode !== undefined) state.bonusMode = savedData.bonusMode;
            if (savedData.bonusPayoutTarget !== undefined) state.bonusPayoutTarget = savedData.bonusPayoutTarget;
            if (savedData.bonusEarned !== undefined) state.bonusEarned = savedData.bonusEarned;
            if (savedData.stockCount !== undefined) state.stockCount = savedData.stockCount;
            return true;
        } catch (e) {
            console.error('Save data parse error', e);
        }
    }
    return false;
}

// 65536配列の動的生成
function ensureLotteryTables() {
    function generateTable(probabilitiesObj) {
        if (!probabilitiesObj) return null;
        let arr = [];
        const keys = Object.keys(FLAGS);
        for (let k of keys) {
            if (k !== 'HAZE' && probabilitiesObj[k]) {
                for (let i = 0; i < probabilitiesObj[k]; i++) arr.push(FLAGS[k]);
            }
        }
        const hazeCount = probabilitiesObj.HAZE !== undefined ? probabilitiesObj.HAZE : (65536 - arr.length);
        for (let i = 0; i < hazeCount; i++) arr.push(FLAGS.HAZE);
        for (let i = arr.length - 1; i > 0; i--) {
            const j = Math.floor(Math.random() * (i + 1));
            [arr[i], arr[j]] = [arr[j], arr[i]];
        }
        return arr;
    }

    let probA = CONFIG.probabilities_A || CONFIG.probabilities;
    if (probA) {
        state.currentLotteryTableA = generateTable(probA[state.currentSetting]);
    }
    
    let probB = CONFIG.probabilities_B || probA;
    if (probB) {
        state.currentLotteryTableB = generateTable(probB[state.currentSetting]);
    }

    let probC = CONFIG.probabilities_C || probA;
    if (probC) {
        state.currentLotteryTableC = generateTable(probC[state.currentSetting]);
    }

    let probD = CONFIG.probabilities_D || probA;
    if (probD) {
        state.currentLotteryTableD = generateTable(probD[state.currentSetting]);
    }
    
    if (CONFIG.probabilities_BB) {
        state.currentLotteryTableBB = generateTable(CONFIG.probabilities_BB[state.currentSetting]);
    }
    
    if (CONFIG.probabilities_RB) {
        state.currentLotteryTableRB = generateTable(CONFIG.probabilities_RB[state.currentSetting]);
    }
}

// DOM要素
const titleScreen = document.getElementById('title-screen');
const btnStartGame = document.getElementById('btn-start-game');

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
        bgmGain.gain.value = bgmSlider.value * 0.05;
        bgmSlider.addEventListener('input', (e) => {
            if (bgmGain) bgmGain.gain.value = e.target.value * 0.05;
            saveGameState();
        });
    }
    
    if (seSlider) {
        seGain.gain.value = seSlider.value * 0.1;
        seSlider.addEventListener('input', (e) => {
            if (seGain) seGain.gain.value = e.target.value * 0.1;
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

function playSoundCoin() {
    // 「チャリーン！」というお金の音 (Cha-ching)
    if (!audioCtx || !seGain) return;
    if (audioCtx.state === 'suspended') audioCtx.resume();
    
    // 最初の「チャッ」部分
    const osc1 = audioCtx.createOscillator();
    const gain1 = audioCtx.createGain();
    osc1.type = 'square';
    osc1.frequency.setValueAtTime(1500, audioCtx.currentTime);
    osc1.frequency.exponentialRampToValueAtTime(800, audioCtx.currentTime + 0.08);
    gain1.gain.setValueAtTime(0.3, audioCtx.currentTime);
    gain1.gain.exponentialRampToValueAtTime(0.01, audioCtx.currentTime + 0.08);
    osc1.connect(gain1);
    gain1.connect(seGain);
    osc1.start();
    osc1.stop(audioCtx.currentTime + 0.08);
    
    // 後半の「リーン！」部分 (高い金属音)
    setTimeout(() => {
        if (!audioCtx || !seGain) return;
        const osc2 = audioCtx.createOscillator();
        const osc3 = audioCtx.createOscillator();
        const gain2 = audioCtx.createGain();
        osc2.type = 'sine';
        osc3.type = 'triangle';
        osc2.frequency.value = 2500;
        osc3.frequency.value = 3200;
        
        gain2.gain.setValueAtTime(0.6, audioCtx.currentTime);
        gain2.gain.exponentialRampToValueAtTime(0.01, audioCtx.currentTime + 0.5);
        
        osc2.connect(gain2);
        osc3.connect(gain2);
        gain2.connect(seGain);
        
        osc2.start();
        osc3.start();
        osc2.stop(audioCtx.currentTime + 0.5);
        osc3.stop(audioCtx.currentTime + 0.5);
    }, 60);
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
function playSoundWin(type) {
    if (type === 'BIG') {
        // BIGボーナスのファンファーレ
        playTone(261.63, 'square', 0.15, 0.6);
        setTimeout(() => playTone(329.63, 'square', 0.15, 0.6), 150);
        setTimeout(() => playTone(392.00, 'square', 0.2, 0.6), 300);
        setTimeout(() => playTone(523.25, 'square', 0.6, 0.8), 450);
    } else if (type === 'REG') {
        // REGボーナスの少し短いファンファーレ
        playTone(392.00, 'square', 0.15, 0.6);
        setTimeout(() => playTone(440.00, 'square', 0.15, 0.6), 150);
        setTimeout(() => playTone(523.25, 'square', 0.4, 0.8), 300);
    } else if (type === 'BELL') {
        // ベル（STAR）のチャリチャリチャリリーン！というお金が連続して入る音
        let delay = 0;
        for (let i = 0; i < 3; i++) {
            setTimeout(() => {
                playTone(1800 + i*100, 'sine', 0.05, 0.3);
                playTone(2400 + i*100, 'triangle', 0.05, 0.3);
            }, delay);
            delay += 40;
            setTimeout(() => {
                playTone(2000 + i*100, 'sine', 0.05, 0.3);
                playTone(2600 + i*100, 'triangle', 0.05, 0.3);
            }, delay);
            delay += 60;
        }
        setTimeout(() => {
            playTone(2500, 'sine', 0.4, 0.5);
            playTone(3200, 'triangle', 0.5, 0.5);
        }, delay + 20);
    } else if (type === 'WATERMELON') {
        // スイカの少し落ち着いた音
        playTone(349.23, 'triangle', 0.15, 0.6);
        setTimeout(() => playTone(392.00, 'triangle', 0.2, 0.6), 150);
    } else if (type === 'CHERRY') {
        // チェリーの「チュッ、っぽん！」というかわいらしい音
        // 最初の「チュッ」部分（短く高い音の下降）
        playTone(2200, 'sine', 0.08, 0.4, true);
        
        // 少し遅れて「っぽん！」部分（余韻のある丸い音の下降）
        setTimeout(() => {
            playTone(1000, 'sine', 0.35, 0.6, true);
            playTone(1500, 'triangle', 0.2, 0.3, true); // ポンというアタック感を強調
        }, 120);
    } else {
        // デフォルトの勝利音
        playTone(261.63, 'square', 0.15, 0.6);
        setTimeout(() => playTone(523.25, 'square', 0.3, 0.8), 150);
    }
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

const bgmAudioNormal = new Audio('assets/bgm/bgm_poke_sync_192.wav');
bgmAudioNormal.loop = true;
let bgmAudioNode = null;

// 8bitレトロ調、草原フィールド風の軽快なループ
const bgmNotes = [
    523.25, 659.25, 783.99, 0,
    698.46, 659.25, 587.33, 0,
    659.25, 523.25, 392.00, 0,
    440.00, 493.88, 523.25, 0,
    523.25, 659.25, 783.99, 0,
    880.00, 783.99, 698.46, 0,
    783.99, 659.25, 523.25, 0,
    587.33, 659.25, 523.25, 0
];
const noteDuration = 0.12; // ちょっと軽快なテンポ

// ボーナス用BGM: BIG (アップテンポでメジャーな高揚感のある進行)
const bgmNotesBB = [
    261.63, 329.63, 392.00, 523.25,
    392.00, 329.63, 261.63, 196.00,
    349.23, 440.00, 523.25, 698.46,
    523.25, 440.00, 349.23, 261.63
];
const noteDurationBB = 0.1; // 速い

// ボーナス用BGM: REG (少しリズミカルな進行)
const bgmNotesRB = [
    392.00, 0,      392.00, 0,
    440.00, 0,      440.00, 0,
    493.88, 392.00, 493.88, 587.33,
    587.33, 0,      523.25, 0
];
const noteDurationRB = 0.12;

function restartBGM() {
    if (isBgmPlaying) {
        clearInterval(bgmInterval);
        bgmAudioNormal.pause();
        currentNote = 0;
        nextNoteTime = audioCtx ? audioCtx.currentTime + 0.1 : 0;
        bgmInterval = setInterval(scheduleBGM, 50);
    }
}

// ボーナス中のストック抽選（プレースホルダー）
function checkBonusStock(winType) {
    if (state.bonusMode === 'NORMAL') return;
    
    // 役ごとの抽選ロジックをここに実装
    if (winType === 'WATERMELON') {
        console.log(`[${state.bonusMode}] ストック抽選: スイカ`);
        // if (Math.random() < 0.1) state.stockCount++;
    } else if (winType === 'CHERRY') {
        console.log(`[${state.bonusMode}] ストック抽選: チェリー`);
        // if (Math.random() < 0.05) state.stockCount++;
    } else if (winType === 'BELL') {
        // ベル当選時
    } else if (winType === 'REPLAY') {
        // リプレイ当選時
    }
}

function scheduleBGM() {
    if (!isBgmPlaying || !audioCtx) {
        if (!bgmAudioNormal.paused) bgmAudioNormal.pause();
        return;
    }
    
    if (audioCtx.state === 'suspended') audioCtx.resume();

    // Web Audio APIへのルーティング（一度だけ実行）
    if (!bgmAudioNode) {
        try {
            bgmAudioNode = audioCtx.createMediaElementSource(bgmAudioNormal);
            bgmAudioNode.connect(bgmGain);
        } catch(e) {
            console.warn(e);
        }
    }

    if (state.bonusMode === 'NORMAL') {
        if (bgmAudioNormal.paused) {
            bgmAudioNormal.play().catch(e => console.warn(e));
        }
        return; // 通常時はWAVを再生してシンセサイザーは止める
    } else {
        if (!bgmAudioNormal.paused) {
            bgmAudioNormal.pause();
            bgmAudioNormal.currentTime = 0;
        }
    }

    // タブがバックグラウンドに行った時などにスケジュールが遅延して大量に発音されるのを防ぐ
    if (nextNoteTime < audioCtx.currentTime) {
        nextNoteTime = audioCtx.currentTime + 0.05;
    }

    let notes = bgmNotes;
    let dur = noteDuration;
    let wave = 'square';
    
    if (state.bonusMode === 'BB') {
        notes = bgmNotesBB;
        dur = noteDurationBB;
    } else if (state.bonusMode === 'RB') {
        notes = bgmNotesRB;
        dur = noteDurationRB;
    }

    // 現在時刻より少し先までスケジュールする
    while (nextNoteTime < audioCtx.currentTime + 0.1) {
        const freq = notes[currentNote % notes.length];
        if (freq > 0) {
            playBGMNote(freq, nextNoteTime, dur, wave);
        }
        nextNoteTime += dur;
        currentNote = (currentNote + 1) % notes.length;
    }
}

function playBGMNote(freq, time, duration, waveType = 'square') {
    const osc = audioCtx.createOscillator();
    const gain = audioCtx.createGain();
    
    osc.type = waveType; 
    osc.frequency.value = freq; 
    
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
        bgmAudioNormal.pause();
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

function determineNextMode(timing) {
    if (!CONFIG.modeTransitions) return 'A';
    const trans = CONFIG.modeTransitions[timing];
    if (!trans) return 'A';
    const probs = trans[state.currentSetting];
    if (!probs) return 'A';
    
    const rnd = Math.random() * 100;
    let sum = 0;
    const modes = ['A', 'B', 'C', 'D'];
    for (let m of modes) {
        sum += (probs[m] || 0);
        if (rnd < sum) return m;
    }
    return 'A'; // fallback
}

// 初期化
function init() {
    ensureLotteryTables(); // 65536配列を保証
    
    const wasLoaded = loadGameState();
    if (!wasLoaded) {
        state.mode = determineNextMode('initial');
    }
    
    setupReels();
    updateUI();
    updateLamp();
    updateDebugUI();
    playCharacterAnimation('idle'); // 起動直後から待機モーションを再生
    
    // イベントリスナー
    btnAuto.addEventListener('click', () => { initAudio(); onAutoToggle(); });
    btnMaxBet.addEventListener('click', () => { initAudio(); onMaxBet(); });
    btnStops.forEach((btn, index) => {
        btn.addEventListener('click', () => { initAudio(); onStop(index); });
    });
    
    // PAYTABLE Modal
    if (btnPaytable) {
        btnPaytable.addEventListener('click', () => {
            initAudio();
            paytableModal.classList.remove('hidden');
        });
    }
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
                
                // ボーナス・モード関連のリセット
                state.mode = determineNextMode('initial');
                state.spinCount = 0;
                state.bonusMode = 'NORMAL';
                state.bonusPayoutTarget = 0;
                state.bonusEarned = 0;
                state.stockCount = 0;
                
                restartBGM(); // BGMも通常に戻す
                
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
                updateDebugUI();
                optionsModal.classList.add('hidden');
            }
        });
    }

    // Add Credit
    if (btnAddCredit) {
        btnAddCredit.addEventListener('click', () => {
            initAudio();
            state.credit += 50;
            playSoundCoin(); // チャリーン！というお金の追加音
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
        state.reelOffsets[i] = -(arr.length * SYMBOL_SIZE);
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
    
    // HPバー（クレジット）の更新
    const hpBarFill = document.getElementById('hp-bar-fill');
    const hpBarText = document.getElementById('hp-bar-text');
    if (hpBarFill && hpBarText) {
        const MAX_HP = 500;
        let hpRatio = Math.min((state.credit / MAX_HP) * 100, 100);
        hpBarFill.style.width = `${hpRatio}%`;
        hpBarText.textContent = `HP (CREDIT): ${state.credit} / ${MAX_HP}`;
        
        if (state.credit < 50) {
            hpBarFill.style.background = 'linear-gradient(90deg, #ff3333, #aa0000)';
        } else {
            hpBarFill.style.background = 'linear-gradient(90deg, #33ff33, #00aa00)';
        }
    }

    // ボーナスUIの更新
    const elBonusInfo = document.getElementById('bonus-info');
    const elBonusLeft = document.getElementById('bonus-left-count');
    const elBbLogo = document.getElementById('bb-mode-logo');
    const elRbLogo = document.getElementById('rb-mode-logo');
    
    if (elBonusInfo && elBonusLeft) {
        if (state.bonusMode !== 'NORMAL') {
            elBonusInfo.classList.remove('hidden');
            let left = state.bonusPayoutTarget - state.bonusEarned;
            if (left < 0) left = 0;
            elBonusLeft.textContent = left;
            
            if (state.bonusMode === 'BB') {
                if (elBbLogo) elBbLogo.classList.remove('hidden');
                if (elRbLogo) elRbLogo.classList.add('hidden');
                elBonusInfo.style.borderColor = '#ff00ff';
                elBonusLeft.style.color = '#ff00ff';
                elBonusLeft.style.textShadow = '0 0 5px #ff00ff';
            } else if (state.bonusMode === 'RB') {
                if (elBbLogo) elBbLogo.classList.add('hidden');
                if (elRbLogo) elRbLogo.classList.remove('hidden');
                elBonusInfo.style.borderColor = '#00ff00';
                elBonusLeft.style.color = '#00ff00';
                elBonusLeft.style.textShadow = '0 0 5px #00ff00';
            }
        } else {
            elBonusInfo.classList.add('hidden');
            if (elBbLogo) elBbLogo.classList.add('hidden');
            if (elRbLogo) elRbLogo.classList.add('hidden');
        }
    }
    saveGameState();
}

function onMaxBet() {
    if (state.isGameActive) return;
    
    if (state.isReplay) {
        state.isReplay = false;
        btnMaxBet.textContent = 'MAX BET (3)';
        btnMaxBet.disabled = true;
        let currentPayouts = CONFIG.payouts;
        if (state.bonusMode === 'BB' && CONFIG.payouts_BB) currentPayouts = CONFIG.payouts_BB;
        if (state.bonusMode === 'RB' && CONFIG.payouts_RB) currentPayouts = CONFIG.payouts_RB;
        let replayCost = (typeof CONFIG !== 'undefined' && currentPayouts && currentPayouts.REPLAY) ? currentPayouts.REPLAY : 3;
        state.credit -= replayCost;
        state.bet = 3;
        if (state.bonusMode === 'BB' || state.bonusMode === 'RB') {
            state.bonusEarned -= replayCost;
        }
        updateUI();
        onLever();
        return;
    }

    if (state.credit >= 3) {
        state.bet = 3;
        state.credit -= 3;
        if (state.bonusMode === 'BB' || state.bonusMode === 'RB') {
            state.bonusEarned -= 3;
        }
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
            if (state.bonusMode === 'NORMAL') state.heldBonusFlag = state.currentFlag;
        }
        updateLamp();
        return;
    }

    if (state.bonusMode !== 'NORMAL') {
        // ボーナス中専用の抽選
        const rng = Math.floor(Math.random() * 65536);
        state.currentRNG = rng + 1;
        
        if (!state.currentLotteryTableBB || !state.currentLotteryTableRB) {
            ensureLotteryTables();
        }
        
        let table = state.bonusMode === 'BB' ? state.currentLotteryTableBB : state.currentLotteryTableRB;
        if (table && table.length === 65536) {
            state.currentFlag = table[rng];
        } else {
            // フォールバック（設定がない場合）
            if (rng < 14000) state.currentFlag = FLAGS.BELL_A;
            else if (rng < 15000) state.currentFlag = FLAGS.REPLAY_A;
            else if (rng < 15200) state.currentFlag = FLAGS.SUICA_A;
            else if (rng < 15400) state.currentFlag = FLAGS.CHERRY_A;
            else state.currentFlag = Math.random() < 0.5 ? FLAGS.CHANCE_A : FLAGS.HAZE;
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

    // ゲーム数加算と天井判定 (通常時のみ)
    state.spinCount++;
    const ceilingsConfig = CONFIG.ceilings || CEILINGS;
    const ceiling = ceilingsConfig[state.mode] || 999;
    if (state.spinCount >= ceiling) {
        // 天井恩恵：BIG:REG = 1:1 （とりあえず BB_A と RB_A をセット）
        let forced = Math.random() < 0.5 ? FLAGS.BB_A : FLAGS.RB_A;
        state.currentFlag = forced;
        state.heldBonusFlag = forced;
        updateLamp();
        return;
    }

    const rng = Math.floor(Math.random() * 65536);
    state.currentRNG = rng + 1; // UI表示用に1~65536とする
    
    if (!state.currentLotteryTableA || state.currentLotteryTableA.length !== 65536) {
        ensureLotteryTables();
    }
    
    // テーブルからフラグを取得
    let targetTable;
    if (state.mode === 'B') targetTable = state.currentLotteryTableB;
    else if (state.mode === 'C') targetTable = state.currentLotteryTableC;
    else if (state.mode === 'D') targetTable = state.currentLotteryTableD;
    else targetTable = state.currentLotteryTableA;
    
    state.currentFlag = targetTable[rng];
    
    if (state.currentFlag >= FLAGS.BB_A && state.currentFlag <= FLAGS.BB_D) {
        state.heldBonusFlag = state.currentFlag;
    } else if (state.currentFlag >= FLAGS.RB_A && state.currentFlag <= FLAGS.RB_B) {
        state.heldBonusFlag = state.currentFlag;
    }
    
    playCharacterAnimation('idle'); // 初期状態のアイドル再生
}

function updateDebugUI() {
    const elRng = document.getElementById('debug-rng');
    const elFlag = document.getElementById('debug-flag');
    const elMode = document.getElementById('debug-mode');
    
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
    if (elMode) {
        elMode.textContent = state.bonusMode;
    }
}

// スライムの小役示唆演出を更新
function updateEnemySlimeColor() {
    const enemy = document.getElementById('enemy-img');
    const core = enemy ? enemy.querySelector('.slime-core') : null;
    if (!enemy || !core) return;
    
    // 既存のクラスをリセットして基本の構造だけ残す
    enemy.className = '';
    core.className = 'slime-core';
    
    const flag = state.currentFlag;
    
    if (flag >= FLAGS.REPLAY_A && flag <= FLAGS.REPLAY_C) {
        // REPLAYなら色が青いゴブリン(スライム)、コアの色が青いスライム
        enemy.classList.add('enemy-slime-blue');
        core.classList.add('core-blue');
    } else if (flag >= FLAGS.BELL_A && flag <= FLAGS.BELL_C) {
        // ベルなら色が黄色いスライム
        enemy.classList.add('enemy-slime-yellow');
        core.classList.add('core-yellow');
    } else if (flag >= FLAGS.SUICA_A && flag <= FLAGS.SUICA_C) {
        // スイカなら緑
        enemy.classList.add('enemy-slime-green');
        core.classList.add('core-green');
    } else if (flag >= FLAGS.CHERRY_A && flag <= FLAGS.CHERRY_C) {
        // チェリーなら赤
        enemy.classList.add('enemy-slime-red');
        core.classList.add('core-red');
    } else if (flag === FLAGS.HAZE) {
        // ハズレはコアの色が赤いスライム（ただのゴブリン/ベースは青）
        enemy.classList.add('enemy-slime-blue');
        core.classList.add('core-red');
    } else if ((flag >= FLAGS.BB_A && flag <= FLAGS.BB_D) || (flag >= FLAGS.RB_A && flag <= FLAGS.RB_B)) {
        // ボーナスは虹色
        enemy.classList.add('enemy-slime-rainbow');
        core.classList.add('core-rainbow');
    } else {
        // デフォルト（通常）
        enemy.classList.add('enemy-slime-blue');
        core.classList.add('core-red');
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
    updateEnemySlimeColor();
    
    playCharacterAnimation('idle'); // 連番画像でのアイドル再生を開始
    
    // アニメーション状態のリセット
    const dust = document.getElementById('dust-cloud-img');
    const knight = document.getElementById('knight-win-img');
    const enemy = document.getElementById('enemy-img');
    const slimeSprite = document.querySelector('.slime-sprite');
    const core = document.querySelector('.slime-core');
    const character = document.getElementById('character-sprite');
    const gameContainer = document.getElementById('game-container');
    
    if (dust) dust.classList.add('hidden');
    if (knight) knight.classList.add('hidden');
    
    if (enemy) {
        enemy.style.display = '';
        enemy.classList.remove('enemy-anim-hit', 'enemy-anim-squash');
    }
    if (slimeSprite) slimeSprite.style.opacity = '1';
    if (core) core.style.display = '';
    
    if (character) {
        character.classList.remove('anim-miss', 'anim-replay', 'anim-bell', 'anim-cherry', 'anim-watermelon', 'anim-bonus');
    }
    if (gameContainer) {
        gameContainer.classList.remove('screen-shake');
    }
    
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
    const len = reelStrips[reelIndex].length;
    const pixelsPerRotation = len * SYMBOL_SIZE; 
    const speedPerMs = 2100 / 780; // 回転速度は常に21コマ/0.78秒に合わせる
    
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
            
            // ターゲットの約1.5コマ上が通過するタイミングでボタンを押す
            let idealPressOffset = -(tIdx + 1.5) * SYMBOL_SIZE;
            
            idealPressOffset = idealPressOffset % pixelsPerRotation;
            if (idealPressOffset > 0) idealPressOffset -= pixelsPerRotation;
            
            let currentOffset = state.reelOffsets[reelIndex] % pixelsPerRotation;
            if (currentOffset > 0) currentOffset -= pixelsPerRotation;
            
            // 目標地点までの距離を計算（順回転方向）
            let distToTarget = idealPressOffset - currentOffset;
            if (distToTarget < 0) distToTarget += pixelsPerRotation;
            
            // 目標地点を通過する直前にストップ
            if (distToTarget <= move) {
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
        [st[0]?.[2], st[1]?.[1], st[2]?.[0]]
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
        
        // 第2停止（ベル）の砂煙演出
        const pressedCount = btnStops.filter(b => b.disabled).length;
        if (pressedCount === 2) {
            if (state.currentFlag >= FLAGS.BELL_A && state.currentFlag <= FLAGS.BELL_C) {
                const dust = document.getElementById('dust-cloud-img');
                const slimeSprite = document.querySelector('.slime-sprite');
                const core = document.querySelector('.slime-core');
                if (dust) dust.classList.remove('hidden');
                if (slimeSprite) slimeSprite.style.opacity = '0';
                if (core) core.style.display = 'none';
            }
        }
        
        let len = reelStrips[reelIndex].length;
        let baseIdx = Math.floor(Math.abs(state.reelOffsets[reelIndex]) / SYMBOL_SIZE);
        
        let bestSlip = 0;
        let maxScore = -999;
        
        // スベリ限界は絶対に4コマ（ルール厳守）
        let maxSlip = 4;
        
        for (let k = 0; k <= maxSlip; k++) {
            let testIdx = (baseIdx - k + len) % len;
            let testSymbols = [
                reelStrips[reelIndex][testIdx],
                reelStrips[reelIndex][(testIdx + 1) % len],
                reelStrips[reelIndex][(testIdx + 2) % len]
            ];
            
            let score = scoreSlip(reelIndex, testSymbols, state.currentFlag, state.stoppedSymbols);
            
            if (score > maxScore) {
                maxScore = score;
                bestSlip = k;
            }
        }
        
        // スリップを適用
        let finalIdx = (baseIdx - bestSlip + len) % len;
        
        if (elSlipDisplays[reelIndex]) {
            elSlipDisplays[reelIndex].textContent = bestSlip;
        }
        
        state.stoppedSymbols[reelIndex] = [
            reelStrips[reelIndex][finalIdx],
            reelStrips[reelIndex][(finalIdx + 1) % len],
            reelStrips[reelIndex][(finalIdx + 2) % len]
        ];
        
        let currentAbsOffset = Math.abs(state.reelOffsets[reelIndex]);
        let targetAbsOffset = finalIdx * SYMBOL_SIZE;
        let distance = currentAbsOffset - targetAbsOffset;
        if (distance < 0) {
            distance += len * SYMBOL_SIZE;
        }
        
        state.slipPixels[reelIndex] = distance;
    } catch (e) {
        console.error("FATAL ERROR in onStop:", e);
        // フォールバック: 即座に止める
        state.slipPixels[reelIndex] = 0;
        if (elSlipDisplays[reelIndex]) {
            elSlipDisplays[reelIndex].textContent = 0;
        }
        let len = reelStrips[reelIndex].length;
        let fallbackIdx = Math.floor(Math.abs(state.reelOffsets[reelIndex]) / SYMBOL_SIZE) % len;
        state.stoppedSymbols[reelIndex] = [
            reelStrips[reelIndex][fallbackIdx],
            reelStrips[reelIndex][(fallbackIdx + 1) % len],
            reelStrips[reelIndex][(fallbackIdx + 2) % len]
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
        let len = reelStrips[i].length;
        // 浮動小数点誤差を避けるため Math.round を使用
        const topIdx = Math.round(Math.abs(state.reelOffsets[i]) / SYMBOL_SIZE) % len;
        indices.push([
            reelStrips[i][topIdx],
            reelStrips[i][(topIdx + 1) % len],
            reelStrips[i][(topIdx + 2) % len]
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
    let winType = null;
    
    let currentPayouts = CONFIG.payouts;
    if (state.bonusMode === 'BB' && CONFIG.payouts_BB) currentPayouts = CONFIG.payouts_BB;
    if (state.bonusMode === 'RB' && CONFIG.payouts_RB) currentPayouts = CONFIG.payouts_RB;

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
                    if (state.bonusMode === 'NORMAL') {
                        bonusWon = true;
                        winType = 'BIG';
                    }
                } else if (flagId >= FLAGS.RB_A && flagId <= FLAGS.RB_B) {
                    if (state.bonusMode === 'NORMAL') {
                        bonusWon = true;
                        if (winType !== 'BIG') winType = 'REG';
                    }
                } else if (flagId >= FLAGS.REPLAY_A && flagId <= FLAGS.REPLAY_C) {
                    isReplay = true;
                    if (typeof CONFIG !== 'undefined' && currentPayouts && currentPayouts.REPLAY) {
                        totalPayout += currentPayouts.REPLAY;
                    }
                } else if (flagId >= FLAGS.BELL_A && flagId <= FLAGS.BELL_C) {
                    totalPayout += currentPayouts.STAR;
                    if (!winType) winType = 'BELL';
                } else if (flagId >= FLAGS.SUICA_A && flagId <= FLAGS.SUICA_C) {
                    totalPayout += currentPayouts.WATERMELON;
                    if (!winType) winType = 'WATERMELON';
                }
                // チェリーは個別で判定済みなのでライン判定での加算はしない
            }
        }
    }
    
    if (cherryWin) {
        totalPayout += currentPayouts.CHERRY;
        if (!winType) winType = 'CHERRY';
    }
    
    if (bonusWon) {
        state.heldBonusFlag = 0;
        if (winType === 'BIG') {
            state.bonusMode = 'BB';
            state.bonusPayoutTarget = currentPayouts.BIG;
        } else {
            state.bonusMode = 'RB';
            state.bonusPayoutTarget = currentPayouts.REG;
        }
        state.bonusEarned = 0;
        
        // モード移行
        state.mode = determineNextMode('bonus');
        
        playSoundWin(winType);
        elMessage.textContent = `BONUS START! 0 / ${state.bonusPayoutTarget}`;
        elMessage.classList.add('flash');
        setTimeout(() => elMessage.classList.remove('flash'), CONFIG.timings.nextWin);
        updateLamp();
        
        // ボーナスアニメーション付与
        const character = document.getElementById('character-sprite');
        if (character) {
            character.classList.add('anim-bonus');
        }
        
        // BIG BONUS用 カットイン
        if (winType === 'BIG') {
            const cutin = document.getElementById('bb-cutin-logo');
            if (cutin) {
                cutin.classList.remove('hidden');
                // 3秒後に非表示
                setTimeout(() => {
                    cutin.classList.add('hidden');
                }, 3000);
            }
        }
        
        // BGM切替のためにリスタート
        restartBGM();
        
        state.bet = 0;
        updateUI();
        updateDebugUI(); // 追加: ボーナス突入時も確実にデバッグUIを更新
        if (state.isAutoMode) triggerNextAutoAction();
        return;
    }

    if (state.bonusMode !== 'NORMAL') {
        if (totalPayout > 0) {
            state.bonusEarned += totalPayout;
            checkBonusStock(winType);
        } else if (isReplay) {
            checkBonusStock('REPLAY');
        } else {
            checkBonusStock(winType || 'HAZE');
        }
        
        if (state.bonusEarned >= state.bonusPayoutTarget) {
            // ボーナス終了
            state.bonusMode = 'NORMAL';
            state.bonusEarned = 0;
            state.bonusPayoutTarget = 0;
            state.spinCount = 0; // G数リセット
            
            updateLamp();
            elMessage.textContent = 'BONUS END!';
            elMessage.classList.add('flash');
            
            // 少し待ってから通常BGMに戻す
            setTimeout(() => {
                elMessage.classList.remove('flash');
                restartBGM();
            }, 2000);
        }
    }
    
    if (totalPayout > 0) {
        playSoundWin(winType);
        
        // 開発用プレースホルダー：小役が揃ったら攻撃モーションを再生し、終わったら待機に戻す
        playCharacterAnimation('attack', () => {
            playCharacterAnimation('idle');
        });
        
        const character = document.getElementById('character-sprite');
        const enemy = document.getElementById('enemy-img');
        const gameContainer = document.getElementById('game-container');
        const knight = document.getElementById('knight-win-img');
        const dust = document.getElementById('dust-cloud-img');

        // 第3停止時のアクション演出（winTypeに応じて）
        
        if (winType === 'BELL') {
            if (character) character.classList.add('anim-bell');
            if (enemy) enemy.classList.add('enemy-anim-squash');
            // 今までの knight_win.png の代わりになるが、一応隠す
            if (knight) knight.classList.add('hidden');
            if (dust) dust.classList.add('hidden');
        } else if (winType === 'CHERRY') {
            if (character) character.classList.add('anim-cherry');
            if (enemy) enemy.classList.add('enemy-anim-hit');
        } else if (winType === 'WATERMELON') {
            if (character) character.classList.add('anim-watermelon');
            if (enemy) enemy.classList.add('enemy-anim-hit');
            if (gameContainer) gameContainer.classList.add('screen-shake');
        }
        
        state.credit += totalPayout;
        elPayout.textContent = totalPayout;
        if (elHeaderPayout) elHeaderPayout.textContent = totalPayout;
        
        if (state.bonusMode !== 'NORMAL' && state.bonusEarned <= state.bonusPayoutTarget) {
            elMessage.textContent = `BONUS: ${state.bonusEarned} / ${state.bonusPayoutTarget}`;
        } else {
            elMessage.textContent = `WIN! +${totalPayout}`;
            elMessage.classList.add('flash');
            setTimeout(() => elMessage.classList.remove('flash'), CONFIG.timings.nextWin);
        }
        
        if (state.isAutoMode) triggerNextAutoAction();
    } else if (isReplay) {
        playSoundReplay();
        
        // リプレイ演出
        const character = document.getElementById('character-sprite');
        if (character) {
            character.classList.add('anim-replay');
        }
        
        elPayout.textContent = 0;
        if (elHeaderPayout) elHeaderPayout.textContent = 0;
        elMessage.textContent = 'REPLAY!';
        elMessage.classList.add('flash');
        // リプレイはクレジットを減らさずに再度回せる
        state.bet = 3;
        state.isReplay = true;
        btnMaxBet.disabled = false;
        btnMaxBet.textContent = 'SPIN (REPLAY)';
        updateUI();
        setTimeout(() => elMessage.classList.remove('flash'), CONFIG.timings.nextWin);
        if (state.isAutoMode) triggerNextAutoAction();
        return; 
    } else {
        elPayout.textContent = 0;
        if (elHeaderPayout) elHeaderPayout.textContent = 0;
        
        // ハズレ演出
        if (state.bonusMode === 'NORMAL') {
            const character = document.getElementById('character-sprite');
            const enemy = document.getElementById('enemy-img');
            if (character) {
                character.classList.add('anim-miss');
            }
            if (enemy) enemy.classList.add('enemy-anim-hit'); // 敵が攻撃してきたような動きに流用
        }
        
        if (state.bonusMode !== 'NORMAL') {
            elMessage.textContent = `BONUS: ${state.bonusEarned} / ${state.bonusPayoutTarget}`;
        } else {
            elMessage.textContent = 'GAME OVER';
        }
        
        if (state.isAutoMode) triggerNextAutoAction();
    }
    
    state.bet = 0;
    updateUI();
    updateDebugUI();
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


// ウィンドウサイズに応じたスケール調整
function resizeGame() {
    const gameContainer = document.getElementById('game-container');
    if (!gameContainer) return;
    
    // ベースとなる解像度（現在の実際のサイズを動的に取得）
    const baseWidth = gameContainer.offsetWidth || 770; 
    const baseHeight = gameContainer.offsetHeight || 960; 
    
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

// タイトル画面の処理
if (btnStartGame && titleScreen) {
    btnStartGame.addEventListener('click', () => {
        initAudio(); // ユーザーのアクションでAudioContextを初期化
        titleScreen.style.transition = 'opacity 0.5s ease-out';
        titleScreen.style.opacity = '0';
        setTimeout(() => {
            titleScreen.style.display = 'none';
        }, 500);
        
        // 起動
        init();
    });
} else {
    // 起動
    init();
}

// デバッグモードのトグル
const debugModeToggle = document.getElementById('debug-mode-toggle');
const debugPopup = document.getElementById('debug-popup');

if (debugModeToggle && debugPopup) {
    debugModeToggle.addEventListener('change', (e) => {
        state.isDebugMode = e.target.checked;
        if (state.isDebugMode) {
            debugPopup.classList.remove('hidden');
        } else {
            debugPopup.classList.add('hidden');
        }
        saveGameState();
    });
}

// デバッグポップアップのドラッグ機能
let isDraggingDebug = false;
let debugDragOffsetX = 0;
let debugDragOffsetY = 0;

if (debugPopup) {
    debugPopup.addEventListener('mousedown', (e) => {
        // セレクトボックスなどをクリックした時はドラッグしない
        if (e.target.tagName.toLowerCase() === 'select' || e.target.tagName.toLowerCase() === 'option') return;
        
        isDraggingDebug = true;
        const rect = debugPopup.getBoundingClientRect();
        // コンテナのスケールを考慮
        const gameContainer = document.getElementById('game-container');
        const transform = window.getComputedStyle(gameContainer).transform;
        let scale = 1;
        if (transform !== 'none') {
            const matrix = new DOMMatrix(transform);
            scale = matrix.a;
        }
        
        debugDragOffsetX = (e.clientX - rect.left) / scale;
        debugDragOffsetY = (e.clientY - rect.top) / scale;
    });

    document.addEventListener('mousemove', (e) => {
        if (!isDraggingDebug) return;
        
        const gameContainer = document.getElementById('game-container');
        const transform = window.getComputedStyle(gameContainer).transform;
        let scale = 1;
        if (transform !== 'none') {
            const matrix = new DOMMatrix(transform);
            scale = matrix.a;
        }
        
        const containerRect = gameContainer.getBoundingClientRect();
        
        // コンテナ内の相対座標に変換
        let newX = (e.clientX - containerRect.left) / scale - debugDragOffsetX;
        let newY = (e.clientY - containerRect.top) / scale - debugDragOffsetY;
        
        debugPopup.style.left = newX + 'px';
        debugPopup.style.top = newY + 'px';
    });

    document.addEventListener('mouseup', () => {
        if (isDraggingDebug) {
            isDraggingDebug = false;
            saveGameState();
        }
    });
}

// グローバルエラーハンドラ
window.addEventListener('error', function(e) {
    const elMessage = document.getElementById('message-display');
    if (elMessage) {
        elMessage.textContent = 'ERR: ' + e.message + ' at ' + e.lineno;
        elMessage.style.color = 'red';
        elMessage.style.fontSize = '12px';
    }
});
