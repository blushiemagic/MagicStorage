using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ModLoader;

namespace MagicStorage.Common {
	internal static class DummyNPCPool {
		private class Loadable : ILoadable {
			public void Load(Mod mod) { }

			public void Unload() {
				entries.Clear();
			}
		}

		private class PoolEntry {
			public UnlockableNPCEntryIcon icon;

			public PoolEntry(int npcType) {
				icon = new UnlockableNPCEntryIcon(npcType);
			}
		}

		private static readonly Dictionary<int, PoolEntry> entries = new();

		private static void GetOrReserve(int npcType, out UnlockableNPCEntryIcon icon) {
			if (!entries.TryGetValue(npcType, out var poolEntry)) {
				poolEntry = new PoolEntry(npcType);
				entries.Add(npcType, poolEntry);
			}

			icon = poolEntry.icon;
		}

		private static void DestroyIcon(int npcType) {
			if (entries.TryGetValue(npcType, out var poolEntry))
				poolEntry.icon = null;
		}

		public static void UpdateEntry(int npcType, Rectangle renderArea) {
			GetOrReserve(npcType, out var icon);

			var info = new BestiaryUICollectionInfo() {
				UnlockState = BestiaryEntryUnlockState.CanShowPortraitOnly_1
			};

			var settings = new EntryIconDrawSettings() {
				iconbox = renderArea,
				IsPortrait = true
			};

			icon?.Update(info, renderArea, settings);
		}

		public static void RenderEntry(int npcType, Rectangle renderArea) {
			GetOrReserve(npcType, out var icon);

			if (icon is null)
				return;

			NPC npc = GetNPC(icon);

			var info = new BestiaryUICollectionInfo() {
				UnlockState = BestiaryEntryUnlockState.CanShowPortraitOnly_1
			};

			var settings = new EntryIconDrawSettings() {
				iconbox = renderArea,
				IsPortrait = true
			};

			Rectangle clip = renderArea;
			clip.Inflate(-4, -4);

			int inflate = (int)(clip.Width * Main.UIScale) - clip.Width;
			clip.Inflate(inflate / 2, inflate / 2);

			var center = clip.Center();
			var offset = (center * Main.UIScale - center).ToPoint();
			clip.Offset(offset);

			Rectangle oldRect = Main.spriteBatch.GraphicsDevice.ScissorRectangle;
			var finalRect = Rectangle.Intersect(clip, oldRect);

			Main.spriteBatch.GraphicsDevice.ScissorRectangle = finalRect;

			float oldScale = npc.scale;

			try {
				// Shrink the NPC
				npc.scale *= Main.UIScale * 0.4f;
				icon.Draw(info, Main.spriteBatch, settings);
			} catch {
				DestroyIcon(npcType);
				MagicStorageMod.Instance.Logger.Error($"Failed to render NPC icon for \"{Lang.GetNPCNameValue(npcType)}\" (ID {npcType})");
			}

			npc.scale = oldScale;

			Main.spriteBatch.GraphicsDevice.ScissorRectangle = oldRect;
		}

		private static Func<UnlockableNPCEntryIcon, NPC> _getCachedNPC;

		private static NPC GetNPC(UnlockableNPCEntryIcon icon) {
			static Func<UnlockableNPCEntryIcon, NPC> MakeFunction() {
				// Generate a System.Linq.Expression delegate that takes an UnlockableNPCEntryIcon as a parameter and returns its _npcCache field
				// This is done to avoid using reflection, which is slow
				var param = Expression.Parameter(typeof(UnlockableNPCEntryIcon));
				var field = typeof(UnlockableNPCEntryIcon).GetField("_npcCache", BindingFlags.NonPublic | BindingFlags.Instance);
				var body = Expression.Field(param, field);
				var lambda = Expression.Lambda<Func<UnlockableNPCEntryIcon, NPC>>(body, param);
				return lambda.Compile();
			}

			return (_getCachedNPC ??= MakeFunction())(icon);
		}
	}
}
