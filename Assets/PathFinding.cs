using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using NodePath = System.Collections.Generic.List<QuadNode>;

using NodeCost = System.Collections.Generic.KeyValuePair<
    System.Collections.Generic.List<QuadNode>,
    AStarCost
>; 


using NodeList = System.Collections.Generic.List<
    System.Collections.Generic.KeyValuePair<
        System.Collections.Generic.List<QuadNode>,
        AStarCost
    >
>;

using RectList = System.Collections.Generic.List<UnityEngine.Rect>;

public class PathFinding : MonoBehaviour
{
    public GameObject plane;
    public float minimumSize;
    private static QuadTree _tree;
    private List<QuadNode> _currentPath;
    public int MaxIterations = 100000;
    private bool displayGraph = true;
    public Vector3 direction;
    public Vector3 goal;
    public GameObject leader;
    
    
    // Start is called before the first frame update
    private void Awake()
    {
        if (_tree != null)
            return;
        var planeBounds = plane.GetComponent<Renderer>().bounds;
        Debug.Log(planeBounds);
        var planeMin = planeBounds.min;
        var planeMax = planeBounds.max;
        var planeRect = new Rect(planeMin.x, planeMin.z, planeBounds.size.x, planeBounds.size.z);
        Debug.Log("plane rect: " + planeRect);
        LayerMask mask = LayerMask.NameToLayer("Obstacle");
        QuadTree.ObstacleMask = 1 << mask.value;
        _tree = new QuadTree(planeRect, minimumSize);
        _tree.BuildGraph();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        HandlePathDemand();

        UpdateDirection();

        if (Input.GetKeyDown(KeyCode.Space))
            displayGraph = !displayGraph;
    }

    private void HandlePathDemand()
    {
        if (gameObject != leader || !Input.GetMouseButtonDown(1)) return;
        RaycastHit hit;

        if (!Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 300))
            return;
        goal = hit.point;
        var position = leader.transform.position;
        _currentPath = FindPath(new Vector2(position.x, position.z), new Vector2(goal.x, goal.z));

        if (_currentPath == null) {
            Debug.Log("Did not find a path");
        }
    }

    private void UpdateDirection()
    {
        if (leader != gameObject && leader != null) {
            direction = (leader.transform.position - transform.position).normalized;
            return;
        }

        if (_currentPath == null)
            return;
        if (_currentPath.Count == 0)
            direction = goal - transform.position;
        else if (Utility.VectorIsInRect(transform.position, _currentPath[0].Rect))
            _currentPath.RemoveAt(0);
        if (_currentPath.Count > 0)
            direction = Utility.Vec2to3(_currentPath[0].Rect.center) - transform.position;
        
        direction = direction.normalized;
    }

    private List<QuadNode> FindPath(Vector2 start, Vector2 goal)
    {
        var graph = FindStartNode(start);
        var fringe = InitFringe(graph, start, goal);
        var expanded = new List<QuadNode>();
        int currentIteration = 0;

        while (fringe.Count > 0 && currentIteration < MaxIterations) {
            ++currentIteration;
            var current = fringe[0];
            var node = current.Key;
            var currentNode = node.Last();
            fringe.RemoveAt(0);
            var str = "";
            foreach (var quadNode in node) {
                str += quadNode.Rect + " ";
            }
            if (Utility.VectorIsInRect(goal, currentNode.Rect))
                return node;
            if (expanded.Contains(currentNode))
                continue;
            expanded.Add(currentNode);
            fringe = ExpandFringe(fringe, current, currentNode, goal);
        }
        return null;
    }

    private NodeList InitFringe(QuadNode graph, Vector2 start, Vector2 goal)
    {
        var path = new NodePath() {graph}; ;
        var h = ComputeHeuristic(graph.Rect.center, start);
        var g = 0;
        var fringe = new NodeList() {new NodeCost(path, new AStarCost(h, g))};
        return fringe;
    }

    private static QuadNode FindStartNode(Vector2 start)
    {
        return _tree.NodeFromPoint(start).Node;
    }

    private static NodeList ExpandFringe(NodeList fringe, NodeCost current, QuadNode currentNode, Vector3 goal)
    {
        var cost = current.Value.Cost + 1;

        foreach (var child in currentNode.Children) {
            var h = ComputeHeuristic(new Vector2(goal.x, goal.z), child.Rect.center);
            var lastBetterIndex = fringe.FindLastIndex(x => x.Value.Cost + x.Value.Heuristic <= h + cost);
            var newRectList = new List<QuadNode>(current.Key) {child};
            var newElement = new NodeCost(newRectList, new AStarCost(cost, h));
            fringe.Insert(lastBetterIndex + 1, newElement);
        }

        return fringe;
    }

    private static float ComputeHeuristic(Vector2 start, Vector2 end, string metrics="Euclidean")
    {
        return metrics switch
        {
            "Euclidean" => (float) Math.Sqrt(Math.Pow(end.x - start.x, 2) + Math.Pow(end.y - start.y, 2)),
            "Manhattan" => Math.Abs(end.x - start.x) + Math.Abs(end.y - start.y),
            _ => ComputeHeuristic(start, end)
        };
    }
    
    private void OnDrawGizmos()
    {
        if (displayGraph)
            _tree?.OnDrawGizmos();
        if (_currentPath == null || _currentPath.Count == 0) return;
        Gizmos.color = Color.blue;
        var lastPosition = _currentPath[0].Rect.center;
        foreach (var node in _currentPath.GetRange(1, _currentPath.Count - 1)) {
            var newPosition = node.Rect.center;
            Gizmos.DrawLine(Utility.Vec2to3(lastPosition), Utility.Vec2to3(newPosition));
            lastPosition = newPosition;
        }
    }
}

