using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MagicStorageTM.Storage;
using Terraria;
using Terraria.ID;

namespace MagicStorageTM.Content
{
    /// <summary>
    /// Shadow Diamond: first kill of each boss per world (MagicStorage's ShadowDiamondDrop).
    /// Radiant Jewel: Moon Lord, 10% / 18% / 25% (normal / expert / master) with pity on failures.
    /// Implemented as a postfix on NPC.NPCLoot; dropped straight into the world.
    /// </summary>
    internal static class MSDrops
    {
        // npc type → (normal stack, expert stack); shared keys for multi-part bosses
        private static readonly Dictionary<int, (int key, int normal, int expert)> Diamonds = new Dictionary<int, (int, int, int)>
        {
            [NPCID.KingSlime] = (NPCID.KingSlime, 1, 1),
            [NPCID.EyeofCthulhu] = (NPCID.EyeofCthulhu, 1, 2),
            [NPCID.EaterofWorldsHead] = (NPCID.EaterofWorldsHead, 1, 1),
            [NPCID.EaterofWorldsBody] = (NPCID.EaterofWorldsHead, 1, 1),
            [NPCID.EaterofWorldsTail] = (NPCID.EaterofWorldsHead, 1, 1),
            [NPCID.BrainofCthulhu] = (NPCID.BrainofCthulhu, 1, 1),
            [NPCID.SkeletronHead] = (NPCID.SkeletronHead, 1, 1),
            [NPCID.QueenBee] = (NPCID.QueenBee, 1, 1),
            [NPCID.Deerclops] = (NPCID.Deerclops, 1, 1),
            [NPCID.WallofFlesh] = (NPCID.WallofFlesh, 2, 2),
            [NPCID.TheDestroyer] = (NPCID.TheDestroyer, 1, 1),
            [NPCID.Retinazer] = (NPCID.Retinazer, 1, 1),
            [NPCID.Spazmatism] = (NPCID.Retinazer, 1, 1),
            [NPCID.SkeletronPrime] = (NPCID.SkeletronPrime, 1, 1),
            [NPCID.Plantera] = (NPCID.Plantera, 1, 2),
            [NPCID.Golem] = (NPCID.Golem, 1, 1),
            [NPCID.DukeFishron] = (NPCID.DukeFishron, 1, 1),
            [NPCID.CultistBoss] = (NPCID.CultistBoss, 1, 1),
            [NPCID.MoonLordCore] = (NPCID.MoonLordCore, 2, 3),
            [NPCID.QueenSlimeBoss] = (NPCID.QueenSlimeBoss, 1, 1),
            [NPCID.HallowBoss] = (NPCID.HallowBoss, 1, 1),
        };

        public static void Apply(Harmony h)
        {
            MethodInfo loot = typeof(NPC).GetMethod("NPCLoot", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
            if (loot == null) { Log.Warn("[Drops] NPC.NPCLoot not found; material drops disabled"); return; }
            h.Patch(loot, postfix: new HarmonyMethod(typeof(MSDrops).GetMethod(nameof(NPCLoot_Postfix), BindingFlags.Static | BindingFlags.NonPublic)));
        }

        private static void NPCLoot_Postfix(NPC __instance)
        {
            try
            {
                if (Main.netMode != 0) return;
                NPC npc = __instance;
                if (Diamonds.TryGetValue(npc.type, out var d))
                {
                    bool isEow = npc.type == NPCID.EaterofWorldsHead || npc.type == NPCID.EaterofWorldsBody || npc.type == NPCID.EaterofWorldsTail;
                    bool twinsAlive = (npc.type == NPCID.Retinazer && NPC.AnyNPCs(NPCID.Spazmatism)) || (npc.type == NPCID.Spazmatism && NPC.AnyNPCs(NPCID.Retinazer));
                    if ((!isEow || npc.boss) && !twinsAlive && !StorageWorld.DiamondBosses.Contains(d.key))
                    {
                        lock (StorageWorld.Sync) StorageWorld.DiamondBosses.Add(d.key);
                        Drop(npc, MSItems.TypeOf("shadow-diamond"), Main.expertMode ? d.expert : d.normal);
                    }
                }

                if (npc.type == NPCID.MoonLordCore)
                {
                    float chance = Main.masterMode ? 0.25f : Main.expertMode ? 0.18f : 0.10f;
                    chance *= 1f + StorageWorld.RadiantJewelFailures * (2f / 3f);
                    if (Main.rand.NextFloat() < chance)
                    {
                        StorageWorld.RadiantJewelFailures = 0;
                        Drop(npc, MSItems.TypeOf("radiant-jewel"), 1);
                    }
                    else
                    {
                        StorageWorld.RadiantJewelFailures++;
                    }
                }
            }
            catch (Exception ex) { Log.Error("[Drops] NPCLoot postfix failed", ex); }
        }

        private static void Drop(NPC npc, int type, int stack)
        {
            if (type <= 0 || stack <= 0) return;
            Item.NewItem(npc.GetItemSource_Loot(), npc.Center, type, stack);
        }
    }
}
