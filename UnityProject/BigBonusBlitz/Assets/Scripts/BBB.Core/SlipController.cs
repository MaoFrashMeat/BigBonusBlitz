using System;
using System.Collections.Generic;

namespace BBB.Core
{
    /// <summary>
    /// リール制御。押した位置から最大 maxSlip コマ手前までを候補にし、
    /// 「役の成立ルールを破らず、かつ残りリールがどこで押されても破綻しない」停止位置を選ぶ。
    ///
    /// ルール（2026-09-08 確定）
    ///  - 滑りは最大4コマ
    ///  - 当選役以外は揃えない。ハズレ・リーチ目は何も揃えない
    ///  - リプレイ・ベルは必ず揃える（どこで押しても・どの押し順でも）
    ///  - スイカ・チェリーは4コマ以内に無ければこぼす（揃えられる位置は優先）
    ///  - ボーナスは目押し次第（揃えられる位置は優先）。持ち越しボーナスは小役非当選Gのみ狙う
    ///  - 変則押しペナルティなし（押し順に依らず同じ判定）
    ///  - ベル・リプレイは A/B/C の区別なくどのラインで揃ってもよい
    ///  - 役が2ライン同時に揃う停止形は作らない
    ///
    /// 先読み: 各候補について、残りリールの全押し位置(20)×全滑り(0〜4)で有効な停止が存在するか
    /// （必須役はさらに成立できるか）を再帰的に確認する。状態は「各リールの上段コマ番号」で表し、
    /// フラグごとにメモ化する（21^3 状態）。
    /// </summary>
    public static class SlipController
    {
        public const int DefaultMaxSlip = 4;
        private const int None = -1;

        public struct Result
        {
            public int slip;
            public int stopIndex;      // 上段に来るコマ番号
            public Symbol[] symbols;   // [上, 中, 下]
        }

        /// <summary>この役は「どこで押しても必ず揃える」対象か。</summary>
        public static bool MustAlign(Flag f) => f.IsReplay() || f.IsBell();

        /// <summary>
        /// true: ベル／リプレイは A/B/C の区別なく「どのラインで揃ってもよい」扱い（配列制約が緩む）。
        /// false: WIN_COMBOS 通りサブフラグごとに成立ラインを限定する。
        /// </summary>
        public static bool AnyLineForBellReplay { get => WinCombos.AnyLineForBellReplay; set => WinCombos.AnyLineForBellReplay = value; }

        private static bool SameFamily(Flag a, Flag b)
        {
            if (a == b) return true;
            if (!AnyLineForBellReplay) return false;
            return (a.IsBell() && b.IsBell()) || (a.IsReplay() && b.IsReplay());
        }

        // ------------------------------------------------------------------
        // 公開API
        // ------------------------------------------------------------------

        /// <summary>
        /// 上段が baseIdx にある瞬間に停止操作されたときの停止位置を決める。
        /// stoppedIdx は各リールの停止コマ番号（未停止は -1）。
        /// </summary>
        public static Result Stop(Symbol[][] strips, int reelIndex, int baseIdx, Flag flag, Flag heldBonus,
                                  int[] stoppedIdx, int maxSlip = DefaultMaxSlip)
        {
            var ctx = Context.Get(strips, flag, heldBonus, maxSlip);
            int len = strips[reelIndex].Length;
            int bestSlip = 0;
            long bestScore = long.MinValue;
            var st = (int[])stoppedIdx.Clone();
            for (int k = 0; k <= maxSlip; k++)
            {
                st[reelIndex] = ((baseIdx - k) % len + len) % len;
                long score = ctx.Score(st);
                if (score > bestScore) { bestScore = score; bestSlip = k; }
            }
            int finalIdx = ((baseIdx - bestSlip) % len + len) % len;
            return new Result { slip = bestSlip, stopIndex = finalIdx, symbols = Window(strips[reelIndex], finalIdx) };
        }

