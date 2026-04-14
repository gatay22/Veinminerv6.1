using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;
using System.IO;

namespace MageReworkPlugin
{
    [ApiVersion(2, 1)]
    public class MageRework : TerrariaPlugin
    {
        public override string Name => "Mage Unique Projectiles";
        public override string Author => "Gemini AI";
        public override Version Version => new Version(1, 2, 0);

        public MageRework(Main game) : base(game) { }

        public override void Initialize()
        {
            ServerApi.Hooks.NetGetData.Register(this, OnGetData);
        }

        private void OnGetData(GetDataEventArgs args)
        {
            if (args.MsgID != PacketTypes.ProjectileNew) return;

            using (var reader = new BinaryReader(new MemoryStream(args.Msg.readBuffer, args.Index, args.Length)))
            {
                short identity = reader.ReadInt16();
                float posX = reader.ReadSingle();
                float posY = reader.ReadSingle();
                float vX = reader.ReadSingle();
                float vY = reader.ReadSingle();
                byte owner = reader.ReadByte();
                short type = reader.ReadInt16();

                TSPlayer player = TShock.Players[owner];
                if (player == null || !player.Active || player.SelectedItem == null || !player.SelectedItem.magic) return;

                int extraType = 0;
                float dmgMult = 0.4f;
                bool isHoming = false;
                string name = player.SelectedItem.Name.ToLower();

                // --- SISTEM REWORK: EFEK BEDA-BEDA TIAP SENJATA ---

                if (name.Contains("staff")) // Tipe Tongkat
                {
                    extraType = 122; // Mana Bolt (Biru, Ngejar)
                    isHoming = true;
                    dmgMult = 0.5f;
                }
                else if (name.Contains("book") || player.SelectedItem.type == 113) // Tipe Buku
                {
                    extraType = 27; // Water Stream Style (Cepat, Linear)
                    isHoming = false;
                    dmgMult = 0.45f;
                }
                else if (name.Contains("rod") || name.Contains("wand")) // Tipe Batang/Wand
                {
                    extraType = 521; // Shadow Spark (Mistik, Ngejar)
                    isHoming = true;
                    dmgMult = 0.4f;
                }
                else if (player.SelectedItem.useStyle == 5) // Tipe Pistol (Laser/Gun)
                {
                    extraType = 82; // Pink Laser (Instan, Lurus)
                    isHoming = false;
                    dmgMult = 0.35f;
                }
                else if (name.Contains("harp") || name.Contains("bell") || name.Contains("guitar")) // Tipe Alat Musik
                {
                    extraType = 76; // Music Note (Melayang acak)
                    isHoming = true;
                }
                else // Senjata Mage lainnya (Misc)
                {
                    extraType = 121; // Crystal Shard
                    isHoming = false;
                }

                // --- PROSES SPAWN PROYEKTIL ---
                if (extraType != 0)
                {
                    Vector2 velocity = new Vector2(vX, vY);

                    if (isHoming)
                    {
                        NPC target = FindClosestNPC(posX, posY, 600f);
                        if (target != null)
                        {
                            Vector2 direction = target.Center - new Vector2(posX, posY);
                            direction.Normalize();
                            velocity = direction * (new Vector2(vX, vY).Length() * 0.9f);
                        }
                    }

                    int p = Projectile.NewProjectile(null, posX, posY, velocity.X, velocity.Y, extraType, 
                        (int)(player.SelectedItem.damage * dmgMult), 1f, owner);
                    
                    NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, null, p);
                }

                // --- BURST ATTACK SPEED (KADANG-KADANG) ---
                if (Main.rand.NextDouble() < 0.15)
                {
                    vX *= 2.0f;
                    vY *= 2.0f;
                    NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, null, identity);
                }
            }
        }

        private NPC FindClosestNPC(float x, float y, float maxDist)
        {
            NPC closest = null;
            float minDist = maxDist;
            foreach (NPC npc in Main.npc)
            {
                if (npc != null && npc.active && !npc.friendly && npc.lifeMax > 5 && !npc.dontTakeDamage)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), npc.Center);
                    if (dist < minDist) { minDist = dist; closest = npc; }
                }
            }
            return closest;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
            base.Dispose(disposing);
        }
    }
}
