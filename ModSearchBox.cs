using System;
using Microsoft.Win32.SafeHandles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria.GameContent.UI.Elements;

namespace MagicStorage
{
    public class ModSearchBox
    {
        UITextPanel<string> _modButton;
        public int ModIndex { get; private set; } = ModIndexAll;
        public string ModName { get; private set; }
        public Action OnChanged;

        public ModSearchBox(Action onChanged)
        {
            OnChanged = onChanged;
        }

        public UIPanel Button => _modButton;

        public void InitLangStuff()
        {
            if (_modButton == null)
            {
                _modButton = new UITextPanel<string>(MakeModButtonText(), 0.8f);
            }
        }

        void SetSearchMod(int index, bool silent)
        {
            if (ModIndex == index) return;
            ModIndex = index;
            if (_modButton != null)
                _modButton.SetText(MakeModButtonText());
            ModName = "";
            if (index > -1) ModName = MagicStorage.Instance.AllMods[index];
            if (!silent) OnChanged?.Invoke();
        }

        public void Reset(bool silent)
        {
            SetSearchMod(ModIndexAll, silent);
        }

        public const int ModIndexBaseGame = -1;
        public const int ModIndexAll = -2;

        string MakeModButtonText()
        {
            if (ModIndex == ModIndexAll)
                return "All mods";
            else if (ModIndex == ModIndexBaseGame)
            {
                return "Terraria";
            }
            else
                return MagicStorage.Instance.AllMods[ModIndex];
        }

        public void Update(MouseState curMouse, MouseState oldMouse)
        {
            Rectangle dim = InterfaceHelper.GetFullRectangle(_modButton);
            if (curMouse.X > dim.X && curMouse.X < dim.X + dim.Width && curMouse.Y > dim.Y && curMouse.Y < dim.Y + dim.Height)
            {
                _modButton.BackgroundColor = new Color(73, 94, 171);
                var allMods = MagicStorage.Instance.AllMods;
                int index = ModIndex;
                if (curMouse.LeftButton == ButtonState.Pressed && oldMouse.LeftButton == ButtonState.Released)
                {
                    index++;
                    if (index >= allMods.Length)
                        index = ModIndexAll;
                }
                else if (curMouse.RightButton == ButtonState.Pressed && oldMouse.RightButton == ButtonState.Released)
                {
                    index--;
                    if (index < -2)
                        index = allMods.Length - 1;
                }

                SetSearchMod(index, false);
            }
            else
            {
                _modButton.BackgroundColor = new Color(63, 82, 151) * 0.7f;
            }
        }
    }
}
