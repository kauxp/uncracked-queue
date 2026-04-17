using UnityEngine;
using UnityEngine.InputSystem;

namespace QueueDungeon.Core {
    public class InputManager : MonoBehaviour {
        private GameState currentState = GameState.Start;

        private void OnEnable() {
            CoreEventManager.OnStateChanged += HandleStateChanged;
        }

        private void OnDisable() {
            CoreEventManager.OnStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(GameState s) {
            currentState = s;
        }

        private void Update() {
            if (currentState == GameState.Stop || currentState == GameState.Win) return;
            if (Keyboard.current == null) return;

            if (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame)
                CoreEventManager.OnInputDirection?.Invoke(Direction.Up);
            else if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame)
                CoreEventManager.OnInputDirection?.Invoke(Direction.Left);
            else if (Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame)
                CoreEventManager.OnInputDirection?.Invoke(Direction.Down);
            else if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame)
                CoreEventManager.OnInputDirection?.Invoke(Direction.Right);
        }
    }
}
