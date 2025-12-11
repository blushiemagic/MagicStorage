using MagicStorage.Components;
using MagicStorage.Items;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Terraria;
using Terraria.DataStructures;

namespace MagicStorage.Common.Systems.Auditing {
	internal abstract class AuditEntry {
		private readonly List<LazyParameter> _parameters = [];

		public AuditFile Source { get; set; }

		public int ParameterCount => _parameters.Count;

		public DateTime Timestamp { get; private set; } = DateTime.UtcNow;

		private LazyParameter<int> _playerParameter;
		public int PlayerIndex => _playerParameter.Value;

		public abstract AuditAction Action { get; }

		public AuditEntry() => _playerParameter = CreateParameter(-1);

		public AuditEntry(Player player) => _playerParameter = CreateParameter(new AuditPlayer(player), -1, static (file, obj) => file.Players.Add(obj));

		public void EvaluateParameters() {
			if (Source is AuditFile source) {
				foreach (var parameter in _parameters)
					parameter.Evaluate(source);
			}
		}

		protected LazyParameter<T> CreateParameter<T>() {
			var parameter = new FixedParameter<T>(default);
			_parameters.Add(parameter);
			return parameter;
		}

		protected LazyParameter<T> CreateParameter<T>(T value) {
			var parameter = new FixedParameter<T>(value);
			_parameters.Add(parameter);
			return parameter;
		}

		protected LazyParameter<T> CreateParameter<T>(Func<AuditFile, T> valueProvider) {
			var parameter = new LazyParameter<T>(default, valueProvider);
			_parameters.Add(parameter);
			return parameter;
		}

		protected LazyParameter<T> CreateParameter<T>(T defaultValue, Func<AuditFile, T> valueProvider) {
			var parameter = new LazyParameter<T>(defaultValue, valueProvider);
			_parameters.Add(parameter);
			return parameter;
		}

		protected LazyParameter<TSource, TValue> CreateParameter<TSource, TValue>(TSource source, Func<AuditFile, TSource, TValue> valueProvider) {
			var parameter = new LazyParameter<TSource, TValue>(default, source, valueProvider);
			_parameters.Add(parameter);
			return parameter;
		}

		protected LazyParameter<TSource, TValue> CreateParameter<TSource, TValue>(TSource source, TValue defaultValue, Func<AuditFile, TSource, TValue> valueProvider) {
			var parameter = new LazyParameter<TSource, TValue>(defaultValue, source, valueProvider);
			_parameters.Add(parameter);
			return parameter;
		}

		protected void ReplaceParameter<T>(ref T parameter, T newInstance) where T : LazyParameter {
			ArgumentNullException.ThrowIfNull(parameter);
			ArgumentNullException.ThrowIfNull(newInstance);

			int index = _parameters.IndexOf(parameter);
			if (index < 0)
				throw new InvalidOperationException("Original parameter was not found in the list of parameters.");

			_parameters[index] = newInstance;
			parameter = newInstance;
		}

		protected void ReplaceParameter<T>(ref LazyParameter<T> parameter, T value) => ReplaceParameter(ref parameter, new FixedParameter<T>(value));

		public virtual void Deserialize(BinaryReader reader) {
			Timestamp = DateTime.FromBinary(reader.ReadInt64());
			ReplaceParameter(ref _playerParameter, reader.ReadUInt16());
		}

		public virtual void Serialize(BinaryWriter writer) {
			writer.Write(Timestamp.ToBinary());
			writer.Write((ushort)PlayerIndex);
		}

		public sealed override string ToString() {
			if (Source is null)
				return "[Missing audit source, entry cannot be decoded]";

			StringBuilder sb = new();
			Stringify(Source, sb);

			string playerName = PlayerIndex >= 0 ? Source.Players.GetNameFromIndex(PlayerIndex) : "Unknown Player";

			return new StringBuilder($"[{Timestamp:yyyy-MM-dd HH:mm:ss}] \"{playerName}\", {Action}").Append(sb).ToString();
		}

		protected virtual void Stringify(AuditFile source, StringBuilder builder) { }

		internal string NetRepresentation() {
			StringBuilder sb = new();
			NetStringify(sb);

			return new StringBuilder($"{Action} | timestamp {Timestamp:yyyy-MM-dd HH:mm:ss}, player table {PlayerIndex}").Append(sb).ToString();
		}

