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

		public int EntryCount => _entries.Count;

		public IEnumerable<AuditEntry> Entries => _entries.AsReadOnly();

		internal void ForceNoChanges() => _lastCount = _entries.Count;

		public void AddEntry(AuditEntry entry) {
			entry.Source = this;
			entry.EvaluateParameters();
			_entries.Add(entry);
		}

		public void ClearEverything() {
			_playerTable.Clear();
			_itemTable.Clear();
			_componentTable.Clear();

			_entries.Clear();
			_lastCount = 0;
		}

		public static void DeserializeOne<T>(BinaryReader reader, ref T instance) where T : AuditFile {
			try {
				NetHelper.Report(false, "[AUDIT]   Deserializing player table...");
				AuditPlayerTable.DeserializeOne(reader, ref instance._playerTable);

				NetHelper.Report(false, "[AUDIT]   Deserializing item table...");
				AuditItemTable.DeserializeOne(reader, ref instance._itemTable);

				NetHelper.Report(false, "[AUDIT]   Deserializing component table...");
				AuditComponentTable.DeserializeOne(reader, ref instance._componentTable);

				int count = reader.ReadInt32();

				NetHelper.Report(false, $"[AUDIT]   Deserializing {count} audit entries...");

				instance._entries.Clear();
				for (int i = 0; i < count; i++) {
					var entry = instance.DeserializeEntry(reader);

					NetHelper.Report(false, $"[AUDIT]     {entry.NetRepresentation()}");

					instance._entries.Add(entry);
				}
			} catch (Exception ex) {
				MagicStorageMod.Instance.Logger.Error("Failed to deserialize audit file", ex);
				instance._playerTable = new();
				instance._itemTable = new();
				instance._componentTable = new();
				instance._entries.Clear();
			} finally {
				instance._lastCount = instance._entries.Count;
			}
		}

		private AuditEntry DeserializeEntry(BinaryReader reader) {
			AuditAction action = (AuditAction)reader.ReadByte();

			AuditEntry entry = action switch {
				AuditAction.DepositOne => new DepositOne(),
				AuditAction.DepositMany => new DepositMany(),
				AuditAction.WithdrawOne => new WithdrawOne(),
				AuditAction.WithdrawMany => new WithdrawMany(),
				AuditAction.UnitDeactivate => new StorageUnitDeactivation(),
				AuditAction.UnitActivate => new StorageUnitActivation(),
				AuditAction.UnitCoreRemove => new StorageUnitCoreRemoval(),
				AuditAction.UnitCoreInsert => new StorageUnitCoreInsertion(),
				AuditAction.SellItems => new StorageControlSellItems(),
				AuditAction.DestroyItem => new StorageControlDeleteItem(),
				AuditAction.CraftRequest => new CraftRequest(),
				AuditAction.ControlDeleteUnloadedItems => new StorageControlDeleteUnloadedItems(),
				AuditAction.ControlDeleteUnloadedData => new StorageControlDeleteUnloadedData(),
				AuditAction.LinkRemoteAccess => new LinkRemoteAccess(),
				AuditAction.LinkPortableAccess => new LinkPortableAccess(),
				AuditAction.SecurityNetworkAssignment => new SecurityNetworkAssignment(),
				AuditAction.SecurityNetworkModification => new SecurityNetworkModification(),
				AuditAction.SecurityNetworkDelete => new SecurityNetworkDeletion(),
				AuditAction.SecurityNetworkJoin => new SecurityNetworkJoin(),
				AuditAction.ControlCompactCoins => new StorageControlCompactCoins(),
				AuditAction.StatusServerAdmin => new StatusAdministratorAssignment(),
				AuditAction.StatusServerOperatorGranted => new StatusOperatorAssignment(),
				AuditAction.StatusServerOperatorRemoved => new StatusOperatorRemoval(),
				_ => throw new ArgumentOutOfRangeException($"Audit action ID ({action}) was outside the range of expected values"),
			};

			entry.Source = this;
			entry.Deserialize(reader);
			entry.EvaluateParameters();

			return entry;
		}

		public void Serialize(BinaryWriter writer) {
			try {
				NetHelper.Report(false, "[AUDIT]   Serializing player table...");
				_playerTable.Serialize(writer);

				NetHelper.Report(false, "[AUDIT]   Serializing item table...");
				_itemTable.Serialize(writer);

				NetHelper.Report(false, "[AUDIT]   Serializing component table...");
				_componentTable.Serialize(writer);

				NetHelper.Report(false, $"[AUDIT]   Serializing {_entries.Count} audit entries...");

				writer.Write(_entries.Count);
				foreach (AuditEntry entry in _entries) {
					// Ensure that the entry is ready for serialization
					entry.EvaluateParameters();

					NetHelper.Report(false, $"[AUDIT]     {entry.NetRepresentation()}");

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
