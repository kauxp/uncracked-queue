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
        private SpriteRenderer spriteRenderer;
        private bool hasKey = false;

        private void Start() {
            spriteRenderer = GetComponent<SpriteRenderer>();
            originalColor = spriteRenderer.color;
            gridPosition = startGridPosition;
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

        private void HandleMove(MoveCommand cmd) {
            Vector2Int targetPos = gridPosition + cmd.ToOffset();

            if (GridManager.Instance != null && GridManager.Instance.IsValidPosition(targetPos)) {
                gridPosition = targetPos;
                StopAllCoroutines();
                StartCoroutine(SmoothMove(GridManager.Instance.GridToWorld(gridPosition)));
                
                var gm = FindAnyObjectByType<GameManager>();
                if (gm != null) gm.IncrementScore();
            } else {
                CoreEventManager.OnStopClicked?.Invoke(); // Hit boundary, game over!
            }
        }

        private IEnumerator SmoothMove(Vector3 dest) {
            while (Vector3.Distance(transform.position, dest) > 0.01f) {
                transform.position = Vector3.MoveTowards(transform.position, dest, moveSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = dest;
        }

        private void HandlePenalty() {
            StartCoroutine(PenaltyRoutine());
        }

        private IEnumerator PenaltyRoutine() {
            if (spriteRenderer != null) spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(0.15f);
            if (spriteRenderer != null) spriteRenderer.color = originalColor;
        }

        public void HitObstacle() {
            // Collision with obstacle -> lose
            CoreEventManager.OnStopClicked?.Invoke();
        }

        private void HandleKeyCollected() {
            hasKey = true;
        }

        private void OnTriggerEnter2D(Collider2D other) {
            if (other.CompareTag("Key")) {
                CoreEventManager.OnKeyCollected?.Invoke();
                other.gameObject.SetActive(false);
            }
            else if (other.CompareTag("Exit") && hasKey) {
                CoreEventManager.OnPlayerReachedExit?.Invoke();
            }
        }
    }
}
