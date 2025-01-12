using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class HomeownerAI : MonoBehaviour
{
    private GameManager gameManager;
    private Animator animator;
    public GameObject thief;

    public Grid grid;
    public Tilemap walkableGround;

    public float moveSpeed = 4f;
    private float initialMoveSpeed;
    public float detectionRadius;
    public Color gizmoColor = Color.red;

    private Graph graph;
    private AntColonyOptimization aco;
    private List<Vector2> path;
    private int currentNodeIndex = 0;

    private Queue<Graph.Node> lastMoves = new Queue<Graph.Node>();
    private Graph.Node wanderTargetNode;
    private bool isWandering = false;

    void Start()
    {   
        animator = GetComponent<Animator>();
        gameManager = FindObjectOfType<GameManager>();

        initialMoveSpeed = moveSpeed;
        graph = new Graph();
        graph.CreateNodesFromGrid(walkableGround);

        aco = new AntColonyOptimization(graph, 0.5f, 2.0f, 1.0f, 50);
    }

    void Update()
    {        
        Vector2 thiefPos = SnapToGrid(thief.transform.position);

        if (graph.IsPositionInNodes(thiefPos) && SquaredDistance(transform.position, thiefPos) <= detectionRadius * detectionRadius && path == null)
        {
            path = FindPathUsingACO(thiefPos);
            currentNodeIndex = 0;
        }

        if (path != null && path.Count > 0)
        {
            Debug.Log(path.Count);
            animator.SetBool("IsMoving", true);
            MoveAlongPath();
        }
        else
        {
            Wander();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Thief"))
        {
            gameManager.ThiefCaught();
        }
    }

    private void MoveAlongPath()
    {
        if (currentNodeIndex < path.Count)
        {
            Vector2 targetPosition = path[currentNodeIndex];
            Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
            float distance = Vector2.Distance(transform.position, targetPosition);

            transform.position = Vector2.MoveTowards(transform.position, targetPosition, (moveSpeed + 1.5f) * Time.deltaTime);

            animator.SetFloat("X", direction.x);
            animator.SetFloat("Y", direction.y);
            animator.SetBool("IsMoving", direction.magnitude > 0.1f);

            if (distance < 0.1f)
            {
                currentNodeIndex++;
            }

        }
        else
        {
            path = null;
        }
    }

    void Wander()
    {
        if (!isWandering)
        {
            isWandering = true;

            Graph.Node currentNode = graph.GetNodeAtPosition(SnapToGrid(transform.position));

            if (currentNode.neighbors.Count > 0)
            {
                wanderTargetNode = GetRandomNeighbor(currentNode);
            }
            else
            {
                isWandering = false;
                return;
            }
        }

        if (wanderTargetNode == null)
        {
            isWandering = false;
            return;
        }

        Vector2 direction = (wanderTargetNode.position - (Vector2)transform.position).normalized;

        float distance = Vector2.Distance(transform.position, wanderTargetNode.position);
        transform.position = Vector2.MoveTowards(transform.position, wanderTargetNode.position, moveSpeed * Time.deltaTime);

        animator.SetFloat("X", direction.x);
        animator.SetFloat("Y", direction.y);
        animator.SetBool("IsMoving", distance > 0.1f);

        if (distance < 0.1f)
        {
            lastMoves.Enqueue(wanderTargetNode);

            if (lastMoves.Count > 50)
            {
                lastMoves.Dequeue();
            }

            isWandering = false;
        }
    }

    private float SquaredDistance(Vector2 point1, Vector2 point2)
    {
        float dx = point2.x - point1.x;
        float dy = point2.y - point1.y;
        return dx * dx + dy * dy;
    }

    Graph.Node GetRandomNeighbor(Graph.Node currentNode)
    {
        List<Graph.Node> availableNeighbors = new List<Graph.Node>(currentNode.neighbors);

        foreach (Graph.Node lastMove in lastMoves)
        {
            if (availableNeighbors.Contains(lastMove))
            {
                availableNeighbors.Remove(lastMove);
            }
        }

        if (availableNeighbors.Count == 0)
        {
            return currentNode.neighbors[Random.Range(0, currentNode.neighbors.Count)];
        }

        return availableNeighbors[Random.Range(0, availableNeighbors.Count)];
    }

    private List<Vector2> FindPathUsingACO(Vector2 thiefPos)
    {
        Vector2 startPosition = SnapToGrid(transform.position);
        return aco.FindBestPath(startPosition, thiefPos);
    }

    private Vector2 SnapToGrid(Vector2 position)
    {
        Vector3Int cellPosition = grid.WorldToCell(position);
        Vector2 snappedPosition2D = grid.CellToWorld(cellPosition);
        return snappedPosition2D + new Vector2(0.5f, 0.5f);
    }

    public void IncreaseSpeed(float amount)
    {
        moveSpeed += amount;
    }

    public void ResetSpeed()
    {
        moveSpeed = initialMoveSpeed;
    }

    // private void DrawDetectionRadius()
    // {
    //     Gizmos.color = gizmoColor;
    //     Gizmos.DrawWireSphere(transform.position, detectionRadius);
    // }

    // private void OnDrawGizmosSelected()
    // {
    //     DrawDetectionRadius();
    // }

    // private void OnDrawGizmos()
    // {
    //     if (graph != null)
    //     {
    //         graph.DrawGizmos();
    //     }
    // }

}

public class Graph
{
    public class Node
    {
        public Vector2 position;
        public List<Node> neighbors;

        public Node(Vector2 position)
        {
            this.position = position;
            neighbors = new List<Node>();
        }
    }

    public List<Node> nodes;
    public Dictionary<(int, int), float> distanceCache;

    public int NumNodes => nodes.Count;
    public float[,] PheromoneLevels { get; private set; }
    public float initialPheromoneLevel = 1.0f;

    public Graph()
    {
        nodes = new List<Node>();
        distanceCache = new Dictionary<(int, int), float>();
    }

    public List<Node> GetNodes()
    {
        return nodes;
    }

    public void AddNode(Vector2 position)
    {
        nodes.Add(new Node(position));
    }

    public void AddEdge(Node node1, Node node2)
    {
        if (!node1.neighbors.Contains(node2)) node1.neighbors.Add(node2);
        if (!node2.neighbors.Contains(node1)) node2.neighbors.Add(node1);

        int index1 = GetNodeIndex(node1.position);
        int index2 = GetNodeIndex(node2.position);

        float distance = Vector2.Distance(node1.position, node2.position);
        distanceCache[(index1, index2)] = distance;
        distanceCache[(index2, index1)] = distance;
    }

    public Node GetRandomNeighbor(Node node)
    {
        if (node.neighbors.Count > 0)
        {
            int randomIndex = Random.Range(0, node.neighbors.Count);
            return node.neighbors[randomIndex];
        }
        return null;
    }
    
    public void CreateNodesFromGrid(Tilemap walkableGround)
    {
        List<Vector2> walkablePositions = new List<Vector2>();

        foreach (Vector3Int pos in walkableGround.cellBounds.allPositionsWithin)
        {
            if (walkableGround.HasTile(pos))
            {
                Vector3 worldPos = walkableGround.CellToWorld(pos);
                Vector2 nodePosition = new Vector2(worldPos.x + 0.5f, worldPos.y + 0.5f);
                walkablePositions.Add(nodePosition);
            }
        }

        foreach (var position in walkablePositions)
        {
            AddNode(position);
        }

        foreach (var node in nodes)
        {
            Vector2 nodePosition = node.position;
            Vector2[] adjacentPositions = new Vector2[]
            {
                nodePosition + Vector2.up,
                nodePosition + Vector2.down,
                nodePosition + Vector2.left,
                nodePosition + Vector2.right,
            };

            foreach (var adjacentPosition in adjacentPositions)
            {
                if (IsPositionInNodes(adjacentPosition))
                {
                    AddEdge(node, GetNodeAtPosition(adjacentPosition));
                }
            }
        }

        PheromoneLevels = new float[NumNodes, NumNodes];
        for (int i = 0; i < NumNodes; i++)
        {
            for (int j = 0; j < NumNodes; j++)
            {
                PheromoneLevels[i, j] = initialPheromoneLevel;
            }
        }
    }

    public Node GetNodeAtPosition(Vector2 position)
    {
        foreach (Node node in nodes)
        {
            if (node.position == position)
            {
                return node;
            }
        }
        return null;
    }

    public int GetNodeIndex(Vector2 position)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].position == position)
            {
                return i;
            }
        }
        return -1;
    }

    public bool IsPositionInNodes(Vector2 position)
    {
        foreach (Node node in nodes)
        {
            if (node.position == position)
            {
                return true;
            }
        }
        return false;
    }

    // public void DrawGizmos()
    // {
    //     if (nodes != null)
    //     {
    //         Gizmos.color = Color.red;
    //         foreach (Node node in nodes)
    //         {
    //             Gizmos.DrawSphere(node.position, 0.5f);

    //             Gizmos.color = Color.green;
    //             foreach (Node neighbor in node.neighbors)
    //             {
    //                 Gizmos.DrawLine(node.position, neighbor.position);
    //             }
    //         }
    //     }
    // }

}

