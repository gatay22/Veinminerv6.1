using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;
using System.IO;
using System.Collections.Generic;

namespace MageReworkPlugin
{
    [ApiVersion(2, 1)]
    public class MageRework : TerrariaPlugin
    {
        public override string Name => "Mage Balanced Fixed";
        public override string Author => "Gemini AI";
        public override Version Version => new Version(1, 5, 0);

        private Dictionary<int, long> _lastExtraProjTime = new Dictionary<int, long>();

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

                // --- 1. FILTER GLOBAL ANTI-SPAM ---
                long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                if (!_lastExtraProjTime.ContainsKey(owner)) _lastExtraProjTime[owner] = 0;
                
                // Cooldown dinaikkan ke 300ms khusus untuk senjata tipe semburan/sparking
                int cooldownValue = 300;
                if (currentTime - _lastExtraProjTime[owner] < cooldownValue) return;
                
                _lastExtraProjTime[owner] = currentTime;

                // --- 2. PEMILIHAN PROYEKTIL YANG RINGAN (TIDAK MELEDUK) ---
                int extraType = 0;
                float dmgMult = 0.3f;
                bool shouldHome = true;
                string name = player.SelectedItem.Name.ToLower();

                if (name.Contains("sparking"))
                {
                    // Ganti ke ID 504 (Blue Flare) - Sangat ringan, tidak nyepam partikel pink
                    extraType = 504; 
                    dmgMult = 0.2f;
                }
                else if (name.Contains("staff"))
                {
                    extraType = 122; // Mana Bolt
                    dmgMult = 0.4f;
                }
                else if (name.Contains("book"))
                {
                    extraType = 121; // Crystal Shard (Visual bersih)
                    shouldHome = false;
                }
                else 
                {
                    extraType = 122; // Default ke Mana Bolt yang stabil
                }

                if (extraType != 0)
                {
                    Vector2 velocity = new Vector2(vX, vY);

                    if (shouldHome)
                    {
                        NPC target = FindClosestNPC(posX, posY, 500f);
                        if (target != null)
                        {
                            Vector2 direction = target.Center - new Vector2(posX, posY);
                            direction.Normalize();
                            velocity = direction * 9f; // Kecepatan dikurangi biar gak liar
                        }
                    }

                    int p = Projectile.NewProjectile(null, posX, posY, velocity.X, velocity.Y, extraType, 
                        (int)(player.SelectedItem.damage * dmgMult), 1f, owner);
                    
                    NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, null, p);
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
