using MagicStorage.CrossMod;
using MagicStorage.UI.States;
using System;
using System.Collections.Generic;
using Terraria.UI;

namespace MagicStorage.UI {
	internal abstract class BaseOptionUIPage : BaseStorageUIPage {
		public int option;

		public BaseOptionUIPage(BaseStorageUI parent, string name) : base(parent, name) { }
	}

	internal abstract class BaseOptionUIPage<TOption, TElement> : BaseOptionUIPage where TElement : BaseOptionElement {
		private readonly List<TElement> buttons = new();

		public BaseOptionUIPage(BaseStorageUI parent, string name) : base(parent, name) {
			OnPageSelected += InitOptionButtons;
		}

		public abstract IEnumerable<TOption> GetOptions();

		public abstract TElement CreateElement(TOption option);

		public abstract int GetOptionType(TElement element);

		public override void OnInitialize() => InitOptionButtons();

		public override void Recalculate() {
			base.Recalculate();

			InitOptionButtons();
		}

		private void InitOptionButtons() {
			foreach (TElement button in buttons)
				button.Remove();

			buttons.Clear();

			const int leftOrig = 20, topOrig = 20;

			CalculatedStyle dims = GetInnerDimensions();

			const int buttonSizeWithBuffer = 32 + 10;

			int columns = Math.Max(1, (int)(dims.Width - leftOrig * 2) / buttonSizeWithBuffer);

			int index = 0;

			foreach (TOption option in GetOptions()) {
				TElement element = CreateElement(option);
				element.OnClick += ClickOption;

				element.Left.Set(leftOrig + buttonSizeWithBuffer * (index % columns), 0f);
				element.Top.Set(topOrig + buttonSizeWithBuffer * (index / columns), 0f);

				Append(element);
				buttons.Add(element);
			}
		}

		private void ClickOption(UIMouseEvent evt, UIElement e) {
			int newOption = GetOptionType(e as TElement);

			if (newOption != option)
				StorageGUI.needRefresh = true;

			option = newOption;
		}
	}

	internal class SortingPage : BaseOptionUIPage<SortingOption, SortingOptionElement> {
		public SortingPage(BaseStorageUI parent) : base(parent, "Sorting") { }

		public override SortingOptionElement CreateElement(SortingOption option) => new(option);

		public override IEnumerable<SortingOption> GetOptions() => SortingOptionLoader.GetOptions(craftingGUI: StoragePlayer.LocalPlayer.StorageCrafting());

		public override int GetOptionType(SortingOptionElement element) => element.option.Type;
	}

	internal class FilteringPage : BaseOptionUIPage<FilteringOption, FilteringOptionElement> {
		public FilteringPage(BaseStorageUI parent) : base(parent, "Filtering") { }

		public override FilteringOptionElement CreateElement(FilteringOption option) => new(option);

		public override IEnumerable<FilteringOption> GetOptions() => FilteringOptionLoader.GetOptions(craftingGUI: StoragePlayer.LocalPlayer.StorageCrafting());

		public override int GetOptionType(FilteringOptionElement element) => element.option.Type;
	}
}
