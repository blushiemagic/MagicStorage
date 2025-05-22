using MagicStorage.Common.Systems;
using Microsoft.Xna.Framework;
using SerousCommonLib.UI;
using SerousCommonLib.UI.Layouts;
using System;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;

namespace MagicStorage.UI.Security {
	public class NetworkReport : UIPanel {
		private UIText _nameText;
		private UIText _creatorText;
		private UIText _idText;

		private UIPanel _restrictedPanel;
		private RestrictedIcon _restrictedIcon;
		private UIText _restrictedLabel;
		private NetworkConfigurationIcon _config;

		public SecuritySystem.NetworkView View { get; private set; }

		public Color? DefaultBorderColorOverride { get; set; }

		public event Action<NetworkReport> OnSetNetwork;
		public event Action<NetworkReport> OnConfigClick;

		public NetworkReport(SecuritySystem.NetworkView view) {
			/*
			this.GetLayoutManager().Attributes = new LayoutAttributes()
				.WithSize(
					width: LayoutUnit.Fill,
					height: LayoutUnit.DynamicSize,
					minHeight: new LayoutUnit(pixels: 32)
				);
			*/
			Width.Set(0, 1f);
			MinHeight.Set(32, 0f);

			BackgroundColor = Utility.PanelColorWithoutTransparency;
			
			_nameText = new UIText(string.Empty);
			_creatorText = new UIText(string.Empty);
			_idText = new UIText(string.Empty);

			_restrictedPanel = new UIPanel() {
				BackgroundColor = Utility.PanelColorWithoutTransparency
			};
			_restrictedPanel.ClearMargins();

			/*
			_restrictedPanel.GetLayoutManager().Attributes = new LayoutAttributes()
				.WithSize(
					width: LayoutUnit.DynamicSize,
					height: LayoutUnit.DynamicSize,
					minWidth: new LayoutUnit(pixels: 20),
					minHeight: new LayoutUnit(pixels: 20))
				.AddConstraint(LayoutConstraintType.TopToTopOf, null, new LayoutUnit(pixels: 4))
				.AddConstraint(LayoutConstraintType.LeftToLeftOf, null, new LayoutUnit(pixels: 4));
			*/
			_restrictedPanel.Left.Set(4, 0f);
			_restrictedPanel.Top.Set(4, 0f);
			_restrictedPanel.MinWidth.Set(20, 0f);
			_restrictedPanel.MinHeight.Set(20, 0f);

			_restrictedIcon = new RestrictedIcon(true);
			_restrictedLabel = new UIText(string.Empty);

			InitNetworkElements(view);

			/*
			var layout = new VerticalLayout() {
				Spacing = new LayoutUnit(pixels: 6)
			};
			layout.ClearPaddingAndMargins();

			layout.AddElement(_nameText);
			layout.AddElement(_creatorText);
			layout.AddElement(_idText);

			layout.GetLayoutManager().Attributes = new LayoutAttributes()
				.WithSize(
					width: LayoutUnit.Fill,
					height: LayoutUnit.DynamicSize,
					minHeight: new LayoutUnit(pixels: 32)
				);
			*/
			var layout = new UIElement();
			layout.ClearPaddingAndMargins();
			layout.Width.Set(0, 1f);
			layout.Height.Set(_nameText.MinHeight.Pixels + 12 + _creatorText.MinHeight.Pixels + 8 + _idText.MinHeight.Pixels, 0f);
			layout.MinHeight.Set(32, 0f);
			
			_nameText.Width.Set(0, 1f);
			layout.Append(_nameText);

			_creatorText.TextOriginX = 0f;
			_creatorText.Width.Set(0, 1f);
			_creatorText.Top.Set(_nameText.MinHeight.Pixels + 12, 0f);
			layout.Append(_creatorText);

			_idText.TextOriginX = 0f;
			_idText.Width.Set(0, 1f);
			_idText.Top.Set(_creatorText.Top.Pixels + _creatorText.MinHeight.Pixels + 8, 0f);
			layout.Append(_idText);

			// Button needs to be created here so that the layout can reference it
			_config = new NetworkConfigurationIcon();

			/*
			var restrictedLayout = new HorizontalLayout() {
				Spacing = new LayoutUnit(pixels: 4)
			};
			restrictedLayout.ClearPaddingAndMargins();

			restrictedLayout.AddElement(_restrictedLabel);
			restrictedLayout.AddElement(_restrictedIcon);

			restrictedLayout.GetLayoutManager().Attributes = new LayoutAttributes()
				.AddConstraint(LayoutConstraintType.RightToLeftOf, _config, new LayoutUnit(pixels: 4))
				.WithSize(
					width: LayoutUnit.DynamicSize,
					height: LayoutUnit.DynamicSize,
					minWidth: new LayoutUnit(pixels: 32),
					minHeight: new LayoutUnit(pixels: 32)
				);
			*/
			var restrictedLayout = new UIElement();
			restrictedLayout.ClearPaddingAndMargins();
			restrictedLayout.Width.Set(0, 1f);
			restrictedLayout.Height.Set(Math.Max(_restrictedIcon.Height.Pixels, _restrictedLabel.MinHeight.Pixels), 0f);
			restrictedLayout.MinWidth.Set(32, 0f);
			restrictedLayout.MinHeight.Set(32, 0f);

			_restrictedLabel.Width.Set(-_restrictedIcon.Width.Pixels - 8, 1f);
			restrictedLayout.Append(_restrictedLabel);

			_restrictedIcon.SetRightAlignment(0);
			restrictedLayout.Append(_restrictedIcon);

			_restrictedPanel.Append(restrictedLayout);

			_restrictedPanel.Top.Set(layout.Top.Pixels + layout.Height.Pixels + 8, 0f);
			_restrictedPanel.Width.Set(_restrictedLabel.MinWidth.Pixels + 8 + _restrictedIcon.Width.Pixels + _restrictedPanel.PaddingLeft + _restrictedPanel.PaddingRight, 0f);
			_restrictedPanel.Height.Set(restrictedLayout.Height.Pixels + _restrictedPanel.PaddingTop + _restrictedPanel.PaddingBottom, 0f);

			_config.ClearMargins();
			_config.OnLeftClick += (evt, element) => OnConfigClick?.Invoke(this);
			/*
			// NetworkConfigurationIcon ctor initializes the attributes
			_config.GetLayoutManager().Attributes
				.AddConstraint(LayoutConstraintType.RightToRightOf, null, new LayoutUnit(pixels: 4))
				.AddConstraint(LayoutConstraintType.BottomToBottomOf, null, new LayoutUnit(pixels: 4));
			*/
			_config.SetRightAlignment(4);
			_config.SetBottomAlignment(4);

			Append(layout);
			Append(_restrictedPanel);
			Append(_config);

			Height.Set(_restrictedPanel.Top.Pixels + _restrictedPanel.Height.Pixels + 4 /* + _config.Height.Pixels + 4 */ + PaddingTop + PaddingBottom, 0f);
		}

