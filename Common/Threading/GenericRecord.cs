namespace MagicStorage.Common.Threading {
	/// <summary>
	/// Mutable two-value record used by generic collection helpers.
	/// </summary>
	/// <typeparam name="T1">The first value type.</typeparam>
	/// <typeparam name="T2">The second value type.</typeparam>
	/// <param name="Item1">The initial first value.</param>
	/// <param name="Item2">The initial second value.</param>
	public record class GenericRecord<T1, T2>(T1 Item1, T2 Item2) {
		/// <summary>
		/// Gets or sets the first value.
		/// </summary>
		public T1 Item1 { get; set; } = Item1;

		/// <summary>
		/// Gets or sets the second value.
		/// </summary>
		public T2 Item2 { get; set; } = Item2;
	}
}