		protected virtual void NetStringify(StringBuilder builder) { }

		public abstract class TargetStorage : AuditEntry {
			public Point16 Location { get; private set; } = Point16.NegativeOne;  // Default to an invalid location

			public TargetStorage() : base() { }

			public TargetStorage(Player player, Point16 location) : base(player) => Location = location;

			public override void Deserialize(BinaryReader reader) {
				base.Deserialize(reader);
				Location = reader.ReadPoint16();
			}

			public override void Serialize(BinaryWriter writer) {
				base.Serialize(writer);
				writer.Write(Location);
			}

			protected override void Stringify(AuditFile source, StringBuilder builder) {
				base.Stringify(source, builder);

				if (Location.X >= 0 && Location.Y >= 0) {
					Utility.ConvertToGPSCoordinates(Location, out int compass, out int depth);
					Utility.GetGPSText(compass, depth, out string compassText, out string depthText);
					builder.Append($" at {compassText}, {depthText}");
				} else
					builder.Append(" at an unknown location");
			}

			protected override void NetStringify(StringBuilder builder) {
				base.NetStringify(builder);

				builder.Append($", location {Location}");
			}

			public static class ItemAction {
				public abstract class One : TargetStorage {
					private LazyParameter<AuditItem> _itemParameter;
					public AuditItem Item => _itemParameter.Value;

					public One() : base() => _itemParameter = CreateParameter<AuditItem>();

					public One(Player player, Point16 location, Item item) : base(player, location) => _itemParameter = CreateParameter(item, AuditItem.CreateAndLink);

					public One(Player player, Point16 location, ReducedItem item) : base(player, location) => _itemParameter = CreateParameter(item, AuditItem.CreateAndLink);

					public override void Deserialize(BinaryReader reader) {
						base.Deserialize(reader);
						ReplaceParameter(ref _itemParameter, AuditItem.DeserializeOne(reader));
					}

					public override void Serialize(BinaryWriter writer) {
						base.Serialize(writer);
						Item.Serialize(writer);
					}

					protected override void Stringify(AuditFile source, StringBuilder builder) {
						base.Stringify(source, builder);

						if (Item is AuditItem netItem) {
							builder.Append($" with {Source.Items.GetNameFromIndex(netItem.index)}");
							if (netItem.stack > 1)
								builder.Append($" ({netItem.stack})");
						} else
							builder.Append(" with an unknown item");
					}

					protected override void NetStringify(StringBuilder builder) {
						base.NetStringify(builder);

						builder.Append($", item table {Item.index} stack {Item.stack}");
					}
				}

				public abstract class Many : TargetStorage {
					private LazyParameter<AuditItem[]> _itemsParameter;
					public IEnumerable<AuditItem> Items => _itemsParameter.Value.AsReadOnly();

					public Many() : base() => _itemsParameter = CreateParameter<AuditItem[]>();

					public Many(Player player, Point16 location, Item[] items) : base(player, location) => _itemsParameter = CreateParameter(items, CreateAudits);

					public Many(Player player, Point16 location, ReducedItem[] items) : base(player, location) => _itemsParameter = CreateParameter(items, CreateAudits);

					protected static AuditItem[] CreateAudits(AuditFile source, Item[] collection) {
						var items = new AuditItem[collection.Length];
						for (int i = 0; i < collection.Length; i++)
							items[i] = AuditItem.CreateAndLink(source, collection[i]);
						return items;
					}

					protected static AuditItem[] CreateAudits(AuditFile source, ReducedItem[] collection) {
						var items = new AuditItem[collection.Length];
						for (int i = 0; i < collection.Length; i++)
							items[i] = AuditItem.CreateAndLink(source, collection[i]);
						return items;
					}

					public override void Deserialize(BinaryReader reader) {
						base.Deserialize(reader);
						ReplaceParameter(ref _itemsParameter, DeserializeItems(reader));
					}

					protected static AuditItem[] DeserializeItems(BinaryReader reader) {
						int count = reader.Read7BitEncodedInt();
						var items = new AuditItem[count];
						for (int i = 0; i < count; i++)
							items[i] = AuditItem.DeserializeOne(reader);
						return items;
					}

