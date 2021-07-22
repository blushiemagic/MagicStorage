using System;
using System.Collections.Generic;
using Terraria;

namespace MagicStorage.Sorting
{
    public abstract class CompareFunction
    {
        public abstract int Compare(Item item1, Item item2);

        public int Compare(object object1, object object2)
        {
            if (object1 is Item item && object2 is Item item2)
            {
                return Compare(item, item2);
            }
            if (object1 is Recipe recipe && object2 is Recipe recipe2)
            {
                return Compare(recipe.createItem, recipe2.createItem);
            }
            return 0;
        }
    }

    public class CompareID : CompareFunction
    {
        public override int Compare(Item item1, Item item2)
        {
            return item1.type - item2.type;
        }
    }

    public class CompareName : CompareFunction
    {
        public override int Compare(Item item1, Item item2)
        {
            return string.Compare(item1.Name, item2.Name, StringComparison.OrdinalIgnoreCase);
        }
    }

    public class CompareQuantity : CompareFunction
    {
        public override int Compare(Item item1, Item item2)
        {
            return (int)Math.Ceiling((float)item2.stack / (float)item2.maxStack) - (int)Math.Ceiling((float)item1.stack / (float)item1.maxStack);
        }
    }
}