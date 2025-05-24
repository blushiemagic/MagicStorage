namespace MagicStorage.Common.Systems.Auditing {
	internal enum AuditAction {
		DepositOne,
		DepositMany,
		WithdrawOne,
		WithdrawMany,
		UnitDeactivate,
		UnitActivate,
		UnitCoreRemove,
		UnitCoreInsert,
		SellItems,
		DestroyItem,
		CraftRequest,
		ControlDeleteUnloadedItems,
		ControlDeleteUnloadedData,
		LinkRemoteAccess,
		LinkPortableAccess,
		SecurityNetworkAssignment,
		SecurityNetworkModification,
		SecurityNetworkDelete,
		SecurityNetworkJoin,
		ControlCompactCoins,
		StatusServerAdmin,
		StatusServerOperatorGranted,
		StatusServerOperatorRemoved
	}
}
