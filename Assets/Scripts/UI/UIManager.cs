using UnityEngine;
using UnityEngine.UI;
using QueueDungeon.Core;
using QueueDungeon.Entities;
using System.Collections.Generic;
using TMPro;

namespace QueueDungeon.UI {
    public class UIManager : MonoBehaviour {
        [Header("Queue Panel")]
        public Image[] queueSlots;
        public TextMeshProUGUI[] slotTexts;

        [Header("Icons")]
        public Sprite upArrow;
        public Sprite downArrow;
        public Sprite leftArrow;
        public Sprite rightArrow;
        public Sprite emptySlot;

        [Header("Timers & Status")]
        public TextMeshProUGUI keyStatusText;
        public Image countdownBar;
        public TextMeshProUGUI obstaclePatternsText;

        [Header("Buttons & UI")]
        public Button runButton;
        public Button restartButton;
        public TMP_Dropdown difficultyDropdown;

        [Header("Effects")]
        public Image penaltyFlash;

        [Header("Panels")]
        public GameObject winPanel;
        public GameObject gameOverPanel;
        public TextMeshProUGUI gameOverScoreText;

        private GameState currentState = GameState.Start;

        private List<Image> dynamicQueueSlots = new List<Image>();

        private void OnEnable() {
            CoreEventManager.OnQueueUpdated += UpdateQueueVisuals;
            CoreEventManager.OnTimerUpdated += UpdateTimerVisual;
            CoreEventManager.OnPenalty += FlashPenalty;
            CoreEventManager.OnStateChanged += HandleStateChange;
            CoreEventManager.OnEntitiesSpawned += SetupDynamicObstacleUI;
        }

        private void OnDisable() {
            CoreEventManager.OnQueueUpdated -= UpdateQueueVisuals;
            CoreEventManager.OnTimerUpdated -= UpdateTimerVisual;
            CoreEventManager.OnPenalty -= FlashPenalty;
            CoreEventManager.OnStateChanged -= HandleStateChange;
            CoreEventManager.OnEntitiesSpawned -= SetupDynamicObstacleUI;
        }

        private void Start() {
            if (runButton != null) runButton.onClick.AddListener(() => {
                var gm = FindAnyObjectByType<GameManager>();
                if (gm != null && difficultyDropdown != null) {
                    gm.difficulty = (Difficulty)difficultyDropdown.value;
                }
                CoreEventManager.OnRunClicked?.Invoke();
            });
            if (restartButton != null) restartButton.onClick.AddListener(() => CoreEventManager.OnRestartClicked?.Invoke());

            if (queueSlots != null) dynamicQueueSlots.AddRange(queueSlots);

            UpdateQueueVisuals(new List<MoveCommand>());
            if (penaltyFlash != null) penaltyFlash.gameObject.SetActive(false);
            
            HandleStateChange(GameState.Start);
        }

        private void Update() {
            var gm = FindAnyObjectByType<GameManager>();
            if (gm != null && keyStatusText != null) {
                keyStatusText.text = $"Score: {gm.score}";
            }
        }

        private void SetupDynamicObstacleUI() {
            if (obstaclePatternsText == null) {
                GameObject textObj = new GameObject("DynamicObstacleTextGenerated");
                textObj.transform.SetParent(this.transform, false);
                var rect = textObj.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(1, 1);
                rect.anchorMax = new Vector2(1, 1);
                rect.pivot = new Vector2(1, 1);
                rect.anchoredPosition = new Vector2(-10, -10);
                rect.sizeDelta = new Vector2(500, 500);

                obstaclePatternsText = textObj.AddComponent<TextMeshProUGUI>();
                obstaclePatternsText.fontSize = 32;
                obstaclePatternsText.alignment = TextAlignmentOptions.TopRight;
            }

            var gm = GridManager.Instance;
            string txt = "";

            // Color legend for Player, Key, Door
            if (gm != null) {
                string pc = ColorUtility.ToHtmlStringRGB(gm.playerColor);
                string kc = ColorUtility.ToHtmlStringRGB(gm.keyColor);
                string ec = ColorUtility.ToHtmlStringRGB(gm.exitColor);
                txt += $"<color=#{pc}>■</color> Player   ";
                txt += $"<color=#{kc}>■</color> Key   ";
                txt += $"<color=#{ec}>■</color> Door\n\n";
            }

            // Obstacle patterns
            ObstacleController[] obs = FindObjectsByType<ObstacleController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var o in obs) {
                string hexColor = ColorUtility.ToHtmlStringRGB(o.displayColor);
                txt += $"<color=#{hexColor}>■ {o.label}</color> : ";
                foreach(var d in o.pattern) {
                    txt += new MoveCommand(d).ToArrow() + " ";
                }
                txt += "\n";
            }
            obstaclePatternsText.text = txt;
        }

