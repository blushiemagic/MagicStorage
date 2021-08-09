using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MagicStorage
{
	public class ItemTypeOrderedSet
	{
		private const string Suffix = "~v2";
		private readonly string _name;
		private List<Item> _items = new();
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
			c.Add(_name + Suffix, _items.Select(x => x.type).ToList());
		}

		public void Load(TagCompound tag)
		{
			IList<TagCompound> list = tag.GetList<TagCompound>(_name);
			if (list is not null && list.Count > 0)
			{
				_items = list.Select(ItemIO.Load).ToList();
			}
			else
			{
				IList<int> listV2 = tag.GetList<int>(_name + Suffix);
				if (listV2 is not null)
					_items = listV2.Select(x =>
						{
							if (x >= ItemLoader.ItemCount && ItemLoader.GetItem(x) == null)
								return null;
							Item item = new();
							item.SetDefaults(x);
							item.type = x;
							return item;
						})
						.Where(x => x is not null)
						.ToList();
				else
					_items = new List<Item>();
			}

			_set = new HashSet<int>(_items.Select(x => x.type));
		}
	}
}
