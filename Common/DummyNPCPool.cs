using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
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
			public bool hasUpdated;

			public PoolEntry(int npcType) {
				icon = new UnlockableNPCEntryIcon(npcType);
			}
		}

		private static readonly Dictionary<int, PoolEntry> entries = new();

		private static void GetOrReserve(int npcType, out PoolEntry entry) {
			if (!entries.TryGetValue(npcType, out entry)) {
				entry = new PoolEntry(npcType);
				entries.Add(npcType, entry);
			}
		}

		private static void DestroyIcon(int npcType) {
			if (entries.TryGetValue(npcType, out var poolEntry))
				poolEntry.icon = null;
		}

		internal static void ResetUpdates() {
			foreach (var entry in entries.Values)
				entry.hasUpdated = false;
		}

		public static void UpdateEntry(int npcType, Rectangle renderArea) {
			GetOrReserve(npcType, out var entry);

			if (entry.hasUpdated)
				return;

			entry.hasUpdated = true;

			var info = new BestiaryUICollectionInfo() {
				UnlockState = BestiaryEntryUnlockState.CanShowPortraitOnly_1
			};

			var settings = new EntryIconDrawSettings() {
				iconbox = renderArea,
				IsPortrait = true
			};

			entry.icon?.Update(info, renderArea, settings);
		}

		public static void RenderEntry(int npcType, Rectangle renderArea, float additionalScale = 1f) {
			GetOrReserve(npcType, out var entry);

			if (entry.icon is null)
				return;

			NPC npc = entry.icon._npcCache;

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

			// Save the SpriteBatch parameters to restore them later
			var blendState = Main.spriteBatch.GraphicsDevice.BlendState;
			var samplerState = Main.spriteBatch.GraphicsDevice.SamplerStates[0];
			var rasterizerState = Main.spriteBatch.GraphicsDevice.RasterizerState;

			// Start a new batch with a smaller ScissorRectangle
			Main.spriteBatch.End();

			Rectangle oldRect = Main.spriteBatch.GraphicsDevice.ScissorRectangle;
			var finalRect = Rectangle.Intersect(clip, oldRect);

			Main.spriteBatch.GraphicsDevice.ScissorRectangle = finalRect;

			Main.spriteBatch.Begin(SpriteSortMode.Deferred, blendState, samplerState, DepthStencilState.None, rasterizerState, null, Main.UIScaleMatrix);

			float oldScale = npc.scale;

			try {
				// Shrink the NPC
				npc.scale *= Main.UIScale * 0.4f * additionalScale;
				entry.icon.Draw(info, Main.spriteBatch, settings);
			} catch {
				DestroyIcon(npcType);
				MagicStorageMod.Instance.Logger.Error($"Failed to render NPC icon for \"{Lang.GetNPCNameValue(npcType)}\" (ID {npcType})");
			}

			npc.scale = oldScale;
			
			// Restart the batch with the old ScissorRectangle
			Main.spriteBatch.End();

			Main.spriteBatch.GraphicsDevice.ScissorRectangle = oldRect;

			Main.spriteBatch.Begin(SpriteSortMode.Deferred, blendState, samplerState, DepthStencilState.None, rasterizerState, null, Main.UIScaleMatrix);
		}
	}
}
