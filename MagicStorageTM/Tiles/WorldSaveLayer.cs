using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;
using HarmonyLib;
using Terraria;
using Terraria.IO;
using Terraria.Utilities;

namespace MagicStorageTM.Tiles
{
    /// <summary>
    /// Keeps the vanilla .wld file free of custom tiles.
    ///
    /// Saving: a transpiler on WorldFile.SaveWorldTiles makes every read of Main.tile go through
    /// <see cref="GetTileForSave"/>, which hands vanilla an empty copy (walls, liquid and wires kept)
    /// wherever a custom tile sits. The world itself is never modified. Custom tiles plus any
    /// registered mod data go to a sidecar file next to the world ("World.wld.msdata"), stamped
    /// with the SHA-256 of the .wld written in the same save.
    ///
    /// Loading: after WorldFile.LoadWorld succeeds, the sidecar is read back. If its stamp does not
    /// match the .wld (crash mid-save, world restored from backup, ...) the .bak sidecar is tried;
    /// failing that, tiles are only restored into empty spaces so nothing vanilla is overwritten.
    /// </summary>
    public static class WorldSaveLayer
    {
        private const string Magic = "MSTM";
        private const int FormatVersion = 1;
        public const string Extension = ".msdata";

        private sealed class DataSection
        {
            public string Key;
            public Func<byte[]> Save;
            public Action<byte[]> Load;
            public Action Reset;
        }

        private static readonly List<DataSection> _sections = new List<DataSection>();

        /// <summary>Snapshot captured at the start of a save, written once the .wld is on disk.</summary>
        private static byte[] _pendingSnapshot;

        public static bool LastLoadWasPartial { get; private set; }
        public static int LastPreRestoreCustomTiles { get; private set; }
        public static string LastLoadMessage { get; private set; }

        public static void RegisterData(string key, Func<byte[]> save, Action<byte[]> load, Action reset)
        {
            _sections.Add(new DataSection { Key = key, Save = save, Load = load, Reset = reset });
        }

        public static void Apply(Harmony h)
        {
            const BindingFlags A = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            MethodInfo saveTiles = typeof(WorldFile).GetMethod("SaveWorldTiles", A);
            MethodInfo internalSave = typeof(WorldFile).GetMethod("InternalSaveWorld", A);
            MethodInfo loadWorld = typeof(WorldFile).GetMethod("LoadWorld", A, null, Type.EmptyTypes, null);
            if (saveTiles == null || internalSave == null || loadWorld == null)
                throw new MissingMethodException("WorldFile save/load methods changed; refusing to run without a safe save layer");

            h.Patch(saveTiles, transpiler: new HarmonyMethod(typeof(WorldSaveLayer).GetMethod(nameof(SaveWorldTiles_Transpiler), A)));
            h.Patch(internalSave,
                prefix: new HarmonyMethod(typeof(WorldSaveLayer).GetMethod(nameof(InternalSaveWorld_Prefix), A)),
                postfix: new HarmonyMethod(typeof(WorldSaveLayer).GetMethod(nameof(InternalSaveWorld_Postfix), A)));
            h.Patch(loadWorld, postfix: new HarmonyMethod(typeof(WorldSaveLayer).GetMethod(nameof(LoadWorld_Postfix), A)));
            Log.Info("[Save] World save layer installed");
        }

        // ───────────── Save: keep custom tiles out of the .wld ─────────────

        private static IEnumerable<CodeInstruction> SaveWorldTiles_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo mainTile = AccessTools.Field(typeof(Main), nameof(Main.tile));
            MethodInfo getForSave = AccessTools.Method(typeof(WorldSaveLayer), nameof(GetTileForSave));
            var list = instructions.ToList();
            int pending = 0, replaced = 0;
            for (int k = 0; k < list.Count; k++)
            {
                CodeInstruction ins = list[k];
                if (ins.opcode == OpCodes.Ldsfld && Equals(ins.operand, mainTile))
                {
                    // Drop the array reference; keep any labels on a nop so branches still land.
                    var nop = new CodeInstruction(OpCodes.Nop) { labels = ins.labels, blocks = ins.blocks };
                    list[k] = nop;
                    pending++;
                }
                else if (pending > 0 && (ins.opcode == OpCodes.Call || ins.opcode == OpCodes.Callvirt)
                         && ins.operand is MethodInfo m && m.Name == "Get" && m.DeclaringType == typeof(Tile[,]))
                {
                    list[k] = new CodeInstruction(OpCodes.Call, getForSave) { labels = ins.labels, blocks = ins.blocks };
                    pending--;
                    replaced++;
                }
            }
            if (pending != 0 || replaced == 0)
                throw new InvalidOperationException($"SaveWorldTiles transpiler mismatch (replaced={replaced}, pending={pending}); refusing to patch");
            Log.Info($"[Save] SaveWorldTiles transpiled ({replaced} tile reads filtered)");
            return list;
        }

