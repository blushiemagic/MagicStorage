using MagicStorage.Components;
using MagicStorage.Items;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace MagicStorage.Common.Systems.Auditing {
	internal abstract class AuditEntry {
		public readonly AuditFile source;
		public DateTime Timestamp { get; private set; } = DateTime.UtcNow;

		public int PlayerIndex { get; private set; }

		public abstract AuditAction Action { get; }

		public AuditEntry(AuditFile source) {
			this.source = source;
		}

		public AuditEntry(AuditFile source, Player player) {
			this.source = source;
			PlayerIndex = source.Players.Add(player);
		}

		public virtual void Deserialize(BinaryReader reader) {
			Timestamp = DateTime.FromBinary(reader.ReadInt64());
			PlayerIndex = reader.ReadUInt16();
		}

		public virtual void Serialize(BinaryWriter writer) {
			writer.Write(Timestamp.ToBinary());
			writer.Write((ushort)PlayerIndex);
		}

		public sealed override string ToString() {
			StringBuilder sb = new();
			Stringify(sb);
			return new StringBuilder($"[{Timestamp:yyyy-MM-dd HH:mm:ss}] \"{source.Players.GetNameFromIndex(PlayerIndex)}\", {Action}").Append(sb).ToString();
		}

		protected virtual void Stringify(StringBuilder builder) { }

		public abstract class TargetStorage : AuditEntry {
			public Point16 Location { get; private set; }

			public TargetStorage(AuditFile source) : base(source) { }

			public TargetStorage(AuditFile source, Player player, Point16 location) : base(source, player) {
				Location = location;
			}

			public override void Deserialize(BinaryReader reader) {
				base.Deserialize(reader);
				Location = reader.ReadPoint16();
			}

			public override void Serialize(BinaryWriter writer) {
				base.Serialize(writer);
				writer.Write(Location);
			}

			protected override void Stringify(StringBuilder builder) {
				base.Stringify(builder);
				Utility.ConvertToGPSCoordinates(Location, out int compass, out int depth);
				Utility.GetGPSText(compass, depth, out string compassText, out string depthText);
				builder.Append($" at {compassText}, {depthText}");
			}

			public static class ItemAction {
				public abstract class One : TargetStorage {
					public AuditItem Item { get; private set; }

					public One(AuditFile source) : base(source) { }

					public One(AuditFile source, Player player, Point16 location, Item item) : base(source, player, location) {
						Item = AuditItem.CreateAndLink(item, source);
					}

					public One(AuditFile source, Player player, Point16 location, ReducedItem item) : base(source, player, location) {
						Item = AuditItem.CreateAndLink(item, source);
					}

					public override void Deserialize(BinaryReader reader) {
						base.Deserialize(reader);
						Item = AuditItem.DeserializeOne(reader, source);
					}
					public override void Serialize(BinaryWriter writer) {
						base.Serialize(writer);
						Item.Serialize(writer, source);
					}

					protected override void Stringify(StringBuilder builder) {
						base.Stringify(builder);
						if (Item is not null) {
							builder.Append($" with {source.Items.GetNameFromIndex(Item.index)}");
							if (Item.stack > 1)
								builder.Append($" ({Item.stack})");
						} else
							builder.Append(" with an unknown item");
					}
				}

				public abstract class Many : TargetStorage {
					private AuditItem[] _items;
					public IEnumerable<AuditItem> Items => _items.AsReadOnly();

					public Many(AuditFile source) : base(source) { }

					public Many(AuditFile source, Player player, Point16 location, ReadOnlySpan<Item> items) : base(source, player, location) {
						CreateAudits(items, ref _items);
					}

					public Many(AuditFile source, Player player, Point16 location, ReadOnlySpan<ReducedItem> items) : base(source, player, location) {
						CreateAudits(items, ref _items);
					}

					protected void CreateAudits(ReadOnlySpan<Item> collection, ref AuditItem[] target) {
						target = new AuditItem[collection.Length];
						for (int i = 0; i < collection.Length; i++)
							target[i] = AuditItem.CreateAndLink(collection[i], source);
					}

					protected void CreateAudits(ReadOnlySpan<ReducedItem> collection, ref AuditItem[] target) {
						target = new AuditItem[collection.Length];
						for (int i = 0; i < collection.Length; i++)
							target[i] = AuditItem.CreateAndLink(collection[i], source);
					}

					public override void Deserialize(BinaryReader reader) {
						base.Deserialize(reader);
						DeserializeItems(reader, ref _items);
					}

					protected void DeserializeItems(BinaryReader reader, ref AuditItem[] collection) {
						int count = reader.Read7BitEncodedInt();
						collection = new AuditItem[count];
						for (int i = 0; i < count; i++)
							collection[i] = AuditItem.DeserializeOne(reader, source);
					}

					public override void Serialize(BinaryWriter writer) {
						base.Serialize(writer);
						SerializeItems(writer, _items);
					}

					protected void SerializeItems(BinaryWriter writer, AuditItem[] collection) {
						writer.Write7BitEncodedInt(collection.Length);
						foreach (var item in collection)
							item.Serialize(writer, source);
					}

					protected override void Stringify(StringBuilder builder) {
						base.Stringify(builder);
						if (_items is not null) {
							if (_items.Length == 0)
								builder.Append(" with an empty item collection");
							else {
								builder.Append($" with {_items.Length} item");
								if (_items.Length > 1)
									builder.Append('s');
								builder.Append(": ");

								for (int i = 0; i < _items.Length; i++) {
									if (i > 0)
										builder.Append(", ");
									builder.Append(source.Items.GetNameFromIndex(_items[i].index));
									if (_items[i].stack > 1)
										builder.Append($" ({_items[i].stack})");
								}
							}
						} else
							builder.Append(" with an unknown item collection");
					}
				}
			}

			public abstract class ComponentAction : TargetStorage {
				public int ComponentIndex { get; private set; }

				public ComponentAction(AuditFile source) : base(source) { }

				protected ComponentAction(AuditFile source, Player player, TEStorageComponent component) : base(source, player, component.Position) {
					ComponentIndex = source.Components.Add(component);
				}

				public override void Deserialize(BinaryReader reader) {
					base.Deserialize(reader);
					ComponentIndex = reader.ReadUInt16();
				}

				public override void Serialize(BinaryWriter writer) {
					base.Serialize(writer);
					writer.Write((ushort)ComponentIndex);
				}

				protected override void Stringify(StringBuilder builder) {
					base.Stringify(builder);
					if (source.Components.GetNameFromIndex(ComponentIndex) is string name)
						builder.Append($" on {name}");
					else
						builder.Append(" on an unknown component");
				}

				public static class StorageUnit {
					public abstract class Core : ComponentAction {
						public AuditItem Item { get; private set; }

						public Core(AuditFile source) : base(source) { }

						protected Core(AuditFile source, Player player, TEStorageUnit target, BaseStorageCore item) : base(source, player, target) {
							Item = AuditItem.CreateAndLink(item.Item, source);
						}

						protected Core(AuditFile source, Player player, TEStorageUnit target, ReducedItem item) : base(source, player, target) {
							Item = AuditItem.CreateAndLink(item, source);
						}

						public override void Deserialize(BinaryReader reader) {
							base.Deserialize(reader);
							Item = AuditItem.DeserializeOne(reader, source);
						}

						public override void Serialize(BinaryWriter writer) {
							base.Serialize(writer);
							Item.Serialize(writer, source);
						}

						protected override void Stringify(StringBuilder builder) {
							base.Stringify(builder);
							if (Item is not null) {
								builder.Append($" with {source.Items.GetNameFromIndex(Item.index)}");
								if (Item.stack > 1)
									builder.Append($" ({Item.stack})");
							} else
								builder.Append(" with an unknown item");
						}
					}
				}
			}
		}

		public abstract class Security : AuditEntry {
			public int NetworkID { get; private set; }

			public Security(AuditFile source) : base(source) { }

			protected Security(AuditFile source, Player player, int networkID) : base(source, player) {
				NetworkID = networkID;
			}

			public override void Deserialize(BinaryReader reader) {
				base.Deserialize(reader);
				NetworkID = reader.ReadInt32();
			}

			public override void Serialize(BinaryWriter writer) {
				base.Serialize(writer);
				writer.Write(NetworkID);
			}

			protected override void Stringify(StringBuilder builder) {
				base.Stringify(builder);
				builder.Append($" using network ID {NetworkID}");
			}

			public abstract class WithStorageHeart : Security {
				public Point16 Location { get; private set; }

				public WithStorageHeart(AuditFile source) : base(source) { }

				protected WithStorageHeart(AuditFile source, Player player, TEStorageHeart heart, int networkID) : base(source, player, networkID) {
					Location = heart.Position;
				}

				public override void Deserialize(BinaryReader reader) {
					base.Deserialize(reader);
					Location = reader.ReadPoint16();
				}

				public override void Serialize(BinaryWriter writer) {
					base.Serialize(writer);
					writer.Write(Location);
				}

				protected override void Stringify(StringBuilder builder) {
					base.Stringify(builder);
					Utility.ConvertToGPSCoordinates(Location, out int compass, out int depth);
					Utility.GetGPSText(compass, depth, out string compassText, out string depthText);
					builder.Append($" on the StorageHeart at {compassText}, {depthText}");
				}
			}
		}

		public static class TargetPlayer {
			public abstract class Status : AuditEntry {
				public abstract bool StatusAdded { get; }

				public abstract bool Administrator { get; }

				public Status(AuditFile source) : base(source) { }

				public Status(AuditFile source, Player player) : base(source, player) { }
			}
		}
	}

	// Implementations

	internal class DepositOne : AuditEntry.TargetStorage.ItemAction.One {
		public override AuditAction Action => AuditAction.DepositOne;

		public DepositOne(AuditFile source) : base(source) { }

		public DepositOne(AuditFile source, Player player, TEStorageHeart heart, Item item) : base(source, player, heart.Position, item) { }

		public DepositOne(AuditFile source, Player player, TEStorageHeart heart, ReducedItem item) : base(source, player, heart.Position, item) { }
	}

	internal class DepositMany : AuditEntry.TargetStorage.ItemAction.Many {
		public override AuditAction Action => AuditAction.DepositMany;

		public DepositMany(AuditFile source) : base(source) { }

		public DepositMany(AuditFile source, Player player, TEStorageHeart heart, ReadOnlySpan<Item> items) : base(source, player, heart.Position, items) { }

		public DepositMany(AuditFile source, Player player, TEStorageHeart heart, ReadOnlySpan<ReducedItem> items) : base(source, player, heart.Position, items) { }
	}

	internal class WithdrawOne : AuditEntry.TargetStorage.ItemAction.One {
		public override AuditAction Action => AuditAction.WithdrawOne;

		public WithdrawOne(AuditFile source) : base(source) { }

		public WithdrawOne(AuditFile source, Player player, TEStorageHeart heart, Item item) : base(source, player, heart.Position, item) { }

		public WithdrawOne(AuditFile source, Player player, TEStorageHeart heart, ReducedItem item) : base(source, player, heart.Position, item) { }
	}

	internal class WithdrawMany : AuditEntry.TargetStorage.ItemAction.Many {
		public override AuditAction Action => AuditAction.WithdrawMany;

		public WithdrawMany(AuditFile source) : base(source) { }

		public WithdrawMany(AuditFile source, Player player, TEStorageHeart heart, ReadOnlySpan<Item> items) : base(source, player, heart.Position, items) { }

		public WithdrawMany(AuditFile source, Player player, TEStorageHeart heart, ReadOnlySpan<ReducedItem> items) : base(source, player, heart.Position, items) { }
	}

	internal class StorageUnitDeactivation : AuditEntry.TargetStorage.ComponentAction {
		public override AuditAction Action => AuditAction.UnitDeactivate;

		public StorageUnitDeactivation(AuditFile source) : base(source) { }

		public StorageUnitDeactivation(AuditFile source, Player player, TEAbstractStorageUnit unit) : base(source, player, unit) { }
	}

	internal class StorageUnitActivation : AuditEntry.TargetStorage.ComponentAction {
		public override AuditAction Action => AuditAction.UnitActivate;

		public StorageUnitActivation(AuditFile source) : base(source) { }

		public StorageUnitActivation(AuditFile source, Player player, TEAbstractStorageUnit unit) : base(source, player, unit) { }
	}

	internal class StorageUnitCoreRemoval : AuditEntry.TargetStorage.ComponentAction.StorageUnit.Core {
		public override AuditAction Action => AuditAction.UnitCoreRemove;

		public StorageUnitCoreRemoval(AuditFile source) : base(source) { }

		public StorageUnitCoreRemoval(AuditFile source, Player player, TEStorageUnit target, BaseStorageCore item) : base(source, player, target, item) { }

		public StorageUnitCoreRemoval(AuditFile source, Player player, TEStorageUnit target, ReducedItem item) : base(source, player, target, item) { }
	}

	internal class StorageUnitCoreInsertion : AuditEntry.TargetStorage.ComponentAction.StorageUnit.Core {
		public override AuditAction Action => AuditAction.UnitCoreInsert;

		public StorageUnitCoreInsertion(AuditFile source) : base(source) { }

		public StorageUnitCoreInsertion(AuditFile source, Player player, TEStorageUnit target, BaseStorageCore item) : base(source, player, target, item) { }

		public StorageUnitCoreInsertion(AuditFile source, Player player, TEStorageUnit target, ReducedItem item) : base(source, player, target, item) { }
	}

	internal class StorageControlSellItems : AuditEntry.TargetStorage {
		public int SoldItemCount { get; private set; }
		public long TotalSellValue { get; private set; }

		public override AuditAction Action => AuditAction.SellItems;

		public StorageControlSellItems(AuditFile source) : base(source) { }

		public StorageControlSellItems(AuditFile source, Player player, TEStorageHeart heart, int soldItemCount, long totalSellValue) : base(source, player, heart.Position) {
			SoldItemCount = soldItemCount;
			TotalSellValue = totalSellValue;
		}

		public override void Deserialize(BinaryReader reader) {
			base.Deserialize(reader);
			SoldItemCount = reader.Read7BitEncodedInt();
			TotalSellValue = reader.Read7BitEncodedInt64();
		}

		public override void Serialize(BinaryWriter writer) {
			base.Serialize(writer);
			writer.Write7BitEncodedInt(SoldItemCount);
			writer.Write7BitEncodedInt64(TotalSellValue);
		}
	}

	internal class StorageControlDeleteItem : AuditEntry.TargetStorage.ItemAction.One {
		public override AuditAction Action => AuditAction.DestroyItem;

		public StorageControlDeleteItem(AuditFile source) : base(source) { }

		public StorageControlDeleteItem(AuditFile source, Player player, TEStorageHeart heart, Item item) : base(source, player, heart.Position, item) { }

		public StorageControlDeleteItem(AuditFile source, Player player, TEStorageHeart heart, ReducedItem item) : base(source, player, heart.Position, item) { }
	}

	internal class CraftRequest : AuditEntry.TargetStorage.ItemAction.Many {
		private AuditItem[] _consumedMaterials;

		public IEnumerable<AuditItem> ConsumedMaterials => [.. _consumedMaterials];

		public override AuditAction Action => AuditAction.CraftRequest;

		public CraftRequest(AuditFile source) : base(source) { }

		public CraftRequest(AuditFile source, Player player, TEStorageHeart heart, ReadOnlySpan<Item> results, ReadOnlySpan<Item> consumedMaterials) : base(source, player, heart.Position, results) {
			CreateAudits(consumedMaterials, ref _consumedMaterials);
		}

		public CraftRequest(AuditFile source, Player player, TEStorageHeart heart, ReadOnlySpan<ReducedItem> results, ReadOnlySpan<ReducedItem> consumedMaterials) : base(source, player, heart.Position, results) {
			CreateAudits(consumedMaterials, ref _consumedMaterials);
		}

		public override void Deserialize(BinaryReader reader) {
			base.Deserialize(reader);
			DeserializeItems(reader, ref _consumedMaterials);
		}

		public override void Serialize(BinaryWriter writer) {
			base.Serialize(writer);
			SerializeItems(writer, _consumedMaterials);
		}
	}

	internal class StorageControlDeleteUnloadedItems : AuditEntry.TargetStorage {
		public int ItemsDeleted { get; private set; }

		public override AuditAction Action => AuditAction.ControlDeleteUnloadedItems;

		public StorageControlDeleteUnloadedItems(AuditFile source) : base(source) { }

		public StorageControlDeleteUnloadedItems(AuditFile source, Player player, TEStorageHeart heart, int count) : base(source, player, heart.Position) {
			ItemsDeleted = count;
		}

		public override void Deserialize(BinaryReader reader) {
			base.Deserialize(reader);
			ItemsDeleted = reader.Read7BitEncodedInt();
		}

		public override void Serialize(BinaryWriter writer) {
			base.Serialize(writer);
			writer.Write7BitEncodedInt(ItemsDeleted);
		}
	}

	internal class StorageControlDeleteUnloadedData : AuditEntry.TargetStorage {
		public int ItemsModified { get; private set; }

		public override AuditAction Action => AuditAction.ControlDeleteUnloadedData;

		public StorageControlDeleteUnloadedData(AuditFile source) : base(source) { }

		public StorageControlDeleteUnloadedData(AuditFile source, Player player, TEStorageHeart heart, int count) : base(source, player, heart.Position) {
			ItemsModified = count;
		}

		public override void Deserialize(BinaryReader reader) {
			base.Deserialize(reader);
			ItemsModified = reader.Read7BitEncodedInt();
		}

		public override void Serialize(BinaryWriter writer) {
			base.Serialize(writer);
			writer.Write7BitEncodedInt(ItemsModified);
		}
	}

	internal class LinkRemoteAccess : AuditEntry.TargetStorage.ComponentAction {
		public Point16 HeartLocation { get; private set; }

		public override AuditAction Action => AuditAction.LinkRemoteAccess;

		public LinkRemoteAccess(AuditFile source) : base(source) { }

		public LinkRemoteAccess(AuditFile source, Player player, TEStorageHeart heart, TERemoteAccess access) : base(source, player, access) {
			HeartLocation = heart.Position;
		}

		public override void Deserialize(BinaryReader reader) {
			base.Deserialize(reader);
			HeartLocation = reader.ReadPoint16();
		}

		public override void Serialize(BinaryWriter writer) {
			base.Serialize(writer);
			writer.Write(HeartLocation);
		}
	}

	internal class LinkPortableAccess : AuditEntry.TargetStorage.ItemAction.One {
		public override AuditAction Action => AuditAction.LinkPortableAccess;

		public LinkPortableAccess(AuditFile source) : base(source) { }

		public LinkPortableAccess(AuditFile source, Player player, TECraftingAccess access, PortableCraftingAccess item) : base(source, player, access.Position, item.Item) { }

		public LinkPortableAccess(AuditFile source, Player player, TECraftingAccess access, ReducedItem item) : base(source, player, access.Position, item) { }

		public LinkPortableAccess(AuditFile source, Player player, TEStorageHeart heart, PortableAccess item) : base(source, player, heart.Position, item.Item) { }

		public LinkPortableAccess(AuditFile source, Player player, TEStorageHeart heart, ReducedItem item) : base(source, player, heart.Position, item) { }
	}

	internal class SecurityNetworkAssignment : AuditEntry.Security.WithStorageHeart {
		public override AuditAction Action => AuditAction.SecurityNetworkAssignment;

		public SecurityNetworkAssignment(AuditFile source) : base(source) { }

		public SecurityNetworkAssignment(AuditFile source, Player player, TEStorageHeart heart, int networkID) : base(source, player, heart, networkID) { }
	}

	internal class SecurityNetworkModification : AuditEntry.Security {
		private class State {
			public bool restricted;
			public string password;
		}

		private State _previous = new(), _current = new();

		public bool PreviousPrivate => _previous.restricted;

		public bool CurrentPrivate => _current.restricted;

		public string PreviousPassword => _previous.password;

		public string CurrentPassword => _current.password;

		public override AuditAction Action => AuditAction.SecurityNetworkModification;

		public SecurityNetworkModification(AuditFile source) : base(source) { }

		public SecurityNetworkModification(AuditFile source, Player player, int networkID, string oldPassword, string newPassword, bool oldRestricted, bool newRestricted) : base(source, player, networkID) {
			_previous.password = oldPassword;
			_current.password = newPassword;
			_previous.restricted = oldRestricted;
			_current.restricted = newRestricted;
		}

		public override void Deserialize(BinaryReader reader) {
			base.Deserialize(reader);
			
			BitsByte bb = reader.ReadByte();
			bool passwordChanged = false, hasPreviousPassword = false, hasCurrentPassword = false;
			bb.Retrieve(ref _previous.restricted, ref _current.restricted, ref passwordChanged, ref hasPreviousPassword, ref hasCurrentPassword);

			_previous.password = hasPreviousPassword ? StringScrambling.Unscramble(reader.ReadBytes(reader.Read7BitEncodedInt())) : null;
			_current.password = !hasCurrentPassword ? null : passwordChanged ? StringScrambling.Unscramble(reader.ReadBytes(reader.Read7BitEncodedInt())) : _previous.password;
		}

		public override void Serialize(BinaryWriter writer) {
			base.Serialize(writer);

			bool hasPreviousPassword = _previous.password is not null;
			bool hasCurrentPassword = _current.password is not null;
			bool passwordChanged = _previous.password != _current.password;
			writer.Write(new BitsByte(_previous.restricted, _current.restricted, passwordChanged, hasPreviousPassword, hasCurrentPassword));

			if (hasPreviousPassword) {
				writer.Write7BitEncodedInt(_previous.password.Length);
				writer.Write(StringScrambling.Scramble(_previous.password));
			}

			if (hasCurrentPassword && passwordChanged) {
				writer.Write7BitEncodedInt(_current.password.Length);
				writer.Write(StringScrambling.Scramble(_current.password));
			}
		}
	}

	internal class SecurityNetworkDeletion : AuditEntry.Security {
		public override AuditAction Action => AuditAction.SecurityNetworkDelete;

		public SecurityNetworkDeletion(AuditFile source) : base(source) { }

		public SecurityNetworkDeletion(AuditFile source, Player player, int networkID) : base(source, player, networkID) { }
	}

	internal class SecurityNetworkJoin : AuditEntry.Security {
		public override AuditAction Action => AuditAction.SecurityNetworkJoin;

		public SecurityNetworkJoin(AuditFile source) : base(source) { }

		public SecurityNetworkJoin(AuditFile source, Player player, int networkID) : base(source, player, networkID) { }
	}

	internal class StorageControlCompactCoins : AuditEntry.TargetStorage {
		public override AuditAction Action => AuditAction.ControlCompactCoins;

		public StorageControlCompactCoins(AuditFile source) : base(source) { }

		public StorageControlCompactCoins(AuditFile source, Player player, TEStorageHeart heart) : base(source, player, heart.Position) { }
	}

	internal class StatusAdministratorAssignment : AuditEntry.TargetPlayer.Status {
		public override bool StatusAdded => true;

		public override bool Administrator => true;

		public override AuditAction Action => AuditAction.StatusServerAdmin;

		public StatusAdministratorAssignment(AuditFile source) : base(source) { }

		public StatusAdministratorAssignment(AuditFile source, Player player) : base(source, player) { }
	}

	internal class StatusOperatorAssignment : AuditEntry.TargetPlayer.Status {
		public override bool StatusAdded => true;

		public override bool Administrator => false;

		public override AuditAction Action => AuditAction.StatusServerOperatorGranted;

		public StatusOperatorAssignment(AuditFile source) : base(source) { }

		public StatusOperatorAssignment(AuditFile source, Player player) : base(source, player) { }
	}

	internal class StatusOperatorRemoval : AuditEntry.TargetPlayer.Status {
		public override bool StatusAdded => false;

		public override bool Administrator => false;

		public override AuditAction Action => AuditAction.StatusServerOperatorRemoved;

		public StatusOperatorRemoval(AuditFile source) : base(source) { }

		public StatusOperatorRemoval(AuditFile source, Player player) : base(source, player) { }
	}
}
