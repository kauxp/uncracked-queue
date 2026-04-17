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

        [Header("Entities")]
        public int numObstacles = 4;
        public Color floorColor = new Color(0.12f, 0.12f, 0.14f);
        public Color playerColor = new Color(0.3f, 0.5f, 0.9f);
        public Color keyColor = Color.yellow;
        public Color exitColor = Color.green;

        private Sprite squareSprite;
        private List<Vector2Int> takenCells = new List<Vector2Int>();

        private void Awake() {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
            
            squareSprite = MakeSprite();
        }

        private void OnEnable() {
            CoreEventManager.OnRestartClicked += GenerateScene;
        }

        private void OnDisable() {
            CoreEventManager.OnRestartClicked -= GenerateScene;
        }

        private void Start() {
            GenerateScene();
        }

        private void GenerateScene() {
            // Clean up old generation safely
            var oldEnts = GameObject.Find("DynamicallySpawnedEntities");
            if (oldEnts != null) {
                oldEnts.SetActive(false); // Disables instantly so it won't be found by next scripts
                Destroy(oldEnts);
            }
            
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

            // Spawn Player
            GameObject p = SpawnEntity(parent, "Player", playerColor, 0.8f, out Vector2Int pPos);
            p.tag = "Player";
            p.AddComponent<BoxCollider2D>().isTrigger = true;
            var rb = p.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            p.AddComponent<PlayerController>().startGridPosition = pPos;

            // Spawn Key
            GameObject k = SpawnEntity(parent, "Key", keyColor, 0.5f, out Vector2Int kPos);
            k.tag = "Key";
            k.AddComponent<BoxCollider2D>().isTrigger = true;
            k.AddComponent<KeyItem>();

            // Spawn Exit
            GameObject e = SpawnEntity(parent, "Exit", exitColor, 0.95f, out Vector2Int ePos);
            e.tag = "Exit";
            e.AddComponent<BoxCollider2D>().isTrigger = true;

            // Determine obstacle count from difficulty
            var gameManager = FindAnyObjectByType<GameManager>();
            int obsCount = 3; // default medium
            if (gameManager != null) {
                switch (gameManager.difficulty) {
                    case Difficulty.Easy:   obsCount = 2; break;
                    case Difficulty.Medium: obsCount = 3; break;
                    case Difficulty.Hard:   obsCount = 4; break;
                }
            }

            // Spawn Obstacles
            string[] names = {"Hunter", "Patrol", "Scout", "Rover"};
            for (int i = 0; i < obsCount; i++) {
                Color c = Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.7f, 1f);
                string n = names[i % names.Length] + " " + (i + 1);
                
                GameObject o = SpawnEntity(parent, "Obstacle_" + i, c, 0.7f, out Vector2Int oPos);
                var oc = o.AddComponent<ObstacleController>();
                oc.label = n;
                oc.displayColor = c;
                oc.startGridPosition = oPos;
                
                int pathLen = Random.Range(2, 5);
                oc.pattern = new List<Direction>();
                for (int j = 0; j < pathLen; j++) oc.pattern.Add((Direction)Random.Range(0, 4));
            }

            // Center Camera perfectly over the grid
            Camera.main.orthographic = true;
            Camera.main.transform.position = new Vector3(width * cellSize * 0.5f, height * cellSize * 0.5f, -10f);
            Camera.main.orthographicSize = (height * cellSize * 0.5f) + 1f;

            // Tell UI to read the freshly spawned obstacles
            CoreEventManager.OnEntitiesSpawned?.Invoke();
        }

        private GameObject SpawnEntity(Transform parent, string name, Color color, float scale, out Vector2Int pos) {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            
            // Randomly find an untaken cell
            pos = new Vector2Int(Random.Range(0, width), Random.Range(0, height));
            while (takenCells.Contains(pos)) {
                pos = new Vector2Int(Random.Range(0, width), Random.Range(0, height));
            }
            takenCells.Add(pos);

            go.transform.position = GridToWorld(pos);
            go.transform.localScale = Vector3.one * (cellSize * scale);
            
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = squareSprite;
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

        private Sprite MakeSprite() {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
        }
    }
}
