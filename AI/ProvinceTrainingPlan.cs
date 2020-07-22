/// <summary>
/// Representation of a plan to train units in a province
/// Contains the province and the list of unit training plans
/// </summary>

using System.Collections.Generic;

public class ProvinceTrainingPlan
{
    private Province _province;
    private List<UnitTrainingPlan> _unitPlans;

    /// <summary>
    /// Class constructor
	/// Creates an empty list of unit training plans
    /// </summary>
    /// <param name="province">Where the training will take place</param>
    public ProvinceTrainingPlan(Province province)
    {
        _province = province;
        _unitPlans = new List<UnitTrainingPlan>();
    }

    /// <summary>
    /// Class constructor
    /// </summary>
    /// <param name="province">Where the training will take place</param>
    /// <param name="plans">Which unit training plans will be executed the next turn</param>
    public ProvinceTrainingPlan(Province province, List<UnitTrainingPlan> plans)
    {
        _province = province;
        _unitPlans = plans;
    }

    /// <summary>
    /// Add unit training plans, if possible
    /// </summary>
    /// <param name="plans">Unit training plans to add</param>
    /// <returns>Whether the plans were added successfully</returns>
    public bool AddUnitTrainingPlans(List<UnitTrainingPlan> plans)
    {
        int additionalManPowerCost = 0;
        for (int i = 0; i < plans.Count; i++)
        {
            additionalManPowerCost += plans[i].GetManpowerCost();
        }

        if (GetManpowerCost() + additionalManPowerCost <= _province.GetManpower())
        {
            _unitPlans.AddRange(plans);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Get total gold cost of executing province's unit training plans
    /// </summary>
    /// <returns>Cost of executing unit training plans</returns>
    public int GetCost()
    {
        int result = 0;
        for (int i = 0; i < _unitPlans.Count; i++)
        {
            result += _unitPlans[i].GetCost();
        }
        return result;
    }

    /// <summary>
    /// Get total manpower cost of executing province's unit training plans
    /// </summary>
    /// <returns>Manpower cost of executing unit training plans</returns>
    public int GetManpowerCost()
    {
        int result = 0;
        for (int i = 0; i < _unitPlans.Count; i++)
        {
            result += _unitPlans[i].GetManpowerCost();
        }
        return result;
    }

    /// <summary>
    /// Get all unit training plans
    /// </summary>
    /// <returns>List of unit training plans</returns>
    public List<UnitTrainingPlan> GetUnitTrainingPlans()
    {
        return _unitPlans;
    }

}
