using System;
using System.Linq;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;

namespace WhipRework
{
    [ApiVersion(2, 1)]
    public class DynamicWhipPlugin : TerrariaPlugin
    {
        public override string Name => "Homing Whip Rework";
        public override string Author => "Player";
        public override Version Version => new Version(1, 2, 0);

        public DynamicWhipPlugin(Main game) : base(game) { }

        public override void Initialize()
        {
            ServerApi.Hooks.NetGetData.Register(this, OnGetData);
        }

        private void OnGetData(GetDataEventArgs args)
        {
            if (args.MsgID != PacketTypes.PlayerUpdate) return;

            TSPlayer tsPlayer = TShock.Players[args.Msg.whoAmI];
            if (tsPlayer == null || !tsPlayer.Active || tsPlayer.TPlayer == null) return;

            Player p = tsPlayer.TPlayer;
            Item item = p.inventory[p.selectedItem];

            if (item != null && item.damage > 0 && item.summon)
            {
                if (p.itemAnimation == p.itemAnimationMax - 1 && p.itemAnimation > 0)
                {
                    ApplyHomingWhip(tsPlayer, item);
                }
            }
        }

        private void ApplyHomingWhip(TSPlayer tsPlayer, Item item)
        {
            Vector2 pos = tsPlayer.TPlayer.Center;
            int projID;

            // Pilih ID proyektil berdasarkan nama cambuk
            if (item.name.Contains("Snapthorn")) projID = 181; // Bee (Homing otomatis)
            else if (item.name.Contains("Fire") || item.name.Contains("Spicer")) projID = 504; // Daybreak/Solar Flare
            else if (item.name.Contains("Cool") || item.name.Contains("Ice")) projID = 344; // Frost Shard
            else if (item.name.Contains("Kaleidoscope")) projID = 636; // Rainbow Crystal
            else projID = 307; // Chlorophyte Orb (Homing sangat kuat)

            // LOGIKA MENCARI MUSUH TERDEKAT (Homing)
            Vector2 velocity = new Vector2(tsPlayer.TPlayer.direction * 10f, 0f);
            float closestDist = 500f; // Jarak deteksi musuh (pixel)
            int targetNPC = -1;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && !npc.friendly && npc.damage > 0)
                {
                    float dist = Vector2.Distance(pos, npc.Center);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        targetNPC = i;
                    }
                }
            }

            // Jika musuh ketemu, arahkan velocity ke musuh
            if (targetNPC != -1)
            {
                velocity = Vector2.Normalize(Main.npc[targetNPC].Center - pos) * 12f;
            }

            int proj = Projectile.NewProjectile(null, pos, velocity, projID, item.damage, 5f, tsPlayer.Index);
            
            // Beritahu proyektil siapa targetnya (untuk proyektil tertentu)
            if (targetNPC != -1) {
                Main.projectile[proj].ai[0] = targetNPC;
            }

            NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, null, proj);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
            base.Dispose(disposing);
        }
    }
}