					public override void Serialize(BinaryWriter writer) {
						base.Serialize(writer);
						SerializeItems(writer, _itemsParameter.Value);
					}

					protected static void SerializeItems(BinaryWriter writer, AuditItem[] collection) {
						writer.Write7BitEncodedInt(collection.Length);
						foreach (var item in collection)
							item.Serialize(writer);
					}

					protected override void Stringify(AuditFile source, StringBuilder builder) {
						base.Stringify(source, builder);

						if (_itemsParameter.Value is AuditItem[] items) {
							if (items.Length == 0)
								builder.Append(" with an empty item collection");
							else {
								builder.Append($" with {items.Length} item");
								if (items.Length > 1)
									builder.Append('s');
								builder.Append(": ");

								for (int i = 0; i < items.Length; i++) {
									if (i > 0)
										builder.Append(", ");
									builder.Append(source.Items.GetNameFromIndex(items[i].index));
									if (items[i].stack > 1)
										builder.Append($" ({items[i].stack})");
								}
							}
						} else
							builder.Append(" with an unknown item collection");
					}

					protected override void NetStringify(StringBuilder builder) {
						base.NetStringify(builder);

						var items = _itemsParameter.Value;
						builder.Append($", item count {items.Length} items (");

						for (int i = 0; i < items.Length; i++) {
							if (i > 0)
								builder.Append(", ");

							builder.Append($"table {items[i].index} stack {items[i].stack}");
						}

						builder.Append(')');
					}
				}
			}

			public abstract class ComponentAction : TargetStorage {
				private LazyParameter<int> _componentParameter;
				public int ComponentIndex => _componentParameter.Value;

				public ComponentAction() : base() => _componentParameter = CreateParameter(-1);

				protected ComponentAction(Player player, TEStorageComponent component) : base(player, component.Position) => _componentParameter = CreateParameter(new AuditComponent(component), -1, static (file, obj) => file.Components.Add(obj));

				public override void Deserialize(BinaryReader reader) {
					base.Deserialize(reader);
					ReplaceParameter(ref _componentParameter, reader.ReadUInt16());
				}

				public override void Serialize(BinaryWriter writer) {
					base.Serialize(writer);
					writer.Write((ushort)ComponentIndex);
				}

				protected override void Stringify(AuditFile source, StringBuilder builder) {
					base.Stringify(source, builder);

					if (ComponentIndex >= 0 && source.Components.GetNameFromIndex(ComponentIndex) is string name)
						builder.Append($" on {name}");
					else
						builder.Append(" on an unknown component");
				}

				protected override void NetStringify(StringBuilder builder) {
					base.NetStringify(builder);

					builder.Append($", component table {ComponentIndex}");
				}

				public static class StorageUnit {
					public abstract class Core : ComponentAction {
						private LazyParameter<AuditItem> _itemParameter;
						public AuditItem Item => _itemParameter.Value;

						protected Core() : base() => _itemParameter = CreateParameter<AuditItem>();

						protected Core(Player player, TEStorageUnit target, BaseStorageCore item) : base(player, target) => _itemParameter = CreateParameter(item.Item, AuditItem.CreateAndLink);

						protected Core(Player player, TEStorageUnit target, ReducedItem item) : base(player, target) => _itemParameter = CreateParameter(item, AuditItem.CreateAndLink);

						public override void Deserialize(BinaryReader reader) {
							base.Deserialize(reader);
							ReplaceParameter(ref _itemParameter, AuditItem.DeserializeOne(reader));
						}

						public override void Serialize(BinaryWriter writer) {
							base.Serialize(writer);
							Item.Serialize(writer);
						}

						protected override void Stringify(AuditFile source, StringBuilder builder) {
							base.Stringify(source, builder);

							if (Item is not null) {
								builder.Append($" with {source.Items.GetNameFromIndex(Item.index)}");
								if (Item.stack > 1)
									builder.Append($" ({Item.stack})");
							} else
								builder.Append(" with an unknown item");
						}

						protected override void NetStringify(StringBuilder builder) {
							base.NetStringify(builder);

							builder.Append($", item table {Item.index} stack {Item.stack}");
						}
					}
				}
			}
		}

