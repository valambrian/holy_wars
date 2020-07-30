/// <summary>
/// A geographical unit in the game
/// </summary>
[System.Serializable]
public class ProvinceData
{
    public int id;
    public string name;
    public int income;
    public int manpower;
    public int favor;
    public int raceId;
    public int factionId;
    public int[] trainable;
    public UnitData[] units;
    public UnitTrainingOrderData[] training;

    public ProvinceData CloneAs(int newId)
    {
        ProvinceData result = new ProvinceData();
        result.id = newId;
        result.name = name;
        result.income = income;
        result.manpower = manpower;
        result.favor = favor;
        result.raceId = raceId;
        result.factionId = factionId;
        result.trainable = trainable;
        result.units = units;
        result.training = training;
        return result;
    }

    public override string ToString()
    {
        return "Province " + name + ", id: " + id + ", income: " + income + ", manpower: " + manpower + ", race: " + raceId + ", owner: " + factionId;
    }
}
