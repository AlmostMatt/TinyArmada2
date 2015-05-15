using UnityEngine;
using System.Collections.Generic;

public enum Tile {WATER=0, SHORE=1, GRASS=2, BUILDING=3, TEMP=4};

public class Map : MonoBehaviour {
	public GameObject buildingObject;
	public GameObject baseObject;
    public GameObject treeObject;

    // grid size
	private int w = 30;
    private int h = 20;
    private float tileSize = 1f;
	
	private List<List<Tile>> map;
	private List<Building> buildings;

	// texture coordinates
	private float tUnit = 0.25f; // 4x4
	private Vector2 tStone = new Vector2(0,0);
	private Vector2 tGrass = new Vector2(0,1);

	// mesh
	private Mesh mesh;
	private List<Vector3> newVerts = new List<Vector3>();
	private List<int> newTris = new List<int>();
	private List<Vector2> newUV = new List<Vector2>();

    private int squareCount = 0;

	// Use this for initialization
	void Start () {
	/*
		RandomSet<int> rs = new RandomSet<int>();
		rs.Add(1);
		rs.Add(2);
		rs.Add(3);
		rs.Add(4);
		rs.Add(10);
		rs.Add(11);
		rs.Add(12);
		foreach (int x in rs) {
			Debug.Log("rs contains " + x);
		}
		int y = rs.popRandom();
		Debug.Log("rs POP " + y);
		foreach (int x in rs) {
			Debug.Log("rs contains " + x);
		}
		HashSet<int> s2 = new HashSet<int>();
		s2.UnionWith(rs);
		foreach (int x in s2) {
			Debug.Log("s2 contains " + x);
		}
*/
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void generateMap(List<Player> players) {
		map = new List<List<Tile>>();
		buildings = new List<Building>();
		// default - water
		for (int x = 0; x < w; ++x) {
			map.Add(new List<Tile>());
			for (int y = 0; y < h; ++y) {
				map[x].Add(Tile.WATER);
			}
		}

        // possibly: do floodfill of tiles to check 'reachability' of each body of water / dock
        // or keep track of cycles and only create land where cycles exists
        // or just dig trenches/channels/rivers/bays afterward

		HashSet<Vector2> tempTiles = new HashSet<Vector2>();

		// terrain
		int numislands = 10;//15;
		int islandSizeMin = 3;
		int islandSizeMax = 18;
        int treeDensity = 4;
		int numcolonies = 20;
        //Random.seed = 
        System.Random rnd = new System.Random();
        int i = 0;
        while (i < numislands) {
            int x = Random.Range(1,w-1);
            int y = Random.Range(1,h-1);
            if (!isWater(getTile (x,y))) { continue; }

            // a set of water values that could be made into land
            RandomSet<Vector2> adjacentWater = new RandomSet<Vector2>(rnd);
            adjacentWater.Add(new Vector2(x,y));

			int numland = Random.Range(islandSizeMin, islandSizeMax + 1);
            for (int n = 0; n < numland && adjacentWater.Count > 0; n++) {
                Vector2 randTile = adjacentWater.popRandom();
                setTile(randTile, Tile.GRASS);
                for (int tr=0; tr<treeDensity; ++tr) {
                    GameObject tree = Instantiate(treeObject);
                    tree.transform.parent = transform;
                    tree.transform.localPosition = mapToGame(randTile) + new Vector3(Random.Range(-tileSize*0.45f, tileSize*0.45f),
                                                                                Random.Range(-tileSize*0.45f, tileSize*0.45f), 0);
                    float sz = Random.Range(tileSize * 0.8f, tileSize * 1.2f); // tree radius is about 0.29 by default
                    tree.transform.localScale = new Vector3(sz, sz, 1f);
                }
                foreach (Vector2 adj in getNeighbours4(randTile)) {
					if (isWater(getTile(adj))) {
						// exclude the outer border
						if (adj.x > 0 && adj.y > 0 && adj.x < w - 1 && adj.y < h - 1) {
	                        adjacentWater.Add(adj);
						}
                    }
				}
			}
			// Mark an area around each island as reserved so that islands don't touch each other
			foreach (Vector2 adj in adjacentWater) {
				setTile(adj, Tile.TEMP);
				tempTiles.Add(adj);
				foreach (Vector2 adj2 in getNeighbours8(adj)) {
					if (isWater(getTile(adj2))) {
						setTile(adj2, Tile.TEMP);
						tempTiles.Add(adj2);
					}
				}
			}
            ++i;
        }

		foreach (Vector2 t in tempTiles) {
			setTile (t, Tile.WATER);
		}

		// find coastal tiles (and only ocean reachable tiles at that)
		HashSet<Vector2> seen = new HashSet<Vector2>();
		Queue<Vector2> todo = new Queue<Vector2>();
		RandomSet<Vector2> coastal = new RandomSet<Vector2>(rnd);
		// since I excluded the outer border, it is definitely water
		todo.Enqueue(new Vector2(0,0));
		seen.Add(todo.Peek());
		while (todo.Count > 0) {
			Vector2 randTile = todo.Dequeue();
			foreach (Vector2 adj in getNeighbours4(randTile)) {
				if (seen.Contains(adj)) {
					continue;
				} else if (isWater(getTile(adj))) {
					todo.Enqueue(adj);
				} else {
					coastal.Add(adj);
				}
				seen.Add(adj);
			}
		}

		// players and bases
		foreach (Player p in players) {
			if (p.isNeutral || coastal.Count == 0) continue;
			Vector2 pos = coastal.popRandom(); // new Vector2(Random.Range(3, w-3), Random.Range(3, h-3));
			setTile(pos, Tile.BUILDING);
			Building building = Instantiate(baseObject).GetComponent<Building>();
			building.init(pos, BuildingType.BASE);
			building.setOwner(p);
			buildings.Add(building);
		}

		// colonies
		for (i = 0; i<numcolonies && coastal.Count > 0; i++) {
			Vector2 pos = coastal.popRandom();// = new Vector2(Random.Range(0, w), Random.Range(0, h));
            //if (!isBuildable(getTile(pos))) {
            //    i--;
            //    continue;
            //}
			setTile(pos, Tile.BUILDING);
			Building building = Instantiate(buildingObject).GetComponent<Building>();
			building.init(pos, BuildingType.COLONY);
			building.setOwner(players[0]);
			buildings.Add(building);
     	}
        // spin docks
		generateMesh();
        Pathing.updateMap(this);
	}

    private Vector2 textureCoord(Tile tile) {
        switch(tile) {
        case Tile.WATER:
            return new Vector2(1,0);
        case Tile.BUILDING:
			return new Vector2(0,0);
		case Tile.TEMP:
			return new Vector2(2,0);
		case Tile.GRASS:
            return new Vector2(0,0);
        case Tile.SHORE:
        default:
            return new Vector2(0,0);
        }
    }

	private void generateMesh() {
        mesh = GetComponent<MeshFilter>().mesh;
        for (int x=0; x<w; ++x) {
            for (int y=0; y<h; ++y) {
                Vector3 pos = mapToGame(x,y);
                genSquare(pos.x, pos.y, textureCoord(getTile (x,y)));
            }
        }

        mesh.Clear();
        mesh.vertices = newVerts.ToArray();
        mesh.triangles = newTris.ToArray();
        mesh.uv = newUV.ToArray();
        mesh.Optimize();
        mesh.RecalculateNormals();

        // cleanup for next time
        squareCount = 0;
        newVerts.Clear();
        newTris.Clear();
        newUV.Clear();
    }

    private void genSquare(float x, float y, Vector2 texture) {
        newVerts.Add(new Vector3(x-tileSize/2, y+tileSize/2, 0));
        newVerts.Add(new Vector3(x+tileSize/2, y+tileSize/2, 0));
        newVerts.Add(new Vector3(x+tileSize/2, y-tileSize/2, 0));
        newVerts.Add(new Vector3(x-tileSize/2, y-tileSize/2, 0));
        
        newTris.Add(squareCount*4);
        newTris.Add(squareCount*4 + 1);
        newTris.Add(squareCount*4 + 3);
        newTris.Add(squareCount*4 + 1);
        newTris.Add(squareCount*4 + 2);
        newTris.Add(squareCount*4 + 3);
        
        newUV.Add(new Vector2(tUnit * texture.x, tUnit * texture.y + tUnit));
        newUV.Add(new Vector2(tUnit * texture.x + tUnit, tUnit * texture.y + tUnit));
        newUV.Add(new Vector2(tUnit * texture.x + tUnit, tUnit * texture.y));
        newUV.Add(new Vector2(tUnit * texture.x, tUnit * texture.y));
        squareCount++;
    }
    
    public Vector3 mapToGame(int x, int y) {
        return mapToGame(new Vector2(x,y));
    }
    
    public Vector3 mapToGame(float x, float y) {
        return mapToGame(new Vector2(x,y));
    }
    
    public Vector3 mapToGame(Vector2 mapPos) {
        return tileSize * (Vector3) (mapPos - new Vector2(w/2f, h/2f));
    }

	public Vector2 gameToMap(Vector3 gamePos) {
        Vector2 mapPos = ((1/tileSize) * ((Vector2) gamePos) + new Vector2(w/2f, h/2f));
		mapPos.x = Mathf.RoundToInt(mapPos.x);
		mapPos.y = Mathf.RoundToInt(mapPos.y);
		return mapPos;
	}

	public List<Building> getBuildings() {
		return buildings;
	}
	
	// assumes map coordinates, not game
	public Tile getTile(Vector2 coord) {
		return getTile((int) coord.x, (int) coord.y);
	}
	
	// assumes map coordinates, not game
	public Tile getTile(int x, int y) {
		if (x >= 0 && x < map.Count && y >= 0 && y < map[x].Count) {
			return map[x][y];
		} else {
			return Tile.GRASS; // ideally this doesn't come up much. neighbours don't handle it either.
		}
	}
    
    private void setTile(Vector2 coord, Tile value) {
        map[(int) coord.x][(int) coord.y] = value;
    }

    // helper functions for tile/grid manip
    private bool isWater(Tile tile) {
        return tile == Tile.WATER;
    }

    private bool isGround(Tile tile) {
        return !isWater (tile);
    }

    private bool isBuildable(Tile tile) {
        return isGround(tile) && tile != Tile.BUILDING;
    }

    public bool isWalkable(Tile tile) {
        // consider buildings water for now since they are where boats spawn
        return isWater(tile) || tile == Tile.BUILDING;
    }
    
    public HashSet<Vector2> getNeighbours4(int x, int y) {
        return getNeighbours4(new Vector2(x,y));
    }

    public HashSet<Vector2> getNeighbours4(Vector2 coord) {
        HashSet<Vector2> result = new HashSet<Vector2>();
        if (coord.x > 0) result.Add(new Vector2(coord.x - 1, coord.y));
        if (coord.x < w - 1) result.Add(new Vector2(coord.x + 1, coord.y));
        if (coord.y > 0) result.Add(new Vector2(coord.x, coord.y - 1));
        if (coord.y < h -1) result.Add(new Vector2(coord.x, coord.y + 1));
        return result;
    }

    // include diagonals
    public HashSet<Vector2> getNeighbours8(int x, int y) {
        return getNeighbours8(new Vector2(x,y));
    }

    public HashSet<Vector2> getNeighbours8(Vector2 coord) {
        HashSet<Vector2> result = new HashSet<Vector2>();
        for (int x = Mathf.Max ((int) coord.x - 1, 0); x <= Mathf.Min(w - 1, (int) coord.x + 1); x++) {
            for (int y = Mathf.Max ((int) coord.y - 1, 0); y <= Mathf.Min(h - 1, (int) coord.y + 1); y++) {
                result.Add(new Vector2(x,y));
            }
        }
        result.Remove (coord);
        return result;
    }
}
