using MagicStorage.Common.Systems;
using MagicStorage.CrossMod;
using MagicStorage.UI.States;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.UI;

namespace MagicStorage.UI {
	public delegate void OptionClicked(UIMouseEvent evt, BaseOptionElement target, int option);

	public abstract class BaseOptionUIPage : BaseStorageUIPage {
		// Used to remember which filters were selected
		public int selected;

		public int buttonSize = 32, buttonPadding = 1;

		internal bool filterBaseOptions = false;

		public event OptionClicked OnOptionClicked;

		public BaseOptionUIPage(BaseStorageUI parent, string name) : base(parent, name) {
			OnPageSelected += () => InitOptionButtons(false);
		}

		public override void OnActivate() => InitOptionButtons(true);

		protected void InvokeOnOptionClicked(UIMouseEvent evt, BaseOptionElement e, int option) => OnOptionClicked?.Invoke(evt, e, option);

		public virtual void InitOptionButtons(bool activating) { }

		public void UpdateButtonLayout(int newButtonSize = -1, int newButtonPadding = -1) {
			bool hasChange = false;

			if (newButtonSize > 0 && newButtonSize != buttonSize) {
				buttonSize = newButtonSize;
				hasChange = true;
			}

			if (newButtonPadding > 0 && newButtonPadding != buttonPadding) {
				buttonPadding = newButtonPadding;
				hasChange = true;
			}

			if (hasChange)
				InitOptionButtons(false);
		}

		public abstract bool IsOptionGeneral(BaseOptionElement element);

		public virtual void SetSelection(int option) { }
	}

	public abstract class BaseOptionUIPage<TOption, TElement> : BaseOptionUIPage where TElement : BaseOptionElement {
		private readonly List<TElement> buttons = new();

		public BaseOptionUIPage(BaseStorageUI parent, string name) : base(parent, name) { }

		public abstract IEnumerable<TOption> GetOptions();

		public abstract TElement CreateElement(TOption option);

		protected abstract void OnConfigurationClicked(TElement element);

		public abstract TOption GetOption(TElement element);

		public abstract int GetOptionType(TElement element);

		public sealed override bool IsOptionGeneral(BaseOptionElement element) => IsOptionGeneral((TElement)element);

		public abstract bool IsOptionGeneral(TElement element);

		private bool IsOptionNotGeneral(TElement element) => !IsOptionGeneral(element);

		public abstract void UpdateLoaderSelection(int option, bool generalOption);

		public sealed override void SetSelection(int option) {
			var element = buttons[option];
			UpdateLoaderSelection(GetOptionType(element), IsOptionGeneral(element));
		}

		private const int leftOrig = 20, topOrig = 0;

		public sealed override void InitOptionButtons(bool activating) {
			if (Main.gameMenu)
				return;

			foreach (TElement button in buttons)
				button.Remove();

			buttons.Clear();

			CalculatedStyle dims = GetInnerDimensions();

			int buttonSizeWithBuffer = buttonSize + buttonPadding;

			int columns = Math.Max(1, (int)(dims.Width - leftOrig * 2) / buttonSizeWithBuffer);

			foreach (TOption option in GetOptions()) {
				TElement element = CreateElement(option);
				element.OnLeftClick += ClickOption;

				element.SetSize(buttonSize);

				Append(element);
				buttons.Add(element);
			}

			int index = 0;
			int left = leftOrig, top = topOrig;

			foreach (var element in buttons.Where(IsOptionNotGeneral)) {
				AlignButton(element, activating, index, buttonSizeWithBuffer, columns, ref left, ref top);
				index++;
			}

			left = leftOrig;
			if (index % columns != 0)
				top += buttonSizeWithBuffer;
			index = 0;

			foreach (var element in buttons.Where(IsOptionGeneral)) {
				AlignButton(element, activating, index, buttonSizeWithBuffer, columns, ref left, ref top);
				index++;
			}
		}

