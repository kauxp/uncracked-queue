using UnityEngine;
using System.Collections.Generic;

namespace QueueDungeon.Core {
    public class QueueManager : MonoBehaviour {
        public static QueueManager Instance { get; private set; }

        public int baseMaxSize = 6;
        public int currentMaxSize = 6;

        private Queue<MoveCommand> queue = new Queue<MoveCommand>();
        private GameState currentState = GameState.Start;

        private void Awake() {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void OnEnable() {
            CoreEventManager.OnGameTick += ExecuteNext;
            CoreEventManager.OnInputDirection += Enqueue;
            CoreEventManager.OnClearClicked += ClearQueue;
            CoreEventManager.OnStateChanged += HandleStateChanged;
            CoreEventManager.OnRestartClicked += ResetQueueSize;
        }

        private void OnDisable() {
            CoreEventManager.OnGameTick -= ExecuteNext;
            CoreEventManager.OnInputDirection -= Enqueue;
            CoreEventManager.OnClearClicked -= ClearQueue;
            CoreEventManager.OnStateChanged -= HandleStateChanged;
            CoreEventManager.OnRestartClicked -= ResetQueueSize;
        }

        private void ResetQueueSize() {
            currentMaxSize = baseMaxSize;
            queue.Clear();
            Broadcast();
        }

        private void HandleStateChanged(GameState s) {
            currentState = s;
            if (s == GameState.Start) ResetQueueSize();
        }

        private void Enqueue(Direction dir) {
            if (currentState == GameState.Stop || currentState == GameState.Win) return;
            if (queue.Count >= currentMaxSize) return;
            queue.Enqueue(new MoveCommand(dir));
            Broadcast();
        }

        private void ExecuteNext() {
            if (queue.Count == 0) {
                // Queue empty = Game Over
                CoreEventManager.OnStopClicked?.Invoke();
                return;
            }

            MoveCommand cmd = queue.Dequeue();
            CoreEventManager.OnMoveExecuted?.Invoke(cmd);
            Broadcast();
        }

        public void ClearQueue() {
            queue.Clear();
            Broadcast();
        }

        public int Count => queue.Count;

        private void Broadcast() {
            // Need to pass maxSize info if UI renders it dynamically, 
            // but UIManager can read QueueManager.Instance.currentMaxSize directly.
            CoreEventManager.OnQueueUpdated?.Invoke(new List<MoveCommand>(queue));
        }
    }
}
