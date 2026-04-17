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

        [Header("Icons")]
        public Sprite upArrow;
        public Sprite downArrow;
        public Sprite leftArrow;
        public Sprite rightArrow;
        public Sprite emptySlot;

        [Header("Timers & Status")]
        public TextMeshProUGUI keyStatusText;
        public Image countdownBar;

        [Header("Legend")]
        public TextMeshProUGUI legendText;

        [Header("Buttons & UI")]
        public Button runButton;
        public Button restartButton;

        [Header("Effects")]
        public Image penaltyFlash;

        [Header("Panels")]
        public GameObject winPanel;
        public GameObject gameOverPanel;
        public TextMeshProUGUI gameOverScoreText;
        public TextMeshProUGUI winScoreText;

        private GameState currentState = GameState.Start;
        private List<Image> dynamicQueueSlots = new List<Image>();

        private void OnEnable() {
            CoreEventManager.OnQueueUpdated += UpdateQueueVisuals;
            CoreEventManager.OnTimerUpdated += UpdateTimerVisual;
            CoreEventManager.OnPenalty += FlashPenalty;
            CoreEventManager.OnStateChanged += HandleStateChange;
            CoreEventManager.OnEntitiesSpawned += BuildLegend;
        }

        private void OnDisable() {
            CoreEventManager.OnQueueUpdated -= UpdateQueueVisuals;
            CoreEventManager.OnTimerUpdated -= UpdateTimerVisual;
            CoreEventManager.OnPenalty -= FlashPenalty;
            CoreEventManager.OnStateChanged -= HandleStateChange;
            CoreEventManager.OnEntitiesSpawned -= BuildLegend;
        }

        private void Start() {
            if (runButton != null) runButton.onClick.AddListener(() => {
                CoreEventManager.OnRunClicked?.Invoke();
            });
            if (restartButton != null) restartButton.onClick.AddListener(() => CoreEventManager.OnRestartClicked?.Invoke());

            if (queueSlots != null && queueSlots.Length > 0) {
                dynamicQueueSlots.Clear();
                dynamicQueueSlots.AddRange(queueSlots);
                foreach(var slot in dynamicQueueSlots) slot.gameObject.SetActive(true);
            }

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

        // ── Dynamic Legend (Large Scale & Safe Text) ──────────────

        private GameObject legendPanel;

        private void EnsureLegendPanel() {
            if (legendPanel != null) return;

            if (legendText != null) {
                legendPanel = legendText.transform.parent.gameObject;
                legendText.gameObject.SetActive(false);
                if (legendPanel.GetComponent<VerticalLayoutGroup>() == null) {
                     var vlg = legendPanel.AddComponent<VerticalLayoutGroup>();
                     vlg.childControlWidth = true;
                     vlg.childControlHeight = true;
                     vlg.padding = new RectOffset(20, 20, 20, 20);
                     vlg.spacing = 15;
                }
                return;
            }

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            float panelWidth = 380f; 

            legendPanel = new GameObject("LegendPanel_Auto");
            legendPanel.transform.SetParent(canvas.transform, false);
            var panelRect = legendPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1, 1);
            panelRect.anchorMax = new Vector2(1, 1);
            panelRect.pivot = new Vector2(1, 1);
            panelRect.anchoredPosition = new Vector2(-20, -20);
            panelRect.sizeDelta = new Vector2(panelWidth, 100);

            var bg = legendPanel.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.04f, 0.1f, 0.95f);

            var csf = legendPanel.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var vlg2 = legendPanel.AddComponent<VerticalLayoutGroup>();
            vlg2.padding = new RectOffset(20, 20, 20, 20);
            vlg2.spacing = 15;
            vlg2.childControlWidth = true;
            vlg2.childControlHeight = true;
        }

        private void BuildLegend() {
            EnsureLegendPanel();
            if (legendPanel == null) return;

            // Clear old entries
            foreach (Transform child in legendPanel.transform) {
                if (child.GetComponent<LegendEntry>() != null || child.name == "LegendHeader") {
                    Destroy(child.gameObject);
                }
            }

            var gm = GridManager.Instance;
            if (gm == null) return;

            // Header
            CreateLegendHeader("LEGEND", 36, Color.white);

            // Entries
            CreateLegendEntry(gm.SquareSprite, gm.playerColor, "Player");
            CreateLegendEntry(gm.DiamondSprite, gm.keyColor, "Key");
            CreateLegendEntry(gm.TriangleSprite, gm.exitColor, "Exit");

            ObstacleController[] obstacles = FindObjectsByType<ObstacleController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (obstacles.Length > 0) {
                CreateLegendHeader("\nOBSTACLES", 28, new Color(0.8f, 0.8f, 0.8f));
                HashSet<ObstacleShape> shownShapes = new HashSet<ObstacleShape>();
                foreach (var obs in obstacles) {
                    if (shownShapes.Contains(obs.shape)) continue;
                    shownShapes.Add(obs.shape);

                    string arrows = "";
                    foreach (var d in obs.pattern) {
                        arrows += new MoveCommand(d).ToArrow() + " ";
                    }
                    CreateLegendEntry(gm.GetSpriteForShape(obs.shape), gm.obstacleColor, $"{obs.shape}\n<color=#CCCCCC><size=70%>{arrows.TrimEnd()}</size></color>");
                }
            }
        }

        private void CreateLegendHeader(string text, int fontSize, Color color) {
            GameObject headerObj = new GameObject("LegendHeader");
            headerObj.transform.SetParent(legendPanel.transform, false);
            var headerText = headerObj.AddComponent<TextMeshProUGUI>();
            headerText.fontSize = fontSize;
            headerText.color = color;
            headerText.fontStyle = FontStyles.Bold;
            headerText.text = text;
            headerText.alignment = TextAlignmentOptions.TopLeft;
        }

        private void CreateLegendEntry(Sprite icon, Color color, string text) {
            GameObject row = new GameObject("LegendRow");
            row.transform.SetParent(legendPanel.transform, false);
            
            var hl = row.AddComponent<HorizontalLayoutGroup>();
            hl.childControlWidth = false;
            hl.childControlHeight = false;
            hl.childAlignment = TextAnchor.MiddleLeft;
            hl.spacing = 20;

            var le = row.AddComponent<LegendEntry>();

            // Icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(row.transform, false);
            var iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(40, 40);
            le.iconImage = iconObj.AddComponent<Image>();

            // Text
            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(row.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(280, 50); // taller to fit two lines potentially
            le.labelText = textObj.AddComponent<TextMeshProUGUI>();
            le.labelText.fontSize = 28;
            le.labelText.color = Color.white;
            le.labelText.alignment = TextAlignmentOptions.TopLeft;
            le.labelText.enableWordWrapping = false;
            le.labelText.richText = true;

            le.Setup(icon, color, text);
        }

        // ── Win / Game Over ───────────────────────────────────────

        private void EnsureWinPanel() {
            if (winPanel != null) {
                if (winPanel.name == "WinPanel_Auto") return;
                winPanel.SetActive(false);
            }
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            winPanel = CreateOverlayPanel(canvas.transform, "WinPanel_Auto", new Color(0.02f, 0.15f, 0.02f, 0.95f));
            CreatePanelText(winPanel.transform, "T", "ROUND CLEARED!", 60, Color.green);
            var s = CreatePanelText(winPanel.transform, "S", "Score: 0", 36, Color.white);
            winScoreText = s.GetComponent<TextMeshProUGUI>();
            CreatePanelButton(winPanel.transform, "C", "CONTINUE", () => CoreEventManager.OnContinueClicked?.Invoke());
            winPanel.SetActive(false);
        }

        private void EnsureGameOverPanel() {
            if (gameOverPanel != null) {
                if (gameOverPanel.name == "GameOverPanel_Auto") return;
                gameOverPanel.SetActive(false);
            }
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            gameOverPanel = CreateOverlayPanel(canvas.transform, "GameOverPanel_Auto", new Color(0.2f, 0.02f, 0.02f, 0.95f));
            CreatePanelText(gameOverPanel.transform, "T", "GAME OVER", 64, Color.red);
            var s = CreatePanelText(gameOverPanel.transform, "S", "Score: 0", 36, Color.white);
            gameOverScoreText = s.GetComponent<TextMeshProUGUI>();
            CreatePanelButton(gameOverPanel.transform, "R", "RESTART", () => CoreEventManager.OnRestartClicked?.Invoke());
            gameOverPanel.SetActive(false);
        }

        private GameObject CreateOverlayPanel(Transform parent, string name, Color bgColor) {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            panel.AddComponent<Image>().color = bgColor;
            var vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 30; vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = false; vlg.childControlHeight = false;
            return panel;
        }

        private GameObject CreatePanelText(Transform parent, string name, string text, int fontSize, Color color) {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>().sizeDelta = new Vector2(800, 100);
            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = fontSize; tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center; tmp.fontStyle = FontStyles.Bold;
            return obj;
        }

        private void CreatePanelButton(Transform parent, string name, string label, UnityEngine.Events.UnityAction onClick) {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            btnObj.AddComponent<RectTransform>().sizeDelta = new Vector2(280, 80);
            var img = btnObj.AddComponent<Image>(); img.color = new Color(0.25f, 0.6f, 0.35f);
            var btn = btnObj.AddComponent<Button>(); btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);
            GameObject tObj = new GameObject("L");
            tObj.transform.SetParent(btnObj.transform, false);
            var tRect = tObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero; tRect.anchorMax = Vector2.one;
            tRect.offsetMin = Vector2.zero; tRect.offsetMax = Vector2.zero;
            var tmp = tObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label; tmp.fontSize = 32; tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center; tmp.fontStyle = FontStyles.Bold;
        }

        // ── Queue Management ──────────────────────────────────────

        private void UpdateQueueVisuals(List<MoveCommand> queue) {
            int expectedSize = QueueManager.Instance != null ? QueueManager.Instance.currentMaxSize : queue.Count;

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
                var textUI = img.GetComponentInChildren<TextMeshProUGUI>(true); // Find hidden ones too
                if (textUI == null) {
                    GameObject textObj = new GameObject("ArrowText");
                    textObj.transform.SetParent(img.transform, false);
                    var tRect = textObj.AddComponent<RectTransform>();
                    tRect.anchorMin = Vector2.zero; tRect.anchorMax = Vector2.one;
                    tRect.offsetMin = Vector2.zero; tRect.offsetMax = Vector2.zero;
                    textUI = textObj.AddComponent<TextMeshProUGUI>();
                }

                if (i < queue.Count) {
                    Sprite dirSprite = GetSpriteForDirection(queue[i].Direction);
                    if (dirSprite != null && dirSprite != emptySlot) {
                        img.sprite = dirSprite;
                        img.color = Color.white;
                    } else {
                        img.sprite = null;
                        img.color = new Color(0.2f, 0.25f, 0.35f);
                    }

                    if (i == 0 && currentState == GameState.Run)
                        img.color = new Color(0.18f, 0.75f, 0.4f);

                    if (textUI != null) {
                        textUI.gameObject.SetActive(true);
                        textUI.text = queue[i].ToArrow();
                        textUI.fontSize = 42; // HUGE arrows
                        textUI.color = Color.white;
                        textUI.alignment = TextAlignmentOptions.Center;
                    }
                } else {
                    img.sprite = null;
                    img.color = new Color(0.1f, 0.1f, 0.15f, 0.5f);
                    if (textUI != null) {
                        textUI.gameObject.SetActive(true);
                        textUI.text = "-";
                        textUI.color = new Color(1, 1, 1, 0.15f);
                    }
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
            yield return new WaitForSeconds(0.12f);
            penaltyFlash.gameObject.SetActive(false);
        }

        private void HandleStateChange(GameState state) {
            currentState = state;
            EnsureWinPanel();
            EnsureGameOverPanel();

            if (winPanel != null) {
                winPanel.SetActive(state == GameState.Win);
                if (state == GameState.Win && winScoreText != null) {
                    var gm = FindAnyObjectByType<GameManager>();
                    if (gm != null) winScoreText.text = $"Score: {gm.score}";
                }
            }

            if (gameOverPanel != null) {
                gameOverPanel.SetActive(state == GameState.Stop);
                if (state == GameState.Stop && gameOverScoreText != null) {
                    var gm = FindAnyObjectByType<GameManager>();
                    if (gm != null) gameOverScoreText.text = $"Final Score: {gm.score}";
                }
            }

            if (runButton != null) runButton.gameObject.SetActive(state == GameState.Start);

            if (state == GameState.Start || state == GameState.Stop) {
                UpdateTimerVisual(0f);
            }
        }
    }
}
