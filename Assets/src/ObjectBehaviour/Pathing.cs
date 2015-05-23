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
	
	public Path copy() {
		Path newPath = new Path();
		newPath.start = start;
		newPath.goal = goal;
		newPath.destRadius = destRadius;
		newPath.length = length;
		newPath.arrived = false;
		newPath.points.AddRange(points);
		return newPath;
	}

	public Path reversedCopy() {
		Path newPath = copy();
		newPath.start = goal;
		newPath.goal = start;
		newPath.points.Reverse();
		// generally the start is not a point in the path
		if (newPath.points[0] == newPath.start) {
			newPath.points.RemoveAt(0);
		}
		if (newPath.points.Count == 0 || newPath.points[newPath.points.Count - 1] != newPath.goal) {
			newPath.points.Add(newPath.goal);
		}
		return newPath;
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

    public static void updateMap(Map newmap) {
        map = newmap;
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
                if (!map.isWalkable(map.getTile(x1,y))) {
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
			if (!map.isWalkable(map.getTile(x,y))) {
                return false;
            }
            yoffset += deltaoffset;
			while (yoffset >= 0.5f) {
				if (!map.isWalkable(map.getTile(x,y))) {
                    return false;
                }
                y += (int) Mathf.Sign(y2 - y1);
				yoffset -= 1f;
			}
        }
        return true;
    }

    private static PathNode getNode(Dictionary<Vector2, PathNode> nodeMap, Vector2 tile, HashSet<Vector2> goal) {
        if (nodeMap.ContainsKey(tile)) {
            nodeMap[tile].seenBefore = true;
            return nodeMap[tile];
        }
        PathNode node = new PathNode();
        node.tile = tile;
        node.h = minManhattan(tile, goal);
        nodeMap[tile] = node;
        return node;
    }
	
	// grid coordinates
	private static int manhattan(Vector2 t1, Vector2 t2) {
		// this is slightly better than manhattan for diagonal costs
		int dx = (int) Mathf.Abs(t2.x - t1.x);
		int dy = (int) Mathf.Abs(t2.y - t1.y);
		return 10 * Mathf.Abs(dx - dy) + 14 * Mathf.Min(dx, dy);
	}

	// grid coordinates
	private static int minManhattan(Vector2 t1, HashSet<Vector2> t2Set) {
		// this is slightly better than manhattan for diagonal costs
		int minV = int.MaxValue;
		foreach (Vector2 t2 in t2Set) {
			minV = Mathf.Min (minV, manhattan(t1, t2));
		}
		return minV;
	}

	/*
	 * Given a list of possible destinations, figure out which can be reached most quickly and return a path to it
	 * 
	 * Idea1: priority queue of dests by manhattan and stop after manhattan exceeds path length
	 * Idea2: pathfind with heuristic the minimum of the manhattan distances
	 * 		IE a non-linear graph where all of the desination nodes have a shortcut to a new goal node
	 */
	public static Path findShortestPath(Vector2 p1, HashSet<Vector2> p2Set, float destRadius) {
		Vector2 t1 = map.gameToMap(p1);
		HashSet<Vector2> t2Set = new HashSet<Vector2>();
		// map tile to point for more precision
		Dictionary<Vector2, Vector2> goalMap = new Dictionary<Vector2, Vector2>();
		bool possible = false;
		foreach (Vector2 p2 in p2Set) {
			Vector2 t2 = map.gameToMap(p2);
			goalMap[t2] = p2;
			t2Set.Add(t2);
			if (map.isWalkable(map.getTile(t2))) {
				possible = true;
			}
		}
		Path path = new Path();
		path.start = p1;
		path.destRadius = destRadius;
		if (!possible) {
			path.goal = path.start;
			path.points.Add(path.goal);
			path.length = 0f;
			return path;
		}
		// try cached partial path (recursively)

		// compute path, (heirarchical?)
		List<Vector2> tiles = astar(t1, t2Set);
		// smooth path
		tiles = smoothed(t1, tiles);
		// cache parts of path (each tile's shortest path to each later tile on the same path is optimal)
		// convert back to game coordinates
		Vector2 prevPoint = path.start;
		path.length = 0f;
		foreach (Vector2 tile in tiles) {
			Vector2 pt = map.mapToGame(tile);
			if (goalMap.ContainsKey(tile)) {
				path.goal = goalMap[tile];
				pt = path.goal;
			}
			path.length += (pt - prevPoint).magnitude;
			path.points.Add(pt);
		}

		return path;
	}

    // takes game coordinates
    public static Path findPath(Vector2 p1, Vector2 p2, float destRadius) {
		HashSet<Vector2> p2Set = new HashSet<Vector2>();
		p2Set.Add(p2);
		return findShortestPath(p1, p2Set, destRadius);
    }

	private static List<Vector2> smoothed(Vector2 start, List<Vector2> path) {
		// TODO: this is not the optimal path.
		// consider: ideal path move 5 straight then turn, but the raycast says I can move 10 straight so it does that instead

		// binary search to see how far you can raycast
		Vector2 current = start;
		List<Vector2> smoothPath = new List<Vector2>();
		int i1 = 0; // first candidate for smoothed path
		while (i1 < path.Count) {
			int i2 = path.Count - 1;
			while (i1 < i2) {
				int i = (i1 + i2 + 1)/2;
				bool canSkip = raycast(current, path[i]);
				if (canSkip) {
					i1 = i;
				} else {
					i2 = i - 1;
				}
			}
			smoothPath.Add (path[i1]);
			i1++;
		}
		return smoothPath;
	}

	// tile based path
	private static List<Vector2> astar(Vector2 t1, HashSet<Vector2> t2Set) {
        // TODO: reverse this to start at t2 and go toward t1 so that inland points fail faster
        // TODO: find nearest water tiles to t2 if t2 is inland

		int MAX_OPEN = 100;
        HeapPriorityQueue<PathNode> open = new HeapPriorityQueue<PathNode>(MAX_OPEN);
        Dictionary<Vector2, PathNode> nodes = new Dictionary<Vector2, PathNode>();
        HashSet<Vector2> closed = new HashSet<Vector2>();

		PathNode node = getNode(nodes, t1, t2Set);
        node.g = 0;
		open.Enqueue(node, node.f);
		Vector2 destTile = new Vector2();
		bool foundDest = false;

        while (open.Count > 0) {
            node = open.Dequeue();
            closed.Add(node.tile);
			if (t2Set.Contains(node.tile)) {
				destTile = node.tile;
				foundDest = true;
                break;
            }
            foreach (Vector2 ntile in map.getNeighbours8(node.tile)) {   
                if (!map.isWalkable(map.getTile(ntile)) || closed.Contains(ntile)) {
                    continue;
                }
				PathNode othernode = getNode(nodes, ntile, t2Set);
                int newg = (ntile.x == node.tile.x || ntile.y == node.tile.y) ? node.g + 10 : node.g + 14;
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
        if (!foundDest || t2Set.Contains(t1)) { // fix for path to current tile when current tile is a wall
            // no path found
            points.Add(t1);
        } else if (foundDest) {
			// it should have stopped after exactly one item in t2set was reached
			PathNode prevN = nodes[destTile];
            while (prevN.parent != null) {
                points.Add (prevN.tile);
                prevN = prevN.parent;
            }
            points.Reverse();
        }
        return points;
	}

	/*
	 * original implementation
	private static List<Vector2> astar(Vector2 t1, Vector2 t2) {
		// TODO: reverse this to start at t2 and go toward t1 so that inland points fail faster
		// TODO: find nearest water tile to t2 if t2 is inland
		
		int MAX_OPEN = 100;
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
			foreach (Vector2 ntile in map.getNeighbours8(node.tile)) {   
				if (!map.isWalkable(map.getTile(ntile)) || closed.Contains(ntile)) {
					continue;
				}
				PathNode othernode = getNode(nodes, ntile, t2);
				int newg = (ntile.x == node.tile.x || ntile.y == node.tile.y) ? node.g + 10 : node.g + 14;
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
		if (!closed.Contains(t2) || nodes[t2].parent == null) { // fix for path to current tile when current tile is a wall
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
	}*/
}