		public void UpdateView(SecuritySystem.NetworkView view) {
			InitNetworkElements(view);
			Recalculate();
		}

		private void InitNetworkElements(SecuritySystem.NetworkView view) {
			View = view;

			_nameText.SetText(View.name);
			_creatorText.SetText(Language.GetText("Mods.MagicStorage.Security.UI.NetworkOwner").Format(View.creator));
			_idText.SetText(Language.GetText("Mods.MagicStorage.Security.UI.NetworkID").Format(View.id));

			_restrictedIcon.Locked = View.restricted;

			_restrictedLabel.SetText(Language.GetText("Mods.MagicStorage.Security.UI." + (View.restricted ? "Private" : "Public")));
		}

		public override void OnActivate() {
			base.OnActivate();

			BorderColor = DefaultBorderColorOverride ?? Color.Black;
		}

		public override void MouseOver(UIMouseEvent evt) {
			base.MouseOver(evt);

			BorderColor = Color.Yellow;
		}

		public override void MouseOut(UIMouseEvent evt) {
			base.MouseOut(evt);

			BorderColor = DefaultBorderColorOverride ?? Color.Black;
		}

		public override void LeftDoubleClick(UIMouseEvent evt) {
			base.LeftDoubleClick(evt);

			if (evt.Target != _config)
				OnSetNetwork?.Invoke(this);
		}
	}
}