        /// <summary>停止図柄（Symbol[][]、未停止 null）指定版。図柄が同じなら判定も同じなので先頭一致のコマ番号に写す。</summary>
        public static Result Stop(Symbol[][] strips, int reelIndex, int baseIdx, Flag flag, Flag heldBonus,
                                  Symbol[][] stopped, int maxSlip = DefaultMaxSlip)
        {
            var idx = new[] { None, None, None };
            for (int r = 0; r < 3; r++)
            {
                if (stopped[r] == null) continue;
                idx[r] = FindWindow(strips[r], stopped[r]);
                if (idx[r] < 0) throw new ArgumentException($"reel {r}: 停止図柄が配列上に無い");
            }
            return Stop(strips, reelIndex, baseIdx, flag, heldBonus, idx, maxSlip);
        }

        /// <summary>
        /// この配列で、その役を「どこで押しても・どの順で押しても」揃えられる（ハズレ等なら避けられる）か。
        /// 配列設計の検証用。
        /// </summary>
        public static bool IsGuaranteed(Symbol[][] strips, Flag flag, Flag heldBonus = Flag.HAZE, int maxSlip = DefaultMaxSlip)
            => Context.Get(strips, flag, heldBonus, maxSlip).IsFeasible(new[] { None, None, None });

        /// <summary>順押し（左→中→右）限定で保証できるか。</summary>
        public static bool IsGuaranteedOrdered(Symbol[][] strips, Flag flag, Flag heldBonus = Flag.HAZE, int maxSlip = DefaultMaxSlip)
            => Context.Get(strips, flag, heldBonus, maxSlip).IsFeasibleOrdered(new[] { None, None, None });

        /// <summary>順押し限定で破綻する左リールの押し位置（診断用）。</summary>
        public static List<int> FindUnguaranteedLeftPushesOrdered(Symbol[][] strips, Flag flag, Flag heldBonus = Flag.HAZE, int maxSlip = DefaultMaxSlip)
        {
            var ctx = Context.Get(strips, flag, heldBonus, maxSlip);
            var list = new List<int>();
            int len = strips[0].Length;
            for (int push = 0; push < len; push++)
            {
                bool any = false;
                for (int k = 0; k <= maxSlip && !any; k++)
                {
                    var st = new[] { ((push - k) % len + len) % len, None, None };
                    if (ctx.IsFeasibleOrdered(st)) any = true;
                }
                if (!any) list.Add(push);
            }
            return list;
        }

        /// <summary>先読みキャッシュを捨てる（配列を編集したあと等）。</summary>
        public static void ClearCache() => Context.Clear();

        /// <summary>空状態から見て破綻する「最初の押し（リール, 位置）」を列挙する（診断用）。</summary>
        public static List<(int reel, int push)> FindUnguaranteedPushes(Symbol[][] strips, Flag flag, Flag heldBonus = Flag.HAZE, int maxSlip = DefaultMaxSlip)
        {
            var ctx = Context.Get(strips, flag, heldBonus, maxSlip);
            var list = new List<(int, int)>();
            int len = strips[0].Length;
            for (int r = 0; r < 3; r++)
                for (int push = 0; push < len; push++)
                {
                    bool any = false;
                    for (int k = 0; k <= maxSlip && !any; k++)
                    {
                        var st = new[] { None, None, None };
                        st[r] = ((push - k) % len + len) % len;
                        if (ctx.IsFeasible(st)) any = true;
                    }
                    if (!any) list.Add((r, push));
                }
            return list;
        }

        public static Symbol[] Window(Symbol[] strip, int topIdx)
        {
            int len = strip.Length;
            return new[] { strip[topIdx % len], strip[(topIdx + 1) % len], strip[(topIdx + 2) % len] };
        }

        private static int FindWindow(Symbol[] strip, Symbol[] win)
        {
            int len = strip.Length;
            for (int i = 0; i < len; i++)
                if (strip[i] == win[0] && strip[(i + 1) % len] == win[1] && strip[(i + 2) % len] == win[2]) return i;
            return -1;
        }

        /// <summary>この停止形（未停止 null 可）がルールを破っていないか。</summary>
        public static bool CheckSlipValidity(int reelIndex, Symbol[] test, Flag flag, Flag heldBonus, Symbol[][] stopped)
        {
            var st = new Symbol[3][];
            for (int i = 0; i < 3; i++) st[i] = i == reelIndex ? test : stopped[i];
            return IsStateValid(st, flag, heldBonus);
        }