internal class QuadTree
{
    private Rect _bounds;
    private QuadTree[] _children = null;
    private float _minimumSize;
    public static int ObstacleMask;
    public QuadNode Node;
    private Location _location;

    public QuadTree(Rect bounds, float minSize)
    {
        _bounds = bounds;
        _minimumSize = minSize;
        Node = new QuadNode(bounds) {Children = new NodePath()};
        if (bounds.size.x < _minimumSize || bounds.size.y < _minimumSize || !HasObstacle(bounds))
            return;
        _children = new QuadTree[4];
        var newWidth = bounds.width / 2;
        var newHeight = bounds.height / 2;

        _children[0] = new QuadTree(new Rect(bounds.xMin, bounds.yMin, newWidth, newHeight), minSize);
        _children[1] = new QuadTree(new Rect(bounds.xMin, bounds.center.y, newWidth, newHeight), minSize);
        _children[2] = new QuadTree(new Rect(bounds.center.x, bounds.yMin, newWidth, newHeight), minSize);
        _children[3] = new QuadTree(new Rect(bounds.center.x, bounds.center.y, newWidth, newHeight), minSize);
        for (var i = 0; i < 4; i++)
            _children[i]._location = IndexToLocation(i);
    }

    public (QuadTree, QuadTree) GETChildren(Location location)
    {
        return location switch
        {
            Location.N => (GETChild(Location.NE), GETChild(Location.NW)),
            Location.S => (GETChild(Location.SE), GETChild(Location.SW)),
            Location.E => (GETChild(Location.NE), GETChild(Location.SE)),
            Location.W => (GETChild(Location.NW), GETChild(Location.SW)),
            _ => throw new ArgumentOutOfRangeException(nameof(location), location,
                "Trying to get children from wrong location, consider using GETChild.")
        };
    }

    public QuadTree GETChild(Location location)
    {
        return location switch
        {
            Location.SE => _children[2],
            Location.NE => _children[3],
            Location.SW => _children[0],
            Location.NW => _children[1],
            _ => throw new ArgumentOutOfRangeException(nameof(location), location,
                "Trying to get child from wrong location, consider using GETChildren.")
        };
    }

    private static Location IndexToLocation(int index)
    {
        return index switch
        {
            0 => Location.SW,
            1 => Location.NW,
            2 => Location.SE,
            3 => Location.NE,
            _ => throw new ArgumentOutOfRangeException(nameof(index), index, "Getting location of index over 3")
        };
    }
    
    private static bool HasObstacle(Rect rect)
    {
        var center = new Vector3(rect.center.x, 0, rect.center.y);
        var size = new Vector3(rect.size.x / 2, 3, rect.size.y / 2);
        var colliders = Physics.OverlapBox(center, size, Quaternion.identity, ObstacleMask);
        return colliders != null && colliders.Length != 0;
    }

    public void BuildGraph()
    {
        foreach (var child in _children) {
            for (var i = 0; i < 4; i++) {
                var childToConnect = _children[i];
                if (childToConnect == child) continue;
                
                var relativeLocation = child.FindRelativeLocation(IndexToLocation(i));
                child.ConnectChildrenTo(childToConnect, relativeLocation);
                if (child._children != null)
                    child.BuildGraph();
            }
        }
    }
    private Location FindRelativeLocation(Location from)
    {
        var selfLoc = _location;

        return from switch
        {
            Location.NW when selfLoc == Location.NE => Location.W,
            Location.NW when selfLoc == Location.SE => Location.NW,
            Location.NW when selfLoc == Location.SW => Location.N,
            Location.NE when selfLoc == Location.NW => Location.E,
            Location.NE when selfLoc == Location.SE => Location.N,
            Location.NE when selfLoc == Location.SW => Location.NE,
            Location.SE when selfLoc == Location.SW => Location.E,
            Location.SE when selfLoc == Location.NE => Location.S,
            Location.SE when selfLoc == Location.NW => Location.SE,
            Location.SW when selfLoc == Location.NW => Location.S,
            Location.SW when selfLoc == Location.SE => Location.W,
            Location.SW when selfLoc == Location.NE => Location.SW,
            _ => throw new ArgumentOutOfRangeException(nameof(from), from,
                "Unhandled relative location: self: " + Utility.LocationToString(selfLoc) + ", from: " +
                Utility.LocationToString(from))
        };
    }

