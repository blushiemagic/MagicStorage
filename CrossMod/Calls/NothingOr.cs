using System;

namespace MagicStorage.CrossMod.Calls {
	public readonly struct NothingOr<T> {
		private static readonly bool _isClass = typeof(T).IsClass && !typeof(T).IsValueType;

		private readonly bool _isValue;
		private readonly T _value;

		public bool IsNothing => !_isValue;
		public bool IsValue => _isValue;

		public T Value => _isValue ? _value : default;

		// Parameterless ctor = default, implicit

		public NothingOr(T value) {
			_isValue = true;
			_value = value;
		}

		static NothingOr() {
			if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(NothingOr<>))
				throw new InvalidOperationException($"Cannot use {nameof(NothingOr<T>)}<{nameof(T)}> as a type parameter for itself.");
		}
	}
}
