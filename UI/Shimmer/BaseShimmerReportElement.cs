using MagicStorage.Common.Systems.Shimmering;
using System;
using Terraria.UI;

namespace MagicStorage.UI.Shimmer {
	public abstract class BaseShimmerReportElement : UIElement {
		protected ShimmerReportIcon _icon;

		protected IShimmerResultReport _report;

		public IShimmerResultReport Report => _report;

		public void SetReport(IShimmerResultReport report) {
			ArgumentNullException.ThrowIfNull(report);
			_icon.SetReport(report);
			_report = report;
			OnReportSet();
		}

		protected virtual void OnReportSet() { }
	}
}
