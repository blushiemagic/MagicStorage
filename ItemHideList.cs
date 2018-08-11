using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader.IO;

namespace MagicStorage
{
    public class ItemHideList
    {
        readonly string _name;
        List<Item> _items = new List<Item>();
        HashSet<int> _set = new HashSet<int>();

        public ItemHideList(string name)
        {
            _name = name;
        }

        public IEnumerable<Item> Items { get { return _items; } }

        public bool Add(Item item)
        {
            if (_set.Add(item.type))
            {
                _items.Add(item);
                return true;
            }
            return false;
        }

        public void Save(TagCompound c)
        {
            c.Add(_name, _items.Select(ItemIO.Save).ToList());
        }

        public void Load(TagCompound tag)
        {
            var list = tag.GetList<TagCompound>(_name);
            _items = list != null ? list.Select(ItemIO.Load).ToList() : new List<Item>();
            _set = new HashSet<int>(_items.Select(x => x.type));
        }
    }
}