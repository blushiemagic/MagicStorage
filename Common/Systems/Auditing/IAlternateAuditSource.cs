namespace MagicStorage.Common.Systems.Auditing {
	internal interface IAlternateAuditSource<TSelf, TSource, TValue> where TSelf : IAlternateAuditSource<TSelf, TSource, TValue> {
		public abstract TSelf CreateFrom(TSource source);

		static abstract string GetName(TSelf self);

		static abstract TValue GetValue(TSelf self);
	}
}
