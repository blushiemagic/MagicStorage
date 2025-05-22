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

		public int Count => _items.Count;

		public IEnumerable<Item> Items => _items;

		public int? MemoryLimit { get; init; }  // Necessary for TEStorageHeart so that it doesn't take up thousands of bytes when syncing in NetSend/NetReceive

		public ItemTypeOrderedSet(string name)
		{
			_name = name;
		}

		public bool Add(Item item) => Add(item.type);

		public bool Add(int type)
		{
			if (_set.Add(type))
			{
				if (MemoryLimit is int limit)
				{
					// Prioritize unloaded items over loaded items
					while (_unloadedItems.Count > 0 && _set.Count + _unloadedItems.Count >= limit)
						_unloadedItems.RemoveAt(_unloadedItems.Count - 1);

					while (_set.Count >= limit)
					{
						// The implementation of First() may be inconsistent across .NET versions, but that shouldn't matter
						Remove(_set.First());
					}
				}

				_items.Add(new Item(type));
				return true;
			}

			return false;
		}

		public bool Contains(int type) => _set.Contains(type);

		public bool Contains(Item item) => _set.Contains(item.type);

		public bool Remove(Item item) => Remove(item.type);

		public bool Remove(int type)
		{
			if (_set.Remove(type))
			{
				_items.RemoveAll(x => x.type == type);
				return true;
			}

			return false;
		}

		public void Clear()
		{
			_set.Clear();
			_items.Clear();
		}

		public void Save(TagCompound c)
		{
			List<ItemDefinition> list = _set.Select(x => new ItemDefinition(x)).TakeLastIfLimitExists(MemoryLimit).ToList();
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

				_set = new HashSet<int>(_items.Select(static i => i.type));
			}
			else if (tag.GetList<int>(_name + Suffix) is { Count: > 0 } listV2) 
			{
				_items = listV2
					.Where(static x => x < ItemLoader.ItemCount)  // Unable to reliably restore invalid IDs; just ignore them
					.Select(static x => new Item(x))
					.Where(static x => !x.IsAir)  // Filters out deprecated items
					.TakeLastIfLimitExists(MemoryLimit)
					.ToList();

				_set = new HashSet<int>(_items.Select(static i => i.type));
			}
			else if (tag.GetList<ItemDefinition>(_name + Suffix3) is { Count: > 0 } listV3) 
			{
				foreach (var def in listV3.TakeLastIfLimitExists(MemoryLimit))
				{
					if (!def.IsUnloaded)
					{
						_items.Add(new Item(def.Type));
						_set.Add(def.Type);
					}
					else
						_unloadedItems.Add(def);
				}
			} 
			else 
			{
				_items = new List<Item>();
				_set = new HashSet<int>();
			}
		}
	}
}
