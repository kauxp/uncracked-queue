using UnityEngine;

namespace QueueDungeon.Core {
    [System.Serializable]
    public struct MoveCommand {
        public Direction Direction;

        public MoveCommand(Direction dir) { Direction = dir; }

        public Vector2Int ToOffset() {
            switch (Direction) {
                case Direction.Up:    return new Vector2Int(0, 1);
                case Direction.Down:  return new Vector2Int(0, -1);
                case Direction.Left:  return new Vector2Int(-1, 0);
                case Direction.Right: return new Vector2Int(1, 0);
                default:              return Vector2Int.zero;
            }
        }

        public string ToArrow() {
            switch (Direction) {
                case Direction.Up:    return "↑";
                case Direction.Down:  return "↓";
                case Direction.Left:  return "←";
                case Direction.Right: return "→";
                default:              return "·";
            }
        }
    }
}
