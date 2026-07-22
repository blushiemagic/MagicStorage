using MagicStorage.Common.IO;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Common.Commands {
	internal sealed class SerializationVerificationCommand : ModCommand {
		public override string Command => "msverifyserialization";

		public override CommandType Type => CommandType.Chat;

		public override string Usage => this.GetUsageText();

		public override string Description => "Run deterministic compressed-item round-trip checks.";

		public override void Action(CommandCaller caller, string input, string[] args) {
			if (args.Length != 0) {
				caller.Reply($"Usage: {Usage}", Color.Red);
				return;
			}

			(string name, List<Item> items)[] fixtures = [
				("empty", []),
				("single", [new Item(ItemID.StoneBlock, 37) { favorited = true }]),
				("multi", [new Item(ItemID.DirtBlock, 1), new Item(ItemID.Torch, 73), new Item(ItemID.MagicMirror, 1) { favorited = true }])
			];

			List<string> failures = [];
			foreach ((string name, List<Item> items) in fixtures) {
				try {
					int bytes = VerifyRoundTrip(items);
					caller.Reply($"Serialization fixture '{name}' passed ({bytes} bytes).", Color.LightGreen);
				}
				catch (Exception ex) {
					failures.Add($"{name}: {ex.GetType().Name}: {ex.Message}");
					Mod.Logger.Error($"Serialization fixture '{name}' failed", ex);
				}
			}

			if (failures.Count == 0)
				caller.Reply($"Serialization verification passed: {fixtures.Length}/{fixtures.Length} fixtures.", Color.LightGreen);
			else
				caller.Reply($"Serialization verification failed: {fixtures.Length - failures.Count}/{fixtures.Length} fixtures passed.\n{string.Join("\n", failures)}", Color.Red);
		}

		private static int VerifyRoundTrip(List<Item> expected) {
			using MemoryStream stream = new();
			using (BinaryWriter writer = new(stream, System.Text.Encoding.UTF8, leaveOpen: true))
				SaveCompression.SaveItems(expected, writer);

			int bytes = checked((int)stream.Length);
			stream.Position = 0;

			List<Item> actual;
			using (BinaryReader reader = new(stream, System.Text.Encoding.UTF8, leaveOpen: true))
				actual = SaveCompression.LoadItems(reader);

			if (actual.Count != expected.Count)
				throw new InvalidOperationException($"Expected {expected.Count} items, received {actual.Count}.");

			for (int i = 0; i < expected.Count; i++) {
				Item left = expected[i];
				Item right = actual[i];
				if (right.type != left.type || right.stack != left.stack || right.prefix != left.prefix || right.favorited != left.favorited)
					throw new InvalidOperationException($"Item {i} changed: expected type={left.type}, stack={left.stack}, prefix={left.prefix}, favorite={left.favorited}; received type={right.type}, stack={right.stack}, prefix={right.prefix}, favorite={right.favorited}.");
			}

			return bytes;
		}
	}
}
