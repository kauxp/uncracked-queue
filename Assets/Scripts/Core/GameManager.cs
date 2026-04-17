using UnityEngine;
using UnityEngine.InputSystem;

namespace QueueDungeon.Core {
    public class GameManager : MonoBehaviour {
        public GameState CurrentState { get; private set; } = GameState.Start;

        [Header("Survival Settings")]
        public int score = 0;

        private float tickRate = 0.6f;
        private float tickTimer;
        private bool initialized = false;

        private void OnEnable() {
            CoreEventManager.OnRunClicked += StartRun;
            CoreEventManager.OnStopClicked += GameOver;
            CoreEventManager.OnTogglePauseClicked += TogglePause;
            CoreEventManager.OnRestartClicked += Restart;
            CoreEventManager.OnPlayerReachedExit += WinGame;
            CoreEventManager.OnContinueClicked += ContinueRound;
        }

        private void OnDisable() {
            CoreEventManager.OnRunClicked -= StartRun;
            CoreEventManager.OnStopClicked -= GameOver;
            CoreEventManager.OnTogglePauseClicked -= TogglePause;
            CoreEventManager.OnRestartClicked -= Restart;
            CoreEventManager.OnPlayerReachedExit -= WinGame;
            CoreEventManager.OnContinueClicked -= ContinueRound;
        }

        private void Start() {
            // Force-fire the initial state event (bypassing the same-state guard)
            initialized = false;
            ChangeState(GameState.Start);
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
            // Allow first call even if state matches (initialization)
            if (initialized && CurrentState == s) return;
            initialized = true;
            CurrentState = s;
            if (s == GameState.Run) tickTimer = 0f;
            CoreEventManager.OnStateChanged?.Invoke(s);
        }

        private void StartRun() {
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
            initialized = false;
            CoreEventManager.OnClearClicked?.Invoke();
            ChangeState(GameState.Start);
        }

        private void ContinueRound() {
            initialized = false;
            CoreEventManager.OnClearClicked?.Invoke();
            ChangeState(GameState.Start);
        }

        public void IncrementScore() {
            if (CurrentState == GameState.Run) score++;
        }
    }
}
