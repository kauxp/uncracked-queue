using UnityEngine;
using QueueDungeon.Core;
using System.Collections.Generic;
using System.Collections;

namespace QueueDungeon.Entities {
    public class ObstacleController : MonoBehaviour {
        [Header("UI Display")]
        public string label = "patrol guard";
        public Color displayColor = new Color(0.9f, 0.25f, 0.2f);
        public ObstacleShape shape = ObstacleShape.Circle;

        [Header("Movement")]
        public Vector2Int startGridPosition = new Vector2Int(5, 5);
        public List<Direction> pattern = new List<Direction>();
        public float moveSpeed = 15f;

        [HideInInspector] public Vector2Int gridPosition;
        private int patternIndex = 0;

        private void Start() {
            gridPosition = startGridPosition;
            SnapToGrid();
        }

        private void OnEnable() {
            CoreEventManager.OnGameTick += Step;
            CoreEventManager.OnRestartClicked += ResetObstacle;
        }

        private void OnDisable() {
            CoreEventManager.OnGameTick -= Step;
            CoreEventManager.OnRestartClicked -= ResetObstacle;
        }

        public void ResetObstacle() {
            gridPosition = startGridPosition;
            patternIndex = 0;
            SnapToGrid();
        }

        private void SnapToGrid() {
            if (GridManager.Instance != null) {
                transform.position = GridManager.Instance.GridToWorld(gridPosition);
            }
        }

        private void Step() {
            if (pattern.Count == 0) return;

            Direction dir = pattern[patternIndex];
            patternIndex = (patternIndex + 1) % pattern.Count;

            Vector2Int nextPos = gridPosition + new MoveCommand(dir).ToOffset();

            if (GridManager.Instance != null && GridManager.Instance.IsValidPosition(nextPos)) {
                gridPosition = nextPos;
                CheckPlayerHit();
                StopAllCoroutines();
                StartCoroutine(SmoothMove(GridManager.Instance.GridToWorld(gridPosition)));
            } else {
                // Can't move — still check for player collision at current position
                CheckPlayerHit();
            }
        }

        private IEnumerator SmoothMove(Vector3 dest) {
            while (Vector3.Distance(transform.position, dest) > 0.01f) {
                transform.position = Vector3.MoveTowards(transform.position, dest, moveSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = dest;
            // Only check collision after movement is complete
            CheckPlayerHit();
        }

        private void CheckPlayerHit() {
            PlayerController p = FindAnyObjectByType<PlayerController>();
            if (p != null && p.gridPosition == gridPosition) {
                p.HitObstacle();
            }
        }

    }
}
