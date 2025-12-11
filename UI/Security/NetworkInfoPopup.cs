using MagicStorage.Common.Players;
using MagicStorage.Common.Systems;
using MagicStorage.UI.Input;
using Microsoft.Xna.Framework;
using SerousCommonLib.UI;
using System;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;

namespace MagicStorage.UI.Security {
	internal class NetworkInfoPopup : UIPanel {
		private readonly SecurityNetworkNameTextInputBar _inputNetworkName;
		private readonly UIToggleLabel _toggleRestricted;
		private readonly SecurityNetworkPasswordTextInputBar _inputNetworkPassword;
		private readonly PasswordHiddenIndicator _passwordHidden;
		private readonly UITextPanel<LocalizedText> _confirm;
		private readonly UITextPanel<LocalizedText> _delete;
		private readonly UITextPanel<LocalizedText> _cancel;

		private bool _informationDirty = true;
		
		private SecuritySystem.NetworkView _view;
		public required SecuritySystem.NetworkView View {
			get => _view;
			set {
				_view = value;
				_informationDirty = true;
			}
		}

		private bool _updating;
		public required bool IsUpdating {
			get => _updating;
			set {
				if (value != _updating)
					_informationDirty = true;

				_updating = value;
			}
		}

		private readonly UIElement _buttonLayout;
	//	private readonly BaseOrderedLayout _mainLayout;
		private readonly UIElement _mainLayout;
		private const int MAIN_LAYOUT_SPACING = 4;

		public event Action<NetworkInfoPopup> OnConfirm;
		public event Action<NetworkInfoPopup> OnDelete;
		public event Action<NetworkInfoPopup> OnCancel;

		private string _originalPasswordText;  // Used to restore the password immediately before deletion, since that requires the input password to match the actual password

		public string NetworkName => _inputNetworkName.State.InputText;

		public bool Restricted => _toggleRestricted.IsOn;

		public string Password => _inputNetworkPassword.State.InputText;

