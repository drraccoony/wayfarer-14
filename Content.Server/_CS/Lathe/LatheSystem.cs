using Content.Shared.Lathe;
using Content.Shared.Research.Prototypes;

namespace Content.Server.Lathe
{
    public sealed partial class LatheSystem
    {
        public delegate void GetMaterialAmountDelegate(EntityUid uid, LatheComponent component, string material, ref int amount);
        public delegate void DeductMaterialDelegate(EntityUid uid, LatheComponent component, string material, ref int amount);
        public delegate void GetBufferAmountDelegate(EntityUid uid, LatheComponent component, ref int? bufferAmount);

        public event GetMaterialAmountDelegate? OnGetMaterialAmount;
        public event DeductMaterialDelegate? OnDeductMaterial;
        public event GetBufferAmountDelegate? OnGetBufferAmount;

        /// <summary>
        /// Checks if all required materials are available, taking into account buffer contributions.
        /// </summary>
        private bool CheckMaterialAvailability(EntityUid uid, LatheComponent component, LatheRecipePrototype recipe, int quantity)
        {
            foreach (var (mat, amount) in recipe.Materials)
            {
                var required = AdjustMaterial(amount, recipe.ApplyMaterialDiscount, component.FinalMaterialUseMultiplier) * quantity;

                int available = _materialStorage.GetMaterialAmount(uid, mat);
                OnGetMaterialAmount?.Invoke(uid, component, mat, ref available);

                if (available < required)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Deducts materials, consuming from the buffer first, then from storage.
        /// </summary>
        private bool DeductMaterials(EntityUid uid, LatheComponent component, LatheRecipePrototype recipe, int quantity)
        {
            foreach (var (mat, amount) in recipe.Materials)
            {
                var adjustedAmount = recipe.ApplyMaterialDiscount
                    ? (int)(-amount * component.FinalMaterialUseMultiplier)
                    : -amount;
                adjustedAmount *= quantity;

                int toDeduct = -adjustedAmount; // positive amount to deduct
                OnDeductMaterial?.Invoke(uid, component, mat, ref toDeduct);

                if (toDeduct > 0)
                {
                    if (!_materialStorage.TryChangeMaterialAmount(uid, mat, -toDeduct))
                        return false;
                }
            }
            return true;
        }
    }
}
