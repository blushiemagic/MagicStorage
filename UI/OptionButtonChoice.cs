using MagicStorage.CrossMod;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage.UI {
	public class SortingOptionButtonChoice : NewUIButtonChoice {
		private List<SortingOption> options = new();

		public IReadOnlyList<SortingOption> Options => options;

		public override int SelectionType => Choice < 0 || Choice >= options.Count ? -1 : options[Choice].Type;

		public SortingOptionButtonChoice(Action onChanged, int buttonSize, int maxButtonsPerRow, int buttonPadding = 1, Action onGearChoiceSelected = null, bool forceGearIconToNotBeCreated = false) : base(onChanged, buttonSize, maxButtonsPerRow, buttonPadding, onGearChoiceSelected, forceGearIconToNotBeCreated) {
		}

		public void AssignOptions(IEnumerable<SortingOption> options) {
			this.options = options.ToList();

			GenerateAssetsAndText(options, out var textures, out var translations);

			AssignButtons(textures, translations);
		}

		private static void GenerateAssetsAndText(IEnumerable<SortingOption> collection, out Asset<Texture2D>[] textures, out ModTranslation[] translations) {
			List<SortingOption> list = collection.ToList();

			textures = list.Select(o => o.TextureAsset).ToArray();
			translations = list.Select(o => o.Tooltip).ToArray();
		}

		public void AutomaticallyUpdateButtonLayout(int newButtonPadding = -1) {
			int size = options.Count > 15 ? 21 : 32;
			int maxPerRow = options.Count > 15 ? 22 : 15;

			UpdateButtonLayout(size, newButtonPadding, maxPerRow);
		}
	}

	public class FilteringOptionButtonChoice : NewUIButtonChoice {
		private List<FilteringOption> options = new();

		public IReadOnlyList<FilteringOption> Options => options;

		public override int SelectionType => Choice < 0 || Choice >= options.Count ? -1 : options[Choice].Type;

		public FilteringOptionButtonChoice(Action onChanged, int buttonSize, int maxButtonsPerRow, int buttonPadding = 1, Action onGearChoiceSelected = null, bool forceGearIconToNotBeCreated = false) : base(onChanged, buttonSize, maxButtonsPerRow, buttonPadding, onGearChoiceSelected, forceGearIconToNotBeCreated) {
		}

		public void AssignOptions(IEnumerable<FilteringOption> options) {
			this.options = options.ToList();

			GenerateAssetsAndText(options, out var textures, out var translations);

			AssignButtons(textures, translations);
		}

		private static void GenerateAssetsAndText(IEnumerable<FilteringOption> collection, out Asset<Texture2D>[] textures, out ModTranslation[] translations) {
			List<FilteringOption> list = collection.ToList();

			textures = list.Select(o => o.TextureAsset).ToArray();
			translations = list.Select(o => o.Tooltip).ToArray();
		}

		public void AutomaticallyUpdateButtonLayout(int newButtonPadding = -1) {
			int size = options.Count > 15 ? 21 : 32;
			int maxPerRow = options.Count > 15 ? 22 : 15;

			UpdateButtonLayout(size, newButtonPadding, maxPerRow);
		}
	}
}
