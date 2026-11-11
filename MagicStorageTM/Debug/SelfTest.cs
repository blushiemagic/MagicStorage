using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using MagicStorageTM.Content;
using MagicStorageTM.Crafting;
using MagicStorageTM.Storage;
using MagicStorageTM.Tiles;
using MagicStorageTM.UI;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.IO;
using TerrariaModder.Core;
using TerrariaModder.Core.Debug;

namespace MagicStorageTM.Debug
{
    /// <summary>
    /// Developer self-test, driven through Core debug commands ("magic-storage.test-*").
    /// Every world-touching command refuses to run unless the world name starts with "MSTM_Test".
    /// </summary>
    internal static class SelfTest
    {
        public const string TestName = "MSTM_Test";
        private static readonly ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();
        private static string _expectPath;

        public static void Register(ModContext ctx)
        {
            _expectPath = Path.Combine(ctx.ModFolder, "selftest-expected.txt");
            ctx.RegisterCommand("test-player", "Create the MSTM_Test character (title menu only)", a => Run(CreateTestPlayer));
            ctx.RegisterCommand("test-enter", "Enter the MSTM_Test world with the MSTM_Test character (title menu only, matched by name)", a => Run(EnterTestWorld));
            ctx.RegisterCommand("test-build", "Build and exercise a storage network next to the player (MSTM_Test worlds only)", a => Run(Build));
            ctx.RegisterCommand("test-save", "Save the world now (MSTM_Test worlds only)", a => Run(SaveNow));
            ctx.RegisterCommand("test-verify", "Verify the network after the world was reloaded (MSTM_Test worlds only)", a => Run(Verify));
            ctx.RegisterCommand("test-open", "Open the UI: 'storage' or 'crafting' (MSTM_Test worlds only)", a => Run(o => Open(o, a.Length > 0 ? a[0] : "storage")));
            ctx.RegisterCommand("test-close", "Close the Magic Storage UI", a => Run(o => { MagicUI.Close(); o.Add("closed"); }));
        }

        /// <summary>Called every frame on the main thread.</summary>
        public static void Pump()
        {
            while (_queue.TryDequeue(out Action a)) a();
        }

        /// <summary>
        /// Developer UI preview: if "uipreview.flag" (content "storage" or "crafting") is in the mod folder, the
        /// Magic Storage window opens, empty, on the title screen so its layout can be checked from a screenshot.
        /// Called after vanilla content setup (Main.player[] exists by then).
        /// </summary>
        public static void MaybeOpenUIPreview()
        {
            try
            {
                string flag = Path.Combine(CustomTiles.ModFolder ?? "", "uipreview.flag");
                if (!File.Exists(flag) || !Main.gameMenu) return;
                string which = File.ReadAllText(flag).Trim().ToLowerInvariant();
                if (which == "crafting") MagicUI.ToggleCrafting(Point16.Zero, Point16.Zero, remote: false);
                else MagicUI.ToggleStorage(Point16.Zero, Point16.Zero, remote: false);
                Log.Info($"[SelfTest] UI preview opened ({MagicUI.Mode})");
            }
            catch (Exception ex) { Log.Error("[SelfTest] UI preview failed", ex); }
        }

        /// <summary>Core's OnPostUpdate only fires in a world; this pumps the queue on the title menu too.</summary>
        public static void ApplyMenuPump(HarmonyLib.Harmony harmony)
        {
            var target = HarmonyLib.AccessTools.Method(typeof(Main), "DoUpdate");
            if (target == null) { Log.Warn("[SelfTest] Main.DoUpdate not found; menu commands unavailable"); return; }
            harmony.Patch(target, postfix: new HarmonyLib.HarmonyMethod(typeof(SelfTest), nameof(MenuPumpPostfix)));
        }

        private static void MenuPumpPostfix()
        {
            if (!Main.gameMenu) return;
            try { Pump(); }
            catch (Exception ex) { Log.Error("Self-test menu pump failed", ex); }
        }

        private static void Run(Action<List<string>> work)
        {
            var output = new List<string>();
            var done = new ManualResetEventSlim(false);
            _queue.Enqueue(() =>
            {
                try { work(output); }
                catch (Exception ex) { output.Add("EXCEPTION: " + ex); }
                finally { done.Set(); }
            });
            if (!done.Wait(TimeSpan.FromSeconds(60))) output.Add("TIMEOUT waiting for main thread");
            foreach (string line in output) CommandRegistry.Write(line);
        }

