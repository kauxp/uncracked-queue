using System;
using System.Collections.Generic;
using UnityEngine;

namespace QueueDungeon.Core {
    public static class CoreEventManager {
        public static Action<GameState> OnStateChanged;
        public static Action OnGameTick;
        public static Action<Direction> OnInputDirection;
        public static Action OnRunClicked;
        public static Action OnStopClicked;
        public static Action OnClearClicked;
        public static Action OnRestartClicked;
        public static Action<List<MoveCommand>> OnQueueUpdated;
        public static Action<MoveCommand> OnMoveExecuted;
        public static Action OnPenalty;
        public static Action OnKeyCollected;
        public static Action<float> OnTimerUpdated;
        public static Action OnTogglePauseClicked;
        public static Action OnPlayerReachedExit;
        public static Action OnEntitiesSpawned;
        public static Action<int> OnRoundCompleted;
        public static Action OnContinueClicked;
    }
}