        /// <summary>Called by the transpiled SaveWorldTiles in place of Main.tile[x, y].</summary>
        public static Tile GetTileForSave(int x, int y)
        {
            Tile t = Main.tile[x, y];
            if (t != null && t.active() && CustomTiles.IsCustom(t.type))
            {
                var copy = new Tile();
                copy.CopyFrom(t);
                copy.ClearTile();
                copy.ClearBlockPaintAndCoating();
                copy.type = 0;
                copy.frameX = 0;
                copy.frameY = 0;
                return copy;
            }
            return t;
        }

        private static void InternalSaveWorld_Prefix(bool useCloudSaving)
        {
            _pendingSnapshot = null;
            try
            {
                if (Main.worldPathName == null) return;
                string sidecar = GetSidecarPath();
                if (sidecar != null && File.Exists(sidecar))
                    File.Copy(sidecar, sidecar + ".bak", overwrite: true);
                _pendingSnapshot = BuildSnapshotBody();
            }
            catch (Exception ex) { Log.Error("[Save] Failed to capture Magic Storage data before save", ex); }
        }

        private static void InternalSaveWorld_Postfix(bool useCloudSaving)
        {
            byte[] body = _pendingSnapshot;
            _pendingSnapshot = null;
            if (body == null || Main.worldPathName == null) return;
            try
            {
                string worldHash = HashWorldFile(Main.worldPathName, useCloudSaving);
                string sidecar = GetSidecarPath();
                Directory.CreateDirectory(Path.GetDirectoryName(sidecar));
                string tmp = sidecar + ".tmp";
                using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write))
                using (var w = new BinaryWriter(fs))
                {
                    w.Write(Magic.ToCharArray());
                    w.Write(FormatVersion);
                    w.Write(worldHash ?? "");
                    w.Write(Main.ActiveWorldFileData?.UniqueId.ToString() ?? "");
                    w.Write(body.Length);
                    w.Write(body);
                }
                if (File.Exists(sidecar)) File.Delete(sidecar);
                File.Move(tmp, sidecar);
                Log.Debug($"[Save] Wrote {sidecar} ({body.Length} bytes)");
            }
            catch (Exception ex) { Log.Error("[Save] Failed to write Magic Storage sidecar — storage contents were NOT saved this time", ex); }
        }

        /// <summary>Tile records + mod data sections. Runs on whatever thread saves the world.</summary>
        private static byte[] BuildSnapshotBody()
        {
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms))
            {
                var names = CustomTiles.All.Select(d => d.Name).ToList();
                w.Write(names.Count);
                foreach (string n in names) w.Write(n);

                var records = new List<(int x, int y, Tile t)>();
                int maxX = Main.maxTilesX, maxY = Main.maxTilesY;
                for (int x = 0; x < maxX; x++)
                {
                    for (int y = 0; y < maxY; y++)
                    {
                        Tile t = Main.tile[x, y];
                        if (t != null && t.active() && CustomTiles.IsCustom(t.type))
                            records.Add((x, y, t));
                    }
                }
                w.Write(records.Count);
                foreach (var (x, y, t) in records)
                {
                    w.Write(x);
                    w.Write(y);
                    w.Write((ushort)(t.type - CustomTiles.VanillaCount));
                    w.Write(t.frameX);
                    w.Write(t.frameY);
                    w.Write(t.color());
                    byte flags = 0;
                    if (t.inActive()) flags |= 1;
                    if (t.invisibleBlock()) flags |= 2;
                    if (t.fullbrightBlock()) flags |= 4;
                    w.Write(flags);
                }

                w.Write(_sections.Count);
                foreach (var s in _sections)
                {
                    byte[] data = Array.Empty<byte>();
                    try { data = s.Save() ?? Array.Empty<byte>(); }
                    catch (Exception ex) { Log.Error($"[Save] Section '{s.Key}' failed to save", ex); throw; }
                    w.Write(s.Key);
                    w.Write(data.Length);
                    w.Write(data);
                }
                w.Flush();
                return ms.ToArray();
            }
        }

        // ───────────── Load ─────────────

        private static void LoadWorld_Postfix()
        {
            ResetAll();
            LastLoadWasPartial = false;
            LastLoadMessage = null;
            LastPreRestoreCustomTiles = 0;
            if (WorldGen.loadFailed || Main.worldPathName == null) return;
            try
            {
                // Anything custom here came from the vanilla file itself, which should never happen.
                int found = 0;
                for (int x = 0; x < Main.maxTilesX; x++)
                    for (int y = 0; y < Main.maxTilesY; y++)
                    {
                        Tile t = Main.tile[x, y];
                        if (t != null && t.active() && CustomTiles.IsCustom(t.type)) found++;
                    }
                LastPreRestoreCustomTiles = found;
                if (found > 0) Log.Warn($"[Save] {found} custom tiles were present in the vanilla world file before restore");

                string sidecar = GetSidecarPath();
                bool cloud = Main.ActiveWorldFileData?.IsCloudSave ?? false;
                string worldHash = HashWorldFile(Main.worldPathName, cloud);

                string chosen = null;
                bool partial = false;
                if (File.Exists(sidecar) && ReadHeaderHash(sidecar) == worldHash) chosen = sidecar;
                else if (File.Exists(sidecar + ".bak") && ReadHeaderHash(sidecar + ".bak") == worldHash) chosen = sidecar + ".bak";
                else if (File.Exists(sidecar)) { chosen = sidecar; partial = true; }

                if (chosen == null) return;
                Restore(chosen, partial);
            }
            catch (Exception ex)
            {
                LastLoadMessage = "Magic Storage could not read its save data for this world. Check the TerrariaModder log.";
                Log.Error("[Save] Failed to restore Magic Storage data", ex);
            }
        }

        private static void ResetAll()
        {
            foreach (var s in _sections)
            {
                try { s.Reset?.Invoke(); }
                catch (Exception ex) { Log.Error($"[Save] Section '{s.Key}' reset failed", ex); }
            }
        }

        private static string ReadHeaderHash(string path)
        {
            try
            {
                using (var fs = File.OpenRead(path))
                using (var r = new BinaryReader(fs))
                {
                    if (new string(r.ReadChars(4)) != Magic) return null;
                    r.ReadInt32();
                    return r.ReadString();
                }
            }
            catch { return null; }
        }

        private static void Restore(string path, bool partial)
        {
            byte[] body;
            using (var fs = File.OpenRead(path))
            using (var r = new BinaryReader(fs))
            {
                if (new string(r.ReadChars(4)) != Magic) throw new InvalidDataException("Not a Magic Storage sidecar");
                int version = r.ReadInt32();
                if (version > FormatVersion) throw new InvalidDataException($"Sidecar format {version} is newer than this mod supports");
                r.ReadString(); // world hash
                r.ReadString(); // world id
                int len = r.ReadInt32();
                body = r.ReadBytes(len);
            }

            int restored = 0, skipped = 0, unknown = 0;
            using (var ms = new MemoryStream(body))
            using (var r = new BinaryReader(ms))
            {
                int nameCount = r.ReadInt32();
                var defsByIndex = new CustomTileDef[nameCount];
                for (int k = 0; k < nameCount; k++) defsByIndex[k] = CustomTiles.GetByName(r.ReadString());

                int count = r.ReadInt32();
                for (int k = 0; k < count; k++)
                {
                    int x = r.ReadInt32(), y = r.ReadInt32();
                    ushort idx = r.ReadUInt16();
                    short fx = r.ReadInt16(), fy = r.ReadInt16();
                    byte color = r.ReadByte(), flags = r.ReadByte();
                    CustomTileDef def = idx < defsByIndex.Length ? defsByIndex[idx] : null;
                    if (def == null || !WorldGen.InWorld(x, y)) { unknown++; continue; }
                    Tile t = Main.tile[x, y];
                    if (t == null) Main.tile[x, y] = t = new Tile();
                    if (partial && t.active()) { skipped++; continue; }
                    t.ClearTile();
                    t.active(true);
                    t.type = (ushort)def.Type;
                    t.frameX = fx;
                    t.frameY = fy;
                    t.color(color);
                    t.inActive((flags & 1) != 0);
                    t.invisibleBlock((flags & 2) != 0);
                    t.fullbrightBlock((flags & 4) != 0);
                    restored++;
                }

                int sectionCount = r.ReadInt32();
                for (int k = 0; k < sectionCount; k++)
                {
                    string key = r.ReadString();
                    int len = r.ReadInt32();
                    byte[] data = r.ReadBytes(len);
                    DataSection s = _sections.FirstOrDefault(q => q.Key == key);
                    if (s == null) { Log.Warn($"[Save] Unknown data section '{key}' ignored"); continue; }
                    try { s.Load(data); }
                    catch (Exception ex) { Log.Error($"[Save] Section '{key}' failed to load", ex); }
                }
            }

            LastLoadWasPartial = partial;
            if (partial)
                LastLoadMessage = $"Magic Storage: save data did not match this world file (world restored from backup?). Restored {restored} blocks, skipped {skipped} occupied spots.";
            Log.Info($"[Save] Restored {restored} custom tiles from {Path.GetFileName(path)} (partial={partial}, skipped={skipped}, unknown={unknown})");
        }

        // ───────────── Helpers ─────────────

        public static string GetSidecarPath()
        {
            WorldFileData data = Main.ActiveWorldFileData;
            if (data == null || Main.worldPathName == null) return null;
            if (data.IsCloudSave)
                return Path.Combine(CustomTiles.ModFolder ?? ".", "cloud-worlds", data.UniqueId.ToString("N") + Extension);
            return Main.worldPathName + Extension;
        }

        private static string HashWorldFile(string path, bool cloud)
        {
            try
            {
                if (!FileUtilities.Exists(path, cloud)) return null;
                byte[] bytes = FileUtilities.ReadAllBytes(path, cloud);
                using (var sha = SHA256.Create())
                    return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "");
            }
            catch (Exception ex)
            {
                Log.Warn($"[Save] Could not hash world file: {ex.Message}");
                return null;
            }
        }
    }
}
