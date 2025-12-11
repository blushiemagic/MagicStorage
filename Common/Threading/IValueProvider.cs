namespace MagicStorage.Common.Threading {
	public interface IValueProvider<T> : IReadOnlyValueProvider<T> {
		T IReadOnlyValueProvider<T>.Value => Value;

		T Value { get; set; }
	}
}
