using System;
using System.Collections.Generic;
using System.IO;

namespace MagicStorage.Common.Systems.Auditing {
	internal class AuditFile : IAuditable<AuditFile> {
		private AuditPlayerTable _playerTable = new();
		private AuditItemTable _itemTable = new();
		private AuditComponentTable _componentTable = new();

		private int _lastCount;
		private readonly List<AuditEntry> _entries = [];

		internal AuditPlayerTable Players => _playerTable;

		internal AuditItemTable Items => _itemTable;

		internal AuditComponentTable Components => _componentTable;

		public bool HasChanges => _lastCount != _entries.Count;

		internal void ForceNoChanges() => _lastCount = _entries.Count;

		public void AddEntry(AuditEntry entry) => _entries.Add(entry);

		public IEnumerable<AuditEntry> Entries => _entries.AsReadOnly();

		public static void DeserializeOne<T>(BinaryReader reader, ref T instance) where T : AuditFile {
			try {
				AuditPlayerTable.DeserializeOne(reader, ref instance._playerTable);
				AuditItemTable.DeserializeOne(reader, ref instance._itemTable);
				AuditComponentTable.DeserializeOne(reader, ref instance._componentTable);

				int count = reader.ReadInt32();
				instance._entries.Clear();
				for (int i = 0; i < count; i++)
					instance._entries.Add(instance.DeserializeEntry(reader));
			} catch (Exception ex) {
				MagicStorageMod.Instance.Logger.Error("Failed to deserialize audit file", ex);
				instance._playerTable = new();
				instance._itemTable = new();
				instance._componentTable = new();
			} finally {
				instance._entries.Clear();
				instance._lastCount = 0;
			}
		}

		private AuditEntry DeserializeEntry(BinaryReader reader) {
			AuditAction action = (AuditAction)reader.ReadByte();

			AuditEntry entry = action switch {
				AuditAction.DepositOne => new DepositOne(this),
				AuditAction.DepositMany => new DepositMany(this),
				AuditAction.WithdrawOne => new WithdrawOne(this),
				AuditAction.WithdrawMany => new WithdrawMany(this),
				AuditAction.UnitDeactivate => new StorageUnitDeactivation(this),
				AuditAction.UnitActivate => new StorageUnitActivation(this),
				AuditAction.UnitCoreRemove => new StorageUnitCoreRemoval(this),
				AuditAction.UnitCoreInsert => new StorageUnitCoreInsertion(this),
				AuditAction.SellItems => new StorageControlSellItems(this),
				AuditAction.DestroyItem => new StorageControlDeleteItem(this),
				AuditAction.CraftRequest => new CraftRequest(this),
				AuditAction.ControlDeleteUnloadedItems => new StorageControlDeleteUnloadedItems(this),
				AuditAction.ControlDeleteUnloadedData => new StorageControlDeleteUnloadedData(this),
				AuditAction.LinkRemoteAccess => new LinkRemoteAccess(this),
				AuditAction.LinkPortableAccess => new LinkPortableAccess(this),
				AuditAction.SecurityNetworkAssignment => new SecurityNetworkAssignment(this),
				AuditAction.SecurityNetworkModification => new SecurityNetworkModification(this),
				AuditAction.SecurityNetworkDelete => new SecurityNetworkDeletion(this),
				AuditAction.SecurityNetworkJoin => new SecurityNetworkJoin(this),
				AuditAction.ControlCompactCoins => new StorageControlCompactCoins(this),
				AuditAction.StatusServerAdmin => new StatusAdministratorAssignment(this),
				AuditAction.StatusServerOperatorGranted => new StatusOperatorAssignment(this),
				AuditAction.StatusServerOperatorRemoved => new StatusOperatorRemoval(this),
				_ => throw new ArgumentOutOfRangeException($"Audit action ID ({action}) was outside the range of expected values"),
			};

			entry.Deserialize(reader);

			return entry;
		}

		public void Serialize(BinaryWriter writer) {
			try {
				_playerTable.Serialize(writer);
				_itemTable.Serialize(writer);
				_componentTable.Serialize(writer);

				writer.Write(_entries.Count);
				foreach (AuditEntry entry in _entries) {
					writer.Write((byte)entry.Action);
					entry.Serialize(writer);
				}
			} catch (Exception ex) {
				MagicStorageMod.Instance.Logger.Error("Failed to serialize audit file", ex);
			} finally {
				_lastCount = _entries.Count;
			}
		}
	}
}
