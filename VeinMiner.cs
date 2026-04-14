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
        public override string Name => "Mage Ultimate Rework";
        public override string Author => "Gemini AI";
        public override string Description => "Rework senjata mage dengan peluru tambahan dan homing";
        public override Version Version => new Version(1, 1, 0);

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

                // Pengaturan Default
                int extraType = 0;
                float dmgMult = 0.4f;
                bool isHoming = false;
                string name = player.SelectedItem.Name.ToLower();

                // --- LOGIKA PER KATEGORI SENJATA ---
                if (name.Contains("staff")) 
                {
                    extraType = 122; // Mana Bolt (Biru)
                    dmgMult = 0.5f;
                    isHoming = true; 
                }
                else if (name.Contains("book") || player.SelectedItem.type == 113) 
                {
                    extraType = type; // Meniru peluru asli (Echo)
                    dmgMult = 0.35f;
                    isHoming = false; 
                }
                else if (player.SelectedItem.useStyle == 5) // Tipe Senjata Api/Guns
                {
                    extraType = 82; // Pink Laser
                    dmgMult = 0.4f;
                    isHoming = false; 
                }
                else // Wands atau lainnya
                {
                    extraType = 521; // Shadow Spark
                    dmgMult = 0.3f;
                    isHoming = true;
                }

                // --- SPAWN PROJECTILE TAMBAHAN ---
                if (extraType != 0)
                {
                    Vector2 velocity = new Vector2(vX, vY);

                    if (isHoming)
                    {
                        NPC target = FindClosestNPC(posX, posY, 500f);
                        if (target != null)
                        {
                            Vector2 direction = target.Center - new Vector2(posX, posY);
                            direction.Normalize();
                            // Kecepatan peluru homing sedikit lebih santai
                            velocity = direction * (new Vector2(vX, vY).Length() * 0.85f);
                        }
                    }

                    int p = Projectile.NewProjectile(null, posX, posY, velocity.X, velocity.Y, extraType, 
                        (int)(player.SelectedItem.damage * dmgMult), 1f, owner);
                    
                    NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, null, p);
                }

                // --- ATTACK SPEED BURST (KADANG-KADANG) ---
                // Peluang 15% peluru melesat jauh lebih cepat
                if (Main.rand.NextDouble() < 0.15)
                {
                    vX *= 1.85f;
                    vY *= 1.85f;
                    // Sinkronisasi kecepatan baru ke semua player
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
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closest = npc;
                    }
                }
            }
            return closest;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
            }
            base.Dispose(disposing);
        }
    }
}
