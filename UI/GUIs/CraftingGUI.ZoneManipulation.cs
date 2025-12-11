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

		internal static bool[] adjTiles = new bool[TileLoader.TileCount];
		private static bool adjWater;
		private static bool adjLava;
		private static bool adjHoney;
		private static bool zoneSnow;
		private static bool alchemyTable;
		private static bool graveyard;
		private static bool adjShimmer;
		public static bool Campfire { get; private set; }

		public static CraftingInformation ReadCraftingEnvironment() => new(Campfire, zoneSnow, graveyard, adjWater, adjLava, adjHoney, alchemyTable, adjShimmer, adjTiles);

		public static void WriteCraftingEnvironment(in CraftingInformation information)
		{
			Campfire = information.campfire;
			zoneSnow = information.snow;
			graveyard = information.graveyard;
			adjWater = information.water;
			adjLava = information.lava;
			adjHoney = information.honey;
			alchemyTable = information.alchemyTable;
			adjShimmer = information.shimmer;
			adjTiles = [.. information.adjTiles];
		}

		internal static int _executingInGuiEnvironment;
		private static bool _zoneInformationReady;
		internal static bool _blockForStationUpdate;

		internal static void ExecuteInCraftingGuiEnvironment(Action action)
		{
			ArgumentNullException.ThrowIfNull(action);
			ExecuteInCraftingGuiEnvironment_Inner(new ActionWrapperNoArgs(action));
		}

		internal static void ExecuteInCraftingGuiEnvironment<T>(T arg, Action<T> action)
		{
			ArgumentNullException.ThrowIfNull(action);
			ExecuteInCraftingGuiEnvironment_Inner(new ActionWrapper<T>(action, arg));
		}

		internal static void ExecuteInCraftingGuiEnvironment<T1, T2>(T1 arg1, T2 arg2, Action<T1, T2> action)
		{
			ArgumentNullException.ThrowIfNull(action);
			ExecuteInCraftingGuiEnvironment_Inner(new ActionWrapper<T1, T2>(action, arg1, arg2));
		}

		internal static T ExecuteInCraftingGuiEnvironment<T>(Func<T> func)
		{
			ArgumentNullException.ThrowIfNull(func);
			FuncWrapper<T> wrapper = new(func);
			ExecuteInCraftingGuiEnvironment_Inner(wrapper);
			return wrapper.Result;
		}

		internal static TReturn ExecuteInCraftingGuiEnvironment<TArg, TReturn>(TArg state, Func<TArg, TReturn> func)
		{
			ArgumentNullException.ThrowIfNull(func);
			FunWrapper<TArg, TReturn> wrapper = new(func, state);
			ExecuteInCraftingGuiEnvironment_Inner(wrapper);
			return wrapper.Result;
		}

		internal static TReturn ExecuteInCraftingGuiEnvironment<TArg1, TArg2, TReturn>(TArg1 arg1, TArg2 arg2, Func<TArg1, TArg2, TReturn> func)
		{
			ArgumentNullException.ThrowIfNull(func);
			FunWrapper<TArg1, TArg2, TReturn> wrapper = new(func, arg1, arg2);
			ExecuteInCraftingGuiEnvironment_Inner(wrapper);
			return wrapper.Result;
		}

		#region Nested types
		private abstract class ActionWrapper {
			public abstract void RunAction();
		}

		private class ActionWrapperNoArgs(Action action) : ActionWrapper {
			private readonly Action _action = action;
			public override void RunAction() => _action();
		}

		private class ActionWrapper<T>(Action<T> action, T arg) : ActionWrapper {
			private readonly Action<T> _action = action;
			private readonly T _arg = arg;
			public override void RunAction() => _action(_arg);
		}

		private class ActionWrapper<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg) : ActionWrapper {
			private readonly Action<T1, T2> _action = action;
			private readonly T1 _arg1 = arg1;
			private readonly T2 _arg2 = arg;
			public override void RunAction() => _action(_arg1,  _arg2);
		}

		private class FuncWrapper<T>(Func<T> func) : ActionWrapper {
			private readonly Func<T> _func = func;
			public T Result { get; private set; }
			public override void RunAction() => Result = _func();
		}

		private class FunWrapper<TArg, TReturn>(Func<TArg, TReturn> func, TArg arg) : ActionWrapper {
			private readonly Func<TArg, TReturn> _func = func;
			private readonly TArg _arg = arg;
			public TReturn Result { get; private set; }
			public override void RunAction() => Result = _func(_arg);
		}

		private class FunWrapper<TArg1, TArg2, TReturn>(Func<TArg1, TArg2, TReturn> func, TArg1 arg1, TArg2 arg2) : ActionWrapper {
			private readonly Func<TArg1, TArg2, TReturn> _func = func;
			private readonly TArg1 _arg1 = arg1;
			private readonly TArg2 _arg2 = arg2;
			public TReturn Result { get; private set; }
			public override void RunAction() => Result = _func(_arg1, _arg2);
		}
		#endregion

		private static void ExecuteInCraftingGuiEnvironment_Inner(ActionWrapper action) {
			while (_blockForStationUpdate)
				Thread.Yield();

			int level = Interlocked.Increment(ref _executingInGuiEnvironment);
			if (level > 1)
			{
				// Local capturing
			//	int l = level;
			//	Main.QueueMainThreadAction(() => Main.NewText($"ExecuteInCraftingGuiEnvironment concurrency level: {l}"));

				try {
					while (!_zoneInformationReady)
						Thread.Yield();

					// Zone flags are already set, so we can just execute the action
					action.RunAction();
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

				action.RunAction();
			} finally {
				if (Interlocked.Decrement(ref _executingInGuiEnvironment) <= 0) {
					PlayerZoneCache.FreeCache(false);
					_zoneInformationReady = false;
				}
			}
		}
	}
}