        /// <summary>True only for a local world FILE named MSTM_Test*.wld (the file on disk is what must be protected).</summary>
        private static bool IsTestWorldFile(WorldFileData w) =>
            w != null && !w.IsCloudSave && w.Path != null &&
            Path.GetFileNameWithoutExtension(w.Path).StartsWith(TestName, StringComparison.Ordinal);

        private static bool InTestWorld(List<string> o)
        {
            if (Main.gameMenu || !IsTestWorldFile(Main.ActiveWorldFileData))
            {
                o.Add($"FAIL: refusing to run outside a local world file {TestName}*.wld (current: '{Main.ActiveWorldFileData?.Path}', menu={Main.gameMenu})");
                return false;
            }
            return true;
        }

        private static void Check(List<string> o, bool ok, string what)
        {
            o.Add((ok ? "PASS: " : "FAIL: ") + what);
        }

        // ── character ──

        private static void CreateTestPlayer(List<string> o)
        {
            if (!Main.gameMenu) { o.Add("FAIL: must be in the title menu"); return; }
            Main.LoadPlayers();
            if (Main.PlayerList.Any(p => p.Name == TestName)) { o.Add("OK: test player already exists"); return; }
            var player = new Player { name = TestName, difficulty = 3 }; // journey: no item loss, creative tools
            // Same as PlayerFileData.CreateAndSave, but always a local (non-cloud) save.
            var data = new PlayerFileData(Main.GetPlayerPathFromName(TestName, false), false)
            {
                Metadata = FileMetadata.FromCurrentSettings(FileType.Player),
                Player = player,
            };
            player.inventory[0].SetDefaults(ItemID.CopperPickaxe);
            player.inventory[1].SetDefaults(ItemID.CopperAxe);
            Player.SavePlayer(data, skipMapSave: true);
            Main.LoadPlayers();
            int idx = Main.PlayerList.FindIndex(p => p.Name == TestName);
            o.Add($"OK: created test player at index {idx}: {data.Path}");
        }

        /// <summary>Selects character and world strictly by exact name, so a real save can never be picked.</summary>
        private static void EnterTestWorld(List<string> o)
        {
            if (!Main.gameMenu) { o.Add("FAIL: must be in the title menu"); return; }
            // Vanilla creates Main.player[1..] only when the splash screen ends; loading a world before that
            // crashes in WorldGen.playWorldCallBack (seen 2026-11-10).
            if (Main.showSplash || Main.player == null || Main.player.Any(p => p == null))
            {
                o.Add("FAIL: the game is still on the splash screen; try again in a few seconds");
                return;
            }
            Main.LoadPlayers();
            PlayerFileData player = Main.PlayerList.FirstOrDefault(p => p.Name == TestName && !p.IsCloudSave);
            if (player == null || player.Player == null || player.Player.loadStatus != 0)
            {
                o.Add("FAIL: local character MSTM_Test not found or failed to load (run test-player first)");
                return;
            }
            Main.menuMultiplayer = false;
            Main.SelectPlayer(player); // singleplayer: menuMode = 6
            Main.LoadWorlds();
            WorldFileData world = Main.WorldList.FirstOrDefault(w => IsTestWorldFile(w) &&
                string.Equals(Path.GetFileNameWithoutExtension(w.Path), TestName, StringComparison.Ordinal));
            if (world == null) { o.Add($"FAIL: local world file {TestName}.wld not found"); return; }
            world.SetAsActive();
            WorldGen.playWorld();
            Main.menuMode = 10;
            o.Add($"OK: loading world '{world.Name}' ({world.Path}) as '{player.Name}'");
        }

        // ── build & exercise ──

        private static bool PlaceObject(CustomTileDef def, int topLeftX, int topLeftY, int style, List<string> o)
        {
            int cx = topLeftX + def.OriginX, cy = topLeftY + def.OriginY;
            if (!Terraria.TileObject.CanPlace(cx, cy, def.Type, style, 1, out Terraria.TileObject obj))
            {
                o.Add($"FAIL: CanPlace({def.Name}) at {topLeftX},{topLeftY}");
                return false;
            }
            if (!Terraria.TileObject.Place(obj))
            {
                o.Add($"FAIL: Place({def.Name}) at {topLeftX},{topLeftY}");
                return false;
            }
            Terraria.ObjectData.TileObjectData.CallPostPlacementPlayerHook(cx, cy, def.Type, style, 1, obj.alternate, obj);
            return true;
        }

