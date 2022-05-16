using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;

namespace MagicStorage
{
	public class ModSearchBox
	{
		public const int ModIndexBaseGame = -1;
		public const int ModIndexAll = -2;
		private UITextPanel<string> _modButton;
		public Action OnChanged;

		public int ModIndex { get; private set; } = ModIndexAll;

		public string ModName { get; private set; }

		public UIPanel Button => _modButton;

		public ModSearchBox(Action onChanged)
		{
			OnChanged = onChanged;
		}

		public void InitLangStuff()
		{
			_modButton ??= new UITextPanel<string>(MakeModButtonText(), 0.8f);
		}

		private void SetSearchMod(int index, bool silent)
		{
			if (ModIndex == index)
				return;
			ModIndex = index;
			_modButton?.SetText(MakeModButtonText());
			ModName = "";
			if (index > -1)
				ModName = MagicStorage.AllMods[index].Name;
			if (!silent)
				OnChanged?.Invoke();
		}

		public void Reset(bool silent)
		{
			SetSearchMod(ModIndexAll, silent);
		}

		private string MakeModButtonText()
		{
			return ModIndex switch
			{
				ModIndexAll      => "All mods",
				ModIndexBaseGame => "Terraria",
				_                => MagicStorage.AllMods[ModIndex].Name
			};
		}

		public void Update(MouseState curMouse, MouseState oldMouse)
		{
			Rectangle dim = InterfaceHelper.GetFullRectangle(_modButton);
			if (curMouse.X > dim.X && curMouse.X < dim.X + dim.Width && curMouse.Y > dim.Y && curMouse.Y < dim.Y + dim.Height)
			{
				_modButton.BackgroundColor = new Color(73, 94, 171);

				var allMods = MagicStorage.AllMods;
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
