using System;
using Terraria;

namespace MagicStorage.CrossMod.Calls {
	public readonly struct OneOfMany<T1, T2, T3> {
		private readonly BitsByte _isValue;
		private readonly T1 _value1;
		private readonly T2 _value2;
		private readonly T3 _value3;

		public bool IsValue1 => _isValue[0];
		public bool IsValue2 => _isValue[1];
		public bool IsValue3 => _isValue[2];

		public T1 Value1 => IsValue1 ? _value1 : default;

		public T2 Value2 => IsValue2 ? _value2 : default;

		public T3 Value3 => IsValue3 ? _value3 : default;

		// Parameterless ctor = default, implicit

		public OneOfMany(T1 value) {
			_isValue = new BitsByte(true, false, false);
			_value1 = value;
			_value2 = default;
			_value3 = default;
		}

		public OneOfMany(T2 value) {
			_isValue = new BitsByte(false, true, false);
			_value1 = default;
			_value2 = value;
			_value3 = default;
		}

		public OneOfMany(T3 value) {
			_isValue = new BitsByte(false, false, true);
			_value1 = default;
			_value2 = default;
			_value3 = value;
		}

		static OneOfMany() {
			if (typeof(T1).IsGenericType && typeof(T1).GetGenericTypeDefinition() == typeof(OneOfMany<,,>)
			|| typeof(T2).IsGenericType && typeof(T2).GetGenericTypeDefinition() == typeof(OneOfMany<,,>)
			|| typeof(T3).IsGenericType && typeof(T3).GetGenericTypeDefinition() == typeof(OneOfMany<,,>))
				throw new InvalidOperationException($"Cannot use {nameof(OneOfMany<T1, T2, T3>)}<{nameof(T1)}, {nameof(T2)}, {nameof(T3)}> as a type parameter for itself.");
		}
	}
}