        private static void ClearArea(int x0, int y0, int w, int h, int floorY)
        {
            for (int x = x0; x < x0 + w; x++)
            {
                for (int y = y0; y < y0 + h; y++)
                {
                    Tile t = Main.tile[x, y];
                    t.ClearEverything();
                }
                Tile floor = Main.tile[x, floorY];
                floor.ClearEverything();
                floor.active(true);
                floor.type = TileID.GrayBrick;
            }
            for (int x = x0 - 1; x <= x0 + w; x++)
                for (int y = y0 - 1; y <= floorY + 1; y++)
                    WorldGen.SquareTileFrame(x, y);
        }

        private static void Build(List<string> o)
        {
            if (!InTestWorld(o)) return;
            Player pl = Main.LocalPlayer;
            int px = (int)(pl.Center.X / 16), feet = (int)(pl.Bottom.Y / 16);
            int x0 = px + 3, floorY = feet, top = floorY - 6;
            ClearArea(x0, top, 22, 6, floorY);
            int row = floorY - 2; // 2-tall objects stand on the floor

            bool ok = true;
            ok &= PlaceObject(MSTiles.Heart, x0, row, 0, o);
            ok &= PlaceObject(MSTiles.Unit, x0 + 2, row, 0, o);
            ok &= PlaceObject(MSTiles.Unit, x0 + 4, row, 0, o);
            ok &= PlaceObject(MSTiles.CraftingAccess, x0 + 6, row, 0, o);
            ok &= PlaceObject(MSTiles.Connector, x0 + 8, row + 1, 0, o);
            ok &= PlaceObject(MSTiles.StorageAccess, x0 + 9, row, 0, o);
            ok &= PlaceObject(MSTiles.Unit, x0 + 11, row, 0, o);       // will be broken while empty
            ok &= PlaceObject(MSTiles.Combined1, x0 + 14, floorY - 3, 0, o);
            // A second heart touching the network must be refused.
            bool second = Terraria.TileObject.CanPlace(x0 - 2 + MSTiles.Heart.OriginX, row + MSTiles.Heart.OriginY, MSTiles.Heart.Type, 0, 1, out _);
            Check(o, !second, "a second Storage Heart cannot join the network");
            Check(o, ok, "all objects placed");

            var heart = new Point16(x0, row);
            var scan = Network.Scan(heart);
            Check(o, scan.Hearts.Count == 1 && scan.Units.Count == 3 && scan.CraftingAccesses.Count == 1, $"network scan: hearts={scan.Hearts.Count} units={scan.Units.Count} crafting={scan.CraftingAccesses.Count} members={scan.Members.Count}");
            Check(o, Network.ResolveHeart(new Point16(x0 + 9, row)) == heart, "storage access (via connector) resolves to the heart");
            Tile conn = Main.tile[x0 + 8, row + 1];
            Check(o, conn.frameX == 18 + 36, $"connector framed left+right (frameX={conn.frameX}, frameY={conn.frameY})");

            // Upgrade the first unit with a Demonite upgrade held in hand.
            int slot = pl.selectedItem;
            Item saved = pl.inventory[slot].Clone();
            pl.inventory[slot] = StorageWorld.MakeItem(MSItems.TypeOf("upgrade-demonite"), 1);
            Interactions.RightClick(NetKind.Unit, new Point16(x0 + 2, row));
            Check(o, pl.inventory[slot].IsAir, "upgrade item consumed");
            pl.inventory[slot] = saved;
            Check(o, StorageOps.TierAt(new Point16(x0 + 2, row))?.Key == "Demonite", $"unit upgraded to Demonite (tier={StorageOps.TierAt(new Point16(x0 + 2, row))?.Key})");

            // Break the empty unit: must drop its item and leave the network.
            int dropsBefore = CountWorldItems(MSItems.TypeOf("storage-unit"));
            WorldGen.KillTile(x0 + 11, row);
            Check(o, !Main.tile[x0 + 11, row].active() && !Main.tile[x0 + 12, row + 1].active(), "empty unit fully removed");
            Check(o, CountWorldItems(MSItems.TypeOf("storage-unit")) == dropsBefore + 1, "empty unit dropped its item");
            Check(o, Network.Scan(heart).Units.Count == 2, "network now has 2 units");

            // Storage: 40 + 80 = 120 slots
            StorageOps.GetCapacity(heart, out int used0, out int total);
            Check(o, total == 120, $"capacity 120 (got {total}, used {used0})");
            var deposits = new[] { (ItemID.DirtBlock, 2500), (ItemID.Wood, 300), (ItemID.IronBar, 40), (ItemID.Torch, 99), (ItemID.WorkBench, 1) };
            foreach (var (type, stack) in deposits)
            {
                Item it = StorageWorld.MakeItem(type, stack);
                StorageOps.Deposit(heart, it);
                Check(o, it.IsAir, $"deposited {stack}x {Lang.GetItemNameValue(type)}");
            }
            Item sword = StorageWorld.MakeItem(ItemID.CopperBroadsword, 1, prefix: PrefixID.Legendary);
            StorageOps.Deposit(heart, sword);
            var counts = StorageOps.CountByType(heart);
            Check(o, counts[ItemID.DirtBlock] == 2500 && counts[ItemID.Wood] == 300, "counts aggregated across stacks");

            // Full storage: fill every free slot with an unstackable item (stackables would share one slot),
            // then one more deposit must be refused and the item kept.
            int added = 0;
            for (int k = 0; k < 200; k++)
            {
                if (!StorageOps.Deposit(heart, StorageWorld.MakeItem(ItemID.WoodenHammer, 1))) break;
                added++;
            }
            StorageOps.GetCapacity(heart, out int usedFull, out _);
            Check(o, usedFull == 120, $"storage fills to exactly its capacity (used {usedFull}/120 after {added} hammers)");
            Item extra = StorageWorld.MakeItem(ItemID.Gel, 1);
            bool movedExtra = StorageOps.Deposit(heart, extra);
            Check(o, !movedExtra && !extra.IsAir, "deposit into a full storage is refused and the item kept");
            Item hammers = StorageOps.Withdraw(heart, ItemID.WoodenHammer, -1, 100000);
            Check(o, hammers.stack == added, $"withdrew all {added} hammers");

            // Withdraw exact prefix
            Item got = StorageOps.Withdraw(heart, ItemID.CopperBroadsword, PrefixID.Legendary, 1);
            Check(o, got.type == ItemID.CopperBroadsword && got.prefix == PrefixID.Legendary, "withdrew prefixed sword");
            StorageOps.Deposit(heart, got);

            // Crafting: stations in the interface, craft a Wooden Sword from storage wood.
            var ca = StorageWorld.GetCraftingAccess(new Point16(x0 + 6, row));
            ca.Stations.Clear();
            ca.Stations.Add(StorageWorld.MakeItem(ItemID.WorkBench, 1));
            ca.Stations.Add(StorageWorld.MakeItem(ItemID.IronAnvil, 1));
            var env = CraftingEngine.BuildEnvironment(new Point16(x0 + 6, row), pl);
            Recipe swordRecipe = Enumerable.Range(0, Recipe.numRecipes).Select(i => Main.recipe[i]).FirstOrDefault(r => r.createItem.type == ItemID.WoodenSword);
            Check(o, swordRecipe != null && CraftingEngine.MeetsEnvironment(swordRecipe, env, pl), "Wooden Sword craftable with Work Bench station");
            long woodBefore = StorageOps.CountByType(heart)[ItemID.Wood];
            Item mouseBefore = Main.mouseItem;
            Main.mouseItem = new Item();
            int crafted = CraftingEngine.Craft(swordRecipe, 2, heart, env, pl, includeInventory: false);
            long woodAfter = StorageOps.CountByType(heart)[ItemID.Wood];
            Check(o, crafted == 2 && woodBefore - woodAfter == 14, $"crafted {crafted} swords, wood {woodBefore}->{woodAfter}");
            Main.mouseItem = mouseBefore;
            Recipe compRecipe = Enumerable.Range(0, Recipe.numRecipes).Select(i => Main.recipe[i]).FirstOrDefault(r => r.createItem.type == MSItems.TypeOf("storage-component"));
            Check(o, compRecipe != null && CraftingEngine.MaxCrafts(compRecipe, StorageOps.CountByType(heart)) == 20, "Storage Component recipe (wood + iron groups) craftable 20 times");

            // Combined station counts as a work bench for the player standing next to it.
            pl.AdjTiles();
            bool nearCombined = Math.Abs(pl.Center.X / 16 - (x0 + 15)) < 6;
            if (nearCombined) Check(o, pl.adjTile[TileID.WorkBenches] && pl.adjWaterSource, "Combined Stations act as work bench + water");

            // Remember what must survive a save/reload.
            var expected = StorageOps.CountByType(heart);
            using (var w = new StreamWriter(_expectPath))
            {
                w.WriteLine($"heart {heart.X} {heart.Y}");
                w.WriteLine($"craft {x0 + 6} {row}");
                w.WriteLine($"unitDemonite {x0 + 2} {row}");
                w.WriteLine($"connector {x0 + 8} {row + 1}");
                foreach (var kv in expected) w.WriteLine($"item {kv.Key} {kv.Value}");
            }
            Check(o, StorageOps.FullnessOf(StorageWorld.GetUnit(new Point16(x0 + 2, row)), UnitTier.ByKey("Demonite")) != Fullness.Empty, "unit sprite shows contents");
            o.Add($"DONE build at {x0},{row}; expectations written");
        }

