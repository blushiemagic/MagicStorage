using System;
using MagicStorage.Common.Systems;
using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;

namespace MagicStorage
{
	public class ModSearchBox : UITextPanel<string>
	{
		public const int ModIndexBaseGame = -1;
		public const int ModIndexAll = -2;
		public Action<int, int> OnChanged;

		public int ModIndex { get; private set; } = ModIndexAll;

		public ModSearchBox(Action<int, int> onChanged, float textScale = 1, bool large = false) : base("", textScale, large)
		{
			OnChanged = onChanged;

			BackgroundColor = new Color(63, 82, 151) * 0.7f;

			Reset(true);
		}

		private void SetSearchMod(int index, bool silent)
		{
			if (ModIndex == index)
				return;
			
			int old = ModIndex;
			ModIndex = index;
			
			SetText(MakeModButtonText());

			if (!silent)
				OnChanged?.Invoke(old, ModIndex);
		}

		public static string GetNameFromIndex(int index) {
			string name = index switch
			{
				ModIndexAll      => Language.GetTextValue("Mods.MagicStorage.FilterAllMods"),
				ModIndexBaseGame => "Terraria",
				_                => MagicCache.AllMods[index].Name
			};

			if (name == "ModLoader")
				name = "tModLoader";

			return name;
		}

		public void Reset(bool silent)
		{
			ModIndex = ModIndexAll - 1;
			SetSearchMod(ModIndexAll, silent);
		}

		private string MakeModButtonText() => GetNameFromIndex(ModIndex);

		public override void MouseOver(UIMouseEvent evt) {
			base.MouseOver(evt);

			BackgroundColor = new Color(73, 94, 171);
		}

		public override void MouseOut(UIMouseEvent evt) {
			base.MouseOut(evt);

			BackgroundColor = new Color(63, 82, 151) * 0.7f;
		}

		public override void Click(UIMouseEvent evt) {
			base.Click(evt);

			var allMods = MagicCache.AllMods;
			int index = ModIndex;

			index++;
			if (index >= allMods.Length)
				index = ModIndexAll;

			SetSearchMod(index, false);
		}

		public override void RightClick(UIMouseEvent evt) {
			base.RightClick(evt);

			var allMods = MagicCache.AllMods;
			int index = ModIndex;

			index--;
			if (index < -2)
				index = allMods.Length - 1;

			SetSearchMod(index, false);
		}
	}
}
