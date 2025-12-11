using MagicStorage.Common.Systems;
using MagicStorage.Common.Systems.Shimmering;
using System.Collections.Generic;
using System.Linq;
using Terraria.ID;

namespace MagicStorage.Common.Threading.Refreshing {
	public interface IShimmerItemReportsProvider {
		ShimmerItemReports ShimmerItemReports { get; }
	}

	public class ShimmerItemReports {
		public readonly ListProvider<ItemReport> reports;

		public ShimmerItemReports(List<ItemReport> staticReportsList) {
			reports = new(staticReportsList);
		}

		public void CollectObjects(int selectedItem) {
			if (selectedItem > ItemID.None)
				reports.AddRange(MagicCache.ShimmerInfos[selectedItem].GetShimmerReports().OfType<ItemReport>());
		}

		public void CopyFromStaticCollection() {
			reports.CopyFromStatic();
		}

		public void CopyToStaticCollection() {
			reports.OverwriteStatic();
		}

		public void ClearStaticCollection() {
			reports.ClearStatic();
		}
	}
}
