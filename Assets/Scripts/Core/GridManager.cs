using UnityEngine;
using System.Collections.Generic;
using QueueDungeon.Entities;

namespace QueueDungeon.Core {
    public class GridManager : MonoBehaviour {
        public static GridManager Instance { get; private set; }

        [Header("Grid Bounds")]
        public int width = 10;
        public int height = 10;
        public float cellSize = 1f;

        [Header("Entity Colors")]
        public Color floorColor = new Color(0.12f, 0.12f, 0.14f);
        public Color playerColor = new Color(0.3f, 0.5f, 0.9f);
        public Color keyColor = Color.yellow;
        public Color exitColor = Color.green;
        public Color obstacleColor = new Color(0.9f, 0.25f, 0.2f);

        [Header("Scaling")]
        public int roundsCompleted = 0;

        // Shape sprites
        private Sprite squareSprite;
        private Sprite circleSprite;
        private Sprite diamondSprite;
        private Sprite triangleSprite;
        private Sprite crossSprite;

        // Public accessors for UI legend
        public Sprite SquareSprite => squareSprite;
        public Sprite DiamondSprite => diamondSprite;
        public Sprite TriangleSprite => triangleSprite;
        public Sprite CircleSprite => circleSprite;
        public Sprite CrossSprite => crossSprite;

        private List<Vector2Int> takenCells = new List<Vector2Int>();

        private static readonly ObstacleShape[] allShapes = {
            ObstacleShape.Circle, ObstacleShape.Diamond, ObstacleShape.Triangle, ObstacleShape.Cross
        };

        private void Awake() {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
            
            squareSprite = MakeSquareSprite();
            circleSprite = MakeCircleSprite(32);
            diamondSprite = MakeDiamondSprite(32);
            triangleSprite = MakeTriangleSprite(32);
            crossSprite = MakeCrossSprite(32);
        }

        private void OnEnable() {
            CoreEventManager.OnRestartClicked += HandleRestart;
            CoreEventManager.OnContinueClicked += HandleContinue;
        }

        private void OnDisable() {
            CoreEventManager.OnRestartClicked -= HandleRestart;
            CoreEventManager.OnContinueClicked -= HandleContinue;
        }

        private void Start() {
            GenerateScene();
        }

        private void HandleRestart() {
            roundsCompleted = 0;
            GenerateScene();
        }

        private void HandleContinue() {
            roundsCompleted++;
            CoreEventManager.OnRoundCompleted?.Invoke(roundsCompleted);
            GenerateScene();
        }

        public Sprite GetSpriteForShape(ObstacleShape shape) {
            switch (shape) {
                case ObstacleShape.Circle:   return circleSprite;
                case ObstacleShape.Diamond:  return diamondSprite;
                case ObstacleShape.Triangle: return triangleSprite;
                case ObstacleShape.Cross:    return crossSprite;
                default:                     return squareSprite;
            }
        }

        private void GenerateScene() {
            // Clean up old dynamically spawned entities
            var oldEnts = GameObject.Find("DynamicallySpawnedEntities");
            if (oldEnts != null) {
                oldEnts.SetActive(false);
                Destroy(oldEnts);
            }

            // Clean up any pre-existing scene entities (manual Player, Key, Exit, Obstacles)
            // This prevents duplicates when user has manually placed entities in the scene
            CleanupExistingEntities();
            
            takenCells.Clear();
            var parent = new GameObject("DynamicallySpawnedEntities").transform;

            // Floor visuals
            for(int r = 0; r < height; r++) {
                for(int c = 0; c < width; c++) {
                    GameObject f = new GameObject($"Floor_{c}_{r}");
                    f.transform.SetParent(parent);
                    f.transform.position = GridToWorld(new Vector2Int(c, r));
                    f.transform.localScale = Vector3.one * (cellSize * 0.95f);
                    var sr = f.AddComponent<SpriteRenderer>();
                    sr.sprite = squareSprite;
                    sr.color = floorColor;
                    sr.sortingOrder = -5;
                }
            }

            // Spawn Player (square shape)
            GameObject p = SpawnEntity(parent, "Player", playerColor, squareSprite, 0.8f, out Vector2Int pPos);
            p.tag = "Player";
            p.AddComponent<BoxCollider2D>().isTrigger = true;
            var rb = p.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            p.AddComponent<PlayerController>().startGridPosition = pPos;

            // Spawn Key (diamond shape)
            GameObject k = SpawnEntity(parent, "Key", keyColor, diamondSprite, 0.6f, out Vector2Int kPos);
            k.tag = "Key";
            k.AddComponent<BoxCollider2D>().isTrigger = true;
            k.AddComponent<KeyItem>();

            // Spawn Exit (triangle shape)
            GameObject e = SpawnEntity(parent, "Exit", exitColor, triangleSprite, 0.85f, out Vector2Int ePos);
            e.tag = "Exit";
            e.AddComponent<BoxCollider2D>().isTrigger = true;

            // Dynamic obstacle count (Medium default)
            int baseObsCount = 3;
            int obsCount = baseObsCount + (roundsCompleted / 5);
            int maxObs = (width * height) - 3;
            obsCount = Mathf.Min(obsCount, maxObs);

            // Dynamic pattern length
            int basePatternLen = 2;
            int difficultyBonus = 1; // Medium default

            // Spawn Obstacles — same color, different shapes
            for (int i = 0; i < obsCount; i++) {
                ObstacleShape shape = allShapes[i % allShapes.Length];
                Sprite shapeSprite = GetSpriteForShape(shape);
                string label = $"{shape} {i + 1}";

                GameObject o = SpawnEntity(parent, "Obstacle_" + i, obstacleColor, shapeSprite, 0.7f, out Vector2Int oPos);
                o.AddComponent<BoxCollider2D>().isTrigger = true;
                var oRb = o.AddComponent<Rigidbody2D>();
                oRb.bodyType = RigidbodyType2D.Kinematic;

                var oc = o.AddComponent<ObstacleController>();
                oc.label = label;
                oc.displayColor = obstacleColor;
                oc.shape = shape;
                oc.startGridPosition = oPos;
                
                int patternLen = basePatternLen + difficultyBonus + (roundsCompleted / 3);
                patternLen = Mathf.Clamp(patternLen, 2, 8);
                
                oc.pattern = new List<Direction>();
                for (int j = 0; j < patternLen; j++) {
                    oc.pattern.Add((Direction)Random.Range(0, 4));
                }
            }

            // Center Camera
            Camera.main.orthographic = true;
            Camera.main.transform.position = new Vector3(width * cellSize * 0.5f, height * cellSize * 0.5f, -10f);
            Camera.main.transform.localScale = Vector3.one;
            Camera.main.orthographicSize = (height * cellSize * 0.5f) + 1f;

            // Tell UI to rebuild legend
            CoreEventManager.OnEntitiesSpawned?.Invoke();
        }

