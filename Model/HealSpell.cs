/// <summary>
/// Class representing a dungeon exploration event
/// </summary>

using System.Collections.Generic;

public class HealSpell : Spell
{
    /// <summary>
    /// Class constructor
    /// </summary>
    public HealSpell() : base("Heal", SpellType.RESTORATIVE)
    {
    }

	/// <summary>
	/// Choose a unit stack from the list of potential targets and cast the spell on it
	/// </summary>
    /// <param name="potentialTargets">The list of potential targets</param>
    public override void CastOn(List<UnitStack> potentialTargets)
    {
        if (potentialTargets.Count > 0)
        {
            UnitStack toTarget = potentialTargets[0];
            int wounds = toTarget.GetWoundPoints();
            int candidateWounds;
            for (int i = 1; i < potentialTargets.Count; i++)
            {
                candidateWounds = potentialTargets[i].GetWoundPoints();
                if (candidateWounds > wounds)
                {
                    toTarget = potentialTargets[i];
                    wounds = candidateWounds;
                }
            }
            if (wounds > 0)
            {
                toTarget.Heal();
                toTarget.AffectBySpell(this);
            }
        }
    }

}
