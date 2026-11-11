using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using TerrariaModder.Core.UI;

namespace VanillaUISkin
{
    /// <summary>
    /// Re-draws TerrariaModder's flat UI boxes with vanilla textures.
    ///
    /// Core funnels every box through UIRenderer.DrawRect and paints it with one of the semantic UIColors
    /// (PanelBg, ItemBg, Button, InputBg, ScrollTrack, TooltipBg…). We intercept DrawRect, recognise the
    /// semantic colour (compared against the live UIColors values, so Core's colour-blind themes still work)
    /// and draw what tModLoader/vanilla would draw for that element instead:
    ///   panel → UIPanel (PanelBackground + PanelBorder, 12px corners), item slot → Inventory_Back,
    ///   button/input → small UIPanel with vanilla hover colour, scrollbar → Scrollbar/ScrollbarInner,
    ///   tooltip → Utils.DrawInvBG. Colours we don't recognise are left untouched.
    /// </summary>
    internal static class Skin
    {
        // tModLoader / vanilla UIPanel palette
        private static readonly Color PanelColor = new Color(63, 82, 151) * 0.7f;
        private static readonly Color PanelHover = new Color(73, 94, 171);
        private static readonly Color InnerColor = new Color(33, 43, 79) * 0.8f;
        private static readonly Color Gold = new Color(255, 231, 69);
        private static readonly Color CloseHover = new Color(180, 50, 50) * 0.9f;
        private static readonly Color TooltipColor = new Color(23, 25, 81, 255) * 0.925f;
        private static readonly Color SlotColor = new Color(220, 220, 220, 220); // Main.inventoryBack default
        private static readonly Color LineColor = new Color(20, 25, 55) * 0.8f;
        private static readonly Color TrackColor = new Color(20, 25, 55) * 0.85f;
        private static readonly Color OnColor = new Color(40, 120, 60) * 0.85f;
        private static readonly Color OnHover = new Color(55, 150, 75);

        private static Asset<Texture2D> _panelBg, _panelBorder, _scrollbar, _scrollbarInner;
        private static bool _texturesFailed;
        private static bool _drawing;
        private static bool _errorLogged;

        /// <summary>&gt; 0 while DraggablePanel.BeginDraw draws the window frame.</summary>
        internal static int ChromeDepth;

        public static bool Enabled = true;

        private static bool Ready()
        {
            if (!Enabled || _texturesFailed || Main.spriteBatch == null || Main.Assets == null) return false;
            if (_panelBg != null) return true;
            try
            {
                _panelBg = Main.Assets.Request<Texture2D>("Images/UI/PanelBackground", AssetRequestMode.ImmediateLoad);
                _panelBorder = Main.Assets.Request<Texture2D>("Images/UI/PanelBorder", AssetRequestMode.ImmediateLoad);
                _scrollbar = Main.Assets.Request<Texture2D>("Images/UI/Scrollbar", AssetRequestMode.ImmediateLoad);
                _scrollbarInner = Main.Assets.Request<Texture2D>("Images/UI/ScrollbarInner", AssetRequestMode.ImmediateLoad);
                return true;
            }
            catch (Exception ex)
            {
                _texturesFailed = true;
                Log.Error("Could not load vanilla UI textures; falling back to TerrariaModder's flat style", ex);
                return false;
            }
        }

        private static bool Eq(Color4 a, Color4 b) => a.R == b.R && a.G == b.G && a.B == b.B && a.A == b.A;

        // ── Harmony entry points (positional args so parameter names in Core don't matter) ──

        /// <summary>Prefix for UIRenderer.DrawRect(int,int,int,int,byte,byte,byte,byte). Return false = handled.</summary>
        public static bool DrawRectPrefix(int __0, int __1, int __2, int __3, byte __4, byte __5, byte __6, byte __7)
        {
            if (_drawing || __2 <= 0 || __3 <= 0 || !Ready()) return true;
            _drawing = true;
            try
            {
                return !Route(Main.spriteBatch, __0, __1, __2, __3, new Color4(__4, __5, __6, __7));
            }
            catch (Exception ex)
            {
                LogOnce(ex);
                return true;
            }
            finally { _drawing = false; }
        }

        /// <summary>Prefix for both UIRenderer.DrawPanel overloads: a framed panel is always a UIPanel.</summary>
        public static bool DrawPanelPrefix(int __0, int __1, int __2, int __3)
        {
            if (_drawing || __2 <= 0 || __3 <= 0 || !Ready()) return true;
            _drawing = true;
            try
            {
                Panel(Main.spriteBatch, __0, __1, __2, __3, PanelColor, Color.Black);
                return false;
            }
            catch (Exception ex)
            {
                LogOnce(ex);
                return true;
            }
            finally { _drawing = false; }
        }

