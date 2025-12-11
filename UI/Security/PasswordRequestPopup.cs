using MagicStorage.Common.Systems;
using MagicStorage.UI.Input;
using SerousCommonLib.UI;
using System;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;

namespace MagicStorage.UI.Security {
	internal class PasswordRequestPopup : UIPanel {
		private readonly UIText _question;
		private readonly SecurityNetworkAccessPasswordTextInputBar _inputPassword;
		private readonly PasswordHiddenIndicator _passwordHidden;
		private readonly UITextPanel<LocalizedText> _cancel;

		//	private readonly BaseOrderedLayout _mainLayout;
		private readonly UIElement _mainLayout;

		public event Action<string> OnPasswordEntered;
		public event Action<PasswordRequestPopup> OnCancel;

		public PasswordRequestPopup(SecuritySystem.NetworkView view) {
			BackgroundColor = Utility.PanelColorWithoutTransparency;
			SetPadding(pixels: 8);
			this.ClearMargins();

			/*
			this.GetLayoutManager().Attributes = new LayoutAttributes()
				.WithGravity(LayoutGravityType.CenterBoth)
				.WithSize(
					width: new LayoutUnit(-80, 1f),
					height: LayoutUnit.DynamicSize
				);
			*/
			HAlign = 0.5f;
			VAlign = 0.5f;
			Width.Set(-80, 1f);

			_passwordHidden = new PasswordHiddenIndicator();
			_passwordHidden.OnVisibilityChanged += self => _inputPassword.State.HideContents = self.Hidden;

			_question = new UIText(string.Empty) {
				TextOriginX = 0f
			};

			_inputPassword = new SecurityNetworkAccessPasswordTextInputBar();
			_inputPassword.State.HideContents = _passwordHidden.Hidden;
			_inputPassword.OnInputEnterEvent += self => OnPasswordEntered?.Invoke(self.State.InputText);

			_cancel = new UITextPanel<LocalizedText>(Language.GetText("UI.Cancel"));
			_cancel.SetBasicHoverColorChangeEvents();
			_cancel.OnLeftClick += (evt, element) => OnCancel?.Invoke(this);

			/*
			_mainLayout = new VerticalLayout() {
				Spacing = new LayoutUnit(pixels: 4)
			};
			*/
			_mainLayout = new UIElement();

			UpdateView(view);
			
			_passwordHidden.ClearPaddingAndMargins();

			/*
			_question.GetLayoutManager().Attributes = new LayoutAttributes()
				.WithSize(
					width: LayoutUnit.Fill,
					height: LayoutUnit.DynamicSize)
				.AddConstraint(LayoutConstraintType.LeftToLeftOf, null, LayoutUnit.Zero)
				.AddConstraint(LayoutConstraintType.RightToLeftOf, _passwordHidden, LayoutUnit.Zero);
			*/
			
			/*
			_inputPassword.GetLayoutManager().Attributes = new LayoutAttributes()
				.WithSize(
					width: LayoutUnit.Fill,
					height: new LayoutUnit(pixels: 20))
				.AddConstraint(LayoutConstraintType.LeftToLeftOf, null, LayoutUnit.Zero);
			*/
			_inputPassword.Width.Set(0, 1f);

			/*
			_passwordHidden.GetLayoutManager().Attributes = new LayoutAttributes()
				.InheritSizeFrom(_passwordHidden)
				.AddConstraint(LayoutConstraintType.RightToRightOf, null, new LayoutUnit(pixels: 4))
				.WithGravity(LayoutGravityType.CenterVertical);
			*/
			_passwordHidden.SetRightAlignment(4);
			_passwordHidden.VAlign = 0.5f;

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

			passwordLayout.AddElement(_inputPassword);
			passwordLayout.AddElement(_passwordHidden);
			*/
			var passwordLayout = new UIElement();
			passwordLayout.ClearPaddingAndMargins();
			passwordLayout.Width.Set(-_cancel.MinWidth.Pixels - 8, 1f);

			_inputPassword.Width.Set(-_passwordHidden.Width.Pixels - 8, 1f);
			passwordLayout.Append(_inputPassword);

			passwordLayout.Append(_passwordHidden);

			passwordLayout.Height.Set(_inputPassword.MinHeight.Pixels, 0f);

			_question.Left.Set(4, 0f);
			_question.Width = passwordLayout.Width;

			_mainLayout.ClearPaddingAndMargins();
			/*
			_mainLayout.GetLayoutManager().Attributes = new LayoutAttributes()
				.WithSize(
					width: LayoutUnit.Fill,
					height: LayoutUnit.DynamicSize
				);

			_mainLayout.AddElement(_question);
			_mainLayout.AddElement(passwordLayout);
			*/
			_mainLayout.Width.Set(0, 1f);

			_mainLayout.Append(_question);

			passwordLayout.Top.Set(_question.MinHeight.Pixels + 12, 0f);
			_mainLayout.Append(passwordLayout);

			_mainLayout.Height.Set(passwordLayout.Top.Pixels + 4 + passwordLayout.Height.Pixels, 0f);

			Append(_mainLayout);

			_cancel.SetRightAlignment(0);
			_cancel.Top.Set(_mainLayout.Height.Pixels + 4, 0f);

			Append(_cancel);

			Height.Set(_cancel.Top.Pixels + _cancel.MinHeight.Pixels + PaddingTop + PaddingBottom, 0f);
		}

		public void UpdateView(SecuritySystem.NetworkView view) => _question.SetText(Language.GetText("Mods.MagicStorage.Security.UI.RequestingPassword").Format(view.name));

		public void ResetEvents() => OnPasswordEntered = null;

		public void ClearInputs() {
			_inputPassword.State.Clear();
			_passwordHidden.SetHidden(true);
			_inputPassword.State.HideContents = true;
		}
	}
}
