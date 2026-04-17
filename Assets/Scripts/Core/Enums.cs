namespace QueueDungeon.Core {
    public enum GameState { Start, Run, Pause, Stop, Win }
    public enum Direction { Up, Down, Left, Right, None }
    public enum CellType { Empty = 0, Wall = 1, Obstacle = 2, Key = 3, Exit = 4 }
    public enum ObstacleShape { Circle, Diamond, Triangle, Cross }
}
