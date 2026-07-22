namespace MagicStorage.Common.Systems.RecurrentRecipes {
	internal enum DemandPlannerAuthorityClass {
		Unsupported,
		Direct,
		RecipeGroup,
		VirtualDependency,
		CyclicBounded,
		RootAlternate
	}

	internal static class DemandPlannerAuthority {
		private const bool AuthorizeCyclicBoundedForUi = true;
		private const bool AuthorizeRootAlternateForUi = true;

		public static bool IsVirtualDependencyUiAuthorized => true;

		public static bool IsCyclicBoundedUiAuthorized => AuthorizeCyclicBoundedForUi;

		public static bool IsRootAlternateUiAuthorized => AuthorizeRootAlternateForUi;

		public static DemandPlannerAuthorityClass Classify(InventoryCraftabilityRecipeProbe probe) {
			if (!probe.HasCandidate)
				return DemandPlannerAuthorityClass.Unsupported;

			if ((probe.Flags & InventoryCraftabilityProbeFlags.AlternateSameResultRecipe) != 0)
				return DemandPlannerAuthorityClass.RootAlternate;

			if ((probe.Flags & InventoryCraftabilityProbeFlags.CyclicDependencyRegion) != 0)
				return DemandPlannerAuthorityClass.CyclicBounded;

			if ((probe.Flags & InventoryCraftabilityProbeFlags.VirtualDependency) != 0)
				return DemandPlannerAuthorityClass.VirtualDependency;

			if ((probe.Flags & InventoryCraftabilityProbeFlags.RecipeGroupDependency) != 0)
				return DemandPlannerAuthorityClass.RecipeGroup;

			return DemandPlannerAuthorityClass.Direct;
		}

		public static bool IsUiAuthorized(InventoryCraftabilityRecipeProbe probe) {
			return Classify(probe) switch {
				DemandPlannerAuthorityClass.Direct => true,
				DemandPlannerAuthorityClass.RecipeGroup => true,
				DemandPlannerAuthorityClass.VirtualDependency => IsVirtualDependencyUiAuthorized,
				DemandPlannerAuthorityClass.CyclicBounded => AuthorizeCyclicBoundedForUi,
				DemandPlannerAuthorityClass.RootAlternate => AuthorizeRootAlternateForUi,
				_ => false
			};
		}

		public static bool IsDiagnosticSupported(InventoryCraftabilityRecipeProbe probe) {
			return Classify(probe) switch {
				DemandPlannerAuthorityClass.Direct => true,
				DemandPlannerAuthorityClass.RecipeGroup => true,
				DemandPlannerAuthorityClass.VirtualDependency => true,
				DemandPlannerAuthorityClass.CyclicBounded => true,
				DemandPlannerAuthorityClass.RootAlternate => true,
				_ => false
			};
		}

		public static bool IsChildCandidateSupported(InventoryCraftabilityRecipeProbe probe, bool allowAlternateSameResult) {
			if (!probe.HasCandidate)
				return false;

			return allowAlternateSameResult
				|| (probe.Flags & InventoryCraftabilityProbeFlags.AlternateSameResultRecipe) == 0;
		}

		public static bool IsTreePreviewSupported(InventoryCraftabilityRecipeProbe probe)
			=> Classify(probe) == DemandPlannerAuthorityClass.Direct;
	}
}
