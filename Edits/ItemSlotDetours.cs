using MagicStorage.Common.Systems;
using MagicStorage.UI;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;

namespace MagicStorage.Edits {
	internal class ItemSlotDetours : ILoadable {
		public Mod Mod { get; private set; } = null!;

		public void Load(Mod mod)
		{
			Mod = mod;

			On.Terraria.UI.ItemSlot.MouseHover_ItemArray_int_int += ItemSlot_MouseHover_ItemArray_int_int;
			On.Terraria.UI.ItemSlot.LeftClick_ItemArray_int_int += ItemSlot_LeftClick_ItemArray_int_int;
			On.Terraria.UI.ItemSlot.RightClick_ItemArray_int_int += ItemSlot_RightClick_ItemArray_int_int;
			On.Terraria.UI.ItemSlot.OverrideHover_ItemArray_int_int += ItemSlot_OverrideHover_ItemArray_int_int;
			On.Terraria.UI.ItemSlot.OverrideLeftClick += ItemSlot_OverrideLeftClick;
		}

		private void ItemSlot_MouseHover_ItemArray_int_int(On.Terraria.UI.ItemSlot.orig_MouseHover_ItemArray_int_int orig, Item[] inv, int context, int slot) {
			if (!PreventActions())
				orig(inv, context, slot);
		}

		private void ItemSlot_LeftClick_ItemArray_int_int(On.Terraria.UI.ItemSlot.orig_LeftClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
			if (!PreventActions())
				orig(inv, context, slot);
		}

		private void ItemSlot_RightClick_ItemArray_int_int(On.Terraria.UI.ItemSlot.orig_RightClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
			if (!PreventActions())
				orig(inv, context, slot);
		}

		private void ItemSlot_OverrideHover_ItemArray_int_int(On.Terraria.UI.ItemSlot.orig_OverrideHover_ItemArray_int_int orig, Item[] inv, int context, int slot) {
			if (!PreventActions())
				orig(inv, context, slot);
		}

		private bool ItemSlot_OverrideLeftClick(On.Terraria.UI.ItemSlot.orig_OverrideLeftClick orig, Item[] inv, int context, int slot) {
			return !PreventActions() && orig(inv, context, slot);
		}

		private static bool PreventActions() {
			if (MagicUI.uiInterface?.CurrentState is null || !Main.hasFocus || MagicUI.BlockItemSlotActionsDetour)
				return false;

			UIElement element = MagicUI.uiInterface.CurrentState.GetElementAt(new Microsoft.Xna.Framework.Vector2(Main.mouseX, Main.mouseY));

			if (element is null || object.ReferenceEquals(element, MagicUI.uiInterface.CurrentState))
				return false;  //Not hovering over an element in the state

			bool panelDetected = false;

			while (element.Parent is not null) {
				if (element is UIPanel)
					panelDetected = true;

				element = element.Parent;
			}

			return panelDetected && object.ReferenceEquals(element, MagicUI.uiInterface.CurrentState);
		}

		public void Unload()
		{
			On.Terraria.UI.ItemSlot.MouseHover_ItemArray_int_int -= ItemSlot_MouseHover_ItemArray_int_int;
			On.Terraria.UI.ItemSlot.LeftClick_ItemArray_int_int -= ItemSlot_LeftClick_ItemArray_int_int;
			On.Terraria.UI.ItemSlot.RightClick_ItemArray_int_int -= ItemSlot_RightClick_ItemArray_int_int;
			On.Terraria.UI.ItemSlot.OverrideHover_ItemArray_int_int -= ItemSlot_OverrideHover_ItemArray_int_int;
			On.Terraria.UI.ItemSlot.OverrideLeftClick -= ItemSlot_OverrideLeftClick;

			Mod = null!;
		}
	}
}
