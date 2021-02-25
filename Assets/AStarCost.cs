internal struct AStarCost
{
    public float Cost;
    public float Heuristic;
    
    public AStarCost(float cost, float heuristic)
    {
        this.Cost = cost;
        this.Heuristic = heuristic;
    }
}