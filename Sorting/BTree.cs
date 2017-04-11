using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace MagicStorage.Sorting
{
	public class BTree
	{
		private BTreeNode root;
		private CompareFunction func;

		public BTree(CompareFunction func)
		{
			this.root = new BTreeNode(func, true);
			this.func = func;
		}

		public void Insert(Item item)
		{
			Item pushedItem;
			BTreeNode pushedBranch;
			if (root.Insert(item, out pushedItem, out pushedBranch))
			{
				BTreeNode newRoot = new BTreeNode(func, root, pushedItem, pushedBranch);
				root = newRoot;
			}
		}

		public IEnumerable<Item> GetSortedItems()
		{
			return root.GetSortedItems();
		}
	}

	class BTreeNode
	{
		private const int branchFactor = 32;
		private CompareFunction func;
		private List<Item> elements = new List<Item>(branchFactor);
		private List<BTreeNode> branches = new List<BTreeNode>(branchFactor + 1);
		private bool isLeaf;

		internal BTreeNode(CompareFunction func, bool isLeaf)
		{
			this.func = func;
			this.isLeaf = isLeaf;
		}

		internal BTreeNode(CompareFunction func, BTreeNode branch1, Item element, BTreeNode branch2)
		{
			this.func = func;
			this.isLeaf = false;
			this.branches.Add(branch1);
			this.elements.Add(element);
			this.branches.Add(branch2);
		}

		internal bool Insert(Item item, out Item pushItem, out BTreeNode pushBranch)
		{
			if (isLeaf)
			{
				InsertIntoElements(item);
				if (elements.Count == branchFactor)
				{
					Split(out pushItem, out pushBranch);
					return true;
				}
				pushItem = null;
				pushBranch = null;
				return false;
			}
			else
			{
				Item pushedItem;
				BTreeNode pushedBranch;
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
				pushItem = null;
				pushBranch = null;
				return false;
			}
		}

		private int InsertIntoElements(Item item)
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
				else
				{
					result = ItemData.Compare(item, elements[check]);
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
						elements[check].stack += item.stack;
						return -1;
					}
				}
			}
			elements.Insert(min, item.Clone());
			return min;
		}

		private int InsertIntoBranch(Item item, out Item pushItem, out BTreeNode pushNode)
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
				else
				{
					result = ItemData.Compare(item, elements[check]);
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
						elements[check].stack += item.stack;
						pushItem = null;
						pushNode = null;
						return -1;
					}
				}
			}
			if (branches[min].Insert(item, out pushItem, out pushNode))
			{
				return min;
			}
			return -1;
		}

		private void Split(out Item pushItem, out BTreeNode pushBranch)
		{
			BTreeNode newNeighbor = new BTreeNode(func, isLeaf);
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

		internal IEnumerable<Item> GetSortedItems()
		{
			if (isLeaf)
			{
				return elements;
			}
			else
			{
				return elements.SelectMany((element, index) => new AppendEnumerable<Item>(branches[index].GetSortedItems(), element)).Concat(branches[elements.Count].GetSortedItems()); 
			}
		}

		internal IEnumerable<Item> GetSortedItemsOld()
		{
			if (isLeaf)
			{
				for (int k = 0; k < elements.Count; k++)
				{
					yield return elements[k];
				}
			}
			else
			{
				
				for (int k = 0; k < elements.Count; k++)
				{
					foreach (Item item in branches[k].GetSortedItems())
					{
						yield return item;
					}
					yield return elements[k];
				}
				foreach (Item item in branches[elements.Count].GetSortedItems())
				{
					yield return item;
				}
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