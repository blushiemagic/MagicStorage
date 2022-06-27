using System;
using System.Linq;

namespace MagicStorage {
	public static class Utility {
		public static string GetSimplifiedGenericTypeName(this Type type) {
			//Handle all invalid cases here:
			if (type.FullName is null)
				return type.Name;

			if (!type.IsGenericType)
				return type.FullName;

			string parent = type.GetGenericTypeDefinition().FullName!;

			//Include all but the "`X" part
			parent = parent[..parent.IndexOf('`')];

			//Construct the child types
			return $"{parent}<{string.Join(", ", type.GetGenericArguments().Select(GetSimplifiedGenericTypeName))}>";
		}
	}
}
