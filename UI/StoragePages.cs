using MagicStorage.CrossMod;
using MagicStorage.UI.States;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.UI;

namespace MagicStorage.UI {
	public delegate void OptionClicked(UIMouseEvent evt, UIElement target, int option);

	internal abstract class BaseOptionUIPage : BaseStorageUIPage {
		public int option;

		public int buttonSize = 32, buttonPadding = 1;

		internal bool filterBaseOptions = false;

		public event OptionClicked OnOptionClicked;

		public BaseOptionUIPage(BaseStorageUI parent, string name) : base(parent, name) {
			OnPageSelected += () => InitOptionButtons(false);
		}

		public override void OnActivate() => InitOptionButtons(true);

		protected void InvokeOnOptionClicked(UIMouseEvent evt, UIElement e, int option) => OnOptionClicked?.Invoke(evt, e, option);

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

		public abstract void SetLoaderSelection(int selected);
	}

	internal abstract class BaseOptionUIPage<TOption, TElement> : BaseOptionUIPage where TElement : BaseOptionElement {
		private readonly List<TElement> buttons = new();

		public BaseOptionUIPage(BaseStorageUI parent, string name) : base(parent, name) { }

		public abstract IEnumerable<TOption> GetOptions();

		public abstract TElement CreateElement(TOption option);

		protected abstract void OnConfigurationClicked(TElement element);

		public abstract TOption GetOption(TElement element);

		public abstract int GetOptionType(TElement element);

		public sealed override void InitOptionButtons(bool activating) {
			if (Main.gameMenu)
				return;

			foreach (TElement button in buttons)
				button.Remove();

			buttons.Clear();

			const int leftOrig = 20, topOrig = 0;

			CalculatedStyle dims = GetInnerDimensions();

			int buttonSizeWithBuffer = buttonSize + buttonPadding;

			int columns = Math.Max(1, (int)(dims.Width - leftOrig * 2) / buttonSizeWithBuffer);

			int index = 0;

			foreach (TOption option in GetOptions()) {
				TElement element = CreateElement(option);
				element.OnClick += ClickOption;

				element.Left.Set(leftOrig + buttonSizeWithBuffer * (index % columns), 0f);
				element.Top.Set(topOrig + buttonSizeWithBuffer * (index / columns), 0f);
				element.SetSize(buttonSize);

				Append(element);
				buttons.Add(element);

				if (!activating) {
					element.Activate();
					element.Recalculate();
				}

				index++;
			}
		}

		internal void ClickOption(UIMouseEvent evt, UIElement e) {
			if (MagicStorageConfig.ButtonUIMode == ButtonConfigurationMode.ModernConfigurable) {
				OnConfigurationClicked(e as TElement);
				StorageGUI.needRefresh = true;
				SoundEngine.PlaySound(SoundID.MenuTick);
				return;
			}

			option = GetOptionType(e as TElement);

			StorageGUI.needRefresh = true;
			SoundEngine.PlaySound(SoundID.MenuTick);

			SetLoaderSelection(option);

			InvokeOnOptionClicked(evt, e, option);
		}
	}

	internal class SortingPage : BaseOptionUIPage<SortingOption, SortingOptionElement> {
		public SortingPage(BaseStorageUI parent) : base(parent, "Sorting") { }

		public override SortingOptionElement CreateElement(SortingOption option) => new(option);

		public override SortingOption GetOption(SortingOptionElement element) => element.option;

		public override IEnumerable<SortingOption> GetOptions() {
			IEnumerable<SortingOption> orig = SortingOptionLoader.GetVisibleOptions(craftingGUI: StoragePlayer.LocalPlayer.StorageCrafting());

			if (!filterBaseOptions)
				return orig;

			return orig.Except(SortingOptionLoader.BaseOptions, ReferenceEqualityComparer.Instance).OfType<SortingOption>();
		}

		public override int GetOptionType(SortingOptionElement element) => element.option.Type;

		protected override void OnConfigurationClicked(SortingOptionElement element) => MagicStorageMod.Instance.optionsConfig.ToggleEnabled(element.option);

		public override void SetLoaderSelection(int selected) => SortingOptionLoader.Selected = selected;
	}

	internal class FilteringPage : BaseOptionUIPage<FilteringOption, FilteringOptionElement> {
		public FilteringPage(BaseStorageUI parent) : base(parent, "Filtering") { }

		public override FilteringOptionElement CreateElement(FilteringOption option) => new(option);

		public override FilteringOption GetOption(FilteringOptionElement element) => element.option;

		public override IEnumerable<FilteringOption> GetOptions() {
			IEnumerable<FilteringOption> orig = FilteringOptionLoader.GetVisibleOptions(craftingGUI: StoragePlayer.LocalPlayer.StorageCrafting());

			if (!filterBaseOptions)
				return orig;

			return orig.Except(FilteringOptionLoader.BaseOptions, ReferenceEqualityComparer.Instance).OfType<FilteringOption>();
		}

		public override int GetOptionType(FilteringOptionElement element) => element.option.Type;

		protected override void OnConfigurationClicked(FilteringOptionElement element) => MagicStorageMod.Instance.optionsConfig.ToggleEnabled(element.option);

		public override void SetLoaderSelection(int selected) => FilteringOptionLoader.Selected = selected;
	}
}