		public abstract class Security : AuditEntry {
			public int NetworkID { get; private set; } = -2;

			public Security() : base() { }

			public Security(Player player, int networkID) : base(player) => NetworkID = networkID;

			public override void Deserialize(BinaryReader reader) {
				base.Deserialize(reader);
				NetworkID = reader.ReadInt32();
			}

			public override void Serialize(BinaryWriter writer) {
				base.Serialize(writer);
				writer.Write(NetworkID);
			}

			protected override void Stringify(AuditFile source, StringBuilder builder) {
				base.Stringify(source, builder);

				if (NetworkID >= -1)
					builder.Append($" using network ID {NetworkID}");
				else if (NetworkID == -1)
					builder.Append(" using the default unassigned network");
				else
					builder.Append(" using an unknown network ID");
			}

			protected override void NetStringify(StringBuilder builder) {
				base.NetStringify(builder);

				builder.Append($", network {NetworkID}");
			}

			public abstract class WithStorageHeart : Security {
				public Point16 Location { get; private set; } = Point16.NegativeOne;

				public WithStorageHeart() : base() { }

				public WithStorageHeart(Player player, TEStorageHeart heart, int networkID) : base(player, networkID) => Location = heart.Position;

				public override void Deserialize(BinaryReader reader) {
					base.Deserialize(reader);
					Location = reader.ReadPoint16();
				}

				public override void Serialize(BinaryWriter writer) {
					base.Serialize(writer);
					writer.Write(Location);
				}

				protected override void Stringify(AuditFile source, StringBuilder builder) {
					base.Stringify(source, builder);

					if (Location.X >= 0 && Location.Y >= 0) {
						Utility.ConvertToGPSCoordinates(Location, out int compass, out int depth);
						Utility.GetGPSText(compass, depth, out string compassText, out string depthText);
						builder.Append($" on the StorageHeart at {compassText}, {depthText}");
					} else
						builder.Append(" on the StorageHeart at an unknown location");
				}

				protected override void NetStringify(StringBuilder builder) {
					base.NetStringify(builder);

					builder.Append($", location {Location}");
				}
			}
		}

		public static class TargetPlayer {
			public abstract class Status : AuditEntry {
				public abstract bool StatusAdded { get; }

				public abstract bool Administrator { get; }

				public Status() : base() { }

				public Status(Player player) : base(player) { }
			}
		}
	}

	// Implementations

	internal class DepositOne : AuditEntry.TargetStorage.ItemAction.One {
		public override AuditAction Action => AuditAction.DepositOne;

		public DepositOne() : base() { }

		public DepositOne(Player player, TEStorageHeart heart, Item item) : base(	player, heart.Position, item) { }

		public DepositOne(Player player, TEStorageHeart heart, ReducedItem item) : base(player, heart.Position, item) { }
	}

	internal class DepositMany : AuditEntry.TargetStorage.ItemAction.Many {
		public override AuditAction Action => AuditAction.DepositMany;

		public DepositMany() : base() { }

		public DepositMany(Player player, TEStorageHeart heart, Item[] items) : base(player, heart.Position, items) { }

		public DepositMany(Player player, TEStorageHeart heart, ReducedItem[] items) : base(player, heart.Position, items) { }
	}

	internal class WithdrawOne : AuditEntry.TargetStorage.ItemAction.One {
		public override AuditAction Action => AuditAction.WithdrawOne;

		public WithdrawOne() : base() { }

		public WithdrawOne(Player player, TEStorageHeart heart, Item item) : base(player, heart.Position, item) { }

		public WithdrawOne(Player player, TEStorageHeart heart, ReducedItem item) : base(player, heart.Position, item) { }
	}

	internal class WithdrawMany : AuditEntry.TargetStorage.ItemAction.Many {
		public override AuditAction Action => AuditAction.WithdrawMany;

		public WithdrawMany() : base() { }

		public WithdrawMany(Player player, TEStorageHeart heart, Item[] items) : base(player, heart.Position, items) { }

		public WithdrawMany(Player player, TEStorageHeart heart, ReducedItem[] items) : base(player, heart.Position, items) { }
	}

	internal class StorageUnitDeactivation : AuditEntry.TargetStorage.ComponentAction {
		public override AuditAction Action => AuditAction.UnitDeactivate;

