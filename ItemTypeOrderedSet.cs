using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader.IO;

namespace MagicStorage
{
    public class ItemTypeOrderedSet
    {
        readonly string _name;
        List<Item> _items = new List<Item>();
        HashSet<int> _set = new HashSet<int>();
        public int Count { get { return _items.Count; } }

        public ItemTypeOrderedSet(string name)
        {
            _name = name;
        }

        public IEnumerable<Item> Items { get { return _items; } }

        public bool Add(Item item)
        {
            return Add(item.type);
        }

        public bool Add(int type)
        {
            var item = new Item();
            item.SetDefaults(type);
            if (_set.Add(item.type))
            {
                _items.Add(item);
                return true;
            }

            return false;
        }

        public bool Contains(int type)
        {
            return _set.Contains(type);
        }

        public bool Contains(Item item)
        {
            return _set.Contains(item.type);
        }

        public bool Remove(Item item)
        {
            var type = item.type;
            return Remove(type);
        }

        public bool Remove(int type)
        {
            if (_set.Remove(type))
            {
                _items.RemoveAll(x => x.type == type);
                return true;
            }

            return false;
        }

        public bool RemoveAt(int index)
        {
            var item = _items[index];
            if (_set.Remove(item.type))
            {
                _items.RemoveAt(index);
                return true;
            }

            return false;
        }

        const string Suffix = "~v2";

        public void Save(TagCompound c)
        {
            c.Add(_name + Suffix, _items.Select(x => (int) x.type).ToList());
        }

        public void Load(TagCompound tag)
        {
            var list = tag.GetList<TagCompound>(_name);
            if (list != null && list.Count > 0)
                _items = list.Select(ItemIO.Load).ToList();
            else
            {
                var listV2 = tag.GetList<int>(_name + Suffix);
                if (listV2 != null)
                {
                    _items = listV2
                        .Select(x =>
                                {
                                    var item = new Item();
                                    item.SetDefaults(x);
                                    item.type = x;
                                    return item;
                                }).ToList();
                }
                else
                    _items = new List<Item>();
            }

            _set = new HashSet<int>(_items.Select(x => x.type));
        }
    }
}