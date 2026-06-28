using MagicStorage.Common.Systems.Debugging;
using System.Diagnostics.CodeAnalysis;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;

namespace MagicStorage {
	partial class NetHelper {
		internal static bool TryGetEntityFromLocation<T>(object packetSource, Point16 location, [NotNullWhen(true)] out T tileEntity)
			where T : TileEntity
		{
			if (!TryFindEntity(packetSource, location, out TileEntity te))
			{
				tileEntity = null;
				return false;
			}

			if (te is not T expectedEntity) {
				if (DebugControls.Get(DebugControls.Names.InvalidNetcodeValues))
					PrintInvalidTileEntityReport<T>(packetSource, te, location);
				else
					PrintInvalidNetcodeValuesReport(packetSource);

				tileEntity = null;
				return false;
			}

			tileEntity = expectedEntity;
			return true;
		}

		internal static bool TryFindEntity(object packetSource, Point16 location, [NotNullWhen(true)] out TileEntity tileEntity)
		{
			if (location.ResolveToTileEntity() is not TileEntity te) {
				if (DebugControls.Get(DebugControls.Names.InvalidNetcodeValues)) {
					if (Main.netMode == NetmodeID.Server)
						PrintInvalidArgumentReport(packetSource, "A Tile Entity at location (X: {0}, Y: {1}) does not exist on the server", location.X, location.Y);
					else
						PrintInvalidArgumentReport(packetSource, "A Tile Entity at location (X: {0}, Y: {1}) does not exist on this client", location.X, location.Y);
				} else
					PrintInvalidNetcodeValuesReport(packetSource);

				tileEntity = null;
				return false;
			}

			tileEntity = te;
			return true;
		}

		internal static void PrintInvalidNetcodeValuesReport(object packetSource) {
			using var debugging = DebugMessage.Create();

			debugging.Report(false, "Handling packet {0} failed.  Set control \"{1}\" to true for more details.", packetSource, DebugControls.Names.InvalidNetcodeValues);
		}

		internal static void PrintInvalidTileEntityReport<T>(object packetSource, TileEntity entity, Point16 entityLocation)
			where T : TileEntity
		{
			PrintInvalidArgumentReport(packetSource, "The Tile Entity at location (X: {0}, Y: {1}) is of type {2}, expected {3}", entityLocation.X, entityLocation.Y, entity.GetType().FullName, typeof(T).Name);
		}

		internal static void PrintInvalidTileEntityReport<T1, T2>(object packetSource, TileEntity entity, Point16 entityLocation)
			where T1 : TileEntity
			where T2 : TileEntity
		{
			PrintInvalidArgumentReport(packetSource, "The Tile Entity at location (X: {0}, Y: {1}) is of type {2}, expected {3} or {4}", entityLocation.X, entityLocation.Y, entity.GetType().FullName, typeof(T1).Name, typeof(T2).Name);
		}

		internal static void PrintInvalidArgumentReport(object packetSource, string info) {
			using var debugging = DebugMessage.Create();

			debugging
				.Report(true, "Packet {0} had a data mismatch", packetSource)
				.Indent()
				.Report(false, info);
		}

		internal static void PrintInvalidArgumentReport(object packetSource, string infoFmt, object arg) {
			using var debugging = DebugMessage.Create();

			debugging
				.Report(true, "Packet {0} had a data mismatch", packetSource)
				.Indent()
				.Report(false, infoFmt, arg);
		}

		internal static void PrintInvalidArgumentReport(object packetSource, string infoFmt, object arg0, object arg1) {
			using var debugging = DebugMessage.Create();

			debugging
				.Report(true, "Packet {0} had a data mismatch", packetSource)
				.Indent()
				.Report(false, infoFmt, arg0, arg1);
		}

		internal static void PrintInvalidArgumentReport(object packetSource, string infoFmt, object arg0, object arg1, object arg2) {
			using var debugging = DebugMessage.Create();

			debugging
				.Report(true, "Packet {0} had a data mismatch", packetSource)
				.Indent()
				.Report(false, infoFmt, arg0, arg1, arg2);
		}

		internal static void PrintInvalidArgumentReport(object packetSource, string infoFmt, params object[] args) {
			using var debugging = DebugMessage.Create();

			debugging
				.Report(true, "Packet {0} had a data mismatch", packetSource)
				.Indent()
				.Report(false, infoFmt, args);
		}
	}
}