		public StorageUnitDeactivation() : base() { }

		public StorageUnitDeactivation(Player player, TEAbstractStorageUnit unit) : base(player, unit) { }
	}

	internal class StorageUnitActivation : AuditEntry.TargetStorage.ComponentAction {
		public override AuditAction Action => AuditAction.UnitActivate;

		public StorageUnitActivation() : base() { }

		public StorageUnitActivation(Player player, TEAbstractStorageUnit unit) : base(player, unit) { }
	}

	internal class StorageUnitCoreRemoval : AuditEntry.TargetStorage.ComponentAction.StorageUnit.Core {
		public override AuditAction Action => AuditAction.UnitCoreRemove;

		public StorageUnitCoreRemoval() : base() { }

		public StorageUnitCoreRemoval(Player player, TEStorageUnit target, BaseStorageCore item) : base(player, target, item) { }

		public StorageUnitCoreRemoval(Player player, TEStorageUnit target, ReducedItem item) : base(player, target, item) { }
	}

	internal class StorageUnitCoreInsertion : AuditEntry.TargetStorage.ComponentAction.StorageUnit.Core {
		public override AuditAction Action => AuditAction.UnitCoreInsert;

		public StorageUnitCoreInsertion() : base() { }

		public StorageUnitCoreInsertion(Player player, TEStorageUnit target, BaseStorageCore item) : base(player, target, item) { }

		public StorageUnitCoreInsertion(Player player, TEStorageUnit target, ReducedItem item) : base(player, target, item) { }
	}

	internal class StorageControlSellItems : AuditEntry.TargetStorage {
		public int SoldItemCount { get; private set; }
		public long TotalSellValue { get; private set; }

		public override AuditAction Action => AuditAction.SellItems;

		public StorageControlSellItems() : base() { }

		public StorageControlSellItems(Player player, TEStorageHeart heart, int soldItemCount, long totalSellValue) : base(player, heart.Position) {
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

		protected override void Stringify(AuditFile source, StringBuilder builder) {
			base.Stringify(source, builder);

			int[] coins = Utils.CoinsSplit(TotalSellValue);
			builder.Append($" on {SoldItemCount} items at price {coins[3]}p {coins[2]}g {coins[1]}s {coins[0]}c");
		}

		protected override void NetStringify(StringBuilder builder) {
			base.NetStringify(builder);

			int[] coins = Utils.CoinsSplit(TotalSellValue);
			builder.Append($", count {SoldItemCount}, value {coins[3]}p {coins[2]}g {coins[1]}s {coins[0]}c");
		}
	}

	internal class StorageControlDeleteItem : AuditEntry.TargetStorage.ItemAction.One {
		public override AuditAction Action => AuditAction.DestroyItem;

		public StorageControlDeleteItem() : base() { }

		public StorageControlDeleteItem(Player player, TEStorageHeart heart, Item item) : base(player, heart.Position, item) { }

		public StorageControlDeleteItem(Player player, TEStorageHeart heart, ReducedItem item) : base(player, heart.Position, item) { }
	}

	internal class CraftRequest : AuditEntry.TargetStorage.ItemAction.Many {
		private LazyParameter<AuditItem[]> _consumedMaterialsParameter;

		public IEnumerable<AuditItem> ConsumedMaterials => _consumedMaterialsParameter.Value is AuditItem[] array ? [.. array] : [];

		public override AuditAction Action => AuditAction.CraftRequest;

		public CraftRequest() : base() => _consumedMaterialsParameter = CreateParameter<AuditItem[]>();

		public CraftRequest(Player player, TEStorageHeart heart, Item[] results, Item[] consumedMaterials) : base(player, heart.Position, results) {
			_consumedMaterialsParameter = CreateParameter(consumedMaterials, CreateAudits);
		}

		public CraftRequest(Player player, TEStorageHeart heart, ReducedItem[] results, ReducedItem[] consumedMaterials) : base(player, heart.Position, results) {
			_consumedMaterialsParameter = CreateParameter(consumedMaterials, CreateAudits);
		}

		public override void Deserialize(BinaryReader reader) {
			base.Deserialize(reader);
			ReplaceParameter(ref _consumedMaterialsParameter, DeserializeItems(reader));
		}