        private static int CountWorldItems(int type)
        {
            int n = 0;
            for (int i = 0; i < Main.item.Length; i++)
                if (Main.item[i] != null && Main.item[i].active && Main.item[i].type == type) n += Main.item[i].stack;
            return n;
        }

        private static void SaveNow(List<string> o)
        {
            if (!InTestWorld(o)) return;
            WorldFile.SaveWorld();
            string sidecar = WorldSaveLayer.GetSidecarPath();
            Check(o, File.Exists(sidecar), "sidecar written: " + sidecar);
        }

        private static void Verify(List<string> o)
        {
            if (!InTestWorld(o)) return;
            if (!File.Exists(_expectPath)) { o.Add("FAIL: no expectations file; run test-build first"); return; }
            Check(o, WorldSaveLayer.LastPreRestoreCustomTiles == 0, $"vanilla .wld contained no custom tiles (found {WorldSaveLayer.LastPreRestoreCustomTiles})");
            Check(o, !WorldSaveLayer.LastLoadWasPartial, "sidecar matched the world file (full restore)");
            Point16 heart = Point16.NegativeOne, craft = Point16.NegativeOne, unit = Point16.NegativeOne, conn = Point16.NegativeOne;
            var expected = new Dictionary<int, long>();
            foreach (string line in File.ReadAllLines(_expectPath))
            {
                var p = line.Split(' ');
                switch (p[0])
                {
                    case "heart": heart = new Point16(int.Parse(p[1]), int.Parse(p[2])); break;
                    case "craft": craft = new Point16(int.Parse(p[1]), int.Parse(p[2])); break;
                    case "unitDemonite": unit = new Point16(int.Parse(p[1]), int.Parse(p[2])); break;
                    case "connector": conn = new Point16(int.Parse(p[1]), int.Parse(p[2])); break;
                    case "item": expected[int.Parse(p[1])] = long.Parse(p[2]); break;
                }
            }
            Check(o, Network.IsHeart(heart), "heart restored");
            Check(o, StorageOps.TierAt(unit)?.Key == "Demonite", "upgraded unit restored with its tier");
            Check(o, Main.tile[conn.X, conn.Y].active() && Main.tile[conn.X, conn.Y].type == MSTiles.Connector.Type, "connector restored");
            var counts = StorageOps.CountByType(heart);
            bool same = expected.Count == counts.Count && expected.All(kv => counts.TryGetValue(kv.Key, out long c) && c == kv.Value);
            Check(o, same, $"storage contents identical after reload ({counts.Count} item types, expected {expected.Count})");
            if (!same)
                foreach (var kv in expected)
                    o.Add($"  type {kv.Key}: expected {kv.Value}, got {(counts.TryGetValue(kv.Key, out long c) ? c : 0)}");
            var stations = StorageWorld.CraftingAccesses.TryGetValue(craft, out var ca) ? ca.Stations.Count : -1;
            Check(o, stations == 2, $"crafting stations restored ({stations})");
            var sword = StorageOps.Aggregate(heart).FirstOrDefault(e => e.Type == ItemID.CopperBroadsword);
            Check(o, sword != null && sword.Prefix == PrefixID.Legendary, "prefix preserved");
        }

        private static void Open(List<string> o, string which)
        {
            if (!InTestWorld(o)) return;
            Point16 heart = Point16.NegativeOne, craft = Point16.NegativeOne;
            foreach (string line in File.ReadAllLines(_expectPath))
            {
                var p = line.Split(' ');
                if (p[0] == "heart") heart = new Point16(int.Parse(p[1]), int.Parse(p[2]));
                if (p[0] == "craft") craft = new Point16(int.Parse(p[1]), int.Parse(p[2]));
            }
            if (which == "crafting") MagicUI.ToggleCrafting(craft, heart, remote: false);
            else MagicUI.ToggleStorage(heart, heart, remote: false);
            o.Add($"opened {which}: mode={MagicUI.Mode}");
        }
    }
}
