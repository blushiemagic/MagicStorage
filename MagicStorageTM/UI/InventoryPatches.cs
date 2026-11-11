using System;
using System.Reflection;
using HarmonyLib;
using MagicStorageTM.Storage;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.UI;

namespace MagicStorageTM.UI
{
    /// <summary>Shift-click an inventory item while the storage window is open to deposit it (like a chest).</summary>
    internal static class InventoryPatches
    {
        private const int InventoryContext = 0;
        private const int CursorDepositIcon = 8; // vanilla "move to chest" cursor

        public static void Apply(Harmony h)
        {
            const BindingFlags A = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var args = new[] { typeof(Item[]), typeof(int), typeof(int) };
            MethodInfo hover = typeof(ItemSlot).GetMethod("OverrideHover", A, null, args, null);
            MethodInfo click = typeof(ItemSlot).GetMethod("OverrideLeftClick", A, null, args, null);
            if (hover != null) h.Patch(hover, postfix: new HarmonyMethod(typeof(InventoryPatches).GetMethod(nameof(OverrideHover_Postfix), A)));
            else Log.Warn("[UI] ItemSlot.OverrideHover not found; no deposit cursor");
            if (click != null) h.Patch(click, prefix: new HarmonyMethod(typeof(InventoryPatches).GetMethod(nameof(OverrideLeftClick_Prefix), A)));
            else Log.Warn("[UI] ItemSlot.OverrideLeftClick not found; shift-click deposit disabled");
        }

        private static bool Applies(Item[] inv, int context, int slot)
        {
            return MagicUI.Mode == UIMode.Storage
                && context == InventoryContext
                && inv == Main.LocalPlayer.inventory
                && slot >= 0 && slot < inv.Length
                && !inv[slot].IsAir && !inv[slot].favorited
                && ItemSlot.ShiftInUse;
        }

        private static void OverrideHover_Postfix(Item[] inv, int context, int slot)
        {
            try
            {
                if (Applies(inv, context, slot)) Main.cursorOverride = CursorDepositIcon;
            }
            catch { }
        }

        private static bool OverrideLeftClick_Prefix(Item[] inv, int context, int slot, ref bool __result)
        {
            try
            {
                if (!Applies(inv, context, slot)) return true;
                if (StorageOps.Deposit(MagicUI.Heart, inv[slot]))
                    SoundEngine.PlaySound(SoundID.Grab);
                else
                    Content.Interactions.Notify("The storage system is full.");
                __result = true;
                return false;
            }
            catch (Exception ex)
            {
                Log.Error("[UI] Shift-click deposit failed", ex);
                return true;
            }
        }
    }
}