        // ------------------------------------------------------------------
        // 成立ルール
        // ------------------------------------------------------------------

        private static bool Contains(Symbol[] a, Symbol s) => Array.IndexOf(a, s) >= 0;

        private static bool IsStateValid(Symbol[][] st, Flag flag, Flag heldBonus)
        {
            // チェリーは左リール枠内で成立するので、非チェリー役では左にチェリー禁止
            if (st[0] != null && !flag.IsCherry() && Contains(st[0], Symbol.CHERRY)) return false;
            if (st[0] == null || st[1] == null || st[2] == null) return true;
            // 成立ラインの本数（同じラインに複数サブフラグが乗っても1本と数える）
            int lineMask = 0;
            foreach (var (w, line) in WinCombos.CompletedWins(st))
            {
                if (SameFamily(w, flag) || (w == heldBonus && heldBonus != Flag.HAZE)) { lineMask |= 1 << line; continue; }
                return false;
            }
            // 2ライン同時成立は禁止（2026-09-08 確定）
            int lines = 0;
            for (int m = lineMask; m != 0; m >>= 1) lines += m & 1;
            return lines <= 1;
        }

        private static bool IsAligned(Symbol[][] st, Flag flag)
        {
            if (flag.IsCherry()) return Contains(st[0], Symbol.CHERRY);
            foreach (var (w, _) in WinCombos.CompletedWins(st)) if (SameFamily(w, flag)) return true;
            return false;
        }

        // ------------------------------------------------------------------
        // 先読み（メモ化）
        // ------------------------------------------------------------------

        private sealed class Context
        {
            private static readonly Dictionary<(Symbol[][], Flag, Flag, int, bool), Context> _cache =
                new Dictionary<(Symbol[][], Flag, Flag, int, bool), Context>();

            public static void Clear() => _cache.Clear();

            public static Context Get(Symbol[][] strips, Flag flag, Flag held, int maxSlip)
            {
                var key = (strips, flag, held, maxSlip, AnyLineForBellReplay);
                if (!_cache.TryGetValue(key, out var c)) { c = new Context(strips, flag, held, maxSlip); _cache[key] = c; }
                return c;
            }

            private readonly Symbol[][] _strips;
            private readonly Flag _flag, _held;
            private readonly int _maxSlip;
            private readonly int _len;
            private readonly int _n;            // len + 1（-1 を 0 に写す）
            private readonly byte[] _valid;     // 0=未計算 1=ok 2=ng
            private readonly byte[] _feasible;  // 0=未計算 1=ok 2=ng
            private readonly byte[] _feasibleOrdered;
            private readonly short[] _alignable;// -1=未計算
            private readonly bool _mustAlign;

            private Context(Symbol[][] strips, Flag flag, Flag held, int maxSlip)
            {
                _strips = strips; _flag = flag; _held = held; _maxSlip = maxSlip;
                _len = strips[0].Length;
                _n = _len + 1;
                int size = _n * _n * _n;
                _valid = new byte[size];
                _feasible = new byte[size];
                _feasibleOrdered = new byte[size];
                _alignable = new short[size];
                for (int i = 0; i < size; i++) _alignable[i] = -1;
                _mustAlign = MustAlign(flag);
            }

            private int Key(int[] st) => ((st[0] + 1) * _n + (st[1] + 1)) * _n + (st[2] + 1);

            private Symbol[][] Symbols(int[] st)
            {
                var s = new Symbol[3][];
                for (int i = 0; i < 3; i++) s[i] = st[i] < 0 ? null : Window(_strips[i], st[i]);
                return s;
            }

            private bool Valid(int[] st)
            {
                int k = Key(st);
                if (_valid[k] == 0) _valid[k] = IsStateValid(Symbols(st), _flag, _held) ? (byte)1 : (byte)2;
                return _valid[k] == 1;
            }

