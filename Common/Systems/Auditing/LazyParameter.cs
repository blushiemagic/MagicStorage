using System;

namespace MagicStorage.Common.Systems.Auditing {
	internal abstract class LazyParameter {
		public abstract void Evaluate(AuditFile file);
	}

	internal class LazyParameter<T>(T defaultValue, Func<AuditFile, T> valueProvider) : LazyParameter {
		private readonly Func<AuditFile, T> _valueProvider = valueProvider;

		public T Value { get; protected set; } = defaultValue;

		public override void Evaluate(AuditFile file) => Value = _valueProvider(file);
	}

	internal class LazyParameter<TSource, TValue>(TValue defaultValue, TSource source, Func<AuditFile, TSource, TValue> valueProvider) : LazyParameter<TValue>(defaultValue, null) {
		private readonly Func<AuditFile, TSource, TValue> _valueProvider = valueProvider;
		private readonly TSource _source = source;

		public override void Evaluate(AuditFile file) => Value = _valueProvider(file, _source);
	}

	internal class FixedParameter<T>(T value) : LazyParameter<T>(value, null) {
		private readonly T _fixedValue = value;

		public override void Evaluate(AuditFile file) => Value = _fixedValue;
	}
}