        public static void ChromePrefix() => ChromeDepth++;

        public static Exception ChromeFinalizer(Exception __exception)
        {
            ChromeDepth--;
            return __exception;
        }

        // ── Routing ──

        /// <summary>Draws the vanilla equivalent of a semantic rect. Returns false to leave it to Core.</summary>
        private static bool Route(SpriteBatch sb, int x, int y, int w, int h, Color4 c)
        {
            bool thin = w <= 3 || h <= 3;
            if (thin)
            {
                // DrawPanel's hard-coded outline (used if the JIT inlined DrawPanel past our prefix): the UIPanel has its own border.
                if (c.R == 100 && c.G == 100 && c.B == 120 && c.A == 255) return true; // handled = not drawn
                if (Eq(c, UIColors.Accent)) { Flat(sb, x, y, w, h, Gold); return true; }
                if (Eq(c, UIColors.Border)) { Flat(sb, x, y, w, h, Color.Black * 0.8f); return true; }
                if (Eq(c, UIColors.Divider) || Eq(c, UIColors.Button)) { Flat(sb, x, y, w, h, LineColor); return true; }
                return false;
            }

            if (Eq(c, UIColors.PanelBg)) { Panel(sb, x, y, w, h, PanelColor, Color.Black); return true; }

            // Window chrome: tModLoader windows have no header strip; the title sits on the panel.
            if (ChromeDepth > 0 && Eq(c, UIColors.HeaderBg)) return true; // handled = not drawn

            if (Eq(c, UIColors.CloseBtn)) { Panel(sb, x, y, w, h, PanelColor, Color.Black); return true; }
            if (Eq(c, UIColors.CloseBtnHover)) { Panel(sb, x, y, w, h, CloseHover, Gold); return true; }

            if (Eq(c, UIColors.ItemBg)) { Slot(sb, x, y, w, h, SlotKind.Normal); return true; }
            if (Eq(c, UIColors.ItemHoverBg)) { Slot(sb, x, y, w, h, SlotKind.Hover); return true; }
            if (Eq(c, UIColors.ItemActiveBg)) { Slot(sb, x, y, w, h, SlotKind.Active); return true; }

            if (Eq(c, UIColors.InputBg)) { Panel(sb, x, y, w, h, PanelColor, Color.Black); return true; }
            if (Eq(c, UIColors.InputFocusBg) || Eq(c, UIColors.HeaderBg)) { Panel(sb, x, y, w, h, PanelHover, Color.Black); return true; }

            if (Eq(c, UIColors.Button)) { Panel(sb, x, y, w, h, PanelColor, Color.Black); return true; }
            if (Eq(c, UIColors.ButtonHover)) { Panel(sb, x, y, w, h, PanelHover, Color.Black); return true; }
            if (Eq(c, UIColors.Accent)) { Panel(sb, x, y, w, h, PanelHover, Gold); return true; }

            // "On" state of toggles (Success colour, any alpha): green panel, brighter when hovered (alpha < 255).
            if (c.R == UIColors.Success.R && c.G == UIColors.Success.G && c.B == UIColors.Success.B)
            {
                Panel(sb, x, y, w, h, c.A < 255 ? OnHover : OnColor, Color.Black);
                return true;
            }

            if (Eq(c, UIColors.SectionBg)) { Panel(sb, x, y, w, h, InnerColor, Color.Black); return true; }
            if (Eq(c, UIColors.TooltipBg)) { Utils.DrawInvBG(sb, x, y, w, h, TooltipColor); return true; }

            if (Eq(c, UIColors.ScrollTrack)) { Bar(sb, _scrollbar.Value, x, y, w, h, Color.White); return true; }
            if (Eq(c, UIColors.ScrollThumb) || Eq(c, UIColors.SliderThumb)) { Bar(sb, _scrollbarInner.Value, x, y, w, h, Color.White * 0.85f); return true; }
            if (Eq(c, UIColors.SliderThumbHover)) { Bar(sb, _scrollbarInner.Value, x, y, w, h, Color.White); return true; }
            if (Eq(c, UIColors.SliderTrack)) { Flat(sb, x, y, w, h, TrackColor); return true; }

            return false;
        }

        // ── Primitives ──

        private enum SlotKind { Normal, Hover, Active }

