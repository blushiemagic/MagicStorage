using MagicStorage.Components;

namespace MagicStorage.Common.Systems.Auditing {
	internal class AuditComponent(TEStorageComponent component) : IAlternateAuditSource<AuditComponent, TEStorageComponent, int> {
		public int Type { get; } = component.Type;

		public string Name { get; } = component.Name;

		static string IAlternateAuditSource<AuditComponent, TEStorageComponent, int>.GetName(AuditComponent self) => self.Name;

		static int IAlternateAuditSource<AuditComponent, TEStorageComponent, int>.GetValue(AuditComponent self) => self.Type;

		AuditComponent IAlternateAuditSource<AuditComponent, TEStorageComponent, int>.CreateFrom(TEStorageComponent source) => new(source);
	}
}
