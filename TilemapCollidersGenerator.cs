using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AS.UnityTool
{
    [RequireComponent(typeof(Tilemap))]
    public class TilemapCollidersGenerator : MonoBehaviour
    {
        // Constants
        private const int _empty = 0;
        private const int _used = 1;
        private const int _covered = 2;
        private const string _colliderChildName = "ColliderContainer";
        
        // Inspector
        [SerializeField, Tooltip("Where the colliders will be added, if Child is selected a child GameObject will be created.")] private CollidersGameObjectTarget collidersGameObject = CollidersGameObjectTarget.ThisGameObject;
        [SerializeField, Tooltip("Whether the colliders can overlap on each other. Overlapping may result in a lower number of colliders.")] private OverlapAllowed collidersCanOverlap = OverlapAllowed.NotAllowed;
        
        // Fields
        private Tilemap _tilemap;
        private GameObject _colliderContainer;
        
        // Properties
        private Tilemap TargetTilemap
        {
            get
            {
                if (_tilemap != null) { return _tilemap; }
                _tilemap = GetComponent<Tilemap>();
                return _tilemap;
            }
        }
        
        // Enums
        private enum CollidersGameObjectTarget { ThisGameObject, Child }
        private enum OverlapAllowed { Allowed, NotAllowed }
        
        // Methods
        public void Generate()
        {
            // Remove previously generated colliders
            RemoveAllColliders();
            
            // Set the target GameObject which will hold the colliders
            SetColliderTarget();
            
            // Resize the Tilemap bounds (in case it has changed)
            TargetTilemap.ResizeBounds();
            
            // Caching Tilemap data
            BoundsInt bounds = TargetTilemap.cellBounds;
            TileBase[] allTiles = TargetTilemap.GetTilesBlock(bounds);

            // If there is no tiles, just return
            if (allTiles.Length == 0) return;
            
            // Generate a grid based on the current Tilemap
            int[,] grid = GenerateGrid(allTiles, bounds.size);
            
            // Find the bounds the cover the used cells in the grid
            List<BoundsInt> colliderBounds = FindBounds(grid, bounds.size);
            
            // Generate a BoxCollider2d for each bound
            GenerateCollidersFromBounds(colliderBounds, bounds);
            
        }
        public void RemoveAllColliders()
        {
            var colliders = GetComponents<BoxCollider2D>();

            foreach (var c in colliders)
            {
                DestroyImmediate(c);
            }
            
            var childCollider = transform.Find(_colliderChildName);
            
            if (childCollider != null)
            {
                DestroyImmediate(childCollider.transform.gameObject);
            }
        }
        private int[,] GenerateGrid(TileBase[] allTiles, Vector3Int size)
        {
            int[,] grid = new int[size.x, size.y];
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    grid[x, y] = allTiles[x + y * size.x] != null ? _used : _empty;
                }
            }

            return grid;
        }
        private List<BoundsInt> FindBounds(int[,] grid, Vector3Int size)
        {
            List<BoundsInt> colliderBounds = new List<BoundsInt>();
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    if (grid[x, y] == _used)
                    {
                        BoundsInt newBounds = TryExtendBounds(x, y, ref grid);
                        colliderBounds.Add(newBounds);
                    }
                }
            }

            return colliderBounds;
        }
        private BoundsInt TryExtendBounds(int x, int y, ref int[,] grid)
        {
            int xLeft = 0;
            while (x - xLeft >= 0 && (grid[x - xLeft, y] == _used || collidersCanOverlap == OverlapAllowed.Allowed && grid[x - xLeft, y] == _covered))
            {
                grid[x-xLeft, y] = _covered;
                xLeft++;
            }

            int xRight = 1;
            while (x + xRight < grid.GetLength(0) && (grid[x + xRight, y] == _used || collidersCanOverlap == OverlapAllowed.Allowed && grid[x + xRight, y] == _covered))
            {
                grid[x+xRight, y] = _covered;
                xRight++;
            }

            int yDown = 1;
            while (y - yDown >= 0 && LineIsAvailable(y - yDown, x - xLeft+1, x + xRight-1, grid))
            {
                SetLineAsCovered(y - yDown, x - xLeft+1, x + xRight-1, _covered, ref grid);
                yDown++;
            }

            int yUp = 1;
            while (y + yUp < grid.GetLength(1) && LineIsAvailable(y + yUp, x - xLeft+1, x + xRight-1, grid))
            {
                SetLineAsCovered(y + yUp, x - xLeft+1, x + xRight-1, _covered, ref grid);
                yUp++;
            }
            
            return new BoundsInt(x - xLeft+1, y - yDown+1, 0, xRight + xLeft - 1, yUp + yDown - 1, 1);
        }
        private void SetLineAsCovered(int yCoord, int xMin, int xMax, int value, ref int[,] grid)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                grid[x, yCoord] = _covered;
            }
        }
        private bool LineIsAvailable(int y, int xMin, int xMax, int[,] grid)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                if (collidersCanOverlap == OverlapAllowed.NotAllowed)
                {
                    if (grid[x, y] != _used)
                        return false;
                }
                else
                {
                    if (grid[x, y] == _empty)
                        return false;
                }

            }

            return true;
        }
        private void GenerateCollidersFromBounds(List<BoundsInt> colliderBounds, BoundsInt tilemapBounds)
        {
            foreach (var cb in colliderBounds)
            {
                BoxCollider2D newCollider = _colliderContainer.AddComponent<BoxCollider2D>();
                newCollider.size = new Vector2(cb.size.x, cb.size.y);
                newCollider.offset = new Vector2(cb.x + cb.size.x/2f + tilemapBounds.position.x, cb.y + cb.size.y/2f  + tilemapBounds.position.y);
            }
        }
        private void SetColliderTarget()
        {
            if (collidersGameObject == CollidersGameObjectTarget.ThisGameObject)
            {
                _colliderContainer = this.gameObject;
            }
            else
            {
                _colliderContainer = new GameObject(_colliderChildName);
                _colliderContainer.transform.SetParent(this.gameObject.transform);
            }
        }
        private void PrintGrid(int[,] grid)
        {
            string res = String.Empty;
            
            for (int y = grid.GetLength(1)-1; y >=0; y--)
            {
                for (int x = 0; x < grid.GetLength(0); x++)
                {
                    res += grid[x, y] + " ";
                }

                res += "\n";
            }
            
            Debug.Log(res);
        }
    }
    
#if UNITY_EDITOR
    [CustomEditor(typeof(TilemapCollidersGenerator))]
    public class CustomInspectorScript : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            TilemapCollidersGenerator collidersGenerator = (TilemapCollidersGenerator)target;
            
            GUILayout.Space(10);
            if (GUILayout.Button ("Generate colliders"))
            {
                collidersGenerator.Generate();
            }
            if (GUILayout.Button ("Remove all colliders"))
            {
                collidersGenerator.RemoveAllColliders();
            }
        }
    }
#endif
}