		private void AlignButton(TElement element, bool activating, int index, int buttonSizeWithBuffer, int columns, ref int left, ref int top) {
			element.Left.Set(left, 0f);
			element.Top.Set(top, 0f);

			if (!activating) {
				element.Activate();
				element.Recalculate();
			}

			if ((index + 1) % columns == 0) {
				left = leftOrig;
				top += buttonSizeWithBuffer;
			} else
				left += buttonSizeWithBuffer;
		}

		internal void ClickOption(UIMouseEvent evt, UIElement e) {
			if (MagicStorageConfig.ButtonUIMode == ButtonConfigurationMode.ModernConfigurable) {
				OnConfigurationClicked(e as TElement);
				MagicUI.SetRefresh(forceFullRefresh: true);
				SoundEngine.PlaySound(SoundID.MenuTick);
				return;
			}

			var element = (TElement)e;
			var type = GetOptionType(element);

			SetSelection(type);

			MagicUI.SetRefresh(forceFullRefresh: true);
			SoundEngine.PlaySound(SoundID.MenuTick);

			InvokeOnOptionClicked(evt, element, type);
		}
	}

	internal class SortingPage : BaseOptionUIPage<SortingOption, SortingOptionElement> {
		public SortingPage(BaseStorageUI parent) : base(parent, "Sorting") { }

		public override SortingOptionElement CreateElement(SortingOption option) => new(option);

		public override SortingOption GetOption(SortingOptionElement element) => element.option;

		public override IEnumerable<SortingOption> GetOptions() {
			IEnumerable<SortingOption> orig = SortingOptionLoader.GetVisibleOptions(craftingGUI: StoragePlayer.IsStorageCraftingOrDecrafting());

			if (!filterBaseOptions)
				return orig;

			return orig.Except(SortingOptionLoader.BaseOptions, ReferenceEqualityComparer.Instance).OfType<SortingOption>();
		}

		public override int GetOptionType(SortingOptionElement element) => element.option.Type;

		public override bool IsOptionGeneral(SortingOptionElement element) => false;

		protected override void OnConfigurationClicked(SortingOptionElement element) => MagicStorageMod.Instance.optionsConfig.ToggleEnabled(element.option);

		public override void UpdateLoaderSelection(int option, bool generalOption) => SortingOptionLoader.Selected = option;
	}

	internal class FilteringPage : BaseOptionUIPage<FilteringOption, FilteringOptionElement> {
		// Used to remember which filters were selected
		public readonly HashSet<int> generalSelections = new();

		public FilteringPage(BaseStorageUI parent) : base(parent, "Filtering") { }

		public override FilteringOptionElement CreateElement(FilteringOption option) => new(option);

		public override FilteringOption GetOption(FilteringOptionElement element) => element.option;

		public override IEnumerable<FilteringOption> GetOptions() {
			IEnumerable<FilteringOption> orig = FilteringOptionLoader.GetVisibleOptions(craftingGUI: StoragePlayer.IsStorageCraftingOrDecrafting());

			if (!filterBaseOptions)
				return orig;

			return orig.Except(FilteringOptionLoader.BaseOptions, ReferenceEqualityComparer.Instance).OfType<FilteringOption>();
		}

		public override int GetOptionType(FilteringOptionElement element) => element.option.Type;

		public override bool IsOptionGeneral(FilteringOptionElement element) => element.option.IsGeneralFilter;

		protected override void OnConfigurationClicked(FilteringOptionElement element) => MagicStorageMod.Instance.optionsConfig.ToggleEnabled(element.option);

		public override void UpdateLoaderSelection(int option, bool generalOption) {
			if (!generalOption)
				FilteringOptionLoader.Selected = option;
			else {
				if (FilteringOptionLoader.GeneralSelections.Contains(option))
					FilteringOptionLoader.GeneralSelections.Remove(option);
				else
					FilteringOptionLoader.GeneralSelections.Add(option);
			}
		}
	}
}
