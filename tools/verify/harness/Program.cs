using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BBB.Core;
using Newtonsoft.Json;

static class Program
{
    // Resources/Data の場所。BBB_ROOT（リポジトリの根）があればそれ、無ければ実行ファイルの場所から UnityProject を探して上る
    static readonly string Root = FindDataDir();
    static string FindDataDir()
    {
        string env = Environment.GetEnvironmentVariable("BBB_ROOT");
        var starts = new List<string>();
        if (!string.IsNullOrEmpty(env)) starts.Add(env);
        starts.Add(AppContext.BaseDirectory); starts.Add(Directory.GetCurrentDirectory());
        foreach (var start in starts)
        {
            var dir = new DirectoryInfo(start);
            while (dir != null)
            {
                string cand = Path.Combine(dir.FullName, "UnityProject", "BigBonusBlitz", "Assets", "Resources", "Data");
                if (Directory.Exists(cand)) return cand.Replace('\\', '/') + "/";
                dir = dir.Parent;
            }
        }
        throw new DirectoryNotFoundException("UnityProject/BigBonusBlitz/Assets/Resources/Data が見つからない。BBB_ROOT にリポジトリの根を入れるか、リポジトリの中で実行する");
    }
    static readonly JsonSerializerSettings JS = new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace };
    static readonly List<string> Problems = new List<string>();
    static readonly Dictionary<string, int> ProblemCount = new Dictionary<string, int>();

    static void Bad(string what)
    {
        ProblemCount[what] = ProblemCount.TryGetValue(what, out var c) ? c + 1 : 1;
        if (ProblemCount[what] <= 1) Problems.Add(what);
    }

    static void Main()
    {
        var cfg = JsonConvert.DeserializeObject<GameConfig>(File.ReadAllText(Root + "game_config.json"), JS);
        var wf = JsonConvert.DeserializeObject<Dictionary<string, WorkflowRole>>(File.ReadAllText(Root + "workflow_config.json"), JS);
        var en = JsonConvert.DeserializeObject<EnemyTableSet>(File.ReadAllText(Root + "enemy_tables.json"), JS).tables;

        // --- リプレイが再遊技になっているかの確認 ---
        {
            var mr = new SlotMachine(cfg, wf, en, new SystemRandom(1), 1) { Credit = 100_000 };
            var pr = new SystemRandom(1);
            int shown = 0;
            long spent = 0, paid = 0, replayG = 0, normalG = 0;
            for (int g = 0; g < 200_000; g++)
            {
                bool wasReplay = mr.IsReplay;
                int before = mr.Credit;
                if (mr.Credit < 3 && !mr.IsReplay) mr.Credit += 100_000;
                mr.MaxBet();
                int afterBet = mr.Credit;
                mr.Lever();
                for (int i = 0; i < 3; i++) mr.Stop(i, pr.Next(20), 4, (float)pr.NextDouble());
                var rr = mr.Evaluate();
                if (wasReplay)
                {
                    replayG++;
                    if (before - afterBet != 0) Bad($"リプレイ消化Gでエンバーが {before - afterBet} 減っている");
                    if (shown < 2) { Console.WriteLine($"  リプレイ消化G: {before} → BET後 {afterBet}（減り {before - afterBet}）"); shown++; }
                }
                else { normalG++; spent += 3; }
                paid += rr.win.payout;
            }
            Console.WriteLine($"  20万G: 通常 {normalG:N0}G / リプレイ消化 {replayG:N0}G（{100.0 * replayG / (normalG + replayG):F1}%）  投入 {spent:N0}  払い出し {paid:N0}  機械割 {100.0 * paid / spent:F1}%");
            Console.WriteLine();
        }

        // --- リプレイが再遊技になっているかの確認 ---
        {
            var mr = new SlotMachine(cfg, wf, en, new SystemRandom(1), 1) { Credit = 100_000 };
            var pr = new SystemRandom(1);
            long spent = 0, paid = 0, replayG = 0, normalG = 0;
            for (int g = 0; g < 300_000; g++)
            {
                bool wasReplay = mr.IsReplay;
                if (mr.Credit < 3 && !mr.IsReplay) mr.Credit += 100_000;
                int before = mr.Credit;
                mr.MaxBet();
                int used = before - mr.Credit;
                mr.Lever();
                for (int i = 0; i < 3; i++) mr.Stop(i, pr.Next(20), 4, (float)pr.NextDouble());
                var rr = mr.Evaluate();
                if (wasReplay) { replayG++; if (used != 0) Bad($"リプレイ消化Gでエンバーが {used} 減っている"); }
                else { normalG++; if (used != 3 && !(used == 0 && mr.LastBetWasFree)) Bad($"通常Gの投入が {used}"); spent += used; }   // ライフ（装備込み）で BET が無料になるGがある
                paid += rr.win.payout;
            }
            Console.WriteLine($"リプレイ: 通常 {normalG:N0}G / 再遊技 {replayG:N0}G（{100.0 * replayG / (normalG + replayG):F1}%）  投入 {spent:N0}  払い出し {paid:N0}  機械割 {100.0 * paid / spent:F1}%");
            Console.WriteLine();
        }

        // --- 押し方を変えて何通りか回し、不変条件を毎G確認する ---
        string[] styles = { "順押し", "ナビ従", "ナビ無視", "ランダム" };
        for (int st = 0; st < styles.Length; st++)
        {
            var m = new SlotMachine(cfg, wf, en, new SystemRandom(1000 + st), 1) { Credit = 1_000_000 };
            var push = new SystemRandom(50 + st);
            int maxAtSpins = 0, maxBattleG = 0, maxEngageG = 0, atRuns = 0, atInBonus = 0, atResumed = 0;
            int chapters = 0, treasures = 0, routes = 0, stageMoves = 0, bossAmbush = 0;
            int torchOuts = 0, deaths = 0, refills = 0, torchesBought = 0, stranded = 0;
            var condHits = new Dictionary<string, int>();
            var engageSrc = new Dictionary<string, int>();
            int drops = 0, bagFull = 0, curseOffers = 0, curseTaken = 0;
            var rarityHits = new Dictionary<string, int>();
            int fwd = 0, bwd = 0; long setbackSum = 0, deepSum = 0, chapterG = 0, lastChapterG = 0;
            long torchSpent = 0;
            var stageVisits = new Dictionary<string, int>();
            long soulsBefore = 0;
            var rankHits = new Dictionary<string, int>(); int techOk = 0, techTry = 0; long rankBonusSouls = 0;

            for (int g = 0; g < 250_000; g++)
            {
                // --- BET 前 ---
                if (m.Credit < SlotMachine.BetCost && !m.IsReplay) { m.Credit += 1_000_000; }
                bool wasAt = m.InAt;
                soulsBefore = m.Wallet.Souls;

                if (!m.MaxBet()) { Bad("MaxBet が失敗する"); break; }
                m.Lever();

                // --- レバー後の不変条件 ---
                if (m.Navi.Active && m.Navi2.Active) Bad("2種類のナビが同時に出ている");
                // 押し順ナビは AT 中とボーナス中に出る
                if (m.Navi2.Active && !m.InAt && m.BonusMode == BonusMode.NORMAL) Bad("AT でもボーナスでもないのに押し順ナビが出ている");
                if (m.Navi.Active && m.BonusMode != BonusMode.NORMAL) Bad("ボーナス中に択ナビが出ている");
                if (m.PseudoPlay && m.HeldBonusFlag == Flag.HAZE) Bad("持ち越しが無いのに擬似遊技");
                if (m.PseudoPlay && m.CurrentFlag != m.HeldBonusFlag) Bad("擬似遊技なのにボーナスを狙っていない");
                // AT 中にボーナスが入るのは想定内（AT の G は止まって、ボーナス後に再開する）
                if (m.InBattle && m.BattleMonster == null) Bad("バトル中なのにモンスターが居ない");
                if (m.InBattle && (m.BattleHp <= 0 || m.BattleHp > m.BattleHpMax)) Bad("バトルの HP が範囲外");
                if (m.AtSpinsRemaining < 0) Bad("AT の残りGが負");
                if (m.AtEntryRemaining < 0) Bad("洞窟前兆の残りGが負");
                if (m.BonusAnnounceRemaining < 0) Bad("ボーナス前兆の残りGが負");
                if (m.Tier2SpinCount > m.EngageMaxSpins) Bad("エンゲージが規定Gを超えて続く");
                if (m.AdventureEnabled)
                {
                    if (m.CurrentStage == null) Bad("現在のステージが設定に無い");
                    if (m.Adv.spinsLeft < 0) Bad("ステージの残りGが負");
                    if (m.Adv.nextId != null && cfg.adventure.Find(m.Adv.nextId) == null) Bad("決まったルートの先が無い");
                    if (!m.Adv.visited.Contains(m.Adv.nodeId)) Bad("現在地が通過記録に無い");
                    if (m.Adv.stockAtSpins < 0 || m.Adv.stockAtExpect < 0) Bad("宝の貯金が負");
                    var rc = cfg.adventure.resource;
                    if (rc != null && rc.enabled)
                    {
                        if (m.Adv.torches < 0) Bad("松明の本数が負");
                        if (m.Adv.torches > rc.maxTorches) Bad("松明が上限を超えた");
                        if (m.Adv.torchSpins < 0) Bad("松明の残りGが負");
                        if (m.Adv.torchSpins > m.TorchSpinsPerUnit) Bad("松明の残りGが 1 本ぶんを超えた");
                        if (m.Adv.torches == 0 && m.Adv.torchSpins > 0) Bad("松明 0 本なのに残量がある");
                        if (m.Adv.torches > 0 && m.Adv.torchSpins == 0) Bad("松明があるのに残量が 0");
                        if (m.Adv.MustReturn) Bad("街へ帰らずに次のGへ進んでいる");
                    }
                }
                if (m.InAt && !wasAt) atRuns++;
                maxAtSpins = Math.Max(maxAtSpins, m.AtSpinsRemaining);
                maxBattleG = Math.Max(maxBattleG, m.BattleSpinsRemaining);
                maxEngageG = Math.Max(maxEngageG, m.Tier2SpinCount);

                // --- 押し順 ---
                var order = new List<int> { 0, 1, 2 };
                if (m.Navi2.Active && st != 2)
                {
                    var rest = new List<int> { 0, 1, 2 }; rest.Remove(m.Navi2.first);
                    order = new List<int> { m.Navi2.first, rest[0], rest[1] };
                }
                else if (m.Navi.Active && st != 2)
                {
                    int f = m.Navi.first, cor = m.Navi.correctReel;
                    int third = new[] { 0, 1, 2 }.First(i => i != f && i != cor);
                    order = new List<int> { f, cor, third };
                }
                else if (st == 3)
                {
                    order = order.OrderBy(_ => push.Next(100)).ToList();
                }
                foreach (int i in order) m.Stop(i, push.Next(20), 4, (float)push.NextDouble());

                var r = m.Evaluate();
                if (r.tech.Active) { techTry++; if (r.techSuccess) { techOk++; string rk = r.techRank?.id ?? "-"; rankHits[rk] = rankHits.GetValueOrDefault(rk) + 1; rankBonusSouls += r.techBonusSouls; } }
                if (r.techSuccess && r.techRank == null && m.Slip[r.tech.reel] == 0 && r.tech.kind == TechKind.Vita) Bad("ビタ成功なのにランクが無い");
                if (r.techRank != null && !r.techSuccess) Bad("失敗なのにランクが付いた");
                if (r.techBonusSouls > 0 && (r.techRank == null || r.techRank.bonusPercent <= 0)) Bad("上乗せ 0% なのにソウルが増えた");

                // --- 判定後の不変条件 ---
                if (m.Credit < 0) { Bad("クレジットが負になった"); break; }
                if (m.Wallet.Souls < 0) Bad("ソウルが負");
                if (m.Wallet.Souls < soulsBefore && r.soulsGained > 0) Bad("ソウルが増えるはずが減った");
                if (r.soulsGained > 0 && m.Wallet.Souls != soulsBefore + r.soulsGained) Bad("ソウルの加算量が合わない");
                if (r.win.payout < 0) Bad("払い出しが負");
                if (r.bonusStarted && m.BonusMode == BonusMode.NORMAL) Bad("ボーナス開始なのに通常モード");
                if (r.atEnded && m.InAt) Bad("AT 終了なのにまだ AT 中");
                if (r.battleResolved.HasValue && m.InBattle) Bad("バトル決着なのにまだバトル中");
                // 残り 0G はこのGを消化しきる意味。判定後に AT が終わるのが正しい
                if (r.naviCorrect.HasValue && !m.InAt && !r.atEnded && m.BonusMode == BonusMode.NORMAL && !r.bonusEnded) Bad("AT 外・ボーナス外でナビ正誤が出ている");
                if (m.Bet != 0 && !m.IsReplay) Bad("判定後に BET が残っている");
                if (m.InAt && m.BonusMode != BonusMode.NORMAL) atInBonus++;
                if (r.bonusEnded && m.InAt) atResumed++;
                if (r.torchRefilled) refills++;
                if (r.equipDropped != null)
                {
                    drops++;
                    if (r.equipBagFull) bagFull++;
                    var rr = EquipDirector.RarityOf(cfg.equipment, r.equipDropped);
                    rarityHits[rr.name] = rarityHits.TryGetValue(rr.name, out var rc) ? rc + 1 : 1;
                    if (r.equipDropped.level < 1) Bad("装備の深さが 0 以下");
                    if (string.IsNullOrEmpty(r.equipDropped.slot)) Bad("装備の部位が空");
                }
                if (r.curseOffer != null)
                {
                    curseOffers++;
                    // 半分は受ける（実プレイに近づける）
                    if (curseOffers % 2 == 0) { m.Curse.Taken.Add(r.curseOffer); curseTaken++; }
                    m.Curse.Offer = null;
                }
                if (m.Curse.Taken.Count > cfg.curse.maxStack) Bad("呪いが上限を超えて積まれた");
                if (m.Equip.Bag.Count > cfg.equipment.bagSize) Bad("鞄の上限を超えた");
                if (r.precursorStarted)
                {
                    string src = r.workflow.category == "ENEMY" ? r.workflow.roleKey : (r.bossAmbush ? "ボス到達" : "高確ステージ");
                    engageSrc[src] = engageSrc.TryGetValue(src, out var sc2) ? sc2 + 1 : 1;
                }
                if (m.AdventureEnabled && m.Adv.MustReturn)
                {
                    string reason = m.Adv.returnReason;
                    m.Adv.returnReason = null;
                    var rc2 = cfg.adventure.resource;
                    if (reason == "torch")
                    {
                        torchOuts++;
                        if (m.Adv.torches != 0) Bad("松明切れなのに本数が残っている");
                        string stayed = m.Adv.nodeId;
                        if (cfg.adventure.Find(stayed) == null) Bad("松明切れで進行が壊れた");
                    }
                    else
                    {
                        deaths++;
                        AdventureDirector.ApplyDeathPenalty(cfg.adventure, m.Adv, m.Wallet);
                        if (m.Adv.nodeId != cfg.adventure.start) Bad("力尽きても章の最初に戻らない");
                        if (m.Adv.stockAtSpins != 0 || m.Adv.stockAtExpect != 0) Bad("力尽きても宝の貯金が残る");
                        if (m.RescueCredit > 0 && m.Credit < m.RescueCredit) m.Credit = m.RescueCredit;
                    }
                    m.EndRun();   // 潜行が終わったので拾い物と呪いは流す
                    // 街で補給した扱い: ソウルが許す範囲で 3 本まで買う（実プレイに近づける）
                    while (m.Adv.torches < 3 && m.Wallet.Souls >= rc2.torchCost)
                    {
                        if (AdventureDirector.AddTorch(cfg.adventure, m.Adv, 1, m.TorchSpinsPerUnit) <= 0) break;
                        m.Wallet.Souls -= rc2.torchCost;
                        torchesBought++;
                    }
                    if (m.Adv.torches <= 0) { stranded++; m.Adv.torches = 1; m.Adv.torchSpins = m.TorchSpinsPerUnit; }
                }
                if (r.chapterCleared) { setbackSum += r.chapterSetbacks; deepSum += m.Adv.deepest; chapterG += g - lastChapterG; lastChapterG = g; chapters++; if (m.Adv.nodeId != cfg.adventure.start) Bad("章クリア後に開始ステージへ戻っていない"); if (m.Adv.visited.Count != 1) Bad("章クリア後に通過記録が残っている"); }
                if (r.treasure != null) treasures++;
                if (r.routeDecided != null) routes++;
                if (r.routeCondition != null && !r.stageChanged && !r.chapterCleared && !r.returnedToTown)   // 同じGで力尽きて街へ戻ると行き先は消える
                {
                    string cl = AdventureDirector.DescribeCondition(r.routeCondition);
                    condHits[cl] = condHits.TryGetValue(cl, out var cn) ? cn + 1 : 1;
                    if (m.Adv.nextId != r.routeCondition.to) Bad("条件で決めた行き先が入っていない");
                    if (m.Adv.decidedPriority != r.routeCondition.priority) Bad("条件の優先度が記録されていない");
                }
                if (m.AdventureEnabled)
                {
                    foreach (var kv in m.Adv.counters) if (kv.Value < 0) Bad("数えものが負");
                    if (m.Adv.replayChain < 0) Bad("リプレイ連続数が負");
                    if (m.Adv.decidedPriority > 0 && string.IsNullOrEmpty(m.Adv.nextId)) Bad("条件で決めたのに行き先が空");
                }
                if (r.bossAmbush) bossAmbush++;
                if (r.stageChanged)
                {
                    stageMoves++;
                    if (r.stageAdvanced) fwd++; else bwd++;
                    if (r.stageTo != m.Adv.nodeId) Bad("移動先と現在地が違う");
                    if (m.IsTier2 || m.EnemyActive || m.InAt || m.BonusMode != BonusMode.NORMAL) Bad("何かを抱えたままステージが動いた");
                    if (r.bossAmbush && m.PrecursorRemaining <= 0) Bad("ボスの前兆が始まっていない");
                    stageVisits[r.stageTo] = stageVisits.TryGetValue(r.stageTo, out var c) ? c + 1 : 1;
                }
            }

            Console.WriteLine($"{styles[st],-8} AT {atRuns,4} 回  最大: AT残り {maxAtSpins,5}G / 狩猟 {maxBattleG}G / エンゲージ {maxEngageG}G  ソウル {m.Wallet.Souls:N0}  AT中ボーナス {atInBonus}G/再開 {atResumed}回");
            Console.WriteLine($"          冒険: 章クリア {chapters} / 移動 {stageMoves} / ルート決定 {routes} / 宝 {treasures} / ボス待ち伏せ {bossAmbush}   到達: " + string.Join(" ", stageVisits.OrderBy(k => k.Key).Select(k => $"{k.Key}={k.Value}")));
            Console.WriteLine($"          資源: 松明切れ {torchOuts} / 力尽き {deaths} / 道中入手 {refills} / 購入 {torchesBought} / ソウル不足で買えず {stranded}");
            Console.WriteLine("          条件: " + (condHits.Count == 0 ? "成立なし" : string.Join(" / ", condHits.OrderByDescending(k => k.Value).Select(k => $"{k.Key} {k.Value}回"))));
            Console.WriteLine("          エンゲージの出どころ: " + string.Join(" / ", engageSrc.OrderByDescending(k => k.Value).Select(k => $"{k.Key} {k.Value}回 {100.0 * k.Value / Math.Max(1, engageSrc.Values.Sum()):F0}%")));
            Console.WriteLine($"          技術介入: 出題 {techTry} 成功 {techOk} / ランク: " + string.Join(" / ", rankHits.OrderByDescending(k => k.Value).Select(k => $"{k.Key} {k.Value}")) + $" / 上乗せソウル {rankBonusSouls}");
            Console.WriteLine($"          拾い物 {drops} 個（鞄満杯 {bagFull}）  呪い 提示 {curseOffers} / 受けた {curseTaken}   " + string.Join(" ", rarityHits.OrderByDescending(k => k.Value).Select(k => $"{k.Key}{k.Value}")));
            Console.WriteLine($"          進行: 前進 {fwd} / 後退 {bwd}（前進率 {100.0 * fwd / Math.Max(1, fwd + bwd):F0}%）  章あたり 平均 {(chapters > 0 ? chapterG / chapters : 0)}G / 後退 {(chapters > 0 ? (double)setbackSum / chapters : 0):F1} 回");
        }

        // --- 極端な設定でも落ちないか ---
        var edge = JsonConvert.DeserializeObject<GameConfig>(File.ReadAllText(Root + "game_config.json"), JS);
        edge.at.monsters.Clear();
        edge.shop.items.Clear();
        edge.hero.lines.Clear();
        edge.atExpect.ranks.Clear();
        edge.at.entryPrecursorSpins.Clear();
        edge.bonusPrecursorSpins.Clear();
        edge.ceilingBonus.Clear();
        edge.adventure.treasures.Clear();
        foreach (var n in edge.adventure.nodes) { n.routeByFlag.Clear(); n.treasure.Clear(); }
        edge.adventure.resource.refillByFlag.Clear();
        edge.adventure.resource.startTorches = 0;   // 松明 0 本で始めても落ちないか
        try
        {
            var m2 = new SlotMachine(edge, wf, en, new SystemRandom(9), 1) { Credit = 1_000_000 };
            var p2 = new SystemRandom(3);
            for (int g = 0; g < 60_000; g++)
            {
                if (m2.Credit < 3 && !m2.IsReplay) m2.Credit += 100_000;
                m2.MaxBet(); m2.Lever();
                for (int i = 0; i < 3; i++) m2.Stop(i, p2.Next(20), 4, (float)p2.NextDouble());
                m2.Evaluate();
            }
            Console.WriteLine($"空テーブルでも 6万G 完走（AT {m2.InAt} / ソウル {m2.Wallet.Souls}）");
        }
        catch (Exception e) { Bad("空テーブルで例外: " + e.GetType().Name + " " + e.Message); }

        // --- 設定 1〜6 で機械割 ---
        Console.WriteLine("\n設定別の機械割:");
        for (int setting = 1; setting <= 6; setting++)
        {
            var m3 = new SlotMachine(cfg, wf, en, new SystemRandom(777), setting) { Credit = 100_000_000 };
            var p3 = new SystemRandom(21);
            long cin = 0, cout = 0;
            for (int g = 0; g < 2_000_000; g++)
            {
                bool rep = m3.IsReplay;
                m3.MaxBet(); m3.Lever();
                var order = new List<int> { 0, 1, 2 };
                if (m3.Navi2.Active) { var rest = new List<int> { 0, 1, 2 }; rest.Remove(m3.Navi2.first); order = new List<int> { m3.Navi2.first, rest[0], rest[1] }; }
                else if (m3.Navi.Active) { int f = m3.Navi.first, c = m3.Navi.correctReel; int t = new[] { 0, 1, 2 }.First(i => i != f && i != c); order = new List<int> { f, c, t }; }
                foreach (int i in order) m3.Stop(i, p3.Next(20), 4, (float)p3.NextDouble());
                var r = m3.Evaluate();
                if (m3.AdventureEnabled && m3.Adv.MustReturn)
                {
                    var rc3 = cfg.adventure.resource;
                    if (m3.Adv.returnReason == "credit") AdventureDirector.ApplyDeathPenalty(cfg.adventure, m3.Adv, m3.Wallet);
                    m3.Adv.returnReason = null;
                    AdventureDirector.AddTorch(cfg.adventure, m3.Adv, rc3.maxTorches, m3.TorchSpinsPerUnit);
                }
                if (!rep) cin += 3;
                cout += r.win.payout;
            }
            Console.WriteLine($"  設定{setting}: {100.0 * cout / cin:F1}%");
        }

        // 冒険の有無で機械割がどれだけ動くか（設定1・順押し）
        Console.WriteLine();
        Console.WriteLine("冒険の出玉への影響（設定1・50万G）:");
        foreach (bool advOn in new[] { false, true })
        {
            var c2 = JsonConvert.DeserializeObject<GameConfig>(File.ReadAllText(Root + "game_config.json"), JS);
            c2.adventure.enabled = advOn;
            var m4 = new SlotMachine(c2, wf, en, new SystemRandom(777), 1) { Credit = 100_000_000 };
            var p4 = new SystemRandom(21);
            long cin = 0, cout = 0;
            for (int g = 0; g < 500_000; g++)
            {
                bool rep = m4.IsReplay;
                m4.MaxBet(); m4.Lever();
                for (int i = 0; i < 3; i++) m4.Stop(i, p4.Next(20), 4, (float)p4.NextDouble());
                var r4 = m4.Evaluate();
                cout += r4.win.payout;
                if (m4.AdventureEnabled && m4.Adv.MustReturn)
                {
                    if (m4.Adv.returnReason == "credit") AdventureDirector.ApplyDeathPenalty(c2.adventure, m4.Adv, m4.Wallet);
                    m4.Adv.returnReason = null;
                    AdventureDirector.AddTorch(c2.adventure, m4.Adv, c2.adventure.resource.maxTorches, m4.TorchSpinsPerUnit);
                }
                if (!rep) cin += 3;
            }
            Console.WriteLine($"  冒険 {(advOn ? "あり" : "なし")}: {100.0 * cout / cin:F1}%");
        }

        // 路銀を補充せずに回して、力尽きるまでの G 数を見る（E: 死に戻り）
        Console.WriteLine();
        Console.WriteLine("路銀 500 枚から補充なしで回す（10 回・設定1・ナビ従・松明は買える分だけ補給）:");
        {
            var lives = new List<int>();
            int survived = 0;
            for (int trial = 0; trial < 10; trial++)
            {
                var m5 = new SlotMachine(cfg, wf, en, new SystemRandom(4242 + trial), 1) { Credit = 500 };
                var p5 = new SystemRandom(11 + trial);
                var rc5 = cfg.adventure.resource;
                int g5 = 0;
                bool died = false;
                for (; g5 < 100_000 && !died; g5++)
                {
                    if (m5.Credit < SlotMachine.BetCost && !m5.IsReplay) { died = true; break; }
                    m5.MaxBet(); m5.Lever();
                    var ord = new List<int> { 0, 1, 2 };
                    if (m5.Navi2.Active) { var rest = new List<int> { 0, 1, 2 }; rest.Remove(m5.Navi2.first); ord = new List<int> { m5.Navi2.first, rest[0], rest[1] }; }
                    else if (m5.Navi.Active) { int f = m5.Navi.first, c = m5.Navi.correctReel; int t = new[] { 0, 1, 2 }.First(i => i != f && i != c); ord = new List<int> { f, c, t }; }
                    foreach (int i in ord) m5.Stop(i, p5.Next(20), 4, (float)p5.NextDouble());
                    var r5 = m5.Evaluate();
                    if (r5.ranOutOfCredit) { died = true; break; }
                    if (m5.Adv.MustReturn)
                    {
                        m5.Adv.returnReason = null;
                        while (m5.Adv.torches < 3 && m5.Wallet.Souls >= rc5.torchCost)
                        {
                            if (AdventureDirector.AddTorch(cfg.adventure, m5.Adv, 1, m5.TorchSpinsPerUnit) <= 0) break;
                            m5.Wallet.Souls -= rc5.torchCost;
                        }
                        // 松明が買えないときは路銀を削って買う（実プレイの詰み回避）
                        if (m5.Adv.torches <= 0) { AdventureDirector.AddTorch(cfg.adventure, m5.Adv, 1, m5.TorchSpinsPerUnit); }
                    }
                }
                if (died) lives.Add(g5); else survived++;
            }
            if (lives.Count > 0) Console.WriteLine($"  力尽きた {lives.Count} 回: 平均 {lives.Average():F0} G  最短 {lives.Min()} G  最長 {lives.Max()} G");
            if (survived > 0) Console.WriteLine($"  10万G 生き延びた: {survived} 回");
        }

        // --- ステータスを振り切ると出玉がどれだけ動くか（設定1・ナビ従・50万G）---
        Console.WriteLine();
        Console.WriteLine("ステータスの出玉への影響（設定1・50万G・ナビ従）:");
        foreach (var (name, life, tech, luck) in new[] {
            ("振らない        ", 0, 0, 0),
            ("ライフ全振り    ", 20, 0, 0),
            ("テクニック全振り", 0, 20, 0),
            ("ラック全振り    ", 0, 0, 20),
            ("均等に振る      ", 20, 20, 20) })
        {
            var c6 = JsonConvert.DeserializeObject<GameConfig>(File.ReadAllText(Root + "game_config.json"), JS);
            var m6 = new SlotMachine(c6, wf, en, new SystemRandom(777), 1) { Credit = 100_000_000 };
            m6.Stats.Life = life; m6.Stats.Technique = tech; m6.Stats.Luck = luck;
            var p6 = new SystemRandom(21);
            long cin = 0, cout = 0, freeBets = 0, defeats = 0;
            for (int g = 0; g < 500_000; g++)
            {
                bool rep = m6.IsReplay;
                int before = m6.Credit;
                m6.MaxBet();
                if (!rep) cin += before - m6.Credit;
                if (m6.LastBetWasFree) freeBets++;
                m6.Lever();
                var ord = new List<int> { 0, 1, 2 };
                if (m6.Navi2.Active) { var rest = new List<int> { 0, 1, 2 }; rest.Remove(m6.Navi2.first); ord = new List<int> { m6.Navi2.first, rest[0], rest[1] }; }
                else if (m6.Navi.Active) { int f = m6.Navi.first, c = m6.Navi.correctReel; int t = new[] { 0, 1, 2 }.First(i => i != f && i != c); ord = new List<int> { f, c, t }; }
                foreach (int i in ord) m6.Stop(i, p6.Next(20), 4, (float)p6.NextDouble());
                var r6 = m6.Evaluate();
                cout += r6.win.payout;
                if (r6.enemyResolved == true) defeats++;
                if (m6.AdventureEnabled && m6.Adv.MustReturn)
                {
                    if (m6.Adv.returnReason == "credit") AdventureDirector.ApplyDeathPenalty(c6.adventure, m6.Adv, m6.Wallet);
                    m6.Adv.returnReason = null;
                    AdventureDirector.AddTorch(c6.adventure, m6.Adv, c6.adventure.resource.maxTorches, m6.TorchSpinsPerUnit);
                }
            }
            Console.WriteLine($"  {name}: 機械割 {100.0 * cout / Math.Max(1, cin):F1}%   BET無料 {freeBets:N0}G   討伐 {defeats:N0}");
        }

        // --- AT（洞窟）の中身を測る ---
        Console.WriteLine();
        Console.WriteLine("AT の中身（設定1・ナビ従・100万G）:");
        {
            var m7 = new SlotMachine(cfg, wf, en, new SystemRandom(555), 1) { Credit = 100_000_000 };
            var p7 = new SystemRandom(31);
            long atG = 0, atIn = 0, atOut = 0, naviHit = 0, naviMiss = 0, common = 0, zoneG = 0;
            int runs = 0, battles = 0, curLen = 0;
            var atLens = new List<int>();
            var zoneHits = new Dictionary<string, int>();
            for (int g = 0; g < 1_000_000; g++)
            {
                bool inAt = m7.InAt && m7.BonusMode == BonusMode.NORMAL;
                bool rep = m7.IsReplay;
                bool wasAt = m7.InAt;
                int before = m7.Credit;
                m7.MaxBet();
                m7.Lever();
                if (!wasAt && m7.InAt) runs++;
                var ord = new List<int> { 0, 1, 2 };
                if (m7.Navi2.Active) { var rest = new List<int> { 0, 1, 2 }; rest.Remove(m7.Navi2.first); ord = new List<int> { m7.Navi2.first, rest[0], rest[1] }; }
                else if (m7.Navi.Active) { int f = m7.Navi.first, c = m7.Navi.correctReel; int t = new[] { 0, 1, 2 }.First(i => i != f && i != c); ord = new List<int> { f, c, t }; }
                foreach (int i in ord) m7.Stop(i, p7.Next(20), 4, (float)p7.NextDouble());
                var r7 = m7.Evaluate();
                if (inAt)
                {
                    atG++; curLen++;
                    if (!rep) atIn += before - m7.Credit == 0 ? 0 : 3;
                    atOut += r7.win.payout;
                    if (r7.naviCorrect == true) naviHit++;
                    else if (r7.naviCorrect == false) naviMiss++;
                    else if (r7.win.winType == WinType.BELL) common++;
                    if (m7.AtZone != null) zoneG++;
                }
                if (r7.zoneStarted != null) zoneHits[r7.zoneStarted.name] = zoneHits.TryGetValue(r7.zoneStarted.name, out var zc) ? zc + 1 : 1;
                if (r7.battleStarted) battles++;
                if (r7.atEnded) { atLens.Add(curLen); curLen = 0; }
                if (m7.AdventureEnabled && m7.Adv.MustReturn)
                {
                    if (m7.Adv.returnReason == "credit") AdventureDirector.ApplyDeathPenalty(cfg.adventure, m7.Adv, m7.Wallet);
                    m7.Adv.returnReason = null;
                    AdventureDirector.AddTorch(cfg.adventure, m7.Adv, cfg.adventure.resource.maxTorches, m7.TorchSpinsPerUnit);
                }
            }
            Console.WriteLine($"  AT {runs} 回 / 合計 {atG:N0}G（1 回あたり {(runs > 0 ? atG / runs : 0)}G）");
            Console.WriteLine($"  純増 {(atG > 0 ? (atOut - atIn) / (double)atG : 0):+0.00;-0.00} 枚/G   1 回あたり {(runs > 0 ? (atOut - atIn) / runs : 0):N0} 枚");
            Console.WriteLine($"  ベル: ナビ正解 {naviHit:N0} / 外し {naviMiss:N0} / 共通 {common:N0}");
            Console.WriteLine($"  ゾーン滞在 {zoneG:N0}G（AT の {(atG > 0 ? 100.0 * zoneG / atG : 0):F0}%）  狩猟 {battles} 回");
            if (atLens.Count > 0)
            {
                atLens.Sort();
                Console.WriteLine($"  1 回のG数: 中央 {atLens[atLens.Count / 2]}G / 平均 {atLens.Average():F0}G / 最長 {atLens[atLens.Count - 1]}G");
                Console.Write("  分布: ");
                foreach (int th in new[] { 50, 100, 200, 300, 400, 600, 800 })
                    Console.Write($"{th}G以上 {100.0 * atLens.Count(x => x >= th) / atLens.Count:F0}%  ");
                Console.WriteLine();
            }
            Console.WriteLine("  ゾーン別: " + string.Join(" / ", zoneHits.OrderByDescending(k => k.Value).Select(k => $"{k.Key} {k.Value}回")));
        }

        Console.WriteLine();
        if (Problems.Count == 0) Console.WriteLine("不変条件の違反なし");
        else foreach (var pr in Problems) Console.WriteLine($"  NG {pr}（{ProblemCount[pr]} 回）");
    }
}
