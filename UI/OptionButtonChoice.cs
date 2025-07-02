using MagicStorage.CrossMod;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MagicStorage.UI {
	public class SortingOptionButtonChoice : NewUIButtonChoice {
		private List<SortingOption> options = new();

		public IReadOnlyList<SortingOption> Options => options;

		public SortingOptionButtonChoice(Action onChanged, int buttonSize, int maxButtonsPerRow, int buttonPadding = 1, Action onGearChoiceSelected = null, bool forceGearIconToNotBeCreated = false) : base(onChanged, buttonSize, maxButtonsPerRow, buttonPadding, onGearChoiceSelected, forceGearIconToNotBeCreated) {
		}

		public void AssignOptions(IEnumerable<SortingOption> options) {
			this.options = options.ToList();

			int selection = SelectionType;
			HashSet<int> generalSelections = [.. GeneralSelections];

			AssignButtons(options.Select(static o => new ButtonChoiceInfo(o.TextureAsset, o.Tooltip, false)));

			// Restore the selection
			// If no match was found, the first option will automatically be selected
			bool didAnything = false;
			bool hasSelection = false;
			for (int i = 0; i < this.options.Count; i++) {
				int remapped = RemapChoice(i);
				if (remapped == selection) {
					if (!hasSelection) {
						Choice = i;
						hasSelection = true;
						didAnything = true;
					}
				} else if (generalSelections.Contains(remapped)) {
					GeneralChoices.Add(i);
					didAnything = true;
				}
			}

			if (didAnything)
				OnChanged();
		}

		public void AutomaticallyUpdateButtonLayout(int newButtonPadding = -1) {
			int size = options.Count > 15 ? 21 : 32;
			int maxPerRow = options.Count > 15 ? 22 : 15;

			UpdateButtonLayout(size, newButtonPadding, maxPerRow);
		}

		public override void ClickChoice(int choice, int remappedChoiceType) {
			base.ClickChoice(choice, remappedChoiceType);

			options[choice].OnSelected(this, choice);
		}

		public override int RemapChoice(int choice) => choice < 0 || choice >= options.Count ? -1 : options[choice].Type;
	}

	public class FilteringOptionButtonChoice : NewUIButtonChoice {
		private List<FilteringOption> options = new();

		public IReadOnlyList<FilteringOption> Options => options;

		public FilteringOptionButtonChoice(Action onChanged, int buttonSize, int maxButtonsPerRow, int buttonPadding = 1, Action onGearChoiceSelected = null, bool forceGearIconToNotBeCreated = false) : base(onChanged, buttonSize, maxButtonsPerRow, buttonPadding, onGearChoiceSelected, forceGearIconToNotBeCreated) {
		}

		public void AssignOptions(IEnumerable<FilteringOption> options) {
			this.options = options.ToList();

			int selection = SelectionType;
			HashSet<int> generalSelections = [.. GeneralSelections];

			AssignButtons(options.Select(static o => new ButtonChoiceInfo(o.TextureAsset, o.Tooltip, o.IsGeneralFilter)));

			// Restore the selection(s)
			// If no match was found, the first non-general option will automatically be selected
			bool didAnything = false;
			bool hasSelection = false;
			for (int i = 0; i < this.options.Count; i++) {
				int remapped = RemapChoice(i);
				if (remapped == selection) {
					if (!hasSelection) {
						Choice = i;
						hasSelection = true;
						didAnything = true;
					}
				} else if (generalSelections.Contains(remapped)) {
					GeneralChoices.Add(i);
					didAnything = true;
				}
			}

			if (didAnything)
				OnChanged();
		}

		public void AutomaticallyUpdateButtonLayout(int newButtonPadding = -1) {
			int size = options.Count > 15 ? 21 : 32;
			int maxPerRow = options.Count > 15 ? 22 : 15;

			UpdateButtonLayout(size, newButtonPadding, maxPerRow);
		}

		public override void ClickChoice(int choice, int remappedChoiceType) {
			base.ClickChoice(choice, remappedChoiceType);

			options[choice].OnSelected(this, choice);
		}

		public override int RemapChoice(int choice) => choice < 0 || choice >= options.Count ? -1 : options[choice].Type;
	}
}
