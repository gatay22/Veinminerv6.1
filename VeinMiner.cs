using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace MageReworkPlugin
{
    [ApiVersion(2, 1)]
    public class MageAstralBalanced : TerrariaPlugin
    {
        public override string Name => "Mage Astral Balanced";
        public override string Author => "Gemini AI";
        public override Version Version => new Version(5, 1, 2);

        private Dictionary<int, long> _lastTripleStrike = new Dictionary<int, long>();

        public MageAstralBalanced(Main game) : base(game) { }

        public override void Initialize()
        {
            ServerApi.Hooks.NpcStrike.Register(this, OnNpcStrike);
        }

        private void OnNpcStrike(NpcStrikeEventArgs args)
        {
            TSPlayer player = TShock.Players[args.Player.whoAmI];
            if (player == null || !player.Active || player.SelectedItem == null || !player.SelectedItem.magic) return;

            NPC target = args.Npc;
            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            if (!_lastTripleStrike.ContainsKey(player.Index)) _lastTripleStrike[player.Index] = 0;

            // --- 1. ADAPTIVE BURST ---
            args.Damage = (int)(args.Damage * 1.4f);

            // --- 2. TRIPLE STRIKE ---
            if (Main.rand.NextDouble() < 0.20 && (currentTime - _lastTripleStrike[player.Index] > 1500))
            {
                _lastTripleStrike[player.Index] = currentTime;

                for (int i = 0; i < 2; i++)
                {
                    int extraDmg = (int)(args.Damage * 0.75f);
                    
                    // Gunakan metode StrikeNPC versi lama yang lebih stabil di API TShock
                    target.StrikeNPC(extraDmg, 0f, 0); 
                    
                    // Gunakan SendData untuk sinkronisasi damage ke semua player
                    NetMessage.SendData((int)PacketTypes.NpcStrike, -1, -1, null, target.whoAmI, extraDmg);
                }

                for (int j = 0; j < 12; j++)
                {
                    Dust.NewDust(target.position, target.width, target.height, 15, 0, -2, 100, Color.Cyan, 1.2f);
                }
                player.SendMessage("--- ASTRAL TRIPLE HIT ---", Color.DeepSkyBlue);
            }
            // --- 3. ASTRAL LINK ---
            else if (Main.rand.NextDouble() < 0.40) 
            {
                NPC nextTarget = FindNextTarget(target, 200f);
                if (nextTarget != null)
                {
                    int linkDmg = (int)(args.Damage * 0.4f);
                    nextTarget.StrikeNPC(linkDmg, 0f, 0);
                    NetMessage.SendData((int)PacketTypes.NpcStrike, -1, -1, null, nextTarget.whoAmI, linkDmg);
                    DrawAstralLine(target.Center, nextTarget.Center);
                }
            }

            // --- 4. MANA RECOVERY ---
            player.TPlayer.statMana += 3;
            // Gunakan statManaMax2 untuk validasi
            if (player.TPlayer.statMana > player.TPlayer.statManaMax2) 
                player.TPlayer.statMana = player.TPlayer.statManaMax2;
        }

        private NPC? FindNextTarget(NPC current, float range)
        {
            NPC? closest = null;
            float minDist = range;
            foreach (NPC npc in Main.npc)
            {
                if (npc != null && npc.active && !npc.friendly && npc.whoAmI != current.whoAmI && npc.lifeMax > 5)
                {
                    float dist = Vector2.Distance(current.Center, npc.Center);
                    if (dist <= minDist)
                    {
                        minDist = dist;
                        closest = npc;
                    }
                }
            }
            return closest;
        }

        private void DrawAstralLine(Vector2 start, Vector2 end)
        {
            Vector2 step = (end - start) / 5;
            for (int i = 0; i < 5; i++)
            {
                Dust.NewDust(start + (step * i), 2, 2, 15, 0, 0, 100, Color.Cyan, 0.8f);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.NpcStrike.Deregister(this, OnNpcStrike);
            }
            base.Dispose(disposing);
        }
    }
}
