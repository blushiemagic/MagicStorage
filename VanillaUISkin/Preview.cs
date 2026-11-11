using System.IO;
using Terraria.ID;
using TerrariaModder.Core.UI;
using TerrariaModder.Core.UI.Widgets;

namespace VanillaUISkin
{
    /// <summary>
    /// Developer preview: if "preview.flag" exists in the mod folder, a window showing every widget type
    /// opens on the title screen (no input needed), so the skin can be checked from a screenshot.
    /// </summary>
    internal static class Preview
    {
        private static DraggablePanel _panel;
        private static readonly TextInput _input = new TextInput("Search...", 40);
        private static readonly ScrollView _scroll = new ScrollView();
        private static readonly Slider _slider = new Slider();
        private static int _sliderValue = 40;
        private static bool _check = true;

        private static readonly int[] Items =
        {
            ItemID.CopperBroadsword, ItemID.DirtBlock, ItemID.Torch, ItemID.Wood, ItemID.IronBar, ItemID.Gel,
            ItemID.WorkBench, ItemID.HealingPotion, ItemID.Diamond, ItemID.MagicMirror,
        };

        public static void Init(string modFolder)
        {
            if (!File.Exists(Path.Combine(modFolder, "preview.flag"))) return;
            _panel = new DraggablePanel("vanilla-ui-skin-preview", "Vanilla UI Skin preview", 560, 470);
            _panel.RegisterDrawCallback(Draw);
            _panel.Open(60, 120);
            Log.Info("Preview window enabled (preview.flag present)");
        }

        public static void Unload()
        {
            if (_panel == null) return;
            _panel.UnregisterDrawCallback();
            _panel.Close();
            _panel = null;
        }

        private static void Draw()
        {
            if (_panel == null || !_panel.BeginDraw()) return;
            try
            {
                int x = _panel.ContentX, y = _panel.ContentY + 4, w = _panel.ContentWidth;

                _input.Update();
                _input.Draw(x, y, 200, 26);
                Button.Draw(x + 208, y, 110, 26, "Sort: Default");
                Button.Draw(x + 324, y, 70, 26, "Accent", UIColors.Accent, UIColors.ButtonHover, UIColors.AccentText);
                Toggle.Draw(x + 400, y, 120, 26, "Toggle on", true);
                y += 34;

                // item slots: normal, hover, active (Magic Storage draws them exactly like this)
                for (int i = 0; i < Items.Length; i++)
                {
                    int sx = x + i * 44;
                    var bg = i == 1 ? UIColors.ItemHoverBg : i == 2 ? UIColors.ItemActiveBg : UIColors.ItemBg;
                    UIRenderer.DrawRect(sx, y, 42, 42, bg);
                    UIRenderer.DrawItem(Items[i], sx + 6, y + 6, 30, 30);
                }
                y += 50;

                // list rows / section / divider / tooltip box
                UIRenderer.DrawRect(x, y, 260, 24, UIColors.ItemBg);
                UIRenderer.DrawText("List row (ItemBg)", x + 8, y + 4, UIColors.Text);
                UIRenderer.DrawRect(x + 268, y, 260, 24, UIColors.ItemActiveBg);
                UIRenderer.DrawText("Selected row (ItemActiveBg)", x + 276, y + 4, UIColors.Text);
                y += 32;
                UIRenderer.DrawRect(x, y, 260, 70, UIColors.SectionBg);
                UIRenderer.DrawText("Section (SectionBg)", x + 8, y + 6, UIColors.TextTitle);
                if (Checkbox.Draw(x + 8, y + 36, 20, _check)) _check = !_check;
                UIRenderer.DrawText("Checkbox", x + 34, y + 38, UIColors.TextDim);
                UIRenderer.DrawRect(x + 268, y, 260, 70, UIColors.TooltipBg);
                UIRenderer.DrawRectOutline(x + 268, y, 260, 70, UIColors.Border);
                UIRenderer.DrawText("Tooltip box (TooltipBg)", x + 278, y + 8, UIColors.Text);
                y += 78;
                UIRenderer.DrawRect(x, y, w, 2, UIColors.Divider);
                y += 10;
                _sliderValue = _slider.Draw(x, y, 260, 20, _sliderValue, 0, 100);
                y += 30;

                // scroll view with rows
                int rows = 12, rowH = 26, viewH = _panel.ContentY + _panel.ContentHeight - y - 6;
                _scroll.Begin(x, y, w, viewH, rows * rowH);
                for (int r = 0; r < rows; r++)
                {
                    int ry = _scroll.ContentY + r * rowH;
                    UIRenderer.DrawRect(_scroll.ContentX, ry, _scroll.ContentWidth, rowH - 2, r == 3 ? UIColors.ItemHoverBg : UIColors.ItemBg);
                    UIRenderer.DrawText($"Scrollable row {r + 1}", _scroll.ContentX + 8, ry + 4, UIColors.Text);
                }
                _scroll.End();
            }
            finally
            {
                _panel.EndDraw();
            }
        }
    }
}