        private void UpdateQueueVisuals(List<MoveCommand> queue) {
            int expectedSize = QueueManager.Instance != null ? QueueManager.Instance.currentMaxSize : queue.Count;

            // Dynamically clone more slots if the penalty increases the max size!
            while (dynamicQueueSlots.Count < expectedSize) {
                if (dynamicQueueSlots.Count > 0 && dynamicQueueSlots[0] != null) {
                    var clone = Instantiate(dynamicQueueSlots[0].gameObject, dynamicQueueSlots[0].transform.parent);
                    dynamicQueueSlots.Add(clone.GetComponent<Image>());
                } else break;
            }

            for (int i = 0; i < dynamicQueueSlots.Count; i++) {
                var img = dynamicQueueSlots[i];
                if (img == null) continue;
                
                img.gameObject.SetActive(i < expectedSize);

                if (i < queue.Count) {
                    img.sprite = GetSpriteForDirection(queue[i].Direction);
                    img.color = (i == 0 && currentState == GameState.Run)
                        ? new Color(0.15f, 0.68f, 0.38f, 1f)
                        : Color.white;
                    
                    var textUI = img.GetComponentInChildren<TextMeshProUGUI>();
                    if (textUI != null) textUI.text = queue[i].ToArrow();
                } else {
                    img.sprite = emptySlot;
                    img.color = new Color(1, 1, 1, 0.3f);
                    
                    var textUI = img.GetComponentInChildren<TextMeshProUGUI>();
                    if (textUI != null) textUI.text = "·";
                }
            }
        }

        private Sprite GetSpriteForDirection(Direction dir) {
            switch (dir) {
                case Direction.Up:    return upArrow;
                case Direction.Down:  return downArrow;
                case Direction.Left:  return leftArrow;
                case Direction.Right: return rightArrow;
                default:              return emptySlot;
            }
        }

        private void UpdateTimerVisual(float normalized) {
            if (countdownBar != null)
                countdownBar.fillAmount = Mathf.Clamp01(1f - normalized);
        }

        private void FlashPenalty() {
            if (penaltyFlash != null) StartCoroutine(PenaltyFlashRoutine());
        }

        private System.Collections.IEnumerator PenaltyFlashRoutine() {
            penaltyFlash.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.15f);
            penaltyFlash.gameObject.SetActive(false);
        }

        private void HandleStateChange(GameState state) {
            currentState = state;

            if (winPanel != null) winPanel.SetActive(state == GameState.Win);

            if (gameOverPanel != null) {
                gameOverPanel.SetActive(state == GameState.Stop);
                if (state == GameState.Stop && gameOverScoreText != null) {
                    var gm = FindAnyObjectByType<GameManager>();
                    if (gm != null) gameOverScoreText.text = $"Final Score: {gm.score}";
                }
            }

            if (runButton != null) runButton.gameObject.SetActive(state == GameState.Start);
            if (difficultyDropdown != null) difficultyDropdown.gameObject.SetActive(state == GameState.Start);

            if (state == GameState.Start || state == GameState.Stop) {
                UpdateTimerVisual(0f);
            }
        }
    }
}
