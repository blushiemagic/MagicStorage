using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;

namespace MagicStorage
{
	public class ItemTypeOrderedSet
	{
		public static ItemTypeOrderedSet Empty => new("Empty");

		private const string Suffix = "~v2";
		private const string Suffix3 = "~v3";
		private readonly string _name;
		private List<Item> _items = new();
		private List<ItemDefinition> _unloadedItems = new();
		private HashSet<int> _set = new();

		private bool _itemListDirty;
		private HashSet<int> _pendingAdditions = new();
		private HashSet<int> _pendingRemovals = new();

		public int Count
		{
			get
			{
				if (_itemListDirty)
				{
					UpdateCollections();
					_itemListDirty = false;
				}

				return _items.Count;
			}
		}

		public IEnumerable<Item> Items
		{
			get
			{
				if (_itemListDirty)
				{
					UpdateCollections();
					_itemListDirty = false;
				}

				return _items;
			}
		}

		public int? MemoryLimit { get; init; }

		public ItemTypeOrderedSet(string name)
		{
			_name = name;
		}

		private void UpdateCollections() {
			foreach (int type in _pendingRemovals)
			{
				_pendingAdditions.Remove(type);
				_set.Remove(type);
				_items.RemoveAll(item => item.type == type);
			}

			_pendingRemovals.Clear();

			foreach (int type in _pendingAdditions)
			{
				if (MemoryLimit is { } limit)
				{
					while (_set.Count >= limit)
					{
						int toRemove = _set.First();
						_set.Remove(toRemove);
						_items.RemoveAll(item => item.type == toRemove);
					}
				}

				if (_set.Add(type))
				{
					// This is the first time we've seen this type, or it was previously removed
					_items.Add(new Item(type));
				}
				else
				{
					// Move the item to the end of the list
					List<Item> matches = _items.Where(item => item.type == type).ToList();
					_items.RemoveAll(item => item.type == type);
					_items.AddRange(matches);
				}
			}

			_pendingAdditions.Clear();
		}

		public IEnumerable<int> Get()
		{
			if (_itemListDirty)
			{
				UpdateCollections();
				_itemListDirty = false;
			}

			return [.. _set];
		}

		public bool Add(Item item) => Add(item.type);

		public bool Add(int type)
		{
			bool wasNotPresent = !Contains(type);
			_pendingRemovals.Remove(type);
			_pendingAdditions.Add(type);

			if (wasNotPresent)
				_itemListDirty = true;

			return wasNotPresent;
		}

		public bool Contains(int type) => !_pendingRemovals.Contains(type) && (_set.Contains(type) || _pendingAdditions.Contains(type));

		public bool Contains(Item item) => Contains(item.type);

		public bool Remove(Item item) => Remove(item.type);

		public bool Remove(int type)
		{
			bool wasPresent = Contains(type);
			_pendingAdditions.Remove(type);
			_pendingRemovals.Add(type);

			if (wasPresent)
				_itemListDirty = true;

			return wasPresent;
		}

		public void Clear()
		{
			_set.Clear();
			_items.Clear();
			_unloadedItems.Clear();
			_pendingAdditions.Clear();
			_pendingRemovals.Clear();
			_itemListDirty = false;
		}

		public ItemTypeOrderedSet Clone() {
			if (_itemListDirty) {
				UpdateCollections();
				_itemListDirty = false;
			}

			var clone = new ItemTypeOrderedSet(_name);

			// Only the loaded IDs are relevant
			foreach (int id in _set) {
				// Delay generating the item collection until it's requested
				clone._pendingAdditions.Add(id);
			}

			clone._itemListDirty = true;

			return clone;
		}

		public void Save(TagCompound c)
		{
			HashSet<int> typeSet = new(_set);
			
			// Resolve the set to what it would be if the pending changes were applied
			if (_itemListDirty)
			{
				HashSet<int> additions = new(_pendingAdditions);
				typeSet.UnionWith(additions);
				typeSet.ExceptWith(_pendingRemovals);
			}

			List<ItemDefinition> list = typeSet.Select(x => new ItemDefinition(x)).TakeLastIfLimitExists(MemoryLimit).ToList();
			if (MemoryLimit is int limit && list.Count < limit)
				list.AddRange(_unloadedItems.TakeLast(limit - list.Count));

			c.Add(_name + Suffix3, list);
		}

		public void Load(TagCompound tag)
		{
			if (tag.GetList<TagCompound>(_name) is { Count: > 0 } listV1)
			{
				_items = listV1
					.Select(Utility.SafelyLoadItem)
					.Where(static i => !i.IsAir)
					.TakeLastIfLimitExists(MemoryLimit)
					.ToList();

				_pendingAdditions = new HashSet<int>(_items.Select(static i => i.type));
				_itemListDirty = true;
			}
			else if (tag.GetList<int>(_name + Suffix) is { Count: > 0 } listV2)
			{
				_items = listV2
					.Where(static x => x < ItemLoader.ItemCount)  // Unable to reliably restore invalid IDs; just ignore them
					.Select(static x => new Item(x))
					.Where(static x => !x.IsAir)  // Filters out deprecated items
					.TakeLastIfLimitExists(MemoryLimit)
					.ToList();

				_pendingAdditions = new HashSet<int>(_items.Select(static i => i.type));
				_itemListDirty = true;
			}
			else if (tag.GetList<ItemDefinition>(_name + Suffix3) is { Count: > 0 } listV3)
			{
				foreach (var def in listV3.TakeLastIfLimitExists(MemoryLimit))
				{
					if (!def.IsUnloaded)
						_pendingAdditions.Add(def.Type);
					else
						_unloadedItems.Add(def);
				}

				_itemListDirty = true;
			} 
			else
				Clear();
		}
	}
}
