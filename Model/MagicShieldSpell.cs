/// <summary>
/// Implementation of the magic shield spell
/// </summary>

using System.Collections.Generic;

public class MagicShieldSpell : Spell
{
    public MagicShieldSpell() : base("Magic Shield", SpellType.DEFENSIVE)
    {
    }

	/// <summary>
	/// Select a target for the spell from a list of candidates and cast the spell on it
	/// </summary>
    /// <param name="potentialTargets">The list of potential targets</param>
    public override void CastOn(List<UnitStack> potentialTargets)
    {
        if (potentialTargets.Count > 0)
        {
            UnitStack toTarget = potentialTargets[0];
            int qty = toTarget.GetTotalQty();
            int candidateQty;
            for (int i = 1; i < potentialTargets.Count; i++)
            {
                candidateQty = potentialTargets[i].GetTotalQty();
                if (toTarget.IsAffectedBy(this) || (candidateQty > qty && !potentialTargets[i].IsAffectedBy(this)))
                {
                    toTarget = potentialTargets[i];
                    qty = candidateQty;
                }
            }
            toTarget.AffectBySpell(this);
        }
    }

}
