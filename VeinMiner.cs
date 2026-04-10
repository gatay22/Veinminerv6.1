using System;
using System.Collections.Generic;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;

namespace RPGLevelPlugin
{
    [ApiVersion(2, 1)]
    public class RPGLevel : TerrariaPlugin
    {
        public override string Name => "RPG Leveling Final Fix";
        public override string Author => "Gemini";
        public override Version Version => new Version(1, 2, 4);

        private Dictionary<string, PlayerStats> playerData = new Dictionary<string, PlayerStats>();
        private const int MAX_LEVEL = 30;

        public class PlayerStats
        {
            public int Level = 1;
            public int XP = 0;
            public int NextLevelXP = 100;
        }

        public RPGLevel(Main game) : base(game) { }

        public override void Initialize()
        {
            ServerApi.Hooks.NpcKilled.Register(this, OnNpcKilled);
            ServerApi.Hooks.GameUpdate.Register(this, OnUpdate);
            Commands.ChatCommands.Add(new Command("rpg.check", CheckStats, "level", "stats"));
        }

        private void CheckStats(CommandArgs args)
        {
            if (!playerData.ContainsKey(args.Player.Name))
                playerData[args.Player.Name] = new PlayerStats();

            var stats = playerData[args.Player.Name];
            string levelDisplay = stats.Level >= MAX_LEVEL ? $"{stats.Level} [MAX]" : stats.Level.ToString();
            
            args.Player.SendInfoMessage("--- STATS KARAKTER ---");
            args.Player.SendSuccessMessage($"Level: {levelDisplay}");
            if (stats.Level < MAX_LEVEL)
                args.Player.SendSuccessMessage($"XP: {stats.XP} / {stats.NextLevelXP}");
            
            args.Player.SendInfoMessage($"Bonus: +{stats.Level} Defense & +{stats.Level}% Damage");
            args.Player.SendInfoMessage("----------------------");
        }

        private void OnNpcKilled(NpcKilledEventArgs args)
        {
            if (args.npc.lastInteraction < 0 || args.npc.lastInteraction > 255) return;

            TSPlayer player = TShock.Players[args.npc.lastInteraction];
            if (player == null || !player.Active) return;

            if (!playerData.ContainsKey(player.Name))
                playerData[player.Name] = new PlayerStats();

            var stats = playerData[player.Name];
            if (stats.Level >= MAX_LEVEL) return;

            int xpGain = Math.Max(5, args.npc.lifeMax / 15);
            stats.XP += xpGain;

            if (stats.XP >= stats.NextLevelXP)
            {
                stats.Level++;
                stats.XP = 0;
                stats.NextLevelXP = (int)(stats.NextLevelXP * 1.6); 

                player.GiveItem(74, 1); // 1 Platinum

                if (stats.Level >= MAX_LEVEL)
                    TSPlayer.All.SendMessage($"[LEGEND] {player.Name} mencapai LEVEL 30! 🏆", Color.Cyan);
                else
                    TSPlayer.All.SendMessage($"[LEVEL UP] {player.Name} Level {stats.Level}! +1 Platinum 💰", Color.Gold);
            }
        }

        private void OnUpdate(EventArgs args)
        {
            foreach (TSPlayer tsPlayer in TShock.Players)
            {
                if (tsPlayer == null || !tsPlayer.Active || tsPlayer.TPlayer == null) continue;

                if (playerData.ContainsKey(tsPlayer.Name))
                {
                    var stats = playerData[tsPlayer.Name];
                    Player p = tsPlayer.TPlayer;

                    // Bonus Defense (1 per level)
                    p.statDefense += stats.Level;

                    // Bonus Damage (1% per level)
                    // Menggunakan multiplier dasar yang ada di semua versi 1.4+
                    float boost = (float)stats.Level / 100f;
                    p.meleeDamage += boost;
                    p.rangedDamage += boost;
                    p.magicDamage += boost;
                    p.minionDamage += boost;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.NpcKilled.Deregister(this, OnNpcKilled);
                ServerApi.Hooks.GameUpdate.Deregister(this, OnUpdate);
            }
            base.Dispose(disposing);
        }
    }
}
