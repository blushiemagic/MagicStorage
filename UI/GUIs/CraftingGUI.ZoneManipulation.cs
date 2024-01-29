using System;
using System.Linq;
using System.Threading;
using Terraria;
using Terraria.ModLoader;

namespace MagicStorage {
	partial class CraftingGUI {
		public class PlayerZoneCache {
			public readonly bool[] origAdjTile;
			public readonly bool oldAdjWater;
			public readonly bool oldAdjLava;
			public readonly bool oldAdjHoney;
			public readonly bool oldAlchemyTable;
			public readonly bool oldSnow;
			public readonly bool oldGraveyard;
			public readonly bool adjShimmer;

			private PlayerZoneCache() {
				Player player = Main.LocalPlayer;
				origAdjTile = player.adjTile.ToArray();
				oldAdjWater = player.adjWater;
				oldAdjLava = player.adjLava;
				oldAdjHoney = player.adjHoney;
				oldAlchemyTable = player.alchemyTable;
				oldSnow = player.ZoneSnow;
				oldGraveyard = player.ZoneGraveyard;
				adjShimmer = player.adjShimmer;
			}

			private static PlayerZoneCache cache;

			public static void Cache() {
				if (cache is not null)
					return;

				cache = new PlayerZoneCache();
			}

			public static void FreeCache(bool destroy) {
				if (cache is not PlayerZoneCache c)
					return;

				if (destroy)
					cache = null;

				Player player = Main.LocalPlayer;

				player.adjTile = c.origAdjTile;
				player.adjWater = c.oldAdjWater;
				player.adjLava = c.oldAdjLava;
				player.adjHoney = c.oldAdjHoney;
				player.alchemyTable = c.oldAlchemyTable;
				player.ZoneSnow = c.oldSnow;
				player.ZoneGraveyard = c.oldGraveyard;
				player.adjShimmer = c.adjShimmer;
			}
		}

		private static bool[] adjTiles = new bool[TileLoader.TileCount];
		private static bool adjWater;
		private static bool adjLava;
		private static bool adjHoney;
		private static bool zoneSnow;
		private static bool alchemyTable;
		private static bool graveyard;
		private static bool adjShimmer;
		public static bool Campfire { get; private set; }

		public static CraftingInformation ReadCraftingEnvironment() => new(Campfire, zoneSnow, graveyard, adjWater, adjLava, adjHoney, alchemyTable, adjShimmer, adjTiles);

		private static int _executingInGuiEnvironment;
		private static bool _zoneInformationReady;

		internal static void ExecuteInCraftingGuiEnvironment(Action action)
		{
			ArgumentNullException.ThrowIfNull(action);

			int level = Interlocked.Increment(ref _executingInGuiEnvironment);
			if (level > 1)
			{
				// Local capturing
				int l = level;
				Main.QueueMainThreadAction(() => Main.NewText($"ExecuteInCraftingGuiEnvironment concurrency level: {l}"));

				try {
					while (!_zoneInformationReady)
						Thread.Yield();

					// Zone flags are already set, so we can just execute the action
					action();
				} finally {
					if (Interlocked.Decrement(ref _executingInGuiEnvironment) <= 0) {
						PlayerZoneCache.FreeCache(false);
						_zoneInformationReady = false;
					}
				}

				return;
			}

			PlayerZoneCache.Cache();

			Player player = Main.LocalPlayer;

			try
			{
				player.adjTile = adjTiles;
				player.adjWater = adjWater;
				player.adjLava = adjLava;
				player.adjHoney = adjHoney;
				player.alchemyTable = alchemyTable;
				player.ZoneSnow = zoneSnow;
				player.ZoneGraveyard = graveyard;
				player.adjShimmer = adjShimmer;

				_zoneInformationReady = true;

				action();
			} finally {
				if (Interlocked.Decrement(ref _executingInGuiEnvironment) <= 0) {
					PlayerZoneCache.FreeCache(false);
					_zoneInformationReady = false;
				}
			}
		}
	}
}
