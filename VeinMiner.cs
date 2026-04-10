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
        public override string Name => "RPG Leveling Max 30";
        public override string Author => "Gemini";
        public override Version Version => new Version(1, 2, 1);

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
            else
                args.Player.SendSuccessMessage("XP: MAXIMUM REACHED");
                
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

                player.GiveItem(74, 1); 

                if (stats.Level >= MAX_LEVEL)
                {
                    TSPlayer.All.SendMessage($"[LEGEND] {player.Name} mencapai LEVEL MAKSIMAL (30)! 🏆", Color.Cyan);
                }
                else
                {
                    TSPlayer.All.SendMessage($"[LEVEL UP] {player.Name} Level {stats.Level}! Bonus: 1 Platinum Coin 💰", Color.Gold);
                }
                player.SendSuccessMessage("Kekuatanmu meningkat!");
            }
        }

        private void OnUpdate(EventArgs args)
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (p == null || !p.active) continue;

                if (playerData.ContainsKey(p.name))
                {
                    var stats = playerData[p.name];
                    
                    // Defense Bonus
                    p.statDefense += stats.Level;
                    
                    // Global Damage Bonus (1% per level)
                    // Menggunakan properti standar Terraria untuk modifikasi damage
                    float multiplier = 1f + (stats.Level * 0.01f);
                    p.GetDamage(Terraria.ModLoader.DamageClass.Generic) *= multiplier;
                }
            }
        }
        
        // Cadangan jika GetDamage tetap error, gunakan versi manual ini:
        /*
        private void OnUpdate(EventArgs args)
        {
            foreach (TSPlayer player in TShock.Players)
            {
                if (player == null || !player.Active || player.TPlayer == null) continue;
                if (playerData.ContainsKey(player.Name))
                {
                    var stats = playerData[player.Name];
                    player.TPlayer.statDefense += stats.Level;
                    // Properti vanilla lama:
                    // player.TPlayer.allDamage += (stats.Level * 0.01f);
                }
            }
        }
        */

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
