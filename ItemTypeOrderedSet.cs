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

		public ItemTypeOrderedSet(string name)
		{
			_name = name;
		}

		public bool Add(Item item) => Add(item.type);

		public bool Add(int type)
		{
			Item item = new();
			item.SetDefaults(type);
			if (_set.Add(item.type))
			{
				_items.Add(item);
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

		public bool RemoveAt(int index)
		{
			Item item = _items[index];
			if (_set.Remove(item.type))
			{
				_items.RemoveAt(index);
				return true;
			}

			return false;
		}

		public void Save(TagCompound c)
		{
			c.Add(_name + Suffix3, _items.Select(x => new ItemDefinition(x.type)).Concat(_unloadedItems).ToList());
		}

		public void Load(TagCompound tag)
		{
			if (tag.GetList<TagCompound>(_name) is { Count: > 0 } listV1) 
			{
				_items = listV1.Select(ItemIO.Load).ToList();
			}
			else if (tag.GetList<int>(_name + Suffix) is { Count: > 0 } listV2) 
			{
				_items = listV2
					.Where(x => x < ItemLoader.ItemCount) // Unable to reliably restore invalid IDs; just ignore them
					.Select(x => new Item(x))
					.Where(x => !x.IsAir) // Filters out deprecated items
					.ToList();
			}
			else if (tag.GetList<ItemDefinition>(_name + Suffix3) is { Count: > 0 } listV3) 
			{
				_items = listV3.Where(x => !x.IsUnloaded).Select(x => new Item(x.Type)).ToList();
				_unloadedItems = listV3.Where(x => x.IsUnloaded).ToList();
			} 
			else 
			{
				_items = new List<Item>();
			}

			_set = new HashSet<int>(_items.Select(x => x.type));
		}
	}
}
