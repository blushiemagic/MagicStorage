using System;
using MagicStorageTM.Content;
using MagicStorageTM.Storage;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using TerrariaModder.Core.UI;
using TerrariaModder.Core.UI.Widgets;

namespace MagicStorageTM.UI
{
    public enum UIMode { None, Storage, Crafting }

    /// <summary>
    /// Owns the Magic Storage window (replaces MagicUI/StorageGUI/CraftingGUI). One panel, two views.
    /// The window stays open while the player is in reach of the tile they opened it from (or within
    /// portable range), keeps the inventory open, and closes when a chest/NPC/sign takes over.
    /// </summary>
    public static class MagicUI
    {
        public const string PanelId = "magic-storage";

        public static UIMode Mode { get; private set; }
        public static Point16 Access { get; private set; } = Point16.NegativeOne;
        public static Point16 Heart { get; private set; } = Point16.NegativeOne;
        public static bool Remote { get; private set; }

        private static int _playerRange = -1, _pylonRange = -1;
        private static int _checkTimer;
        private static DraggablePanel _panel;
        private static int _lastX = -1, _lastY = -1;

        public static bool IsOpen => Mode != UIMode.None;

        public static void Initialize()
        {
            _panel = new DraggablePanel(PanelId, "Magic Storage", 520, 470)
            {
                CloseOnEscape = false, // Escape closes the inventory, which closes us
                OnClose = OnPanelClosed,
            };
            _panel.RegisterDrawCallback(Draw);
        }

        public static void Unload()
        {
            Close(silent: true);
            _panel?.UnregisterDrawCallback();
        }

        public static void ToggleStorage(Point16 access, Point16 heart, bool remote, int playerRange = -1, int pylonRange = -1)
            => Toggle(UIMode.Storage, access, heart, remote, playerRange, pylonRange);

        public static void ToggleCrafting(Point16 access, Point16 heart, bool remote, int playerRange = -1, int pylonRange = -1)
            => Toggle(UIMode.Crafting, access, heart, remote, playerRange, pylonRange);

        private static void Toggle(UIMode mode, Point16 access, Point16 heart, bool remote, int playerRange, int pylonRange)
        {
            if (Mode == mode && Access == access)
            {
                Close();
                return;
            }
            Player player = Main.LocalPlayer;
            bool hadSomethingOpen = player.chest != -1 || IsOpen;

            // Close vanilla containers / dialogs the same way MagicStorage does.
            if (player.sign > -1) { player.sign = -1; Main.editSign = false; Main.npcChatText = string.Empty; }
            if (Main.editChest) { Main.editChest = false; Main.npcChatText = string.Empty; }
            if (player.talkNPC > -1) { player.SetTalkNPC(-1); Main.npcChatCornerItem = 0; Main.npcChatText = string.Empty; }
            player.chest = -1;
            Main.stackSplit = 600;

            Mode = mode;
            Access = access;
            Heart = heart;
            Remote = remote;
            _playerRange = playerRange;
            _pylonRange = pylonRange;
            _checkTimer = 0;

            Main.playerInventory = true;
            try { Main.CreativeMenu.CloseMenu(); } catch { }

            _panel.Width = mode == UIMode.Crafting ? 760 : 520;
            _panel.Height = mode == UIMode.Crafting ? 550 : 505; // room for the sort/filter icon rows
            _panel.Title = mode == UIMode.Crafting ? "Magic Storage - Crafting" : HeartTitle();
            if (!_panel.IsOpen)
            {
                if (_lastX >= 0) _panel.Open(_lastX, _lastY);
                else _panel.Open(70, Math.Max(260, (int)(Main.screenHeight * 0.30f)));
            }
            StorageView.OnOpened();
            CraftingView.OnOpened();
            SoundEngine.PlaySound(hadSomethingOpen ? SoundID.MenuTick : SoundID.MenuOpen);
        }

        private static string HeartTitle()
        {
            string name = StorageWorld.Hearts.TryGetValue(Heart, out var h) ? h.Name : "";
            return string.IsNullOrEmpty(name) ? "Magic Storage" : name;
        }

        public static void Close(bool silent = false)
        {
            if (!IsOpen) return;
            Mode = UIMode.None;
            Access = Point16.NegativeOne;
            Heart = Point16.NegativeOne;
            if (_panel != null && _panel.IsOpen)
            {
                _lastX = _panel.X; _lastY = _panel.Y;
                _panel.Close();
            }
            StorageView.OnClosed();
            if (!silent) SoundEngine.PlaySound(SoundID.MenuClose);
        }

        private static void OnPanelClosed()
        {
            if (IsOpen)
            {
                _lastX = _panel.X; _lastY = _panel.Y;
                Mode = UIMode.None;
                Access = Point16.NegativeOne;
                Heart = Point16.NegativeOne;
                StorageView.OnClosed();
                SoundEngine.PlaySound(SoundID.MenuClose);
            }
        }

        public static void OnTileRemoved(Point16 pos)
        {
            if (IsOpen && (pos == Access || pos == Heart)) Close();
        }

        /// <summary>Per-frame validity checks (from FrameEvents.OnPostUpdate).</summary>
        public static void Update()
        {
            if (!IsOpen) return;
            if (Main.gameMenu || Main.netMode != 0) { Close(silent: true); return; }
            Player player = Main.LocalPlayer;
            if (player == null || player.dead) { Close(); return; }
            if (!Main.playerInventory || player.chest != -1 || player.talkNPC != -1 || player.sign != -1) { Close(); return; }

            if (++_checkTimer < 10) return;
            _checkTimer = 0;

            bool accessOk = Mode == UIMode.Crafting ? Network.IsKind(Access, NetKind.CraftingAccess) : Network.ResolveHeart(Access).X >= 0 || Network.IsHeart(Access);
            Point16 heart = Network.ResolveHeart(Access);
            if (!accessOk || heart.X < 0) { Close(); return; }
            Heart = heart;

            if (Remote)
            {
                if (!Interactions.InPortableRange(player, Access, _playerRange, _pylonRange, out string why))
                {
                    Interactions.Notify(why);
                    Close();
                }
            }
            else
            {
                if (!player.InTileEntityInteractionRange(Access.X, Access.Y, 2, 2, TileReachCheckSettings.Simple))
                    Close();
            }
        }

        private static void Draw()
        {
            if (!IsOpen || !_panel.IsOpen) return;
            if (!_panel.BeginDraw()) return;
            try
            {
                int x = _panel.ContentX, y = _panel.ContentY + 4, w = _panel.ContentWidth, h = _panel.ContentHeight - 4;
                if (Mode == UIMode.Storage) StorageView.Draw(x, y, w, h);
                else if (Mode == UIMode.Crafting) CraftingView.Draw(x, y, w, h);
            }
            catch (Exception ex)
            {
                Log.Error("[UI] Draw failed", ex);
                Close(silent: true);
            }
            finally
            {
                _panel.EndDraw();
            }
        }
    }
}
