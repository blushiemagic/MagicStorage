using System;

namespace MagicStorage.Common.Systems.RecurrentRecipes {
	public struct ExcessItemInfo {
		public readonly int type;
		private SharedCounter _stack;
		public int Stack => _stack;
		public readonly int prefix;

		public ExcessItemInfo(int type, SharedCounter stack, int prefix) {
			this.type = type;
			_stack = stack;
			this.prefix = prefix;
		}

		public void UpdateStack(int add) => _stack += add;

		public void ClearStack() => _stack.Reset();

		public override bool Equals(object obj) {
			return obj is ExcessItemInfo info && type == info.type && object.ReferenceEquals(_stack, info._stack) && Stack == info.Stack && prefix == info.prefix;
		}

		public bool EqualsIgnoreStack(ExcessItemInfo other) {
			return type == other.type && prefix == other.prefix;
		}

		public override int GetHashCode() {
			return HashCode.Combine(type, Stack, prefix);
		}
	}
}
