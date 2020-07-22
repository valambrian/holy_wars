/// <summary>
/// Implementation of the confusion spell
/// </summary>

using System.Collections.Generic;

public class ConfusionSpell : Spell
{
    public ConfusionSpell() : base("Confusion", SpellType.OFFENSIVE)
    {
    }

	/// <summary>
	/// Select a target for the spell from a list of candidates
	/// </summary>
    /// <param name="potentialTargets">The list of potential targets</param>
    public override void CastOn(List<UnitStack> potentialTargets)
    {
        if (potentialTargets.Count > 0)
        {
			// note: the spell doesn't work on holy units
            UnitStack toTarget = potentialTargets[0];
            int qty = toTarget.GetTotalQty();
            int candidateQty;
            for (int i = 1; i < potentialTargets.Count; i++)
            {
                candidateQty = potentialTargets[i].GetTotalQty();
                if (toTarget.IsAffectedBy(this) ||
					toTarget.GetUnitType().IsHoly() ||
					(candidateQty > qty &&
					!potentialTargets[i].IsAffectedBy(this) &&
					!potentialTargets[i].GetUnitType().IsHoly()))
                {
                    toTarget = potentialTargets[i];
                    qty = candidateQty;
                }
            }
            if (!toTarget.IsAffectedBy(this) && !toTarget.GetUnitType().IsHoly())
            {
                toTarget.AffectBySpell(this);
            }
        }
    }
}
