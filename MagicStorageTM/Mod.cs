using System;
using HarmonyLib;
using MagicStorageTM.Content;
using MagicStorageTM.Storage;
using MagicStorageTM.Tiles;
using MagicStorageTM.UI;
using Terraria;
using TerrariaModder.Core;
using TerrariaModder.Core.Events;

namespace MagicStorageTM
{
    /// <summary>
    /// Magic Storage for TerrariaModder. Replaces MagicStorageMod + its ModSystems/ModPlayers.
    ///
    /// Load order:
    ///   Initialize       → register custom tiles (types fixed at TileID.Count+), items, Harmony patches
    ///   OnContentReady   → resolve runtime item types
    ///   Main.Initialize_AlmostEverything (splash) → custom tile setup, then recipes (TilePatches.AfterSetup)
    ///   WorldFile.LoadWorld → custom tiles + storage restored from the world sidecar
    /// </summary>
    public sealed class MagicStorageMod : ModBase, IModLifecycle
    {
        private Harmony _harmony;
        private bool _active;

        public override void Initialize(ModContext context)
        {
            Log.Init(context.Logger);
            if (context.IsServer)
            {
                Log.Warn("Magic Storage (TerrariaModder port) is singleplayer-only; it stays inactive on dedicated servers.");
                return;
            }

            CustomTiles.ModFolder = context.ModFolder;
            MSTiles.Register();
            MSItems.Register(context);

            _harmony = new Harmony("terrariamodder.magicstorage");
            try
            {
                WorldSaveLayer.Apply(_harmony);
            }
            catch (Exception ex)
            {
                // Without the save layer our tiles would end up in the vanilla .wld. Refuse to run.
                CustomTiles.PlacementAllowed = false;
                Log.Error("World save layer could not be installed — Magic Storage blocks are DISABLED to protect your worlds", ex);
            }
            TilePatches.Apply(_harmony);
            InventoryPatches.Apply(_harmony);
            MSDrops.Apply(_harmony);

            WorldSaveLayer.RegisterData("storage", StorageWorld.Save, StorageWorld.Load, () =>
            {
                StorageWorld.Reset();
                Network.Invalidate();
                MagicUI.Close(silent: true);
            });
            TilePatches.AfterSetup += MSRecipes.Inject;
            TilePatches.AfterSetup += Debug.SelfTest.MaybeOpenUIPreview;
            TileEvents.Changed += OnTileChanged;

            MagicUI.Initialize();
            Debug.SelfTest.Register(context);
            Debug.SelfTest.ApplyMenuPump(_harmony);
            FrameEvents.OnPostUpdate += OnPostUpdate;
            _active = true;
            Log.Info($"Magic Storage initialized: {CustomTiles.All.Count} tiles (types {CustomTiles.VanillaCount}+)");
        }

        public void OnContentReady(ModContext context)
        {
            if (!_active) return;
            MSItems.Resolve(context);
            // Normally recipes are injected right after vanilla's content setup; cover the case where it already ran.
            if (CustomTiles.SetupDone) MSRecipes.Inject();
        }

        public void OnWorldLoad()
        {
            if (!_active) return;
            Network.Invalidate();
            if (!string.IsNullOrEmpty(WorldSaveLayer.LastLoadMessage))
                Main.NewText(WorldSaveLayer.LastLoadMessage, 255, 120, 80);
            if (!CustomTiles.PlacementAllowed)
                Main.NewText("Magic Storage is disabled: its world-save protection could not be installed. See the TerrariaModder log.", 255, 80, 80);
            else if (Main.netMode != 0)
                Main.NewText("Magic Storage only works in singleplayer.", 255, 120, 80);
        }

        public void OnWorldUnload()
        {
            // Do NOT clear storage here: the world may still be saved after this point (Save & Exit).
            // State is reset when the next world loads (WorldSaveLayer).
            MagicUI.Close(silent: true);
        }

        private static void OnTileChanged(int x, int y) => Network.Invalidate();

        private static void OnPostUpdate()
        {
            try { MagicUI.Update(); }
            catch (Exception ex) { Log.Error("Update failed", ex); }
            try { Debug.SelfTest.Pump(); }
            catch (Exception ex) { Log.Error("Self-test pump failed", ex); }
        }

        public override void Unload()
        {
            FrameEvents.OnPostUpdate -= OnPostUpdate;
            TileEvents.Changed -= OnTileChanged;
            TilePatches.AfterSetup -= MSRecipes.Inject;
            TilePatches.AfterSetup -= Debug.SelfTest.MaybeOpenUIPreview;
            MagicUI.Unload();
            _harmony?.UnpatchAll(_harmony.Id);
        }
    }
}
