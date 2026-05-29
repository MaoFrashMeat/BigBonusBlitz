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

// 状態管理
let state = {
    credit: 50,
    bet: 0,
    reelsSpinning: [false, false, false],
    reelPositions: [0, 0, 0], // 現在の停止位置（インデックス）
    reelOffsets: [0, 0, 0], // アニメーション用のピクセルオフセット
    animationIds: [null, null, null],
    isGameActive: false,
    isAutoMode: false,
    autoPlayTimeoutId: null,
    isReplay: false,
};

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

// 初期化
function init() {
    setupReels();
    updateUI();
    
    // イベントリスナー
    btnAuto.addEventListener('click', onAutoToggle);
    btnMaxBet.addEventListener('click', onMaxBet);
    btnStops.forEach((btn, index) => {
        btn.addEventListener('click', () => onStop(index));
    });

    // キーボード操作対応
    window.addEventListener('keydown', (e) => {
        // Space または テンキーの0
        if (e.code === 'Space' || e.code === 'Numpad0') {
            e.preventDefault(); // Spaceキーでの画面スクロールを防止
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
        onLever();
    } else {
        elMessage.textContent = 'CREDIT NOT ENOUGH';
    }
}

// レバーオン（回転開始）
function onLever() {
    if (state.bet === 0) return;
    state.isGameActive = true;
    elMessage.textContent = 'SPINNING...';
    
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

// リール回転アニメーション（簡易版）
function startSpinning(reelIndex) {
    const speed = 30; // 1フレームあたりの移動ピクセル
    
    function spin() {
        if (!state.reelsSpinning[reelIndex]) return; // 停止指示で抜ける
        
        state.reelOffsets[reelIndex] += speed;
        // 1周分（21コマ）スクロールしたら戻す（ループ）
        if (state.reelOffsets[reelIndex] >= 0) {
            state.reelOffsets[reelIndex] -= (REEL_SYMBOLS * SYMBOL_SIZE);
        }
        updateReelPosition(reelIndex);
        state.animationIds[reelIndex] = requestAnimationFrame(spin);
    }
    spin();
}

// ストップボタン処理
function onStop(reelIndex) {
    if (!state.reelsSpinning[reelIndex]) return;
    
    state.reelsSpinning[reelIndex] = false;
    btnStops[reelIndex].disabled = true;
    cancelAnimationFrame(state.animationIds[reelIndex]);
    
    // パチスロ風の順方向へのスベリ制御（最大1コマ弱の滑り）
    // reelOffsetsは負の値から0に向かって増加（リールは下へスクロール）
    let offset = state.reelOffsets[reelIndex];
    let remainder = Math.abs(offset) % SYMBOL_SIZE;
    
    // offsetを正の方向（回転方向）の直近のSYMBOL_SIZEの倍数にスナップさせる
    offset += remainder;
    
    state.reelOffsets[reelIndex] = offset;
    updateReelPosition(reelIndex);
    
    // 配列上のインデックスを計算
    // 表示上は中央のコマを基準にするなど調整が必要。後ほど実装を詰める。
    
    checkAllStopped();
    
    if (state.isAutoMode && state.reelsSpinning.some(s => s === true)) {
        triggerNextAutoAction();
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
    
    lines.forEach(line => {
        if (line[0] === line[1] && line[1] === line[2]) {
            const sym = line[0];
            if (sym === 'RED7' || sym === 'BLUE7' || sym === 'BAR') totalPayout += 15; // ボーナス（仮で15枚）
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
        state.credit += totalPayout;
        elPayout.textContent = totalPayout;
        elMessage.textContent = `WIN! +${totalPayout}`;
        elMessage.classList.add('flash');
        setTimeout(() => {
            elMessage.classList.remove('flash');
            if (state.isAutoMode) triggerNextAutoAction();
        }, 2000);
    } else if (isReplay) {
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
}

// 起動
init();
