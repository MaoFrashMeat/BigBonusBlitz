using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BBB.Core;
using Newtonsoft.Json;

// UE5 版の C++ 移植（UE5Project/Source/BigBonusBlitz/*/BBBCore）と同じ手順で Core を回し、1G ずつ状態を書き出す。
// C++ 側は BBBCoreTrace コマンドレットが同じ形式で書く。2 つのファイルが 1 行も違わなければ移植は一致。
//   dotnet run --project tools/verify/trace -c Release -- <出力ファイル> [1 通りあたりの G 数]
static class Program
{
    static string FindDataDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            string cand = Path.Combine(dir.FullName, "UnityProject", "BigBonusBlitz", "Assets", "Resources", "Data");
            if (Directory.Exists(cand)) return cand + Path.DirectorySeparatorChar;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("UnityProject/BigBonusBlitz/Assets/Resources/Data");
    }

    static string B(bool v) => v ? "1" : "0";
    static string S(string v) => string.IsNullOrEmpty(v) ? "~" : v;   // C++ 側は null と空を区別しないのでそろえる
    static string NB(bool? v) => v.HasValue ? (v.Value ? "1" : "0") : "~";

    static void Main(string[] args)
    {
        string outPath = args.Length > 0 ? args[0] : "trace_cs.txt";
        int games = args.Length > 1 ? int.Parse(args[1]) : 20000;
        string root = FindDataDir();
        var js = new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace };
        var cfg = JsonConvert.DeserializeObject<GameConfig>(File.ReadAllText(root + "game_config.json"), js);
        var wf = JsonConvert.DeserializeObject<Dictionary<string, WorkflowRole>>(File.ReadAllText(root + "workflow_config.json"), js);
        var en = JsonConvert.DeserializeObject<EnemyTableSet>(File.ReadAllText(root + "enemy_tables.json"), js).tables;

        var sb = new StringBuilder();
        // 乱数そのものの一致
        foreach (int seed in new[] { 0, 1, 42, 1000, -7, int.MaxValue })
        {
            var r = new SystemRandom(seed);
            sb.Append("RNG ").Append(seed);
            for (int i = 0; i < 8; i++) sb.Append(' ').Append(r.Next(1000));
            sb.Append(' ').Append(((long)(r.NextDouble() * 1e15)).ToString());
            sb.Append('\n');
        }

        for (int st = 0; st < 4; st++)
        {
            var m = new SlotMachine(cfg, wf, en, new SystemRandom(1000 + st), 1) { Credit = 1_000_000 };
            var push = new SystemRandom(50 + st);
            int curseOffers = 0;
            for (int g = 0; g < games; g++)
            {
                if (m.Credit < SlotMachine.BetCost && !m.IsReplay) m.Credit += 1_000_000;
                m.MaxBet();
                var hint = m.Lever();

                var order = new List<int> { 0, 1, 2 };
                if (m.Navi2.Active && st != 2)
                {
                    var rest = new List<int> { 0, 1, 2 }; rest.Remove(m.Navi2.first);
                    order = new List<int> { m.Navi2.first, rest[0], rest[1] };
                }
                else if (m.Navi.Active && st != 2)
                {
                    int f = m.Navi.first, cor = m.Navi.correctReel;
                    int third = 0; for (int i = 0; i < 3; i++) if (i != f && i != cor) { third = i; break; }
                    order = new List<int> { f, cor, third };
                }
                else if (st == 3)
                {
                    // OrderBy(_ => push.Next(100)) と同じ並べ方（安定ソート）を素朴に書く
                    var keys = new int[3]; for (int i = 0; i < 3; i++) keys[i] = push.Next(100);
                    var idx = new List<int> { 0, 1, 2 };
                    for (int a = 1; a < 3; a++) for (int b = a; b > 0 && keys[idx[b - 1]] > keys[idx[b]]; b--) { int t = idx[b]; idx[b] = idx[b - 1]; idx[b - 1] = t; }
                    order = new List<int> { order[idx[0]], order[idx[1]], order[idx[2]] };
                }
                foreach (int i in order) m.Stop(i, push.Next(20), 4, (float)push.NextDouble());
                var r = m.Evaluate();

                if (r.curseOffer != null)
                {
                    curseOffers++;
                    if (curseOffers % 2 == 0) m.Curse.Taken.Add(r.curseOffer);
                    m.Curse.Offer = null;
                }
                if (m.AdventureEnabled && m.Adv.MustReturn)
                {
                    string reason = m.Adv.returnReason;
                    m.Adv.returnReason = null;
                    var rc2 = cfg.adventure.resource;
                    if (reason != "torch")
                    {
                        AdventureDirector.ApplyDeathPenalty(cfg.adventure, m.Adv, m.Wallet);
                        if (m.RescueCredit > 0 && m.Credit < m.RescueCredit) m.Credit = m.RescueCredit;
                    }
                    m.EndRun();
                    while (m.Adv.torches < 3 && m.Wallet.Souls >= rc2.torchCost)
                    {
                        if (AdventureDirector.AddTorch(cfg.adventure, m.Adv, 1, m.TorchSpinsPerUnit) <= 0) break;
                        m.Wallet.Souls -= rc2.torchCost;
                    }
                    if (m.Adv.torches <= 0) { m.Adv.torches = 1; m.Adv.torchSpins = m.TorchSpinsPerUnit; }
                }

                sb.Append(st).Append(' ').Append(g)
                  .Append(" f").Append((int)r.flag).Append(" h").Append((int)hint).Append(" rng").Append(m.CurrentRng)
                  .Append(" s").Append(m.StopIndex[0]).Append(',').Append(m.StopIndex[1]).Append(',').Append(m.StopIndex[2])
                  .Append(" sl").Append(m.Slip[0]).Append(',').Append(m.Slip[1]).Append(',').Append(m.Slip[2])
                  .Append(" p").Append(r.win.payout).Append(" w").Append((int)r.win.winType)
                  .Append(" c").Append(m.Credit).Append(" rp").Append(B(m.IsReplay))
                  .Append(" m").Append((int)m.Mode).Append(" bm").Append((int)m.BonusMode).Append(" be").Append(m.BonusEarned)
                  .Append(" hb").Append((int)m.HeldBonusFlag).Append(" ba").Append(m.BonusAnnounceRemaining).Append(" ps").Append(B(m.PseudoPlay))
                  .Append(" at").Append(B(m.InAt)).Append(',').Append(m.AtSpinsRemaining).Append(',').Append(m.AtSet).Append(',').Append(m.AtEntryRemaining)
                  .Append(" jd").Append(B(m.InJudge)).Append(',').Append(m.JudgeRemaining)
                  .Append(" bt").Append(B(m.InBattle)).Append(',').Append(m.BattleHp).Append(',').Append(S(m.BattleMonster?.id))
                  .Append(" t2").Append(B(m.IsTier2)).Append(',').Append(m.Tier2SpinCount).Append(',').Append(m.EngageHp).Append(',').Append(B(m.EnemyActive)).Append(',').Append(m.PrecursorRemaining)
                  .Append(" nv").Append(B(m.Navi.Active)).Append(B(m.Navi2.Active))
                  .Append(" so").Append(m.Wallet.Souls).Append(" em").Append(m.Wallet.Embers)
                  .Append(" lv").Append(m.PlayerLevel).Append(',').Append(m.PlayerExp)
                  .Append(" adv").Append(S(m.Adv.nodeId)).Append(',').Append(m.Adv.spinsLeft).Append(',').Append(S(m.Adv.nextId)).Append(',').Append(m.Adv.torches).Append(',').Append(m.Adv.torchSpins).Append(',').Append(m.Adv.chapter)
                  .Append(" eq").Append(m.Equip.Bag.Count).Append(',').Append(m.Equip.Worn.Count).Append(" cu").Append(m.Curse.Taken.Count)
                  .Append(" r").Append(B(r.bonusStarted)).Append(B(r.bonusEnded)).Append(B(r.atStarted)).Append(B(r.atEnded)).Append(B(r.enemySpawned)).Append(B(r.levelUp)).Append(B(r.stageChanged)).Append(B(r.chapterCleared))
                  .Append(NB(r.enemyResolved)).Append(NB(r.battleResolved)).Append(NB(r.naviCorrect))
                  .Append(" sg").Append(r.soulsGained).Append(" dp").Append(r.defeatPercent).Append(" ax").Append(r.atExpectPercent)
                  .Append(" eqd").Append(S(r.equipDropped?.baseId)).Append(',').Append(r.equipDropped?.rarity ?? -1)
                  .Append(" te").Append(B(r.tech.Active)).Append(B(r.techSuccess)).Append(',').Append(S(r.techRank?.id)).Append(',').Append(r.techSouls)
                  .Append(" jh").Append(S(r.judgeHint)).Append(" rt").Append(S(r.routeDecided))
                  .Append('\n');
            }
        }
        File.WriteAllText(outPath, sb.ToString(), new UTF8Encoding(false));
        Console.WriteLine($"trace: {outPath}");
    }
}
