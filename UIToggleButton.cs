using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Localization;
using Terraria.UI;

namespace MagicStorage
{
    public class UIToggleButton : UIElement
    {
        private int buttonSize;
        private int buttonPadding;
        private readonly Action onChanged;
        private Texture2D _button;
        private LocalizedText _name;
        
        private bool _value;
        
        public bool Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
            }
        }

        public UIToggleButton(Action onChanged, Texture2D button, LocalizedText name, int buttonSize = 21, int buttonPadding = 1)
        {
            this.buttonSize = buttonSize;
            this.buttonPadding = buttonPadding;
            this.onChanged = onChanged;
            this._button = button;
            this._name = name;
            this.Width.Set(buttonSize, 0f);
            this.MinWidth.Set(buttonSize, 0f);
            this.Height.Set(buttonSize, 0f);
            this.MinHeight.Set(buttonSize, 0f);
        }

        public override void Update(GameTime gameTime)
        {
            var oldValue = _value;
            if (StorageGUI.MouseClicked && Parent != null)
            {
                if (MouseOverButton(StorageGUI.curMouse.X, StorageGUI.curMouse.Y))
                {
                    _value = !_value;
                }
            }

            if (oldValue != _value)
            {
                onChanged?.Invoke();
            }
        }

        private bool MouseOverButton(int mouseX, int mouseY)
        {
            Rectangle dim = InterfaceHelper.GetFullRectangle(this);
            float left = dim.X;
            float right = left + buttonSize * Main.UIScale;
            float top = dim.Y;
            float bottom = top + buttonSize * Main.UIScale;
            return mouseX > left && mouseX < right && mouseY > top && mouseY < bottom;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            Texture2D backTexture = MagicStorage.Instance.GetTexture("SortButtonBackground");
            Texture2D backTextureActive = MagicStorage.Instance.GetTexture("SortButtonBackgroundActive");
            CalculatedStyle dim = GetDimensions();
            Texture2D texture = _value ? backTextureActive : backTexture;
            Vector2 drawPos = new Vector2(dim.X, dim.Y);
            Color color = MouseOverButton(StorageGUI.curMouse.X, StorageGUI.curMouse.Y) ? Color.Silver : Color.White;
            Main.spriteBatch.Draw(texture, new Rectangle((int) drawPos.X, (int) drawPos.Y, buttonSize, buttonSize), color);
            Main.spriteBatch.Draw(_button, new Rectangle((int) drawPos.X + 1, (int) drawPos.Y + 1, buttonSize - 1, buttonSize - 1), Color.White);
        }

        public void DrawText()
        {
            if (MouseOverButton(StorageGUI.curMouse.X, StorageGUI.curMouse.Y))
            {
                Main.instance.MouseText(_name.Value);
            }
        }
    }
}