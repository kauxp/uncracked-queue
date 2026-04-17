using UnityEngine;
using QueueDungeon.Core;
using System.Collections;

namespace QueueDungeon.Entities {
    public class PlayerController : MonoBehaviour {
        [Header("Settings")]
        public Vector2Int startGridPosition = new Vector2Int(1, 5);
        public float moveSpeed = 15f;
        public Color flashColor = Color.red;
        
        [HideInInspector] public Vector2Int gridPosition;
        private Color originalColor;
        private Vector3 originalScale;
        private SpriteRenderer spriteRenderer;
        private bool hasKey = false;

        private void Start() {
            spriteRenderer = GetComponent<SpriteRenderer>();
            originalColor = spriteRenderer.color;
            gridPosition = startGridPosition;
            originalScale = transform.localScale;
            SnapToGrid();
        }

        private void OnEnable() {
            CoreEventManager.OnMoveExecuted += HandleMove;
            CoreEventManager.OnPenalty += HandlePenalty;
            CoreEventManager.OnKeyCollected += HandleKeyCollected;
            CoreEventManager.OnRestartClicked += ResetPlayer;
        }

        private void OnDisable() {
            CoreEventManager.OnMoveExecuted -= HandleMove;
            CoreEventManager.OnPenalty -= HandlePenalty;
            CoreEventManager.OnKeyCollected -= HandleKeyCollected;
            CoreEventManager.OnRestartClicked -= ResetPlayer;
        }

        public void ResetPlayer() {
            gridPosition = startGridPosition;
            hasKey = false;
            SnapToGrid();
        }

        private void SnapToGrid() {
            if (GridManager.Instance != null)
                transform.position = GridManager.Instance.GridToWorld(gridPosition);
        }

        private void CheckObstacleHit() {
            var obstacles = FindObjectsByType<ObstacleController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var obs in obstacles) {
                if (obs.gridPosition == this.gridPosition) {
                    HitObstacle();
                    return;
                }
            }
        }

        private void HandleMove(MoveCommand cmd) {
            Vector2Int targetPos = gridPosition + cmd.ToOffset();

            if (GridManager.Instance != null && GridManager.Instance.IsValidPosition(targetPos)) {
                gridPosition = targetPos;
                CheckObstacleHit();

                StopAllCoroutines();
                StartCoroutine(SmoothMove(GridManager.Instance.GridToWorld(gridPosition)));
                
                var gm = FindAnyObjectByType<GameManager>();
                if (gm != null) gm.IncrementScore();
            }
            // If position is invalid (wall), just stay in place — no game over
        }

        private IEnumerator SmoothMove(Vector3 dest) {
            while (Vector3.Distance(transform.position, dest) > 0.01f) {
                transform.position = Vector3.MoveTowards(transform.position, dest, moveSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = dest;
            CheckObstacleHit();
        }

        private void HandlePenalty() {
            // Screen flash is handled by UIManager; keeping this for future 
            // subtle non-color effects (like screen shake or scale pulse)
            StartCoroutine(PenaltyPulse());
        }

        private IEnumerator PenaltyPulse() {
            // Subtle scale pulse instead of color flash
            transform.localScale = originalScale * 1.2f;
            yield return new WaitForSeconds(0.1f);
            transform.localScale = originalScale;
        }

        public void HitObstacle() {
            // Only Player-Obstacle collision causes game over
            CoreEventManager.OnStopClicked?.Invoke();
        }

        private void HandleKeyCollected() {
            hasKey = true;
        }

        private void OnTriggerEnter2D(Collider2D other) {
            if (other.CompareTag("Key")) {
                CoreEventManager.OnKeyCollected?.Invoke();
                Destroy(other.gameObject);
            }
            else if (other.CompareTag("Exit") && hasKey) {
                CoreEventManager.OnPlayerReachedExit?.Invoke();
            }
            else if (other.GetComponent<ObstacleController>() != null) {
                HitObstacle();
            }
        }
    }
}
