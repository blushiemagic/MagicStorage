using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.RuntimeDetour.HookGen;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria.ModLoader;

namespace MagicStorage.Edits {
	public static class DirectDetourManager {
		internal class Loading : ILoadable {
			public Mod Mod { get; private set; }

			public void Load(Mod mod) {
				Mod = mod;
			}

			public void Unload() {
				Mod = null;

				DirectDetourManager.Unload();
			}
		}

		private static readonly List<Hook> detours = new();
		private static readonly List<(MethodInfo, Delegate)> delegates = new();

		private static readonly Dictionary<string, MethodInfo> cachedMethods = new();

		private static bool requestedNativeAccess;

		public static MethodInfo GetCachedMethod(this Type type, string method) {
			string key = $"{type.FullName}::{method}";
			if (cachedMethods.TryGetValue(key, out MethodInfo value))
				return value;

			return cachedMethods[key] = type.GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
		}

		public static MethodInfo GetCachedMethod(this Type type, string method, params Type[] argumentTypes) {
			string key = $"{type.FullName}::{method}";
			if (cachedMethods.TryGetValue(key, out MethodInfo value))
				return value;

			return cachedMethods[key] = type.GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, argumentTypes);
		}

		private static void Unload() {
			foreach (Hook hook in detours)
				hook.Undo();

			foreach ((MethodInfo method, Delegate hook) in delegates)
				HookEndpointManager.Unmodify(method, hook);

			requestedNativeAccess = false;
		}

		private static void TryRequestAccess() {
			if (!requestedNativeAccess) {
				MonoModHooks.RequestNativeAccess();
				requestedNativeAccess = true;
			}
		}

		public static void ILHook(MethodInfo orig, MethodInfo modify) {
			TryRequestAccess();

			try {
				ArgumentNullException.ThrowIfNull(orig);
				ArgumentNullException.ThrowIfNull(modify);

				Delegate hook = Delegate.CreateDelegate(typeof(ILContext.Manipulator), modify);

				HookEndpointManager.Modify(orig, hook);
				delegates.Add((orig, hook));
			} catch (Exception ex) {
				throw new Exception("An error occurred while doing patching in MagicStorage." +
					"\nReport this error to the mod devs and disable the mod in the meantime." +
					"\n\n" +
					ex);
			}
		}

		public static void DetourHook(MethodInfo orig, MethodInfo modify) {
			TryRequestAccess();

			try {
				ArgumentNullException.ThrowIfNull(orig);
				ArgumentNullException.ThrowIfNull(modify);

				Hook hook = new(orig, modify);
				detours.Add(hook);
				hook.Apply();
			} catch (Exception ex) {
				throw new Exception("An error occurred while doing patching in MagicStorage." +
					"\nReport this error to the mod devs and disable the mod in the meantime." +
					"\n\n" +
					ex);
			}
		}
	}
}
