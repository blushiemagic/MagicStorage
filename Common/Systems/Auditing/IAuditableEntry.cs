namespace MagicStorage.Common.Systems.Auditing {
	internal interface IAuditableEntry<TSelf, TSource, TKey> where TSelf : IAuditableEntry<TSelf, TSource, TKey> {
		static abstract TSelf CreateFrom(TSource source);

		static abstract TSelf CreateFrom<TAlternate>(TAlternate source) where TAlternate : IAlternateAuditSource<TAlternate, TSource, TKey>;

		static abstract TKey GetKey(TSelf self);

		static abstract TKey GetKey(TSource source);

		static abstract string GetName(TSelf self);
	}
}
