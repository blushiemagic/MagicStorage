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

		private readonly List<ChoiceElement> choices = new();

		private int buttonSize, buttonPadding, maxButtonsPerRow;

		private readonly bool forceGearIconToNotBeCreated;

		public bool HasGearIcon { get; private set; }

		public virtual int SelectionType => Choice;

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

			int rows = (choices.Count - 1) / maxButtonsPerRow + 1;

			int width = (buttonSize + buttonPadding) * Math.Min(maxButtonsPerRow, choices.Count) - buttonPadding;
			int height = (buttonSize + buttonPadding) * rows - buttonPadding;

			Width.Set(width, 0f);
			MinWidth.Set(width, 0f);

			Height.Set(height, 0f);
			MinHeight.Set(height, 0f);

			//Adjust the positions of the buttons
			int left = 0;
			int top = 0;

			for (int i = 0; i < choices.Count; i++) {
				var choice = choices[i];
				choice.buttonSize = buttonSize;

				choice.Left.Set(left, 0f);
				choice.Top.Set(top, 0f);

				CheckAdjustment(i, ref left, ref top);

				choice.Recalculate();
			}
		}

		void CheckAdjustment(int index, ref int left, ref int top) {
			left += buttonSize + buttonPadding;

			if (index > 0 && (index + 1) % maxButtonsPerRow == 0) {
				left = 0;
				top += buttonSize + buttonPadding;
			}
		}

		public void AssignButtons(Asset<Texture2D>[] textures, ModTranslation[] texts) {
			AssignButtons(textures, texts.Select(t => t.GetTranslation(Language.ActiveCulture)).ToArray());
		}

		public void AssignButtons(Asset<Texture2D>[] textures, LocalizedText[] texts) {
			AssignButtons(textures, texts.Select(t => t.Value).ToArray());
		}

		public void AssignButtons(Asset<Texture2D>[] textures, string[] texts) {
			if (textures.Length != texts.Length || textures.Length == 0)
				throw new ArgumentException("Array Lengths must match and be non-zero");

			bool gearIconIsAvailable = HasGearIcon = !forceGearIconToNotBeCreated && MagicStorageConfig.ButtonUIMode == ButtonConfigurationMode.LegacyWithGear;

			int buttonCount = textures.Length + (gearIconIsAvailable ? 1 : 0);

			int rows = (textures.Length - 1) / maxButtonsPerRow + 1;

			int width = (buttonSize + buttonPadding) * Math.Min(maxButtonsPerRow, buttonCount) - buttonPadding;
			int height = (buttonSize + buttonPadding) * rows - buttonPadding;

			Width.Set(width, 0f);
			MinWidth.Set(width, 0f);

			Height.Set(height, 0f);
			MinHeight.Set(height, 0f);

			foreach (var choice in choices)
				choice.Remove();

			choices.Clear();

			int left = 0;
			int top = 0;

			int i;
			for (i = 0; i < textures.Length; i++) {
				var asset = textures[i];
				var text = texts[i];

				ChoiceElement choice = new(i, asset, text, buttonSize);
				choice.Left.Set(left, 0f);
				choice.Top.Set(top, 0f);

				CheckAdjustment(i, ref left, ref top);

				choices.Add(choice);
				Append(choice);
			}

			if (gearIconIsAvailable) {
				GearIconElement icon = new(buttonSize);
				icon.Left.Set(left, 0f);
				icon.Top.Set(top, 0f);

				choices.Add(icon);
				Append(icon);
			}

			//Always reset the choice to the first option
			Choice = 0;
		}

		protected virtual void OnChanged() {
			_onChanged();
		}

		private class ChoiceElement : UIElement {
			private static Asset<Texture2D> BackTexture => MagicStorageMod.Instance.Assets.Request<Texture2D>("Assets/SortButtonBackground", AssetRequestMode.ImmediateLoad);
			private static Asset<Texture2D> BackTextureActive => MagicStorageMod.Instance.Assets.Request<Texture2D>("Assets/SortButtonBackgroundActive", AssetRequestMode.ImmediateLoad);

			public readonly Asset<Texture2D> texture;
			public readonly string text;

			public readonly int option;
			public int buttonSize;

			//Hack for GearIconElement
			protected bool canInvokeAction = true;

			public ChoiceElement(int option, Asset<Texture2D> texture, LocalizedText text, int buttonSize = 21) : this(option, texture, text.Value, buttonSize) { }

			public ChoiceElement(int option, Asset<Texture2D> texture, ModTranslation translation, int buttonSize = 21) : this(option, texture, translation.GetTranslation(Language.ActiveCulture), buttonSize) { }

			public ChoiceElement(int option, Asset<Texture2D> texture, string text, int buttonSize = 21) {
				this.option = option;
				this.texture = texture;
				this.text = text;
				this.buttonSize = buttonSize;

				Width.Set(buttonSize, 0);
				Height.Set(buttonSize, 0);
			}

			public override void MouseOver(UIMouseEvent evt) {
				base.MouseOver(evt);

				MagicUI.mouseText = text;
			}

			public override void MouseOut(UIMouseEvent evt) {
				base.MouseOut(evt);

				MagicUI.mouseText = "";
			}

			public override void Click(UIMouseEvent evt) {
				base.Click(evt);

				if (canInvokeAction && Parent is NewUIButtonChoice buttons) {
					int old = buttons.Choice;
					buttons.Choice = option;

					if (old != buttons.Choice) {
						SoundEngine.PlaySound(SoundID.MenuTick);
						buttons.OnChanged();
					}
				}
			}

			protected override void DrawSelf(SpriteBatch spriteBatch) {
				if (Parent is not NewUIButtonChoice buttons)
					return;

				CalculatedStyle dim = GetDimensions();
				Asset<Texture2D> background = option == buttons.Choice ? BackTextureActive : BackTexture;
				Vector2 drawPos = new(dim.X, dim.Y);
				Color color = IsMouseHovering ? Color.Silver : Color.White;
				Main.spriteBatch.Draw(background.Value, new Rectangle((int) drawPos.X, (int) drawPos.Y, buttonSize, buttonSize), color);
				Main.spriteBatch.Draw(texture.Value, new Rectangle((int) drawPos.X + 1, (int) drawPos.Y + 1, buttonSize - 1, buttonSize - 1), Color.White);
			}
		}

		private class GearIconElement : ChoiceElement {
			public GearIconElement(int buttonSize = 21) : base(-1, MagicStorageMod.Instance.Assets.Request<Texture2D>("Assets/Config", AssetRequestMode.ImmediateLoad), Language.GetText("Mods.MagicStorage.ButtonConfigGear"), buttonSize) {
				canInvokeAction = false;
			}

			public override void Click(UIMouseEvent evt) {
				base.Click(evt);

				if (Parent is NewUIButtonChoice buttons)
					buttons._onGearChoiceSelected?.Invoke();
			}
		}
	}
}
