using MagicStorage.Common.Systems;
using MagicStorage.Common.Systems.Shimmering;
using Microsoft.Xna.Framework;
using SerousCommonLib.UI;
using System.Collections.Generic;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace MagicStorage.UI.Shimmer {
	public class ShimmerReportList : UIElement {
		private class Entry : BaseShimmerReportElement {
			private UIPanel _panel;
			private UIText _label;

			public Entry() {
				Height.Set(32, 0f);
			}

			protected override void OnReportSet() => SetLabelFromReport();

			private void SetLabelFromReport() {
				string oldText = _label.Text;

				if (_report is null)
					_label.SetText(string.Empty);
				else
					_label.SetText(_report.Label);

				if (_label.Text != oldText)
					_label.Recalculate();
			}

			public override void OnInitialize() {
				base.OnInitialize();

				_panel = new UIPanel();
				_panel.Width.Set(0, 1f);
				_panel.Height.Set(0, 1f);
				_panel.SetPadding(0);
				Append(_panel);

				_icon = new ShimmerReportIcon() {
					VAlign = 0.5f
				};
				_icon.Left.Set(4, 0f);
				_icon.Width.Set(24, 0f);
				_icon.Height.Set(24, 0f);

				_panel.Append(_icon);

				_label = new UIText(string.Empty, 0.8f) {
					DynamicallyScaleDownToWidth = true
				};

				_label.Left.Set(40, 0f);
				_label.MaxWidth.Set(-40, 1f);

				_panel.Append(_label);
			}

			public override void Update(GameTime gameTime) {
				base.Update(gameTime);
				SetLabelFromReport();

				if (IsMouseHovering)
					MagicUI.mouseText = _label.Text;
			}
		}

		private NewUIList _list;

		public ref float ListPadding => ref _list.ListPadding;

		public override void OnInitialize() {
			base.OnInitialize();

			_list = new();
			_list.Width.Set(0, 1f);
			_list.Height.Set(0, 1f);
			Append(_list);
		}

		public void Add(IShimmerResultReport report) {
			Entry entry = new();
			entry.SetReport(report);
			_list.Add(entry);
		}

		public void Add(IEnumerable<IShimmerResultReport> reports) {
			foreach (IShimmerResultReport report in reports)
				Add(report);
		}

		public void AddFrom(int item) => Add(MagicCache.ShimmerInfos[item].GetShimmerReports());

		public void Clear() {
			_list.Clear();
		}

		public void SetScrollbar(NewUIScrollbar scrollbar) {
			_list.SetScrollbar(scrollbar);
		}

		public void UpdateOrder() {
			_list.UpdateOrder();
		}

		public IShimmerResultReport GetReport(int index) => ((Entry)_list._items[index]).Report;
	}
}
