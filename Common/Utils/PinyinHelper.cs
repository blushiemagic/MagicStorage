using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.Localization;

namespace MagicStorage.Common.Utils {
	/// <summary>
	/// Utility class providing pinyin search functionality
	/// </summary>
	public static class PinyinHelper {
		/// <summary>
		/// Cache for pinyin information of item types to avoid redundant calculations
		/// Uses ConcurrentDictionary to ensure thread safety
		/// </summary>
		private static readonly ConcurrentDictionary<int, PinyinInfo> _pinyinCache = new();

		/// <summary>
		/// Cached NPinyin.Core assembly
		/// </summary>
		private static Assembly _nPinyinAssembly;

		/// <summary>
		/// Flag indicating whether NPinyin is loaded (set via reflection to avoid circular dependencies)
		/// </summary>
		private static bool? _nPinyinLoaded;

		/// <summary>
		/// Cached NPinyin.Pinyin type (obtained via reflection to avoid JIT-time type resolution)
		/// </summary>
		private static Type _pinyinType;

		/// <summary>
		/// Cached GetPinyin method
		/// </summary>
		private static MethodInfo _getPinyinMethod;

		/// <summary>
		/// Cached GetInitials method
		/// </summary>
		private static MethodInfo _getInitialsMethod;

		/// <summary>
		/// Lock object for synchronizing initialization of NPinyin types and methods
		/// </summary>
		private static readonly object _pinyinInitLock = new object();

		/// <summary>
		/// Checks if the current game language is Simplified Chinese
		/// </summary>
		/// <returns>Returns true if Simplified Chinese, otherwise false</returns>
		public static bool IsSimplifiedChinese() {
			try {
				// Check the current active language culture
				var culture = Language.ActiveCulture;
				return culture != null && culture.Name == "zh-Hans";
			} catch {
				// If detection fails, default to false (don't enable pinyin search)
				return false;
			}
		}

		/// <summary>
		/// Checks whether pinyin search should be enabled
		/// Only enabled in Simplified Chinese environment and when NPinyin library is loaded
		/// </summary>
		/// <returns>Whether pinyin search is enabled</returns>
		public static bool ShouldEnablePinyinSearch() {
			// Check configuration option, language setting, and whether NPinyin library is loaded
			if (!MagicStorageConfig.EnablePinyinSearch || !IsSimplifiedChinese())
				return false;

			// Ensure NPinyin library is loaded
			return IsNPinyinLoaded();
		}

		/// <summary>
		/// Pinyin information for an item
		/// </summary>
		public class PinyinInfo {
			/// <summary>
			/// Full pinyin (lowercase, no spaces), e.g., "tiekuang"
			/// </summary>
			public string FullPinyin { get; set; } = string.Empty;

			/// <summary>
			/// Pinyin initials (lowercase), e.g., "tk"
			/// </summary>
			public string FirstLetters { get; set; } = string.Empty;
		}

		/// <summary>
		/// Gets pinyin information for an item (with caching)
		/// </summary>
		/// <param name="item">Item instance</param>
		/// <returns>Pinyin information</returns>
		public static PinyinInfo GetPinyinInfo(Item item) {
			if (item?.IsAir != false)
				return new PinyinInfo();

			// Get item name outside lambda to avoid closure capturing item object
			// For the same item.type, item.Name should be stable
			string itemName = item.Name ?? string.Empty;
			int itemType = item.type;

			// Use GetOrAdd to ensure thread safety and avoid redundant calculations
			return _pinyinCache.GetOrAdd(itemType, _ => ConvertToPinyin(itemName));
		}

		/// <summary>
		/// Checks if search text matches item name
		/// Supports three matching methods: Chinese, full pinyin, and pinyin initials
		/// </summary>
		/// <param name="item">Item instance</param>
		/// <param name="searchText">Search text</param>
		/// <returns>Whether it matches</returns>
		public static bool MatchesSearch(Item item, string searchText) {
			if (item?.IsAir != false || string.IsNullOrEmpty(searchText))
				return false;

			// Get item name, use empty string if null
			string itemName = item.Name ?? string.Empty;
			searchText = searchText.Trim();

			// If search text is empty (after Trim), return false directly
			if (string.IsNullOrEmpty(searchText))
				return false;

			// 1. Direct Chinese matching (original functionality, maintained for compatibility)
			// This is the fastest matching method, check first
			// If Chinese matching succeeds, return directly to avoid unnecessary pinyin conversion
			if (!string.IsNullOrEmpty(itemName) && 
				itemName.Contains(searchText, StringComparison.OrdinalIgnoreCase))
				return true;

			// 2. Pinyin matching (only executed when Chinese matching fails, to avoid unnecessary pinyin conversion)
			var pinyinInfo = GetPinyinInfo(item);

			// 2.1 Full pinyin matching (case-insensitive)
			if (!string.IsNullOrEmpty(pinyinInfo.FullPinyin) &&
				pinyinInfo.FullPinyin.Contains(searchText, StringComparison.OrdinalIgnoreCase))
				return true;

			// 2.2 Pinyin initials matching (case-insensitive)
			if (!string.IsNullOrEmpty(pinyinInfo.FirstLetters) &&
				pinyinInfo.FirstLetters.Contains(searchText, StringComparison.OrdinalIgnoreCase))
				return true;

			return false;
		}

