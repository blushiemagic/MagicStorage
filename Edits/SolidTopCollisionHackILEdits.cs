using MagicStorage.Components;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using SerousCommonLib.API;
using System;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
using IL_Collision = Terraria.IL_Collision;

namespace MagicStorage.Edits {
	internal class SolidTopCollisionHackILEdits : Edit {
		private static ILContext.Manipulator _patchIsTopOfTile, _patchIsNotTopOfTile;

		public override void LoadEdits() {
			// For some reason, the game only allows Main.tileSolidTop[] collision on the topmost subtiles in a tile spritesheet,
			// necessitating this dumb hack to make the Storage Unit upgrades not lose collision

			// These functions check "tile.frameY == 0"
			_patchIsTopOfTile = new PatchContext(HackIsTopOfTile).Patch;
			IL_Collision.SolidCollision_Vector2_int_int_bool += _patchIsTopOfTile;
			IL_Collision.HitTiles += _patchIsTopOfTile;
			IL_Collision.SolidTiles_int_int_int_int_bool += _patchIsTopOfTile;
			IL_Collision.StepUp += _patchIsTopOfTile;
			IL_Collision.GetTileRotation += _patchIsTopOfTile;

			// These functions check "tile.frameY != 0"
			_patchIsNotTopOfTile = new PatchContext(HackIsNotTopOfTile).Patch;
			IL_Collision.TileCollision += _patchIsNotTopOfTile;
			IL_Collision.AdvancedTileCollision += _patchIsNotTopOfTile;
			IL_Collision.SlopeCollision += _patchIsNotTopOfTile;
			IL_Collision.noSlopeCollision += _patchIsNotTopOfTile;
		}

		public override void UnloadEdits() {
			if (_patchIsTopOfTile is not null) {
				IL_Collision.SolidCollision_Vector2_int_int_bool -= _patchIsTopOfTile;
				IL_Collision.HitTiles -= _patchIsTopOfTile;
				IL_Collision.SolidTiles_int_int_int_int_bool -= _patchIsTopOfTile;
				IL_Collision.StepUp -= _patchIsTopOfTile;
				IL_Collision.GetTileRotation -= _patchIsTopOfTile;

				_patchIsTopOfTile = null;
			}

			if (_patchIsNotTopOfTile is not null) {
				IL_Collision.TileCollision -= _patchIsNotTopOfTile;
				IL_Collision.AdvancedTileCollision -= _patchIsNotTopOfTile;
				IL_Collision.SlopeCollision -= _patchIsNotTopOfTile;
				IL_Collision.noSlopeCollision -= _patchIsNotTopOfTile;

				_patchIsNotTopOfTile = null;
			}
		}

		private static readonly MethodInfo Tile_get_frameY = typeof(Tile).GetProperty("frameY", BindingFlags.NonPublic | BindingFlags.Instance).GetMethod;

		private class PatchContext(Func<Tile, bool> injectedFunc) {
			private readonly Func<Tile, bool> _injectedFunc = injectedFunc;

			public void Patch(ILContext il) => ILHelper.CommonPatchingWrapper(il, MagicStorageMod.Instance, false, Patch_FrameY_Check);

			private bool Patch_FrameY_Check(ILCursor c, ref string badReturnReason) {
				// For each instance of Tile.frameY being accessed, inject a condition before it
				int found = 0;
				while (FindClause(c, out OpCode branch, out ILLabel label, out int local, out int codeStride)) {
					// Found a match, inject the logic
					found++;

					c.Emit(OpCodes.Ldloc, local);
					c.EmitDelegate(_injectedFunc);
					c.Emit(branch, label);

					// Nop the original code
					for (int i = 0; i < codeStride; i++) {
						c.Next.OpCode = OpCodes.Nop;
						c.Next.Operand = null;
						c.Index++;
					}
				}

				if (found == 0) {
					badReturnReason = "Could not find any valid Tile::frameY clauses used in boolean arithmetic";
					return false;
				}

			//	MagicStorageMod.Instance.Logger.Debug($"Patched {found} clause{(found == 1 ? "" : "s")} in {c.Method.Name}");

				return true;
			}
		}

		private static bool HackIsTopOfTile(Tile tile) {
			if (TileLoader.GetTile(tile.TileType) is StorageUnit)
				return tile.TileFrameY % 36 == 0;

			return tile.TileFrameY == 0;
		}

		private static bool HackIsNotTopOfTile(Tile tile) {
			if (TileLoader.GetTile(tile.TileType) is StorageUnit)
				return tile.TileFrameY % 36 != 0;

			return tile.TileFrameY != 0;
		}

		private static bool FindClause(ILCursor c, out OpCode branchCode, out ILLabel labelOnSuccess, out int tileLocal, out int codeStride) {
			// Boolean arithmetic
			ILLabel label = null;
			int local = -1;
			if (c.TryGotoNext(MoveType.Before, i => i.MatchLdloca(out local),
				i => i.MatchCall(Tile_get_frameY),
				i => i.MatchLdindI2(),
				i => i.MatchLdcI4(0),
				i => i.MatchCeq(),
				i => i.MatchBr(out label))) {
				branchCode = OpCodes.Br;
				labelOnSuccess = label;
				tileLocal = local;
				codeStride = 6;

			//	MagicStorageMod.Instance.Logger.Debug($"Found br clause for {c.Method.Name}");

				return true;
			}

			// Conditional, branch if false
			if (c.TryGotoNext(MoveType.Before, i => i.MatchLdloca(out local),
				i => i.MatchCall(Tile_get_frameY),
				i => i.MatchLdindI2(),
				i => i.MatchBrfalse(out label))) {
				branchCode = OpCodes.Brfalse;
				labelOnSuccess = label;
				tileLocal = local;
				codeStride = 4;

			//	MagicStorageMod.Instance.Logger.Debug($"Found brfalse clause for {c.Method.Name}");

				return true;
			}

			// Conditional, branch if true
			if (c.TryGotoNext(MoveType.Before, i => i.MatchLdloca(out local),
				i => i.MatchCall(Tile_get_frameY),
				i => i.MatchLdindI2(),
				i => i.MatchBrtrue(out label))) {
				branchCode = OpCodes.Brtrue;
				labelOnSuccess = label;
				tileLocal = local;
				codeStride = 4;

			//	MagicStorageMod.Instance.Logger.Debug($"Found brtrue clause for {c.Method.Name}");

				return true;
			}

			branchCode = default;
			labelOnSuccess = null;
			tileLocal = -1;
			codeStride = -1;
			return false;
		}
	}
}