    private void ConnectChildrenTo(QuadTree node, Location from)
    {
        Debug.Log("connection: " + node._bounds + " with " + _bounds + " from " + from);
        if (_children == null)
            node.ConnectToMe(this, Utility.Opposite(from));
        else {
            var childrenToConnect = FindChildrenToConnect(this, from);
            Debug.Log("To co: original: " + _location + " from: " + from + " to co: " + childrenToConnect[0]._location);
            foreach (var child in childrenToConnect) {
                child.ConnectChildrenTo(node, from);
            }
        }
    }

    
    private static List<QuadTree> FindChildrenToConnect(QuadTree node, Location relativeFrom)
    {
        /* ___________
           | NW | NE |
           ___________
           | SW | SE |
           ___________
         NE's children to connect from NW should be its 2 W children. From SW, it should be his SW child.
         So either the location is from if from is the opposite of our direction or it should be
         from - common_location. E.g. NW - common(NE, NW) = NW - N = W 
         !!!!! DOES NOT WORK RECURSIVELY (if NW has children, then NW's NE should be connected to NE)
         */        
        var toConnect = new List<QuadTree>();

        if (Utility.IsLocationComposed(relativeFrom)) 
            toConnect.Add(node.GETChild(relativeFrom));
        else {
            var (child1, child2) = node.GETChildren(relativeFrom);
            toConnect.Add(child1);
            toConnect.Add(child2);
        }
        return toConnect;
    }

    private void ConnectToMe(QuadTree node, Location from)
    {
        /*
         * This method allows for a node of the graph to connect to the current object. If the object has children,
         * it will call the function on the appropriate children
         */
        if (node._children != null) {
            throw new Exception("Trying to connect from a non-leaf");
        }

        if (_children == null) {
            if (Node.Children.Contains(node.Node)) return;
            if (Physics.Linecast(
                    Utility.Vec2to3(_bounds.center), Utility.Vec2to3(node._bounds.center), ObstacleMask) || 
                Physics.Linecast(
                    Utility.Vec2to3(node._bounds.center), Utility.Vec2to3(_bounds.center), ObstacleMask)
            ) {
                Debug.Log("PREVENTED LINK");
                return;
            }

            Node.Children.Add(node.Node);
            return;
        }

        if (!Utility.IsLocationComposed(from)) {
            var (child1, child2) = GETChildren(@from);
            child1.ConnectToMe(node, from);
            child2.ConnectToMe(node, from);
        }
        else {
            var child = GETChild(from);
            child.ConnectToMe(node, from);
        }
    }

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(_bounds.min.x, 0, _bounds.max.y), new Vector3(_bounds.max.x, 0, _bounds.max.y));
        Gizmos.DrawLine(new Vector3(_bounds.max.x, 0, _bounds.max.y), new Vector3(_bounds.max.x, 0, _bounds.min.y));
        Gizmos.DrawLine(new Vector3(_bounds.max.x, 0, _bounds.min.y), new Vector3(_bounds.min.x, 0, _bounds.min.y));
        Gizmos.DrawLine(new Vector3(_bounds.min.x, 0, _bounds.min.y), new Vector3(_bounds.min.x, 0, _bounds.max.y));
        Gizmos.color = Color.green;
        foreach (var c in Node.Children)
            Gizmos.DrawLine(Utility.Vec2to3(c.Rect.center), Utility.Vec2to3(_bounds.center));
        if (_children == null)
            return;
        foreach (var child in _children) {
            child.OnDrawGizmos();
        }
    }

    public QuadTree NodeFromPoint(Vector2 point)
    {
        if (!Utility.VectorIsInRect(point, _bounds))
            return null;
        if (_children == null || _children.Length == 0)
            return this;
        foreach (var node in _children) {
            if (Utility.VectorIsInRect(point, node._bounds))
                return node.NodeFromPoint(point);
        }

        return null;
    }
}

internal class QuadNode
{
    public Rect Rect;
    public List<QuadNode> Children;

    public QuadNode(Rect rect)
    {
        Rect = rect;
    }
    
    public QuadNode(Rect rect, List<QuadNode> children)
    {
        Rect = rect;
        Children = children;
    }
}

public enum Location
{
    N, E, S, W, NW, NE, SE, SW
}