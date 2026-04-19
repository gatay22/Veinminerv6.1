using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace MageReworkPlugin
{
    [ApiVersion(2, 1)]
    public class MageAstralBalanced : TerrariaPlugin
    {
        public override string Name => "Mage Astral Balanced";
        public override string Author => "Gemini AI";
        public override Version Version => new Version(5, 1, 0);

        // Cooldown untuk mencegah Triple Strike muncul terlalu sering
        private Dictionary<int, long> _lastTripleStrike = new Dictionary<int, long>();

        public MageAstralBalanced(Main game) : base(game) { }

        public override void Initialize()
        {
            ServerApi.Hooks.NPCStrike.Register(this, OnNPCStrike);
        }

        private void OnNPCStrike(NPCStrikeEventArgs args)
        {
            TSPlayer player = TShock.Players[args.Player.whoAmI];
            if (player == null || !player.Active || player.SelectedItem == null || !player.SelectedItem.magic) return;

            NPC target = args.Npc;
            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            if (!_lastTripleStrike.ContainsKey(player.Index)) _lastTripleStrike[player.Index] = 0;

            // --- 1. ADAPTIVE BURST (Pasif) ---
            // Damage dasar tetap ada peningkatan tapi tipis saja (1.4x)
            args.Damage = (int)(args.Damage * 1.4f);

            // --- 2. TRIPLE STRIKE (Kondisional & Terukur) ---
            // Peluang 20% DAN hanya bisa muncul setiap 1.5 detik (Cooldown)
            if (Main.rand.NextDouble() < 0.20 && (currentTime - _lastTripleStrike[player.Index] > 1500))
            {
                _lastTripleStrike[player.Index] = currentTime;

                for (int i = 0; i < 2; i++)
                {
                    int extraDmg = (int)(args.Damage * 0.75f);
                    target.StrikeNPC(extraDmg, args.Knockback, args.HitDirection);
                    NetMessage.SendStrikeNPC(target, new NPC.HitInfo { Damage = extraDmg, Knockback = args.Knockback, HitDirection = args.HitDirection });
                }

                // Visual saat Triple Strike berhasil (Pesta Bintang)
                for (int j = 0; j < 12; j++)
                {
                    Dust.NewDust(target.position, target.width, target.height, 15, 0, -2, 100, Color.Cyan, 1.2f);
                }
                player.SendMessage("--- ASTRAL TRIPLE HIT ---", Color.DeepSkyBlue);
            }

            // --- 3. ASTRAL LINK (Hanya muncul saat Triple Strike Gagal) ---
            // Ini biar layar gak penuh. Kalau sudah Triple Hit, Link tidak muncul.
            else if (Main.rand.NextDouble() < 0.40) 
            {
                NPC nextTarget = FindNextTarget(target, 200f);
                if (nextTarget != null)
                {
                    int linkDmg = (int)(args.Damage * 0.4f);
                    nextTarget.StrikeNPC(linkDmg, 0f, 0);
                    NetMessage.SendStrikeNPC(nextTarget, new NPC.HitInfo { Damage = linkDmg });
                    DrawAstralLine(target.Center, nextTarget.Center);
                }
            }

            // --- 4. MANA RECOVERY ---
            player.TPlayer.statMana += 3;
            player.TPlayer.ManaEffect(3);
        }

        private NPC FindNextTarget(NPC current, float range)
        {
            foreach (NPC npc in Main.npc)
            {
                if (npc != null && npc.active && !npc.friendly && npc.whoAmI != current.whoAmI && npc.lifeMax > 5)
                {
                    if (Vector2.Distance(current.Center, npc.Center) <= range) return npc;
                }
            }
            return null;
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
            if (disposing) ServerApi.Hooks.NPCStrike.Deregister(this, OnNPCStrike);
            base.Dispose(disposing);
        }
    }
}
