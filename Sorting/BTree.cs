using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace MagicStorage.Sorting
{
    public class BTree<T>
    {
        private BTreeNode<T> root;
        private CompareFunction func;

        public BTree(CompareFunction func)
        {
            this.root = new BTreeNode<T>(func, true);
            this.func = func;
        }

        public void Insert(T item)
        {
            T pushedItem;
            BTreeNode<T> pushedBranch;
            if (root.Insert(item, out pushedItem, out pushedBranch))
            {
                BTreeNode<T> newRoot = new BTreeNode<T>(func, root, pushedItem, pushedBranch);
                root = newRoot;
            }
        }

        public IEnumerable<T> GetSortedItems()
        {
            return root.GetSortedItems();
        }
    }

    class BTreeNode<T>
    {
        private const int branchFactor = 32;
        private CompareFunction func;
        private List<T> elements = new List<T>(branchFactor);
        private List<BTreeNode<T>> branches = new List<BTreeNode<T>>(branchFactor + 1);
        private bool isLeaf;

        internal BTreeNode(CompareFunction func, bool isLeaf)
        {
            this.func = func;
            this.isLeaf = isLeaf;
        }

        internal BTreeNode(CompareFunction func, BTreeNode<T> branch1, T element, BTreeNode<T> branch2)
        {
            this.func = func;
            this.isLeaf = false;
            this.branches.Add(branch1);
            this.elements.Add(element);
            this.branches.Add(branch2);
        }

        internal bool Insert(T item, out T pushItem, out BTreeNode<T> pushBranch)
        {
            if (isLeaf)
            {
                InsertIntoElements(item);
                if (elements.Count == branchFactor)
                {
                    Split(out pushItem, out pushBranch);
                    return true;
                }
                pushItem = default(T);
                pushBranch = null;
                return false;
            }
            else
            {
                T pushedItem;
                BTreeNode<T> pushedBranch;
                int splitBranch = InsertIntoBranch(item, out pushedItem, out pushedBranch);
                if (splitBranch >= 0)
                {
                    branches.Insert(splitBranch + 1, pushedBranch);
                    elements.Insert(splitBranch, pushedItem);
                    if (elements.Count == branchFactor)
                    {
                        Split(out pushItem, out pushBranch);
                        return true;
                    }
                }
                pushItem = default(T);
                pushBranch = null;
                return false;
            }
        }

        private int InsertIntoElements(T item)
        {
            int min = 0;
            int max = elements.Count;
            while (min < max)
            {
                int check = (min + max) / 2;
                int result = func.Compare(item, elements[check]);
                if (result < 0)
                {
                    max = check;
                }
                else if (result > 0)
                {
                    min = check + 1;
                }
                else if (item is Item)
                {
                    result = ItemData.Compare((Item)(object)item, (Item)(object)elements[check]);
                    if (result < 0)
                    {
                        max = check;
                    }
                    else if (result > 0)
                    {
                        min = check + 1;
                    }
                    else
                    {
                        ((Item)(object)elements[check]).stack += ((Item)(object)item).stack;
                        return -1;
                    }
                }
                else
                {
                    min = check + 1;
                }
            }
            elements.Insert(min, item is Item ? (T)(object)((Item)(object)item).Clone() : item);
            return min;
        }

        private int InsertIntoBranch(T item, out T pushItem, out BTreeNode<T> pushNode)
        {
            int min = 0;
            int max = elements.Count;
            while (min < max)
            {
                int check = (min + max) / 2;
                int result = func.Compare(item, elements[check]);
                if (result < 0)
                {
                    max = check;
                }
                else if (result > 0)
                {
                    min = check + 1;
                }
                else if (item is Item)
                {
                    result = ItemData.Compare((Item)(object)item, (Item)(object)elements[check]);
                    if (result < 0)
                    {
                        max = check;
                    }
                    else if (result > 0)
                    {
                        min = check + 1;
                    }
                    else
                    {
                        ((Item)(object)elements[check]).stack += ((Item)(object)item).stack;
                        pushItem = default(T);
                        pushNode = null;
                        return -1;
                    }
                }
                else
                {
                    min = check + 1;
                }
            }
            if (branches[min].Insert(item, out pushItem, out pushNode))
            {
                return min;
            }
            return -1;
        }

        private void Split(out T pushItem, out BTreeNode<T> pushBranch)
        {
            BTreeNode<T> newNeighbor = new BTreeNode<T>(func, isLeaf);
            newNeighbor.elements.AddRange(elements.GetRange(branchFactor / 2 + 1, branchFactor / 2 - 1));
            pushItem = elements[branchFactor / 2];
            elements.RemoveRange(branchFactor / 2, branchFactor / 2);
            if (!isLeaf)
            {
                newNeighbor.branches.AddRange(branches.GetRange(branchFactor / 2 + 1, branchFactor / 2));
                branches.RemoveRange(branchFactor / 2 + 1, branchFactor / 2);
            }
            pushBranch = newNeighbor;
        }

        internal IEnumerable<T> GetSortedItems()
        {
            if (isLeaf)
            {
                return elements;
            }
            else
            {
                return elements.SelectMany((element, index) => new AppendEnumerable<T>(branches[index].GetSortedItems(), element)).Concat(branches[elements.Count].GetSortedItems()); 
            }
        }
    }

    public class AppendEnumerable<T> : IEnumerable<T>
    {
        private IEnumerable<T> enumerable;
        private T element;

        public AppendEnumerable(IEnumerable<T> enumerable, T element)
        {
            this.enumerable = enumerable;
            this.element = element;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new AppendEnumerator<T>(enumerable, element);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class AppendEnumerator<T> : IEnumerator<T>
    {
        private IEnumerator<T> enumerator;
        private T element;
        private bool enumeratorFinished;
        private bool finished;

        public AppendEnumerator(IEnumerable<T> enumerable, T element)
        {
            this.enumerator = enumerable.GetEnumerator();
            this.element = element;
            this.finished = false;
        }

        public T Current
        {
            get
            {
                if (!enumeratorFinished)
                {
                    return enumerator.Current;
                }
                if (!finished)
                {
                    return element;
                }
                throw new InvalidOperationException();
            }
        }

        object IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }

        public bool MoveNext()
        {
            if (!enumerator.MoveNext())
            {
                if (enumeratorFinished)
                {
                    finished = true;
                    return false;
                }
                enumeratorFinished = true;
                return true;
            }
            return true;
        }

        public void Reset()
        {
            enumerator.Reset();
            finished = false;
            enumeratorFinished = false;
        }

        public void Dispose()
        {
            enumerator.Dispose();
        }
    }
}