using UnityEngine;
using UnityEngine.InputSystem;

namespace QueueDungeon.Core {
    public enum Difficulty { Easy, Medium, Hard }

    public class GameManager : MonoBehaviour {
        public GameState CurrentState { get; private set; } = GameState.Start;

        [Header("Survival Settings")]
        public Difficulty difficulty = Difficulty.Medium;
        public int score = 0;

        private float tickRate = 0.35f;
        private float tickTimer;

        private void OnEnable() {
            CoreEventManager.OnRunClicked += StartRun;
            CoreEventManager.OnStopClicked += GameOver;
            CoreEventManager.OnTogglePauseClicked += TogglePause;
            CoreEventManager.OnRestartClicked += Restart;
            CoreEventManager.OnPlayerReachedExit += WinGame; // Optional now
        }

        private void OnDisable() {
            CoreEventManager.OnRunClicked -= StartRun;
            CoreEventManager.OnStopClicked -= GameOver;
            CoreEventManager.OnTogglePauseClicked -= TogglePause;
            CoreEventManager.OnRestartClicked -= Restart;
            CoreEventManager.OnPlayerReachedExit -= WinGame;
        }

        private void Start() {
            SetDifficultySpeed();
            ChangeState(GameState.Start);
        }

        private void SetDifficultySpeed() {
            switch (difficulty) {
                case Difficulty.Easy: tickRate = 0.5f; break;
                case Difficulty.Medium: tickRate = 0.35f; break;
                case Difficulty.Hard: tickRate = 0.2f; break;
            }
        }

        private void Update() {
            if (CurrentState == GameState.Run) {
                tickTimer += Time.deltaTime;
                CoreEventManager.OnTimerUpdated?.Invoke(tickTimer / tickRate);
                if (tickTimer >= tickRate) {
                    tickTimer = 0f;
                    CoreEventManager.OnGameTick?.Invoke();
                }
            }

            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) {
                CoreEventManager.OnTogglePauseClicked?.Invoke();
            }
        }

        private void ChangeState(GameState s) {
            if (CurrentState == s) return;
            CurrentState = s;
            if (s == GameState.Run) tickTimer = 0f;
            CoreEventManager.OnStateChanged?.Invoke(s);
        }

        private void StartRun() {
            SetDifficultySpeed();
            if (CurrentState == GameState.Start || CurrentState == GameState.Stop) score = 0;
            ChangeState(GameState.Run);
        }
        
        private void GameOver() => ChangeState(GameState.Stop);
        private void WinGame() => ChangeState(GameState.Win);
        
        private void TogglePause() {
            if (CurrentState == GameState.Run) ChangeState(GameState.Pause);
            else if (CurrentState == GameState.Pause) ChangeState(GameState.Run);
        }

        private void Restart() {
            score = 0;
            CoreEventManager.OnClearClicked?.Invoke();
            ChangeState(GameState.Start);
            SetDifficultySpeed();
        }

        public void IncrementScore() {
            if (CurrentState == GameState.Run) score++;
        }
    }
}
