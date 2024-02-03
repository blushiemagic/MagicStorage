using MagicStorage.Common.Systems;
using MagicStorage.UI.States;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace MagicStorage.UI {
	public class NewUIButtonChoice : UIElement {
		private readonly Action _onChanged, _onGearChoiceSelected;

		public int Choice { get; set; }

		public HashSet<int> GeneralChoices { get; private set; } = new();

		private readonly List<ChoiceElement> choices = new();
		private readonly List<ChoiceElement> generalChoices = new();

		private int buttonSize, buttonPadding, maxButtonsPerRow;

		private readonly bool forceGearIconToNotBeCreated;

		public bool HasGearIcon { get; private set; }

		public int SelectionType => RemapChoice(Choice);

		public HashSet<int> GeneralSelections => GeneralChoices.Select(RemapChoice).ToHashSet();

		public event Action<int, int> OnChoiceClicked;

		public NewUIButtonChoice(Action onChanged, int buttonSize, int maxButtonsPerRow, int buttonPadding = 1, Action onGearChoiceSelected = null, bool forceGearIconToNotBeCreated = false) {
			ArgumentNullException.ThrowIfNull(onChanged);

			_onChanged = onChanged;
			_onGearChoiceSelected = onGearChoiceSelected;
			this.buttonSize = buttonSize;
			this.buttonPadding = buttonPadding;
			this.maxButtonsPerRow = maxButtonsPerRow;
			this.forceGearIconToNotBeCreated = forceGearIconToNotBeCreated;

			Height.Set(buttonSize, 0f);
			MinHeight.Set(buttonSize, 0f);

			SetPadding(0);
		}

		public void UpdateButtonLayout(int newButtonSize = -1, int newButtonPadding = -1, int newMaxButtonsPerRow = -1) {
			if (newButtonSize > 0)
				buttonSize = newButtonSize;

			if (newButtonPadding > 0)
				buttonPadding = newButtonPadding;

			if (newMaxButtonsPerRow > 0)
				maxButtonsPerRow = newMaxButtonsPerRow;

			AlignButtonsAndRecalculateDimensions();
		}

		public void AssignButtons(IEnumerable<ButtonChoiceInfo> info) {
			bool gearIconIsAvailable = HasGearIcon = !forceGearIconToNotBeCreated && MagicStorageConfig.ButtonUIMode == ButtonConfigurationMode.LegacyWithGear;

			foreach (var choice in choices)
				choice.Remove();
			foreach (var choice in generalChoices)
				choice.Remove();

			choices.Clear();
			generalChoices.Clear();

			// Create the choices
			int numChoices = 0;
			foreach (var choiceInfo in info) {
				ChoiceElement choice = new(numChoices, choiceInfo.asset, choiceInfo.text, choiceInfo.generalChoice, buttonSize);
				
				if (choiceInfo.generalChoice)
					generalChoices.Add(choice);
				else
					choices.Add(choice);

				Append(choice);

				numChoices++;
			}

			if (gearIconIsAvailable) {
				GearIconElement icon = new(buttonSize);
				choices.Add(icon);
				Append(icon);
			}

			AlignButtonsAndRecalculateDimensions();

			//Always reset the choice to the first option
			Choice = 0;
			GeneralChoices.Clear();
			OnChanged();
		}

		private void CheckAdjustment(int index, ref int left, ref int top) {
			left += buttonSize + buttonPadding;

			if ((index + 1) % maxButtonsPerRow == 0) {
				left = 0;
				top += buttonSize + buttonPadding;
			}
		}

		private void AlignButtonsAndRecalculateDimensions() {
			// Align the choices
			int left = 0;
			int top = 0;

			for (int i = 0; i < choices.Count; i++) {
				var choice = choices[i];
				choice.Left.Set(left, 0f);
				choice.Top.Set(top, 0f);

				CheckAdjustment(i, ref left, ref top);
			}

			// Align the general choices
			if ((choices.Count + 1) % maxButtonsPerRow != 0) {
				left = 0;
				top += buttonSize + buttonPadding;
			}

			for (int i = 0; i < generalChoices.Count; i++) {
				var choice = generalChoices[i];
				choice.Left.Set(left, 0f);
				choice.Top.Set(top, 0f);

				CheckAdjustment(i, ref left, ref top);
			}

			// Set the dimensions for this container
			int buttonCount = choices.Count;
			int generalButtonCount = generalChoices.Count;

			int rows = (buttonCount - 1) / maxButtonsPerRow + 1;
			int generalRows = generalButtonCount == 0 ? 0 : (generalButtonCount - 1) / maxButtonsPerRow + 1;

			int width = (buttonSize + buttonPadding) * Math.Min(maxButtonsPerRow, Math.Max(buttonCount, generalButtonCount)) - buttonPadding;
			int height = (buttonSize + buttonPadding) * (rows + generalRows) - buttonPadding;

			Width.Set(width, 0f);
			MinWidth.Set(width, 0f);

			Height.Set(height, 0f);
			MinHeight.Set(height, 0f);
		}

		public virtual void OnChanged() {
			_onChanged();
		}

		public virtual void ClickChoice(int choice, int remappedChoiceType) {
			OnChoiceClicked?.Invoke(choice, remappedChoiceType);
		}

		public virtual int RemapChoice(int choice) => choice;

		public void DisableGeneralChoicesBasedOnRemapping(int remappedChoiceType) {
			foreach (var choice in generalChoices) {
				if (RemapChoice(choice.option) == remappedChoiceType)
					GeneralChoices.Remove(choice.option);
			}
		}

		private class ChoiceElement : UIElement {
			private static Asset<Texture2D> BackTexture => MagicStorageMod.Instance.Assets.Request<Texture2D>("Assets/SortButtonBackground", AssetRequestMode.ImmediateLoad);
			private static Asset<Texture2D> BackTextureActive => MagicStorageMod.Instance.Assets.Request<Texture2D>("Assets/SortButtonBackgroundActive", AssetRequestMode.ImmediateLoad);
			private static Asset<Texture2D> GeneralBackTextureActive => MagicStorageMod.Instance.Assets.Request<Texture2D>("Assets/SortButtonBackgroundGeneralActive", AssetRequestMode.ImmediateLoad);

			public readonly Asset<Texture2D> texture;
			public readonly LocalizedText text;

			public readonly int option;
			public readonly bool generalChoice;
			public int buttonSize;

			//Hack for GearIconElement
			protected bool canInvokeAction = true;

			public ChoiceElement(int option, Asset<Texture2D> texture, LocalizedText text, bool generalChoice, int buttonSize = 21) {
				this.option = option;
				this.texture = texture;
				this.text = text;
				this.generalChoice = generalChoice;
				this.buttonSize = buttonSize;

				Width.Set(buttonSize, 0);
				Height.Set(buttonSize, 0);
			}

			protected ChoiceElement(int option, string assetPath, string localizationKey, bool generalChoice, AssetRequestMode mode = AssetRequestMode.ImmediateLoad, int buttonSize = 21) {
				this.option = option;
				texture = ModContent.Request<Texture2D>(assetPath, mode);
				text = Language.GetText(localizationKey);
				this.generalChoice = generalChoice;
				this.buttonSize = buttonSize;

				Width.Set(buttonSize, 0);
				Height.Set(buttonSize, 0);
			}

			public override void MouseOver(UIMouseEvent evt) {
				base.MouseOver(evt);

				MagicUI.mouseText = text.Value;
			}

			public override void MouseOut(UIMouseEvent evt) {
				base.MouseOut(evt);

				MagicUI.mouseText = "";
			}

			public override void LeftClick(UIMouseEvent evt) {
				base.LeftClick(evt);

				if (canInvokeAction) {
					var buttons = (NewUIButtonChoice)Parent;
					bool changed;

					if (!generalChoice) {
						int old = buttons.Choice;
						buttons.Choice = option;

						changed = old != buttons.Choice;
					} else {
						if (buttons.GeneralChoices.Contains(option))
							buttons.GeneralChoices.Remove(option);
						else
							buttons.GeneralChoices.Add(option);

						changed = true;
					}

					if (changed) {
						SoundEngine.PlaySound(SoundID.MenuTick);
						buttons.OnChanged();
					}

					buttons.ClickChoice(option, buttons.RemapChoice(option));
				}
			}

			protected override void DrawSelf(SpriteBatch spriteBatch) {
				var buttons = (NewUIButtonChoice)Parent;

				CalculatedStyle dim = GetDimensions();

				Asset<Texture2D> background;
				if (generalChoice)
					background = buttons.GeneralChoices.Contains(option) ? GeneralBackTextureActive : BackTexture;
				else
					background = option == buttons.Choice ? BackTextureActive : BackTexture;
				
				Vector2 drawPos = new(dim.X, dim.Y);
				Color color = IsMouseHovering ? Color.Silver : Color.White;

				Main.spriteBatch.Draw(background.Value, new Rectangle((int) drawPos.X, (int) drawPos.Y, buttonSize, buttonSize), color);
				Main.spriteBatch.Draw(texture.Value, new Rectangle((int) drawPos.X + 1, (int) drawPos.Y + 1, buttonSize - 1, buttonSize - 1), Color.White);
			}
		}

		private class GearIconElement : ChoiceElement {
			public GearIconElement(int buttonSize = 21) : base(-1, "MagicStorage/Assets/Config", "Mods.MagicStorage.ButtonConfigGear", false, buttonSize: buttonSize) {
				canInvokeAction = false;
			}

			public override void LeftClick(UIMouseEvent evt) {
				base.LeftClick(evt);

				((NewUIButtonChoice)Parent)._onGearChoiceSelected?.Invoke();
			}
		}
	}

	public class ButtonChoiceInfo {
		internal readonly Asset<Texture2D> asset;
		internal readonly LocalizedText text;
		internal readonly bool generalChoice;

		public ButtonChoiceInfo(Asset<Texture2D> asset, LocalizedText text, bool generalChoice) {
			this.asset = asset;
			this.text = text;
			this.generalChoice = generalChoice;
		}

		public ButtonChoiceInfo(string assetPath, string localizationKey, bool generalChoice, AssetRequestMode mode = AssetRequestMode.ImmediateLoad) {
			asset = ModContent.Request<Texture2D>(assetPath, mode);
			text = Language.GetText(localizationKey);
			this.generalChoice = generalChoice;
		}
	}
}
