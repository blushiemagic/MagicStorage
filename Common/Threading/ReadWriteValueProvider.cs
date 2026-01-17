namespace MagicStorage.Common.Threading {
	public class ReadWriteValueProvider<T> : IValueProvider<T> {
		T IReadOnlyValueProvider<T>.StaticSource => default;

		public T Value { get; set; }

		public ReadWriteValueProvider() => Value = default;

		public ReadWriteValueProvider(T defaultValue) => Value = defaultValue;

		void IReadOnlyValueProvider<T>.ClearStatic() { }

		void IReadOnlyValueProvider<T>.CopyFromStatic() { }

		void IReadOnlyValueProvider<T>.CopyToStatic() { }
	}
}