public class AntColonyOptimization
{
    private Graph graph;
    private float evaporationRate;
    private float pheromoneImportance;
    private float distanceImportance;
    private int numAnts;

    public AntColonyOptimization(Graph graph, float evaporationRate, float pheromoneImportance, float distanceImportance, int numAnts)
    {
        this.graph = graph;
        this.evaporationRate = evaporationRate;
        this.pheromoneImportance = pheromoneImportance;
        this.distanceImportance = distanceImportance;
        this.numAnts = numAnts;
    }

    public List<Vector2> FindBestPath(Vector2 startPosition, Vector2 targetPosition)
    {
        int startIndex = graph.GetNodeIndex(startPosition);
        int targetIndex = graph.GetNodeIndex(targetPosition);

        if (startIndex == -1)
        {
            Debug.LogError("Start position not found in the graph.");
            return null;
        }

        if (targetIndex == -1)
        {
            Debug.LogError("Target position not found in the graph.");
            return null;
        }

        List<List<int>> allPaths = new List<List<int>>();

        for (int i = 0; i < numAnts; i++)
        {
            List<int> path = ConstructSolution(startIndex, targetIndex);
            allPaths.Add(path);
        }

        List<int> bestPath = allPaths[0];
        float bestPathLength = CalculatePathLength(bestPath);

        foreach (List<int> path in allPaths)
        {
            float pathLength = CalculatePathLength(path);
            if (pathLength < bestPathLength)
            {
                bestPath = path;
                bestPathLength = pathLength;
            }
        }

        // bestPath = bestPath.GetRange(0, Mathf.Min(5, bestPath.Count));

        EvaporatePheromones();
        UpdatePheromones(bestPath, bestPathLength);

        List<Vector2> bestPathPositions = new List<Vector2>();
        foreach (int index in bestPath)
        {
            bestPathPositions.Add(graph.nodes[index].position);
        }

        return bestPathPositions;
    }

