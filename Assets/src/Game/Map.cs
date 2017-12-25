using UnityEngine;
using System.Collections.Generic;

public enum Tile {WATER=0, SHORE=1, GRASS=2, BUILDING=3, TEMP=4};

public class Map : MonoBehaviour {
	public GameObject buildingObject;
	public GameObject baseObject;
    public GameObject treeObject;
	public GameObject islandObject;

    // grid size
	private int w = 30;
    private int h = 20;
    private float tileSize = 1f;
	
	private List<List<Tile>> tileMap;
	private List<Building> buildings;

	// texture coordinates
	private float tUnit = 0.25f; // 4x4
	private Vector2 tStone = new Vector2(0,0);
	private Vector2 tGrass = new Vector2(0,1);

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
		tileMap = new List<List<Tile>>();
		buildings = new List<Building>();
		// default - water
		for (int x = 0; x < w; ++x) {
			tileMap.Add(new List<Tile>());
			for (int y = 0; y < h; ++y) {
				tileMap[x].Add(Tile.WATER);
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
				GameObject island = Instantiate(islandObject);
				island.transform.parent = transform;
				island.transform.localPosition = mapToGame(randTile);
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

		// Floodfill water tiles. For each tile, count the number of adjacent land tiles.
		// Any water tile with 1-2 adjacent land tiles is a valid place for a dock.
		// The set of land tiles adjacent to validDockTiles is the set of validBuildingTiles.
		HashSet<Vector2> seen = new HashSet<Vector2>();
		Queue<Vector2> todo = new Queue<Vector2>();
		HashSet<Vector2> validDockTiles = new HashSet<Vector2>();
		RandomSet<Vector2> validBuildingTiles = new RandomSet<Vector2>(rnd);
		// since I excluded the outer border, it is definitely water
		todo.Enqueue(new Vector2(0,0));
		seen.Add(todo.Peek());
		while (todo.Count > 0) {
			Vector2 randTile = todo.Dequeue();
			HashSet<Vector2> adjacentCoasts = new HashSet<Vector2>();
			foreach (Vector2 adj in getNeighbours4(randTile)) {
				if (isWater(getTile(adj))) {
					if (!seen.Contains(adj)) {
						todo.Enqueue(adj);
					}
				} else {
					adjacentCoasts.Add(adj);
				}
				seen.Add(adj);
			}
			if (adjacentCoasts.Count > 0 && adjacentCoasts.Count < 3) {
				validDockTiles.Add(randTile);
				validBuildingTiles.AddRange(adjacentCoasts);
			}
		}

		// players and bases
		foreach (Player p in players) {
			if (p.isNeutral || validBuildingTiles.Count == 0) continue;
			Vector2 buildingPos = validBuildingTiles.popRandom();
			Vector2 dockTile = getRandomValidNeighbour4(buildingPos, validDockTiles);
			setTile(buildingPos, Tile.BUILDING);
			Building building = Instantiate(baseObject).GetComponent<Building>();
			building.init(buildingPos, dockTile, BuildingType.BASE);
			building.setOwner(p);
			buildings.Add(building);
		}

		// colonies
		for (i = 0; i < numcolonies && validBuildingTiles.Count > 0; i++) {
			Vector2 buildingPos = validBuildingTiles.popRandom();
			Vector2 dockTile = getRandomValidNeighbour4(buildingPos, validDockTiles);
			setTile(buildingPos, Tile.BUILDING);
			Building building = Instantiate(buildingObject).GetComponent<Building>();
			building.init(buildingPos, dockTile, BuildingType.COLONY);
			building.setOwner(players[0]); // Neutral
			buildings.Add(building);
     	}
        Pathing.updateMap(this);

		// Spawn trees.
		for (int x = 0; x < w; ++x) {
			for (int y = 0; y < h; ++y) {
				if (tileMap[x][y] == Tile.GRASS) {
					for (int tr=0; tr<treeDensity; ++tr) {
						GameObject tree = Instantiate(treeObject);
						tree.transform.parent = transform;
						// z = -0.2f puts the tree in front of the sand
						tree.transform.localPosition = mapToGame(x,y) + new Vector3(
							Random.Range(-tileSize*0.45f, tileSize*0.45f),
							Random.Range(-tileSize*0.45f, tileSize*0.45f),
							-0.45f);
						float sz = Random.Range(tileSize * 0.8f, tileSize * 1.2f); // tree radius is about 0.29 by default
						tree.transform.localScale = new Vector3(sz, sz, 1f);
					}
				}
			}
		}
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
		if (x >= 0 && x < tileMap.Count && y >= 0 && y < tileMap[x].Count) {
			return tileMap[x][y];
		} else {
			return Tile.WATER;
		}
	}
    
    private void setTile(Vector2 coord, Tile value) {
        tileMap[(int) coord.x][(int) coord.y] = value;
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
        return isWater(tile);
    }

	public Vector2 getRandomValidNeighbour4(Vector2 currentTile, ICollection<Vector2> validTiles) {
		RandomSet<Vector2> potentialNeighbours = new RandomSet<Vector2>();
		potentialNeighbours.AddRange(getNeighbours4(currentTile));
		potentialNeighbours.Intersect(validTiles);
		return potentialNeighbours.popRandom();
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
