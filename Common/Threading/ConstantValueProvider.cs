namespace MagicStorage.Common.Threading {
	public class ConstantValueProvider<T>(T constantValue) : IReadOnlyValueProvider<T> {
		T IReadOnlyValueProvider<T>.StaticSource { get; } = default;

		T IReadOnlyValueProvider<T>.Value { get; } = constantValue;

		void IReadOnlyValueProvider<T>.ClearStatic() { }

		void IReadOnlyValueProvider<T>.CopyFromStatic() { }

		void IReadOnlyValueProvider<T>.CopyToStatic() { }
	}
}