        private static void Slot(SpriteBatch sb, int x, int y, int w, int h, SlotKind kind)
        {
            float aspect = (float)w / h;
            if (aspect > 1.6f || aspect < 0.6f)
            {
                // List rows / tabs: tModLoader uses UIPanels for these.
                Color bg = kind == SlotKind.Normal ? InnerColor : PanelHover;
                Panel(sb, x, y, w, h, bg, kind == SlotKind.Active ? Gold : Color.Black);
                return;
            }
            Texture2D tex = kind == SlotKind.Active ? TextureAssets.InventoryBack14.Value : TextureAssets.InventoryBack.Value;
            Color col = kind == SlotKind.Hover ? Color.White : SlotColor;
            if (Math.Abs(w - h) <= 4)
                sb.Draw(tex, new Rectangle(x, y, w, h), col);          // exactly like ItemSlot at inventory scale
            else
                NineSlice(sb, tex, x, y, w, h, 10, col);
        }

        /// <summary>Vanilla UIPanel: PanelBackground tinted with the background colour, PanelBorder on top.</summary>
        internal static void Panel(SpriteBatch sb, int x, int y, int w, int h, Color bg, Color border)
        {
            NineSlice(sb, _panelBg.Value, x, y, w, h, 12, bg);
            NineSlice(sb, _panelBorder.Value, x, y, w, h, 12, border);
        }

        /// <summary>9-slice with square corners of <paramref name="corner"/> source pixels (shrunk for tiny rects).</summary>
        private static void NineSlice(SpriteBatch sb, Texture2D t, int x, int y, int w, int h, int corner, Color c)
        {
            int cd = Math.Max(1, Math.Min(corner, Math.Min(w / 2, h / 2)));
            int tw = t.Width, th = t.Height;
            int midSrcW = Math.Max(1, tw - corner * 2), midSrcH = Math.Max(1, th - corner * 2);
            int midW = w - cd * 2, midH = h - cd * 2;

            // corners
            sb.Draw(t, new Rectangle(x, y, cd, cd), new Rectangle(0, 0, corner, corner), c);
            sb.Draw(t, new Rectangle(x + w - cd, y, cd, cd), new Rectangle(tw - corner, 0, corner, corner), c);
            sb.Draw(t, new Rectangle(x, y + h - cd, cd, cd), new Rectangle(0, th - corner, corner, corner), c);
            sb.Draw(t, new Rectangle(x + w - cd, y + h - cd, cd, cd), new Rectangle(tw - corner, th - corner, corner, corner), c);
            if (midW > 0)
            {
                sb.Draw(t, new Rectangle(x + cd, y, midW, cd), new Rectangle(corner, 0, midSrcW, corner), c);
                sb.Draw(t, new Rectangle(x + cd, y + h - cd, midW, cd), new Rectangle(corner, th - corner, midSrcW, corner), c);
            }
            if (midH > 0)
            {
                sb.Draw(t, new Rectangle(x, y + cd, cd, midH), new Rectangle(0, corner, corner, midSrcH), c);
                sb.Draw(t, new Rectangle(x + w - cd, y + cd, cd, midH), new Rectangle(tw - corner, corner, corner, midSrcH), c);
            }
            if (midW > 0 && midH > 0)
                sb.Draw(t, new Rectangle(x + cd, y + cd, midW, midH), new Rectangle(corner, corner, midSrcW, midSrcH), c);
        }

        /// <summary>Vanilla UIScrollbar bar: 6px caps top and bottom, stretched middle (drawn inside the rect).</summary>
        private static void Bar(SpriteBatch sb, Texture2D t, int x, int y, int w, int h, Color c)
        {
            const int cap = 6;
            if (h <= cap * 2)
            {
                sb.Draw(t, new Rectangle(x, y, w, h), c);
                return;
            }
            sb.Draw(t, new Rectangle(x, y, w, cap), new Rectangle(0, 0, t.Width, cap), c);
            sb.Draw(t, new Rectangle(x, y + cap, w, h - cap * 2), new Rectangle(0, cap, t.Width, Math.Max(1, t.Height - cap * 2)), c);
            sb.Draw(t, new Rectangle(x, y + h - cap, w, cap), new Rectangle(0, t.Height - cap, t.Width, cap), c);
        }

        private static void Flat(SpriteBatch sb, int x, int y, int w, int h, Color c)
        {
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(x, y, w, h), c);
        }

        private static void LogOnce(Exception ex)
        {
            if (_errorLogged) return;
            _errorLogged = true;
            Log.Error("Skin draw failed (falling back to flat style for that element)", ex);
        }
    }
}
