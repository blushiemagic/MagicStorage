using System;
using System.Reflection;
using System.Reflection.Emit;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace MagicStorage {
	partial class Utility {
		private static bool onStackHooksDelegateBuilt;
		private delegate void OnStack(Item destination, Item source, int numToTransfer);
		private static OnStack onStackHooksDelegate;

		internal static void UnloadHookDelegate() {
			onStackHooksDelegate = null;
			onStackHooksDelegateBuilt = false;
		}

		private static void BuildOnStackHooksDelegate() {
			if (onStackHooksDelegateBuilt)
				return;

#if TML_2022_09
			onStackHooksDelegate = null;
#else
			// Build a DynamicMethod which invokes the hooks
			FieldInfo Item_globalItems = typeof(Item).GetField("globalItems", BindingFlags.NonPublic | BindingFlags.Instance);
			MethodInfo Item_get_ModItem = typeof(Item).GetProperty("ModItem", BindingFlags.Public | BindingFlags.Instance).GetGetMethod();
			
			FieldInfo ItemLoader_HookOnStack = typeof(ItemLoader).GetField("HookOnStack", BindingFlags.NonPublic | BindingFlags.Static);

			MethodInfo ModItem_OnStack = typeof(ModItem).GetMethod(nameof(ModItem.OnStack), BindingFlags.Public | BindingFlags.Instance);
			MethodInfo GlobalItem_OnStack = typeof(GlobalItem).GetMethod(nameof(GlobalItem.OnStack), BindingFlags.Public | BindingFlags.Instance);

			Type HookList_T_InstanceEnumerator = typeof(HookList<GlobalItem>.InstanceEnumerator);
			MethodInfo HookList_T_Enumerate = typeof(HookList<GlobalItem>).GetMethod(nameof(HookList<GlobalItem>.Enumerate), BindingFlags.Public | BindingFlags.Instance);
			MethodInfo HookList_T_InstanceEnumerator_MoveNext = HookList_T_InstanceEnumerator.GetMethod(nameof(HookList<GlobalItem>.InstanceEnumerator.MoveNext), BindingFlags.Public | BindingFlags.Instance);
			MethodInfo HookList_T_InstanceEnumerator_get_Current = HookList_T_InstanceEnumerator.GetProperty(nameof(HookList<GlobalItem>.InstanceEnumerator.Current), BindingFlags.Public | BindingFlags.Instance).GetGetMethod();

			DynamicMethod dmd = new(typeof(Utility).FullName + ".BuildOnStackHooksDelegate.<>DMD", null, new Type[] { typeof(Item), typeof(Item), typeof(int) }, typeof(MagicStorageMod).Module, skipVisibility: true);
			ILGenerator il = dmd.GetILGenerator();
			LocalBuilder enumerator = il.DeclareLocal(HookList_T_InstanceEnumerator);

			/*
			Desired resulting method:
			
			HookList<GlobalItem>.InstanceEnumerator enumerator = ItemLoader.HookOnStack.Enumerate(arg_0.globalItems);
			while (enumerator.MoveNext()) {
				enumerator.Current.OnStack(arg_0, arg_1, arg_2);
			}
			if (arg_0.ModItem is not null) {
				arg_0.ModItem.OnStack(arg_1, arg_2);
			}
			*/

			// HookList<GlobalItem>.InstanceEnumerator enumerator = ItemLoader.HookOnStack.Enumerate(arg_0.globalItems);
			il.Emit(OpCodes.Ldsfld, ItemLoader_HookOnStack);
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, Item_globalItems);
			il.EmitCall(OpCodes.Call, HookList_T_Enumerate, null);
			il.Emit(OpCodes.Stloc, enumerator);

			// while (enumerator.MoveNext()) {
			Label whileBlockStart = il.DefineLabel();
			Label whileBlockEnd = il.DefineLabel();
			il.MarkLabel(whileBlockStart);
			il.Emit(OpCodes.Ldloc, enumerator);
			il.EmitCall(OpCodes.Call, HookList_T_InstanceEnumerator_MoveNext, null);
			il.Emit(OpCodes.Brfalse_S, whileBlockEnd);

				// enumerator.Current.OnStack(arg_0, arg_1, arg_2);
				il.Emit(OpCodes.Ldloc, enumerator);
				il.EmitCall(OpCodes.Call, HookList_T_InstanceEnumerator_get_Current, null);
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Ldarg_2);
				il.EmitCall(OpCodes.Call, GlobalItem_OnStack, null);
				il.Emit(OpCodes.Br, whileBlockStart);
			
			// }
			il.MarkLabel(whileBlockEnd);

			// if (arg_0.ModItem is not null) {
			Label modItemNotNullBlockEnd = il.DefineLabel();
			il.Emit(OpCodes.Ldarg_0);
			il.EmitCall(OpCodes.Call, Item_get_ModItem, null);
			il.Emit(OpCodes.Brfalse_S, modItemNotNullBlockEnd);
			
				// arg_0.ModItem.OnStack(arg_1, arg_2);
				il.Emit(OpCodes.Ldarg_0);
				il.EmitCall(OpCodes.Call, Item_get_ModItem, null);
				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Ldarg_2);
				il.EmitCall(OpCodes.Call, ModItem_OnStack, null);

			// }
			il.MarkLabel(modItemNotNullBlockEnd);

			il.Emit(OpCodes.Ret);

			onStackHooksDelegate = dmd.CreateDelegate<OnStack>();
#endif
			onStackHooksDelegateBuilt = true;
		}
	}
}
