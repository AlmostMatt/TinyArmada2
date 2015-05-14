using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Priority_Queue;

public class Path
{
    public Vector2 start;
    public Vector2 goal;
    public float destRadius;
    public float length; // in order to do timing predictions, can be approximate
    public bool arrived = false;

    public List<Vector2> points;
    // possibly give every point on the path a radius or a width

    public Path() {
        points = new List<Vector2>();
    }

    public void followPath(Unit u) {
        Vector2 next = nextPoint();
        float nextRadius = (points.Count == 1) ? destRadius : u.radius;
        float dd = (next - (Vector2) u.transform.position).sqrMagnitude;
        if (dd < nextRadius * nextRadius) {
            if (points.Count == 1) {
                u.brake();
                arrived = true;
            } else {
                points.RemoveAt(0);
                // accelerate toward the next node
                followPath(u);
            }
        } else {
            if (points.Count == 1) {
                u.arrival(next);
            } else {
                u.seek(next);
            }
        }
    }

    public Vector2 nextPoint() {
        return points[0];
    }
}

public class PathNode : PriorityQueueNode
{
    public int f { // goal cost, g+h {get; private set}
        get {return g + h;}
    }
    public int g; // cost to reach this node
    public int h; // heuristic from this node
    public PathNode parent;
    public Vector2 tile;
    public bool seenBefore = false;
}

public class Pathing
{
    // Pool and reuse priority open queue and closed set.
    // also the node objects
    private static Map map;
    private static List<List<Tile>> tiles;

    public static void updateMap(Map newmap) {
        map = newmap;
        tiles = map.getTileMap();
        // generate something like a navmesh if desired
    }

    // grid coordinates, uses bresenham
    public static bool raycast(Vector2 t1, Vector2 t2) {
        if (t1.x > t2.x) {
            return raycast (t2, t1);
        }
        int x1 = (int) t1.x, x2 = (int) t2.x;
        int y1 = (int) t1.y, y2 = (int) t2.y;
        int y;
        if (x1 == x2) {
            for (y = Mathf.Min (y1, y2); y<=Mathf.Max (y1,y2);++y) {
                if (!map.isWalkable(tiles[x1][y])) {
                    return false;
                }
            }
			return true;
        }
        float dx = x2-x1, dy=y2-y1;
        float yoffset = 0f;
        float deltaoffset = Mathf.Abs (dy / dx);
        y = y1;
        for (int x = x1; x <= x2; ++x) {
            if (!map.isWalkable(tiles[x][y])) {
                return false;
            }
            yoffset += deltaoffset;
			while (yoffset >= 0.5f) {
                if (!map.isWalkable(tiles[x][y])) {
                    return false;
                }
                y += (int) Mathf.Sign(y2 - y1);
				yoffset -= 1f;
			}
        }
        return true;
    }

    private static PathNode getNode(Dictionary<Vector2, PathNode> nodeMap, Vector2 tile, Vector2 goal) {
        if (nodeMap.ContainsKey(tile)) {
            nodeMap[tile].seenBefore = true;
            return nodeMap[tile];
        }
        PathNode node = new PathNode();
        node.tile = tile;
        node.h = manhattan(tile, goal);
        nodeMap[tile] = node;
        return node;
    }

    // grid coordinates
    private static int manhattan(Vector2 t1, Vector2 t2) {
        // TODO: change this to count diagonals (abs(dx-dy) + diag * min(dx,dy)) but with more abs
        return 10 * (int) (Mathf.Abs(t2.x - t1.x) + Mathf.Abs(t2.y - t1.y));
    }

    // takes game coordinates
    public static Path findPath(Vector2 p1, Vector2 p2, float destRadius) {
        Vector2 t1 = map.gameToMap(p1);
        Vector2 t2 = map.gameToMap(p2);
        //map.isWalkable(tiles[0][0]);
        Path path = new Path();
        path.start = p1;
        path.goal = p2;
        path.destRadius = destRadius;
        // try cached partial path (recursively)

        // try raycast
		if (raycast(t1, t2)) {
			path.points.Add(p2);
			path.length = (path.goal - path.start).magnitude;
		} else {
			// compute path, (heirarchical?)
			List<Vector2> tiles = astar(t1, t2);
			// smooth path

			// cache parts of path
			// convert back to game coordinates
			Vector2 prevPoint = path.start;
			path.length = 0f;
			foreach (Vector2 tile in tiles) {
				Vector2 pt = map.mapToGame(tile);
				path.length += (pt - prevPoint).magnitude;
				path.points.Add(pt);
			}
			path.points[path.points.Count - 1] = path.goal;
		}
        return path;
    }

	// tile based path
	private static List<Vector2> astar(Vector2 t1, Vector2 t2) {
        // TODO: reverse this to start at t2 and go toward t1 so that inland points fail faster
        // TODO: find nearest water tile to t2 if t2 is inland

		int MAX_OPEN = 200;
        HeapPriorityQueue<PathNode> open = new HeapPriorityQueue<PathNode>(MAX_OPEN);
        Dictionary<Vector2, PathNode> nodes = new Dictionary<Vector2, PathNode>();
        HashSet<Vector2> closed = new HashSet<Vector2>();

        PathNode node = getNode(nodes, t1, t2);
        node.g = 0;
		open.Enqueue(node, node.f);

        while (open.Count > 0) {
            node = open.Dequeue();
            closed.Add(node.tile);
            if (node.tile == t2) {
                break;
            }
            foreach (Vector2 ntile in map.getNeighbours4(node.tile)) {   
                if (!map.isWalkable(map.getTile(ntile)) || closed.Contains(ntile)) {
                    continue;
                }
                PathNode othernode = getNode(nodes, ntile, t2);
                int newg = node.g + 10;
                if (othernode.seenBefore) {
                    if (newg < othernode.g) {
                        othernode.g = newg;
                        othernode.parent = node;
                        open.UpdatePriority(othernode, othernode.f);
                    }
                } else {
                    othernode.g = newg;
                    othernode.parent = node;
                    open.Enqueue(othernode, othernode.f);
                }
            }
        }

        List<Vector2> points = new List<Vector2>();
        if (!closed.Contains(t2)) {
            // no path found
            points.Add(t1);
        } else {
            PathNode prevN = nodes[t2];
            while (prevN.parent != null) {
                points.Add (prevN.tile);
                prevN = prevN.parent;
            }
            points.Reverse();
        }
        return points;
	}
}

