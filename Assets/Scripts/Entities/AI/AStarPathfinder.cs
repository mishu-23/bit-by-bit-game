using System.Collections.Generic;
using UnityEngine;

public class AStarPathfinder : MonoBehaviour
{
    [Header("Grid Settings")]
    public float gridSize = 1f;
    public float gridWorldWidth = 50f;
    public LayerMask obstacleLayer = 1;
    public float groundLevel = 0.5f;
    public float obstacleCheckHeight = 1.5f;
    
    [Header("Debug")]
    public bool showGrid = true;
    
    private Node[] grid;
    private int gridSizeX;
    
    public static AStarPathfinder Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeGrid();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void InitializeGrid()
    {
        gridSizeX = Mathf.RoundToInt(gridWorldWidth / gridSize);
        grid = new Node[gridSizeX];
        
        float worldLeft = transform.position.x - gridWorldWidth / 2;
        
        for (int x = 0; x < gridSizeX; x++)
        {
            float worldX = worldLeft + (x * gridSize + gridSize / 2);
            Vector3 obstacleCheckPoint = new Vector3(worldX, obstacleCheckHeight, 0f);
            
            bool walkable = !Physics2D.OverlapCircle(obstacleCheckPoint, gridSize * 0.4f, obstacleLayer);
            Vector3 groundPosition = new Vector3(worldX, groundLevel, 0f);
            grid[x] = new Node(walkable, groundPosition, x);
        }
    }
    
    public List<Vector3> FindPath(Vector3 startPos, Vector3 targetPos)
    {
        startPos.y = groundLevel;
        targetPos.y = groundLevel;
        
        Node startNode = NodeFromWorldPoint(startPos);
        Node targetNode = NodeFromWorldPoint(targetPos);
        
        if (startNode == null || targetNode == null)
        {
            return new List<Vector3> { startPos, targetPos };
        }
        
        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();
        openSet.Add(startNode);
        
        while (openSet.Count > 0)
        {
            Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < currentNode.fCost)
                {
                    currentNode = openSet[i];
                }
            }
            
            openSet.Remove(currentNode);
            closedSet.Add(currentNode);
            
            if (currentNode == targetNode)
            {
                return RetracePath(startNode, targetNode);
            }
            
            foreach (Node neighbour in GetNeighbours(currentNode))
            {
                if (!neighbour.walkable || closedSet.Contains(neighbour))
                    continue;
                
                int newCost = currentNode.gCost + GetDistance(currentNode, neighbour);
                if (newCost < neighbour.gCost || !openSet.Contains(neighbour))
                {
                    neighbour.gCost = newCost;
                    neighbour.hCost = GetDistance(neighbour, targetNode);
                    neighbour.parent = currentNode;
                    
                    if (!openSet.Contains(neighbour))
                        openSet.Add(neighbour);
                }
            }
        }
        
        return new List<Vector3> { startPos, targetPos };
    }
    
    List<Vector3> RetracePath(Node startNode, Node endNode)
    {
        List<Vector3> path = new List<Vector3>();
        Node currentNode = endNode;
        
        while (currentNode != startNode)
        {
            path.Add(currentNode.worldPosition);
            currentNode = currentNode.parent;
        }
        path.Reverse();
        
        return path;
    }
    
    List<Node> GetNeighbours(Node node)
    {
        List<Node> neighbours = new List<Node>();
        
        for (int x = -1; x <= 1; x += 2)
        {
            int checkX = node.gridX + x;
            if (checkX >= 0 && checkX < gridSizeX)
            {
                neighbours.Add(grid[checkX]);
            }
        }
        
        return neighbours;
    }
    
    Node NodeFromWorldPoint(Vector3 worldPosition)
    {
        worldPosition.y = groundLevel;
        float worldLeft = transform.position.x - gridWorldWidth / 2;
        float percentX = (worldPosition.x - worldLeft) / gridWorldWidth;
        percentX = Mathf.Clamp01(percentX);
        
        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        
        if (x >= 0 && x < gridSizeX)
            return grid[x];
        
        return null;
    }
    
    int GetDistance(Node nodeA, Node nodeB)
    {
        return Mathf.Abs(nodeA.gridX - nodeB.gridX);
    }
    
    void OnDrawGizmos()
    {
        if (!showGrid || grid == null) return;
        
        foreach (Node n in grid)
        {
            Gizmos.color = n.walkable ? Color.white : Color.red;
            Gizmos.DrawWireCube(n.worldPosition, Vector3.one * (gridSize - 0.1f));
        }
    }
}

public class Node
{
    public bool walkable;
    public Vector3 worldPosition;
    public int gridX;
    public int gCost;
    public int hCost;
    public Node parent;
    
    public int fCost { get { return gCost + hCost; } }
    
    public Node(bool _walkable, Vector3 _worldPos, int _gridX)
    {
        walkable = _walkable;
        worldPosition = _worldPos;
        gridX = _gridX;
    }
} 