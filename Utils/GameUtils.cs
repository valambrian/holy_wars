using System.Collections.Generic;

public class GameUtils
{
    public static UnitType GetRepresentative(List<Unit> units)
    {
        if (units == null || units.Count == 0)
        {
            return null;
        }
        UnitType result = units[0].GetUnitType();
        if (result.IsTrainable())
        {
            for (int i = 1; i < units.Count; i++)
            {
                UnitType candidate = units[i].GetUnitType();
                if (!candidate.IsTrainable() || candidate.GetTrainingCost() > result.GetTrainingCost())
                {
                    result = candidate;
                    if (!result.IsTrainable())
                    {
                        break;
                    }
                }
            }
        }
        return result;
    }

    public static string DescribeProvinceList(List<Province> provinces)
    {
        string result = "";
        if (provinces.Count > 0)
        {
            result = provinces[0].GetName();
            for (int j = 1; j < provinces.Count; j++)
            {
                result += ", " + provinces[j].GetName();
            }
        }
        return result;
    }

    public static int CalculateProvinceValue(Province province, Faction faction)
    {
        // NOTE: the multipliers should depend on the map's total income and favors
        int baseValue = 2 * province.GetBaseIncome() + 5 * province.GetBaseFavor();
        int result = baseValue;
        List<Province> neighbors = province.GetNeighbors();
        for (int i = 0; i < neighbors.Count; i++)
        {
            if (neighbors[i].GetDwellersRace() == faction.GetRace() && neighbors[i].GetOwnersFaction().IsMinor())
            {
                result += baseValue;
            }
        }
        return result;
    }

}
