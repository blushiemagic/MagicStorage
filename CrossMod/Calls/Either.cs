using System;

namespace MagicStorage.CrossMod.Calls {
	public readonly struct Either<TFirst, TSecond> {
		private readonly bool _isFirst;
		private readonly bool _isSecond;
		private readonly TFirst _first;
		private readonly TSecond _second;

		public bool IsFirstOption => _isFirst;

		public bool IsSecondOption => _isSecond;

		public TFirst FirstOption => IsFirstOption ? _first : default;

		public TSecond SecondOption => IsSecondOption ? _second : default;

		// Parameterless ctor = default, implicit

		public Either(TFirst first) {
			_isFirst = true;
			_isSecond = false;
			_first = first;
			_second = default;
		}

		public Either(TSecond second) {
			_isFirst = false;
			_isSecond = true;
			_first = default;
			_second = second;
		}

		static Either() {
			if (typeof(TFirst).IsGenericType && typeof(TFirst).GetGenericTypeDefinition() == typeof(Either<,>)
			|| typeof(TSecond).IsGenericType && typeof(TSecond).GetGenericTypeDefinition() == typeof(Either<,>))
				throw new InvalidOperationException($"Cannot use {nameof(Either<TFirst, TSecond>)}<{nameof(TFirst)}, {nameof(TSecond)}> as a type parameter for itself.");
		}
	}
}
