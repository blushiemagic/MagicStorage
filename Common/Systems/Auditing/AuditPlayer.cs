using MagicStorage.Common.Players;
using System;
using Terraria;

namespace MagicStorage.Common.Systems.Auditing {
	internal class AuditPlayer(Player player) : IAlternateAuditSource<AuditPlayer, Player, Guid> {
		public Guid Guid { get; } = player.GetModPlayer<SecurityPlayer>().UniqueID;

		public string Name { get; } = player.name;

		static string IAlternateAuditSource<AuditPlayer, Player, Guid>.GetName(AuditPlayer self) => self.Name;

		static Guid IAlternateAuditSource<AuditPlayer, Player, Guid>.GetValue(AuditPlayer self) => self.Guid;

		AuditPlayer IAlternateAuditSource<AuditPlayer, Player, Guid>.CreateFrom(Player source) => new(source);
	}
}