		public NetworkInfoPopup() {
			BackgroundColor = Utility.PanelColorWithoutTransparency;
			SetPadding(pixels: 8);
			this.ClearMargins();

			/*
			this.GetLayoutManager().Attributes = new LayoutAttributes()
				.WithGravity(LayoutGravityType.CenterBoth)
				.WithSize(
					width: new LayoutUnit(-80, 1f),
					height: LayoutUnit.DynamicSize);
			*/
			HAlign = 0.5f;
			VAlign = 0.5f;
			Width.Set(-80, 1f);

			_passwordHidden = new PasswordHiddenIndicator();
			_inputNetworkName = new SecurityNetworkNameTextInputBar();
			_toggleRestricted = new UIToggleLabel(Language.GetText("Mods.MagicStorage.Security.UI.NetworkAccessQuestion"));
			_inputNetworkPassword = new SecurityNetworkPasswordTextInputBar();
			_confirm = new UITextPanel<LocalizedText>(Language.GetText("UI.Save"));
			_confirm.SetBasicHoverColorChangeEvents();
			_delete = new UITextPanel<LocalizedText>(Language.GetText("UI.Delete"));
			_delete.SetBasicHoverColorChangeEvents();
			_cancel = new UITextPanel<LocalizedText>(Language.GetText("UI.Cancel"));
			_cancel.SetBasicHoverColorChangeEvents();
			_buttonLayout = new UIElement();
			_mainLayout = new UIElement();
			
			_passwordHidden.OnVisibilityChanged += self => _inputNetworkPassword.State.HideContents = self.Hidden;
			_passwordHidden.ClearPaddingAndMargins();
			// Layout from the new API is further down in the file
			_passwordHidden.SetRightAlignment(4);
			_passwordHidden.VAlign = 0.5f;

			/*
			_inputNetworkName.GetLayoutManager().Attributes = new LayoutAttributes()
				.WithSize(
					width: LayoutUnit.Fill,
					height: new LayoutUnit(pixels: 20))
				.AddConstraint(LayoutConstraintType.LeftToLeftOf, null, LayoutUnit.Zero)
				.AddConstraint(LayoutConstraintType.RightToLeftOf, _passwordHidden, LayoutUnit.Zero);
			*/
			_inputNetworkName.Width.Set(-_passwordHidden.Width.Pixels - 8, 1f);
			_inputNetworkName.Height.Set(20, 0f);

			/*
			_toggleRestricted.GetLayoutManager().Attributes = new LayoutAttributes()
				.WithSize(
					width: new LayoutUnit(pixels: 120),
					height: new LayoutUnit(pixels: 13))
				.AddConstraint(LayoutConstraintType.LeftToLeftOf, null, new LayoutUnit(pixels: 8));
			*/
			_toggleRestricted.OnLeftClick += (evt, e) => UpdatePasswordAccessibility(((UIToggleLabel)e).IsOn);
			_toggleRestricted.Width.Set(120, 0f);
			_toggleRestricted.Height.Set(13, 0f);
			_toggleRestricted.Left.Set(8, 0f);

			/*
			_inputNetworkPassword.GetLayoutManager().Attributes = new LayoutAttributes()
				.WithSize(
					width: LayoutUnit.Fill,
					height: new LayoutUnit(pixels: 20))
				.AddConstraint(LayoutConstraintType.LeftToLeftOf, null, LayoutUnit.Zero);
			*/
			UpdatePasswordAccessibility(_toggleRestricted.IsOn);
			_inputNetworkPassword.State.HideContents = _passwordHidden.Hidden;
			_inputNetworkPassword.Width.Set(0, 1f);
			_inputNetworkPassword.Height.Set(20, 0f);

			/*
			_passwordHidden.GetLayoutManager().Attributes = new LayoutAttributes()
				.InheritSizeFrom(_passwordHidden)
				.AddConstraint(LayoutConstraintType.RightToRightOf, null, new LayoutUnit(pixels: 4))
				.WithGravity(LayoutGravityType.CenterVertical);
			*/

			/*
			var passwordLayout = new HorizontalLayout() {
				Spacing = new LayoutUnit(pixels: 4)
			};
			passwordLayout.ClearPaddingAndMargins();
			passwordLayout.GetLayoutManager().Attributes = new LayoutAttributes()
				.WithSize(
					width: LayoutUnit.Fill,
					height: LayoutUnit.DynamicSize
				);

			passwordLayout.AddElement(_inputNetworkPassword);
			passwordLayout.AddElement(_passwordHidden);
			*/
			var passwordLayout = new UIElement();
			passwordLayout.Width.Set(0, 1f);
			passwordLayout.Height.Set(_inputNetworkPassword.MinHeight.Pixels, 0f);

			passwordLayout.Append(_inputNetworkPassword);
			passwordLayout.Append(_passwordHidden);

			_confirm.OnLeftClick += (evt, element) => {
				// Ensure that any changes to the text are submitted
				_inputNetworkName.State.Unfocus();

				if (_inputNetworkPassword.State.IsActive)
					_inputNetworkPassword.State.Unfocus();
				else {
					_inputNetworkPassword.State.Activate();
					_inputNetworkPassword.State.Unfocus();
					_inputNetworkPassword.State.Deactivate();
				}

				OnConfirm?.Invoke(this);
			};
			/*
			_confirm.GetLayoutManager().Attributes = new LayoutAttributes()
				.InheritSizeFrom(_confirm)
				.WithGravity(LayoutGravityType.CenterVertical)
				.AddConstraint(LayoutConstraintType.LeftToLeftOf, null, new LayoutUnit(pixels: borderOffset));
			*/
			_confirm.VAlign = 0.5f;

			_delete.OnLeftClick += (evt, element) => {
				// Restore the actual password
				if (_inputNetworkPassword.State.IsActive)
					_inputNetworkPassword.State.Set(_originalPasswordText);
				else {
					_inputNetworkPassword.State.Activate();
					_inputNetworkPassword.State.Set(_originalPasswordText);
					_inputNetworkPassword.State.Deactivate();
				}

				OnDelete?.Invoke(this);
			};
			/*
			_delete.GetLayoutManager().Attributes = new LayoutAttributes()
				.InheritSizeFrom(_delete)
				.WithGravity(LayoutGravityType.CenterBoth);
			*/
			_delete.HAlign = 0.5f;
			_delete.VAlign = 0.5f;

			_cancel.OnLeftClick += (evt, element) => OnCancel?.Invoke(this);
			/*
			_cancel.GetLayoutManager().Attributes = new LayoutAttributes()
				.InheritSizeFrom(_cancel)
				.WithGravity(LayoutGravityType.CenterVertical)
				.AddConstraint(LayoutConstraintType.RightToRightOf, null, new LayoutUnit(pixels: borderOffset));
			*/
			_cancel.VAlign = 0.5f;

			/*
			_buttonLayout.GetLayoutManager().Attributes = new LayoutAttributes()
				.WithSize(
					width: LayoutUnit.Fill,
					height: LayoutUnit.DynamicSize
				);
			*/
			_buttonLayout.Width.Set(0, 1f);
			_buttonLayout.Height.Set(_confirm.MinHeight.Pixels, 0f);

			/*
			_mainLayout = new VerticalLayout() {
				Spacing = new LayoutUnit(pixels: 4)
			};
			_mainLayout.ClearPaddingAndMargins();
			_mainLayout.GetLayoutManager().Attributes = new LayoutAttributes()
				.WithSize(
					width: LayoutUnit.Fill,
					height: LayoutUnit.DynamicSize)
				.AddConstraint(LayoutConstraintType.LeftToLeftOf, null, LayoutUnit.Zero)
				.AddConstraint(LayoutConstraintType.TopToTopOf, null, LayoutUnit.Zero);

			_mainLayout.AddElement(_inputNetworkName);
			_mainLayout.AddElement(_toggleRestricted);
			_mainLayout.AddElement(passwordLayout);
			_mainLayout.AddElement(_buttonLayout);
			*/

			_mainLayout.ClearPaddingAndMargins();
			_mainLayout.Width.Set(0, 1f);
			_mainLayout.Height.Set(0, 1f);

			_mainLayout.Append(_inputNetworkName);

			_toggleRestricted.Top.Set(_inputNetworkName.MinHeight.Pixels + MAIN_LAYOUT_SPACING, 0f);
			_mainLayout.Append(_toggleRestricted);
			
			passwordLayout.Top.Set(_toggleRestricted.Top.Pixels + _toggleRestricted.Height.Pixels + MAIN_LAYOUT_SPACING, 0f);
			_mainLayout.Append(passwordLayout);
			
			_buttonLayout.Top.Set(passwordLayout.Top.Pixels + passwordLayout.Height.Pixels + MAIN_LAYOUT_SPACING, 0f);
			_mainLayout.Append(_buttonLayout);

			_mainLayout.Height.Set(_buttonLayout.Top.Pixels + _buttonLayout.Height.Pixels, 0f);

			_buttonLayout.Append(_confirm);

			if (IsUpdating)
				_buttonLayout.Append(_delete);

			_buttonLayout.Append(_cancel);

			Height.Set(_mainLayout.Height.Pixels + PaddingTop + PaddingBottom, 0f);

			Append(_mainLayout);

			_informationDirty = true;  // Force an update to the UI when it is first created
		}