            private bool AllStopped(int[] st) => st[0] >= 0 && st[1] >= 0 && st[2] >= 0;

            /// <summary>残りリールがどこで押されても有効（必須役は成立）に止められるか。</summary>
            private bool Feasible(int[] st)
            {
                int k = Key(st);
                if (_feasible[k] != 0) return _feasible[k] == 1;
                bool result;
                if (!Valid(st)) result = false;
                else if (AllStopped(st)) result = !_mustAlign || IsAligned(Symbols(st), _flag);
                else
                {
                    result = true;
                    var child = (int[])st.Clone();
                    for (int r = 0; r < 3 && result; r++)
                    {
                        if (st[r] >= 0) continue;
                        for (int push = 0; push < _len && result; push++)
                        {
                            bool any = false;
                            for (int s = 0; s <= _maxSlip && !any; s++)
                            {
                                child[r] = ((push - s) % _len + _len) % _len;
                                if (Feasible(child)) any = true;
                            }
                            if (!any) result = false;
                        }
                        child[r] = None;
                    }
                }
                _feasible[k] = result ? (byte)1 : (byte)2;
                return result;
            }

            /// <summary>成立させられる残り押し位置の数（優先度用）。全停止なら成立で1。</summary>
            private int Alignable(int[] st)
            {
                int k = Key(st);
                if (_alignable[k] >= 0) return _alignable[k];
                int result;
                if (AllStopped(st)) result = IsAligned(Symbols(st), _flag) ? 1 : 0;
                else
                {
                    int next = st[0] < 0 ? 0 : st[1] < 0 ? 1 : 2;
                    result = 0;
                    var child = (int[])st.Clone();
                    for (int push = 0; push < _len; push++)
                    {
                        for (int s = 0; s <= _maxSlip; s++)
                        {
                            child[next] = ((push - s) % _len + _len) % _len;
                            if (!Valid(child)) continue;
                            if (Alignable(child) > 0) { result++; break; }
                        }
                    }
                }
                _alignable[k] = (short)result;
                return result;
            }

            /// <summary>
            /// 候補スコア。段階:
            ///   long.MinValue … ルール違反（指定外の役が揃う／非チェリー時に左チェリー）。絶対に選ばない
            ///   0 以上        … 有効。先読みで「残りがどこで押されても大丈夫」なら +1e9（保証あり）。
            ///                  さらに成立可能性で加点。同点なら滑り最小
            /// 保証ありの候補が無い場合（配列上どうしても詰む位置）は有効な候補の中で最善を選ぶ。
            /// </summary>
            public long Score(int[] st)
            {
                if (!Valid(st)) return long.MinValue;
                long score = Feasible(st) ? 1_000_000_000L : 0L;
                if (_flag == Flag.HAZE || _flag.IsReachMe()) return score;
                score += Alignable(st) * 100L;
                if (AllStopped(st) && IsAligned(Symbols(st), _flag)) score += 100_000;
                return score;
            }

            public bool IsFeasible(int[] st) => Feasible(st);
            public bool IsFeasibleOrdered(int[] st) => FeasibleOrdered(st);

            /// <summary>順押し限定版: 次に止めるのは常に左から順。</summary>
            private bool FeasibleOrdered(int[] st)
            {
                int k = Key(st);
                if (_feasibleOrdered[k] != 0) return _feasibleOrdered[k] == 1;
                bool result;
                if (!Valid(st)) result = false;
                else if (AllStopped(st)) result = !_mustAlign || IsAligned(Symbols(st), _flag);
                else
                {
                    int r = st[0] < 0 ? 0 : st[1] < 0 ? 1 : 2;
                    result = true;
                    var child = (int[])st.Clone();
                    for (int push = 0; push < _len && result; push++)
                    {
                        bool any = false;
                        for (int s = 0; s <= _maxSlip && !any; s++)
                        {
                            child[r] = ((push - s) % _len + _len) % _len;
                            if (FeasibleOrdered(child)) any = true;
                        }
                        if (!any) result = false;
                    }
                }
                _feasibleOrdered[k] = result ? (byte)1 : (byte)2;
                return result;
            }
        }
    }
}
