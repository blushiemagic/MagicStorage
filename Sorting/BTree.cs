using System;
using System.Collections;
using System.Collections.Generic;
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
		private Item[] elements = new Item[branchFactor];
		private BTreeNode[] branches = new BTreeNode[branchFactor + 1];
		private int count = 0;
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
			this.branches[0] = branch1;
			this.elements[0] = element;
			this.branches[1] = branch2;
			this.count = 1;
		}

		internal bool Insert(Item item, out Item pushItem, out BTreeNode pushBranch)
		{
			if (isLeaf)
			{
				InsertIntoElements(item);
				if (count == branchFactor)
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
					for (int k = count; k > splitBranch; k--)
					{
						branches[k + 1] = branches[k];
						elements[k] = elements[k - 1];
					}
					branches[splitBranch + 1] = pushedBranch;
					elements[splitBranch] = pushedItem;
					count++;
					if (count == branchFactor)
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
			int max = count;
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
			for (int k = count; k > min; k--)
			{
				elements[k] = elements[k - 1];
			}
			elements[min] = item.Clone();
			count++;
			return min;
		}

		private int InsertIntoBranch(Item item, out Item pushItem, out BTreeNode pushNode)
		{
			int min = 0;
			int max = count;
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
			Array.Copy(elements, branchFactor / 2 + 1, newNeighbor.elements, 0, branchFactor / 2 - 1);
			count = branchFactor / 2;
			newNeighbor.count = branchFactor / 2 - 1;
			pushItem = elements[branchFactor / 2];
			pushBranch = newNeighbor;
			if (!isLeaf)
			{
				Array.Copy(branches, branchFactor / 2 + 1, newNeighbor.branches, 0, branchFactor / 2);
			}
		}

		internal IEnumerable<Item> GetSortedItems()
		{
			if (isLeaf)
			{
				for (int k = 0; k < count; k++)
				{
					yield return elements[k];
				}
			}
			else
			{
				for (int k = 0; k < count; k++)
				{
					foreach (Item item in branches[k].GetSortedItems())
					{
						yield return item;
					}
					yield return elements[k];
				}
				foreach (Item item in branches[count].GetSortedItems())
				{
					yield return item;
				}
			}
		}
	}
}