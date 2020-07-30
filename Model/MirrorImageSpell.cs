/// <summary>
/// Implementation of the mirror image spell
/// </summary>

using System.Collections.Generic;

public class MirrorImageSpell : Spell
{
    public MirrorImageSpell() : base("Mirror Image", SpellType.UNIT_CREATION)
    {
    }


	/// <summary>
	/// Select a unit stack from a list of existing ones and make an illusory clone of it
	/// </summary>
    /// <param name="existing">The list of existing unit stacks</param>
    public override UnitStack Create(List<UnitStack> existing)
    {
        if (existing.Count > 0)
        {
            UnitStack toClone = existing[0];
            int qty = toClone.GetTotalQty();
            int candidateQty;
            for (int i = 1; i < existing.Count; i++)
            {
                candidateQty = existing[i].GetTotalQty();
                if (candidateQty > qty)
                {
                    toClone = existing[i];
                    qty = candidateQty;
                }
            }

            UnitType illusion = new UnitType(toClone.GetUnitType());
            illusion.SetShield(0);
            illusion.SetArmor(0);
            illusion.SetHitPoints(1);
            illusion.AddAttackQuality(AttackData.Quality.ILLUSORY);

            Unit mirrorImage = new Unit(illusion, toClone.GetTotalQty());
            UnitStack stack = new UnitStack(mirrorImage, toClone.GetProvinceToRetreat());
            stack.AffectBySpell(this);

            return stack;
        }
        return null;
    }

}
