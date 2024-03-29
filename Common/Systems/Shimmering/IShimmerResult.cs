﻿using System.Collections.Generic;
using System.IO;
using Terraria;

namespace MagicStorage.Common.Systems.Shimmering {
	/// <summary>
	/// An interface representing a result from shimmering an item in the Decrafting UI
	/// </summary>
	public interface IShimmerResult {
		IEnumerable<IShimmerResultReport> GetShimmerReports(Item item, int iconicType);

		void OnShimmer(Item item, int iconicType, StorageIntermediary storage, bool net);

		void Send(BinaryWriter writer);

		IShimmerResult Receive(BinaryReader reader);
	}
}
