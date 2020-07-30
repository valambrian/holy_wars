[System.Serializable]
public class GameData
{
    public int turn;
    public int currentPlayerIndex;
    public int currentTurnPhase;
    public UnitTypeData[] units;
    public RaceData[] races;
    public FactionData[] factions;
    public ProvinceData[] provinces;
    public MapCellData[] cells;
    public int startingExpeditionsTurn;
    public int[] expeditions;
    public int[] casters;
    public IntIntPair currentExpedition;
    public ArmyMovementOrderData[] movements;
}