		/// <summary>
		/// Converts Chinese text to pinyin information
		/// Uses reflection to call NPinyin library, avoiding type resolution at JIT time
		/// </summary>
		/// <param name="text">Chinese text</param>
		/// <returns>Pinyin information</returns>
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static PinyinInfo ConvertToPinyin(string text) {
			if (string.IsNullOrEmpty(text))
				return new PinyinInfo();

			// If NPinyin is not loaded, return empty information directly
			// Check via reflection to avoid circular dependencies
			if (!IsNPinyinLoaded())
				return new PinyinInfo();

			// Lazy initialization of NPinyin types and methods (using reflection to avoid JIT-time type resolution)
			// Use double-checked locking pattern to ensure thread safety
			if (_pinyinType == null || _getPinyinMethod == null || _getInitialsMethod == null) {
				try {
					lock (_pinyinInitLock) {
						// Check again to avoid redundant initialization
						if (_pinyinType == null || _getPinyinMethod == null || _getInitialsMethod == null) {
							try {
								// Use reflection to find loaded assembly, avoiding direct reference
								Assembly assembly = FindNPinyinAssembly();
								if (assembly == null)
									return new PinyinInfo();

								// Get type from loaded assembly
								// Add additional null check to prevent issues with assembly itself
								Type pinyinType = null;
								try {
									pinyinType = assembly.GetType("NPinyin.Pinyin", throwOnError: false);
								} catch {
									// If GetType throws exception, pinyinType remains null
								}

								if (pinyinType == null)
									return new PinyinInfo();

								// Get methods (completely avoid using GetMethod, only use GetMethods then manually filter)
								// This completely avoids AmbiguousMatchException
								MethodInfo[] allMethods = GetMethodsSafely(pinyinType);
								if (allMethods == null || allMethods.Length == 0)
									return new PinyinInfo();
								
								// Find GetPinyin(string) and GetInitials(string) methods
								MethodInfo getPinyinMethod = FindMethod(allMethods, "GetPinyin");
								MethodInfo getInitialsMethod = FindMethod(allMethods, "GetInitials");
								
								if (getPinyinMethod == null || getInitialsMethod == null)
									return new PinyinInfo();

								// Only assign to static fields after all checks pass (atomic operation)
								_nPinyinAssembly = assembly;
								_pinyinType = pinyinType;
								_getPinyinMethod = getPinyinMethod;
								_getInitialsMethod = getInitialsMethod;
							} catch (Exception) {
								// If any exception occurs during initialization, return empty information
								// Don't log exception to avoid log pollution
								return new PinyinInfo();
							}
						}
					}
				} catch (Exception) {
					// If exception occurs outside lock, return empty information
					return new PinyinInfo();
				}
			}

			// Check again if methods are initialized (prevent being set to null outside lock)
			// Use local variables to save references, avoiding modification by other threads between check and use
			MethodInfo getPinyin = _getPinyinMethod;
			MethodInfo getInitials = _getInitialsMethod;
			if (getPinyin == null || getInitials == null)
				return new PinyinInfo();

			try {
				// Use reflection to call GetPinyin method (use local variables to ensure thread safety)
				object fullPinyinResult = getPinyin.Invoke(null, new object[] { text });
				string fullPinyin = fullPinyinResult?.ToString() ?? string.Empty;
				// Remove spaces, convert to lowercase
				fullPinyin = fullPinyin.Replace(" ", "").ToLowerInvariant();

				// Use reflection to call GetInitials method (use local variables to ensure thread safety)
				object initialsResult = getInitials.Invoke(null, new object[] { text });
				string firstLetters = initialsResult?.ToString() ?? string.Empty;
				firstLetters = firstLetters.ToLowerInvariant();

				return new PinyinInfo {
					FullPinyin = fullPinyin,
					FirstLetters = firstLetters
				};
			} catch {
				// If conversion fails, return empty information (doesn't affect original search functionality)
				return new PinyinInfo();
			}
		}