		private void UpdatePasswordAccessibility(bool visible) {
			if (visible) {
				_inputNetworkPassword.BackgroundColorOverride = null;
				_inputNetworkPassword.TextColorOverride = null;

				_inputNetworkPassword.State.Activate();
			} else {
				_inputNetworkPassword.BackgroundColorOverride = Color.LightGray;
				_inputNetworkPassword.TextColorOverride = Color.DarkGray;

				_inputNetworkPassword.State.Unfocus();
				_inputNetworkPassword.State.Deactivate();
			}
		}

		public override void Update(GameTime gameTime) {
			if (_informationDirty) {
				UpdateInformation();
				_informationDirty = false;
			}

			base.Update(gameTime);
		}

		public override void OnActivate() {
			// Ensure that the data gets populated
			_informationDirty = true;

			base.OnActivate();
		}

		private void UpdateInformation() {
			if (View.Valid) {
				// The password prompt has to be active for it to be modified
				_inputNetworkPassword.State.Activate();

				_inputNetworkName.State.Set(View.name);
				_toggleRestricted.SetState(View.restricted);

				if (Main.LocalPlayer.GetModPlayer<SecurityPlayer>().TryGetPassword(View.id, out string password) && password is not null) {
					_inputNetworkPassword.State.Set(password);
					_originalPasswordText = password;
				} else {
					_inputNetworkPassword.State.Clear();
					_originalPasswordText = string.Empty;
				}

				UpdatePasswordAccessibility(View.restricted);
			} else {
				// The password prompt has to be active for it to be modified
				_inputNetworkPassword.State.Activate();

				_inputNetworkName.State.Clear();
				_toggleRestricted.SetState(false);
				_inputNetworkPassword.State.Clear();
				_originalPasswordText = string.Empty;
				UpdatePasswordAccessibility(false);
			}

			int borderOffset;
			if (IsUpdating) {
				_confirm.SetText(Language.GetText("UI.Save"));
				_buttonLayout.Append(_delete);
				borderOffset = 80;
			} else {
				_confirm.SetText(Language.GetText("UI.Create"));
				_delete.Remove();
				borderOffset = 16;
			}

			_confirm.Left.Set(borderOffset, 0f);
			_cancel.SetRightAlignment(borderOffset);

			// Just recalculate everything...
			_mainLayout.Recalculate();
		}

		public void ResetEvents() {
			OnConfirm = null;
			OnDelete = null;
			OnCancel = null;
		}

		public void ClearInputs() {
			// The password prompt has to be active for it to be modified
			_inputNetworkPassword.State.Activate();

			_inputNetworkName.State.Clear();
			_toggleRestricted.SetState(false);
			_inputNetworkPassword.State.Clear();
			_originalPasswordText = string.Empty;

			UpdatePasswordAccessibility(false);

			_informationDirty = true;  // Ensure that the UI updates
		}
	}
}