        /// <summary>
        /// Destroys any pre-existing Player, Key, Exit, or Obstacle objects in the scene
        /// that were placed manually (not under DynamicallySpawnedEntities).
        /// </summary>
        private void CleanupExistingEntities() {
            // Destroy pre-existing Players
            foreach (var pc in FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                Destroy(pc.gameObject);
            }
            // Destroy pre-existing Keys
            foreach (var ki in FindObjectsByType<KeyItem>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                Destroy(ki.gameObject);
            }
            // Destroy pre-existing Obstacles
            foreach (var oc in FindObjectsByType<ObstacleController>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                Destroy(oc.gameObject);
            }
            // Destroy any tagged objects
            foreach (string tag in new[] { "Player", "Key", "Exit" }) {
                try {
                    foreach (var go in GameObject.FindGameObjectsWithTag(tag)) {
                        if (go != null && go != this.gameObject) Destroy(go);
                    }
                } catch { }
            }
        }

        private GameObject SpawnEntity(Transform parent, string name, Color color, Sprite sprite, float scale, out Vector2Int pos) {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            
            pos = new Vector2Int(Random.Range(0, width), Random.Range(0, height));
            int attempts = 0;
            while (takenCells.Contains(pos) && attempts < 1000) {
                pos = new Vector2Int(Random.Range(0, width), Random.Range(0, height));
                attempts++;
            }
            takenCells.Add(pos);

            go.transform.position = GridToWorld(pos);
            go.transform.localScale = Vector3.one * (cellSize * scale);
            
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = 1;
            
            return go;
        }

        public Vector3 GridToWorld(Vector2Int p) {
            return new Vector3(p.x * cellSize + cellSize * 0.5f, p.y * cellSize + cellSize * 0.5f, 0);
        }
        
        public bool IsValidPosition(Vector2Int p) {
            return p.x >= 0 && p.x < width && p.y >= 0 && p.y < height;
        }

        // ── Procedural Sprite Generation ──────────────────────────

        private Sprite MakeSquareSprite() {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
        }

        private Sprite MakeCircleSprite(int size) {
            var tex = new Texture2D(size, size);
            float center = size * 0.5f;
            float radius = size * 0.45f;
            for (int y = 0; y < size; y++) {
                for (int x = 0; x < size; x++) {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    tex.SetPixel(x, y, dist <= radius ? Color.white : Color.clear);
                }
            }
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private Sprite MakeDiamondSprite(int size) {
            var tex = new Texture2D(size, size);
            float center = size * 0.5f;
            float halfSize = size * 0.45f;
            for (int y = 0; y < size; y++) {
                for (int x = 0; x < size; x++) {
                    float dx = Mathf.Abs(x - center);
                    float dy = Mathf.Abs(y - center);
                    tex.SetPixel(x, y, (dx / halfSize + dy / halfSize) <= 1f ? Color.white : Color.clear);
                }
            }
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private Sprite MakeTriangleSprite(int size) {
            var tex = new Texture2D(size, size);
            for (int y = 0; y < size; y++) {
                for (int x = 0; x < size; x++) {
                    float normalizedY = (float)y / size;
                    float halfWidth = normalizedY * 0.5f;
                    float centerX = 0.5f;
                    float normalizedX = (float)x / size;
                    tex.SetPixel(x, y, (normalizedX >= centerX - halfWidth && normalizedX <= centerX + halfWidth) ? Color.white : Color.clear);
                }
            }
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private Sprite MakeCrossSprite(int size) {
            var tex = new Texture2D(size, size);
            float center = size * 0.5f;
            float armWidth = size * 0.2f;
            for (int y = 0; y < size; y++) {
                for (int x = 0; x < size; x++) {
                    bool horizontal = Mathf.Abs(y - center) <= armWidth && x >= 2 && x <= size - 3;
                    bool vertical = Mathf.Abs(x - center) <= armWidth && y >= 2 && y <= size - 3;
                    tex.SetPixel(x, y, (horizontal || vertical) ? Color.white : Color.clear);
                }
            }
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
