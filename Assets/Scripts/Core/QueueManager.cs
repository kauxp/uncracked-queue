using UnityEngine;
using System.Collections.Generic;

namespace QueueDungeon.Core {
    public class QueueManager : MonoBehaviour {
        public static QueueManager Instance { get; private set; }

        public int baseMaxSize = 6;
        public int currentMaxSize = 6;

        private Queue<MoveCommand> queue = new Queue<MoveCommand>();
        private GameState currentState = GameState.Start;
        private bool inputReceivedSinceLastTick = false;

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
            inputReceivedSinceLastTick = false;
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
            inputReceivedSinceLastTick = true;
            Broadcast();
        }

        private void ExecuteNext() {
            // Queue completely empty → GAME OVER
            if (queue.Count == 0) {
                CoreEventManager.OnStopClicked?.Invoke();
                return;
            }

            // Player didn't enter any input since last tick → PENALTY (grow queue)
            if (!inputReceivedSinceLastTick) {
                currentMaxSize++;
                CoreEventManager.OnPenalty?.Invoke();
            }

            // Pop and execute the front command
            MoveCommand cmd = queue.Dequeue();
            CoreEventManager.OnMoveExecuted?.Invoke(cmd);

            // Reset the input flag for next tick
            inputReceivedSinceLastTick = false;

            Broadcast();
        }

        public void ClearQueue() {
            queue.Clear();
            Broadcast();
        }

        public int Count => queue.Count;

        private void Broadcast() {
            CoreEventManager.OnQueueUpdated?.Invoke(new List<MoveCommand>(queue));
        }
    }
}
