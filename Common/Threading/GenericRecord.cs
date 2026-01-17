namespace MagicStorage.Common.Threading {
	public record class GenericRecord<T1, T2>(T1 Item1, T2 Item2) {
		public T1 Item1 { get; set; } = Item1;

		public T2 Item2 { get; set; } = Item2;
	}
}
