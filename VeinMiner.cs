using System;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;

namespace WhipRework
{
    [ApiVersion(2, 1)]
    public class DynamicWhipPlugin : TerrariaPlugin
    {
        public override string Name => "Homing Whip Rework Fixed";
        public override string Author => "Player";
        public override Version Version => new Version(1, 2, 2);

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

            if (item != null && item.damage > 0 && item.summon && item.pick == 0)
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
            int projID = 307; // Default: Chlorophyte Orb
            
            string itemName = item.Name ?? "";

            if (itemName.Contains("Snapthorn")) projID = 181;
            else if (itemName.Contains("Fire") || itemName.Contains("Spicer")) projID = 504;
            else if (itemName.Contains("Cool") || itemName.Contains("Ice")) projID = 344;
            else if (itemName.Contains("Kaleidoscope")) projID = 636;
            else if (itemName.Contains("Morning Star")) projID = 729;

            Vector2 velocity = new Vector2(tsPlayer.TPlayer.direction * 10f, 0f);
            float closestDist = 600f; 
            int targetNPC = -1;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc != null && npc.active && !npc.friendly && npc.damage > 0)
                {
                    float dist = Vector2.Distance(pos, npc.Center);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        targetNPC = i;
                    }
                }
            }

            if (targetNPC != -1)
            {
                velocity = Vector2.Normalize(Main.npc[targetNPC].Center - pos) * 12f;
            }

            int proj = Projectile.NewProjectile(null, pos, velocity, projID, item.damage, 5f, tsPlayer.Index);
            
            if (targetNPC != -1 && proj >= 0 && proj < Main.maxProjectiles) 
            {
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
