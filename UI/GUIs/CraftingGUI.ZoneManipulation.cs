using System;
using System.Threading;
using Terraria;
using Terraria.ModLoader;

namespace MagicStorage {
	partial class CraftingGUI {
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

		public static CraftingInformation ReadCraftingEnvironmentFrom(Player player) => new(false, player.ZoneSnow, player.ZoneGraveyard, player.adjWater, player.adjLava, player.adjHoney, player.alchemyTable, player.adjShimmer, (bool[])player.adjTile.Clone());

		internal static int GetCraftingEnvironmentHash() {
			var hash = new HashCode();
			hash.Add(Campfire);
			hash.Add(zoneSnow);
			hash.Add(graveyard);
			hash.Add(adjWater);
			hash.Add(adjLava);
			hash.Add(adjHoney);
			hash.Add(alchemyTable);
			hash.Add(adjShimmer);
			hash.Add(adjTiles?.Length ?? 0);

			if (adjTiles is not null) {
				for (int i = 0; i < adjTiles.Length; i++) {
					if (adjTiles[i])
						hash.Add(i);
				}
			}

			return hash.ToHashCode();
		}

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

		public static void WriteCraftingEnvironmentTo(in CraftingInformation information, Player player)
		{
			player.ZoneSnow = information.snow;
			player.ZoneGraveyard = information.graveyard;
			player.adjWater = information.water;
			player.adjLava = information.lava;
			player.adjHoney = information.honey;
			player.alchemyTable = information.alchemyTable;
			player.adjShimmer = information.shimmer;
			player.adjTile = [.. information.adjTiles];
		}

		internal static int _executingInGuiEnvironment;
		private static bool _zoneInformationReady;
		internal static bool _blockForStationUpdate;
		private static CraftingInformation _playerInformationCache;

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

		internal static void ExecuteInCraftingGuiEnvironment<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3, Action<T1, T2, T3> action)
		{
			ArgumentNullException.ThrowIfNull(action);
			ExecuteInCraftingGuiEnvironment_Inner(new ActionWrapper<T1, T2, T3>(action, arg1, arg2, arg3));
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
			FuncWrapper<TArg, TReturn> wrapper = new(func, state);
			ExecuteInCraftingGuiEnvironment_Inner(wrapper);
			return wrapper.Result;
		}

		internal static TReturn ExecuteInCraftingGuiEnvironment<TArg1, TArg2, TReturn>(TArg1 arg1, TArg2 arg2, Func<TArg1, TArg2, TReturn> func)
		{
			ArgumentNullException.ThrowIfNull(func);
			FuncWrapper<TArg1, TArg2, TReturn> wrapper = new(func, arg1, arg2);
			ExecuteInCraftingGuiEnvironment_Inner(wrapper);
			return wrapper.Result;
		}

		internal static TReturn ExecuteInCraftingGuiEnvironment<TArg1, TArg2, TArg3, TReturn>(TArg1 arg1, TArg2 arg2, TArg3 arg3, Func<TArg1, TArg2, TArg3, TReturn> func)
		{
			ArgumentNullException.ThrowIfNull(func);
			FuncWrapper<TArg1, TArg2, TArg3, TReturn> wrapper = new(func, arg1, arg2, arg3);
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

		private class ActionWrapper<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3) : ActionWrapper {
			private readonly Action<T1, T2, T3> _action = action;
			private readonly T1 _arg1 = arg1;
			private readonly T2 _arg2 = arg2;
			private readonly T3 _arg3 = arg3;
			public override void RunAction() => _action(_arg1,  _arg2, _arg3);
		}

		private class FuncWrapper<T>(Func<T> func) : ActionWrapper {
			private readonly Func<T> _func = func;
			public T Result { get; private set; }
			public override void RunAction() => Result = _func();
		}

		private class FuncWrapper<TArg, TReturn>(Func<TArg, TReturn> func, TArg arg) : ActionWrapper {
			private readonly Func<TArg, TReturn> _func = func;
			private readonly TArg _arg = arg;
			public TReturn Result { get; private set; }
			public override void RunAction() => Result = _func(_arg);
		}

		private class FuncWrapper<TArg1, TArg2, TReturn>(Func<TArg1, TArg2, TReturn> func, TArg1 arg1, TArg2 arg2) : ActionWrapper {
			private readonly Func<TArg1, TArg2, TReturn> _func = func;
			private readonly TArg1 _arg1 = arg1;
			private readonly TArg2 _arg2 = arg2;
			public TReturn Result { get; private set; }
			public override void RunAction() => Result = _func(_arg1, _arg2);
		}

		private class FuncWrapper<TArg1, TArg2, TArg3, TReturn>(Func<TArg1, TArg2, TArg3, TReturn> func, TArg1 arg1, TArg2 arg2, TArg3 arg3) : ActionWrapper {
			private readonly Func<TArg1, TArg2, TArg3, TReturn> _func = func;
			private readonly TArg1 _arg1 = arg1;
			private readonly TArg2 _arg2 = arg2;
			private readonly TArg3 _arg3 = arg3;
			public TReturn Result { get; private set; }
			public override void RunAction() => Result = _func(_arg1, _arg2, _arg3);
		}
		#endregion

		private static void ExecuteInCraftingGuiEnvironment_Inner(ActionWrapper action) {
			while (_blockForStationUpdate)
				Thread.Yield();

			Player player = Main.LocalPlayer;

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
						WriteCraftingEnvironmentTo(_playerInformationCache, player);
						_playerInformationCache = default;
						_zoneInformationReady = false;
					}
				}

				return;
			}

			// The caller has invoked this between the Decrement and WriteCraftingEnvironment calls
			while (_zoneInformationReady)
				Thread.Yield();

			_playerInformationCache = ReadCraftingEnvironmentFrom(player);

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
					WriteCraftingEnvironmentTo(_playerInformationCache, player);
					_playerInformationCache = default;
					_zoneInformationReady = false;
				}
			}
		}
	}
}