		/// <summary>
		/// Safely gets all methods of a type (avoids AmbiguousMatchException)
		/// </summary>
		/// <param name="type">Type to find methods for</param>
		/// <returns>Method array, returns null if failed</returns>
		private static MethodInfo[] GetMethodsSafely(Type type) {
			if (type == null)
				return null;
			
			// Method 1: Without FlattenHierarchy
			try {
				MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
				if (methods != null && methods.Length > 0)
					return methods;
			} catch (AmbiguousMatchException) {
				// If failed, try method 2
			} catch {
				// Other exceptions also try method 2
			}
			
			// Method 2: With FlattenHierarchy
			try {
				MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
				if (methods != null && methods.Length > 0)
					return methods;
			} catch {
				// If still failed, return null
			}
			
			return null;
		}

		/// <summary>
		/// Finds a method with specified name from method array (accepts string or object parameter)
		/// </summary>
		/// <param name="methods">Method array</param>
		/// <param name="methodName">Method name</param>
		/// <returns>Found method, returns null if not found</returns>
		private static MethodInfo FindMethod(MethodInfo[] methods, string methodName) {
			if (methods == null || string.IsNullOrEmpty(methodName))
				return null;
			
			Type stringType = typeof(string);
			
			foreach (MethodInfo method in methods) {
				if (method == null || !method.IsStatic)
					continue;
				
				// method.Name usually doesn't throw exceptions, no need for additional try-catch
				if (method.Name != methodName)
					continue;
				
				try {
					ParameterInfo[] parameters = method.GetParameters();
					if (parameters == null || parameters.Length != 1)
						continue;
					
					ParameterInfo param = parameters[0];
					if (param == null)
						continue;
					
					Type paramType = param.ParameterType;
					if (paramType == null)
						continue;
					
					// Support string or object type parameters
					if (paramType == stringType || paramType == typeof(object))
						return method;
				} catch {
					// Ignore errors for individual methods, continue searching
					continue;
				}
			}
			
			return null;
		}

		/// <summary>
		/// Checks if NPinyin is loaded (via reflection to avoid circular dependencies)
		/// </summary>
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static bool IsNPinyinLoaded() {
			if (_nPinyinLoaded.HasValue)
				return _nPinyinLoaded.Value;

			try {
				// Check CheckModBuildVersionBeforeJIT.nPinyinLoaded via reflection
				// Use current assembly name to avoid hardcoding
				Assembly currentAssembly = Assembly.GetExecutingAssembly();
				string assemblyName = currentAssembly.GetName().Name;
				Type checkType = Type.GetType($"MagicStorage.CheckModBuildVersionBeforeJIT, {assemblyName}");
				if (checkType != null) {
					FieldInfo field = checkType.GetField("nPinyinLoaded", BindingFlags.Public | BindingFlags.Static);
					if (field != null) {
						object value = field.GetValue(null);
						if (value is bool loaded) {
							_nPinyinLoaded = loaded;
							return loaded;
						}
					}
				}
			} catch {
				// If reflection fails, assume not loaded (safe strategy)
			}

			// Default to false to ensure pinyin search is not enabled due to reflection failure
			_nPinyinLoaded = false;
			return false;
		}

		/// <summary>
		/// Gets the loaded NPinyin.Core assembly
		/// Gets from MagicStorageMod to avoid triggering assembly resolution at JIT time
		/// </summary>
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Assembly FindNPinyinAssembly() {
			try {
				// Get assembly reference from MagicStorageMod via reflection
				Type modType = Type.GetType("MagicStorage.MagicStorageMod, MagicStorage");
				if (modType == null)
					return null;

				PropertyInfo prop = modType.GetProperty("NPinyinAssembly", BindingFlags.Public | BindingFlags.Static);
				if (prop == null)
					return null;

				object assemblyObj = prop.GetValue(null);
				if (assemblyObj == null)
					return null;

				return assemblyObj as Assembly;
			} catch (Exception) {
				// If reflection fails, return null
				return null;
			}
		}

		/// <summary>
		/// Clears pinyin cache (for memory management)
		/// </summary>
		public static void ClearCache() {
			_pinyinCache.Clear();
		}

		/// <summary>
		/// Gets cache size (for debugging and monitoring)
		/// </summary>
		public static int CacheSize => _pinyinCache.Count;
	}
}