		public override void Serialize(BinaryWriter writer) {
			base.Serialize(writer);
			SerializeItems(writer, _consumedMaterialsParameter.Value);
		}

		protected override void Stringify(AuditFile source, StringBuilder builder) {
			base.Stringify(source, builder);

			if (_consumedMaterialsParameter.Value is AuditItem[] materials) {
				if (materials.Length == 0)
					builder.Append(" and no consumed materials");
				else {
					builder.Append($" and {materials.Length} consumed material");
					if (materials.Length > 1)
						builder.Append('s');
					builder.Append(": ");

					for (int i = 0; i < materials.Length; i++) {
						if (i > 0)
							builder.Append(", ");
						builder.Append(source.Items.GetNameFromIndex(materials[i].index));
						if (materials[i].stack > 1)
							builder.Append($" ({materials[i].stack})");
					}
				}
			} else
				builder.Append(" and unknown consumed materials");
		}

		protected override void NetStringify(StringBuilder builder) {
			base.NetStringify(builder);

			var materials = _consumedMaterialsParameter.Value;
			builder.Append($", consumed item count {materials.Length} items (");
			
			for (int i = 0; i < materials.Length; i++) {
				if (i > 0)
					builder.Append(", ");
				builder.Append($"table {materials[i].index} stack {materials[i].stack}");
			}

			builder.Append(')');
		}
	}

	internal class StorageControlDeleteUnloadedItems : AuditEntry.TargetStorage {
		public int ItemsDeleted { get; private set; }

		public override AuditAction Action => AuditAction.ControlDeleteUnloadedItems;

		public StorageControlDeleteUnloadedItems() : base() { }

		public StorageControlDeleteUnloadedItems(Player player, TEStorageHeart heart, int count) : base(player, heart.Position) {
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

		protected override void Stringify(AuditFile source, StringBuilder builder) {
			base.Stringify(source, builder);

			builder.Append($" on {ItemsDeleted} unloaded item{(ItemsDeleted != 1 ? "s" : "")}");
		}

		protected override void NetStringify(StringBuilder builder) {
			base.NetStringify(builder);

			builder.Append($", count {ItemsDeleted}");
		}
	}

	internal class StorageControlDeleteUnloadedData : AuditEntry.TargetStorage {
		public int ItemsModified { get; private set; }

		public override AuditAction Action => AuditAction.ControlDeleteUnloadedData;

		public StorageControlDeleteUnloadedData() : base() { }

		public StorageControlDeleteUnloadedData(Player player, TEStorageHeart heart, int count) : base(player, heart.Position) {
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

		protected override void Stringify(AuditFile source, StringBuilder builder) {
			base.Stringify(source, builder);

			builder.Append($" on {ItemsModified} affected item{(ItemsModified != 1 ? "s" : "")}");
		}
	}

	internal class LinkRemoteAccess : AuditEntry.TargetStorage.ComponentAction {
		public Point16 HeartLocation { get; private set; }

		public override AuditAction Action => AuditAction.LinkRemoteAccess;

		public LinkRemoteAccess() : base() { }

		public LinkRemoteAccess(Player player, TEStorageHeart heart, TERemoteAccess access) : base(player, access) {
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

		protected override void Stringify(AuditFile source, StringBuilder builder) {
			base.Stringify(source, builder);

			if (HeartLocation.X >= 0 && HeartLocation.Y >= 0) {
				Utility.ConvertToGPSCoordinates(HeartLocation, out int compass, out int depth);
				Utility.GetGPSText(compass, depth, out string compassText, out string depthText);
				builder.Append($" with the StorageHeart at {compassText}, {depthText}");
			} else
				builder.Append(" with a StorageHeart at an unknown location");
		}
	}

	internal class LinkPortableAccess : AuditEntry.TargetStorage.ItemAction.One {
		public override AuditAction Action => AuditAction.LinkPortableAccess;

		public LinkPortableAccess() : base() { }

		public LinkPortableAccess(Player player, TECraftingAccess access, PortableCraftingAccess item) : base(player, access.Position, item.Item) { }

		public LinkPortableAccess(Player player, TECraftingAccess access, ReducedItem item) : base(player, access.Position, item) { }

