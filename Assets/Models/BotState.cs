using System.Collections.Generic;

public class BotState
{
    public int CurrentLevel { get; set; }
    public string ConnectionId { get; set; }

    public int Collected { get; set; }

    public string ElapsedTime { get; set; }

    public int[][] HeroWindow { get; set; }

    public int X { get; set; }
    public int Y { get; set; }

    public bool isHeroSpace(int x,int y)
    {
        var heroXSpaces = new List<int>() {X+1,X+2};
        var heroYSpaces = new List<int>() {Y-1,Y-2};
        return heroXSpaces.Contains(x) && heroYSpaces.Contains(y);
    }

}
