using MagicStorage.Components;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using SerousCommonLib.API;
using System;
using System.Reflection;
using Terraria;
using Terraria.DataStructures;

namespace MagicStorage.Edits {
	internal class ItemTransferParticleMoreEatingILEdit : Edit {
		public override void LoadEdits() {
			Terraria.GameContent.Drawing.IL_ParticleOrchestrator.Spawn_ItemTransfer += ParticleOrchestrator_Spawn_ItemTransfer;
		}

		public override void UnloadEdits() {
			Terraria.GameContent.Drawing.IL_ParticleOrchestrator.Spawn_ItemTransfer -= ParticleOrchestrator_Spawn_ItemTransfer;
		}

		private static void ParticleOrchestrator_Spawn_ItemTransfer(ILContext il) {
			ILHelper.CommonPatchingWrapper(il, MagicStorageMod.Instance, throwOnFail: false, Patch_ParticleOrchestrator_Spawn_ItemTransfer);
		}

		private static readonly MethodInfo Chest_AskForChestToEatItem = typeof(Chest).GetMethod(nameof(Chest.AskForChestToEatItem), BindingFlags.Public | BindingFlags.Static);

		private static bool Patch_ParticleOrchestrator_Spawn_ItemTransfer(ILCursor c, ref string badReturnReason) {
			var positionLocal = c.Context.MakeLocalVariable<Vector2>();

			int durationLocal = -1;
			if (!c.TryGotoNext(MoveType.Before, i => i.MatchLdloc(out durationLocal),
				i => i.MatchLdcI4(10),
				i => i.MatchAdd(),
				i => i.MatchCall(Chest_AskForChestToEatItem))) {
				badReturnReason = "Could not find Chest.AskForChestToEatItem() call";
				return false;
			}

			c.Emit(OpCodes.Stloc, positionLocal);
			c.Emit(OpCodes.Ldloc, positionLocal);

			// Already know that the call exists, no need to TryGotoNext here
			c.GotoNext(MoveType.After, i => i.MatchCall(Chest_AskForChestToEatItem));

			c.Emit(OpCodes.Ldloc, positionLocal);
			c.Emit(OpCodes.Ldloc, durationLocal);
			c.EmitDelegate<Action<Vector2, int>>((worldPosition, duration) => {
				Point16 tileCoordinates = worldPosition.ToTileCoordinates16();
				if (TileEntity.ByPosition.TryGetValue(tileCoordinates, out TileEntity entity) && entity is TEStorageCenter center)
					center.AskToEatItem(duration);
			});

			return true;
		}
	}
}