		public LinkPortableAccess(Player player, TEStorageHeart heart, PortableAccess item) : base(player, heart.Position, item.Item) { }

		public LinkPortableAccess(Player player, TEStorageHeart heart, ReducedItem item) : base(player, heart.Position, item) { }
	}

	internal class SecurityNetworkAssignment : AuditEntry.Security.WithStorageHeart {
		public override AuditAction Action => AuditAction.SecurityNetworkAssignment;

		public SecurityNetworkAssignment() : base() { }

		public SecurityNetworkAssignment(Player player, TEStorageHeart heart, int networkID) : base(player, heart, networkID) { }
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

		public SecurityNetworkModification() : base() { }

		public SecurityNetworkModification(Player player, int networkID, string oldPassword, string newPassword, bool oldRestricted, bool newRestricted) : base(player, networkID) {
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

		protected override void Stringify(AuditFile source, StringBuilder builder) {
			base.Stringify(source, builder);

			bool privacyChanged = _previous.restricted != _current.restricted;
			bool passwordChanged = _previous.password != _current.password;

			static StringBuilder AddPasswordText(SecurityNetworkModification self, StringBuilder sb) => sb.Append($"password changed from \"{self._previous.password ?? "null"}\" to \"{self._current.password ?? "null"}\"");

			static StringBuilder AddPrivacyText(SecurityNetworkModification self, StringBuilder sb) => sb.Append($"privacy changed from {(self._previous.restricted ? "private" : "public")} to {(self._current.restricted ? "private" : "public")}");

			if (privacyChanged || passwordChanged) {
				builder.Append(" where the ");

				if (privacyChanged && passwordChanged) {
					AddPrivacyText(this, builder);
					builder.Append(" and the ");
					AddPasswordText(this, builder);
				} else if (privacyChanged)
					AddPrivacyText(this, builder);
				else if (passwordChanged)
					AddPasswordText(this, builder);
			} else
				builder.Append(" with no changes");
		}

		protected override void NetStringify(StringBuilder builder) {
			base.NetStringify(builder);

			builder.Append($", old (restricted={_previous.restricted}, password=\"{_previous.password ?? "null"}\") new (restricted={_current.restricted}, password=\"{_current.password ?? "null"}\")");
		}
	}

	internal class SecurityNetworkDeletion : AuditEntry.Security {
		public override AuditAction Action => AuditAction.SecurityNetworkDelete;

		public SecurityNetworkDeletion() : base() { }

		public SecurityNetworkDeletion(Player player, int networkID) : base(player, networkID) { }
	}

	internal class SecurityNetworkJoin : AuditEntry.Security {
		public override AuditAction Action => AuditAction.SecurityNetworkJoin;

		public SecurityNetworkJoin() : base() { }

		public SecurityNetworkJoin(Player player, int networkID) : base(player, networkID) { }
	}

	internal class StorageControlCompactCoins : AuditEntry.TargetStorage {
		public override AuditAction Action => AuditAction.ControlCompactCoins;

		public StorageControlCompactCoins() : base() { }

		public StorageControlCompactCoins(Player player, TEStorageHeart heart) : base(player, heart.Position) { }
	}

	internal class StatusAdministratorAssignment : AuditEntry.TargetPlayer.Status {
		public override bool StatusAdded => true;

		public override bool Administrator => true;

		public override AuditAction Action => AuditAction.StatusServerAdmin;

		public StatusAdministratorAssignment() : base() { }

		public StatusAdministratorAssignment(Player player) : base(player) { }
	}

	internal class StatusOperatorAssignment : AuditEntry.TargetPlayer.Status {
		public override bool StatusAdded => true;

		public override bool Administrator => false;

		public override AuditAction Action => AuditAction.StatusServerOperatorGranted;

		public StatusOperatorAssignment() : base() { }

		public StatusOperatorAssignment(Player player) : base(player) { }
	}

	internal class StatusOperatorRemoval : AuditEntry.TargetPlayer.Status {
		public override bool StatusAdded => false;

		public override bool Administrator => false;

		public override AuditAction Action => AuditAction.StatusServerOperatorRemoved;

		public StatusOperatorRemoval() : base() { }

		public StatusOperatorRemoval(Player player) : base(player) { }
	}
}