    private List<int> ConstructSolution(int startIndex, int targetIndex)
    {
        List<int> path = new List<int> { startIndex };
        int currentIndex = startIndex;

        while (currentIndex != targetIndex)
        {
            int nextIndex = SelectNextNode(currentIndex);
            if (nextIndex == -1)
                break;

            path.Add(nextIndex);
            currentIndex = nextIndex;
        }

        return path;
    }

    private int SelectNextNode(int currentIndex)
    {
        Graph.Node currentNode = graph.nodes[currentIndex];
        List<int> neighborIndices = new List<int>();
        List<float> probabilities = new List<float>();

        foreach (Graph.Node neighbor in currentNode.neighbors)
        {
            int neighborIndex = graph.GetNodeIndex(neighbor.position);
            if (!graph.distanceCache.ContainsKey((currentIndex, neighborIndex))) continue;

            float pheromoneLevel = graph.PheromoneLevels[currentIndex, neighborIndex];
            float distance = graph.distanceCache[(currentIndex, neighborIndex)];
            float desirability = Mathf.Pow(pheromoneLevel, pheromoneImportance) * Mathf.Pow(1.0f / distance, distanceImportance);

            neighborIndices.Add(neighborIndex);
            probabilities.Add(desirability);
        }

        if (neighborIndices.Count == 0) return -1;

        float sumProbabilities = 0f;
        foreach (float probability in probabilities)
        {
            sumProbabilities += probability;
        }

        float randomValue = Random.value * sumProbabilities;
        float cumulativeProbability = 0f;
        for (int i = 0; i < neighborIndices.Count; i++)
        {
            cumulativeProbability += probabilities[i];
            if (randomValue <= cumulativeProbability)
            {
                return neighborIndices[i];
            }
        }

        return neighborIndices[neighborIndices.Count - 1];
    }

    private float CalculatePathLength(List<int> path)
    {
        float pathLength = 0f;
        for (int i = 0; i < path.Count - 1; i++)
        {
            int index1 = path[i];
            int index2 = path[i + 1];
            pathLength += graph.distanceCache[(index1, index2)];
        }
        return pathLength;
    }

    private void EvaporatePheromones()
    {
        for (int i = 0; i < graph.NumNodes; i++)
        {
            for (int j = 0; j < graph.NumNodes; j++)
            {
                graph.PheromoneLevels[i, j] *= 1.0f - evaporationRate;
            }
        }
    }

    private void UpdatePheromones(List<int> bestPath, float bestPathLength)
    {
        float pheromoneDeposit = 1.0f / bestPathLength;
        for (int i = 0; i < bestPath.Count - 1; i++)
        {
            int index1 = bestPath[i];
            int index2 = bestPath[i + 1];
            graph.PheromoneLevels[index1, index2] += pheromoneDeposit;
            graph.PheromoneLevels[index2, index1] += pheromoneDeposit;
        }
    }

}
