	using UnityEngine;
using System.Collections.Generic;

public enum Tile {WATER=0, SHORE=1, GRASS=2, BUILDING=3, TEMP=4};

public class Map : MonoBehaviour {
	public GameObject buildingObject;
	public GameObject baseObject;
    public GameObject treeObject;
	public GameObject islandObject;

	public bool hasBeenGenerated = false;

    // grid size
	public int mapWidth;
    public int mapHeight;
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
		for (int x = 0; x <= mapWidth; x++) {
			Debug.DrawLine(mapToGame(-0.5f + x, -0.5f), mapToGame(-0.5f + x, mapHeight - 0.5f), Color.black, 0f, true);
		}
		for (int y = 0; y <= mapHeight; y++) {
			Debug.DrawLine(mapToGame(-0.5f, -0.5f + y), mapToGame(mapWidth - 0.5f, -0.5f + y), Color.black, 0f, true);
		}
	}

	public void generateMap(List<Player> players, int width=16, int height=16, int tilesBetweenIslands=2) {
		hasBeenGenerated = true;
		mapWidth = width;
		mapHeight = height;
		tileMap = new List<List<Tile>>();
		buildings = new List<Building>();

		// Default the entire map to water.
		RandomSet<Vector2> waterTiles = new RandomSet<Vector2>();
		for (int x = 0; x < mapWidth; ++x) {
			tileMap.Add(new List<Tile>());
			for (int y = 0; y < mapHeight; ++y) {
				tileMap[x].Add(Tile.WATER);
				// The border of the map is water, but we don't want to spawn islands that touch the edge.
				if (x>0 && x<mapWidth-1 && y>0 && y<mapHeight-1) {
					waterTiles.Add(new Vector2(x,y));
				}
			}
		}

		// Terrain
		int numislands = 12;//15;
		int islandSizeMin = 3;
		int islandSizeMax = 18;
        int treeDensity = 4;
		int numcolonies = 20;
        //Random.seed = 
		for (int islandsCreated=0; islandsCreated < numislands && waterTiles.Count > 0; islandsCreated++) {
			Vector2 initialTile = waterTiles.popRandom();

			// A set of water values that could be made into land
			RandomSet<Vector2> adjacentWater = new RandomSet<Vector2>();
			RandomSet<Vector2> islandTiles = new RandomSet<Vector2>();
            adjacentWater.Add(initialTile);

			int islandSize = Random.Range(islandSizeMin, islandSizeMax + 1);
            for (int n = 0; n < islandSize && adjacentWater.Count > 0; n++) {
                Vector2 randTile = adjacentWater.popRandom();
				waterTiles.Remove(randTile);
				adjacentWater.UnionWith(getValidNeighbour4(randTile, waterTiles));

                setTile(randTile, Tile.GRASS);
				GameObject island = Instantiate(islandObject);
				island.transform.parent = transform;
				island.transform.localPosition = mapToGame(randTile);
				islandTiles.Add(randTile);
			}
			// Remove water tiles near the island from the available water tiles for future island generation.
			for (int i=0; i < tilesBetweenIslands; i++) {
				RandomSet<Vector2> temporarySet = new RandomSet<Vector2>();
				foreach (Vector2 tile in islandTiles) {
					temporarySet.UnionWith(getValidNeighbour8(tile, waterTiles));
				}
				islandTiles = temporarySet;
				waterTiles.DifferenceWith(islandTiles);
			}
        }

		// Floodfill water tiles that are reachable from the corner of the map.
		// For each tile, count the number of adjacent land tiles.
		// Any water tile with 1-2 adjacent land tiles is a valid place for a dock.
		// The set of land tiles adjacent to validDockTiles is the set of validBuildingTiles.
		HashSet<Vector2> seen = new HashSet<Vector2>();
		Queue<Vector2> todo = new Queue<Vector2>();
		HashSet<Vector2> validDockTiles = new HashSet<Vector2>();
		RandomSet<Vector2> validBuildingTiles = new RandomSet<Vector2>();
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
				validBuildingTiles.UnionWith(adjacentCoasts);
			}
		}

		// Put each player's base at the potential tile closest to their side of the map. Player1 should spawn at -y
		// Player 0 is neutral. Player 1 should spawn at +y.
		for (int playerIndex=1; playerIndex < players.Count; playerIndex++) {
			float angleFromCenter = (Mathf.PI/2f) + (playerIndex * 2f * Mathf.PI / (players.Count - 1));
			Vector2 preferredTile = 0.9f * new Vector2(
				mapWidth * (0.5f + 0.5f * Mathf.Cos(angleFromCenter)),
				mapHeight * (0.5f + 0.5f * Mathf.Sin(angleFromCenter)));
			float minDistFromPreferred = float.MaxValue;
			Vector2 buildingPos = Vector2.zero;
			foreach (Vector2 possibleTile in validBuildingTiles) {
				// TODO: ignore docks that face away from the center of the map.
				// (The dot product of dockDirection and angleFromCenter should be negative)
				float distFromPreferred = (possibleTile - preferredTile).sqrMagnitude;
				if (distFromPreferred < minDistFromPreferred) {
					minDistFromPreferred = distFromPreferred;
					buildingPos = possibleTile;
				}
			}
			validBuildingTiles.Remove(buildingPos);
			Vector2 dockTile = getValidNeighbour4(buildingPos, validDockTiles).popRandom();
			setTile(buildingPos, Tile.BUILDING);
			Building building = Instantiate(baseObject).GetComponent<Building>();
			building.init(buildingPos, dockTile, BuildingType.BASE);
			building.setOwner(players[playerIndex]);
			buildings.Add(building);
		}

		// colonies
		for (int i = 0; i < numcolonies && validBuildingTiles.Count > 0; i++) {
			Vector2 buildingPos = validBuildingTiles.popRandom();
			Vector2 dockTile = getValidNeighbour4(buildingPos, validDockTiles).popRandom();
			setTile(buildingPos, Tile.BUILDING);
			Building building = Instantiate(buildingObject).GetComponent<Building>();
			building.init(buildingPos, dockTile, BuildingType.COLONY);
			building.setOwner(players[0]); // Neutral
			buildings.Add(building);
     	}
        Pathing.updateMap(this);

		// Spawn trees.
		for (int x = 0; x < mapWidth; ++x) {
			for (int y = 0; y < mapHeight; ++y) {
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
        return tileSize * (Vector3) (mapPos - new Vector2(mapWidth/2f, mapHeight/2f));
    }

	public Vector2 gameToMap(Vector3 gamePos) {
        Vector2 mapPos = ((1/tileSize) * ((Vector2) gamePos) + new Vector2(mapWidth/2f, mapHeight/2f));
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

	// Returns neighbours of currentTile that are also present in validTiles.
	public RandomSet<Vector2> getValidNeighbour4(Vector2 currentTile, ICollection<Vector2> validTiles) {
		RandomSet<Vector2> potentialNeighbours = new RandomSet<Vector2>();
		potentialNeighbours.UnionWith(getNeighbours4(currentTile));
		potentialNeighbours.IntersectWith(validTiles);
		return potentialNeighbours;
	}

	// Returns neighbours of currentTile that are also present in validTiles.
	public RandomSet<Vector2> getValidNeighbour8(Vector2 currentTile, ICollection<Vector2> validTiles) {
		RandomSet<Vector2> potentialNeighbours = new RandomSet<Vector2>();
		potentialNeighbours.UnionWith(getNeighbours8(currentTile));
		potentialNeighbours.IntersectWith(validTiles);
		return potentialNeighbours;
	}

    public HashSet<Vector2> getNeighbours4(int x, int y) {
        return getNeighbours4(new Vector2(x,y));
    }

    public HashSet<Vector2> getNeighbours4(Vector2 coord) {
        HashSet<Vector2> result = new HashSet<Vector2>();
        if (coord.x > 0) result.Add(new Vector2(coord.x - 1, coord.y));
        if (coord.x < mapWidth - 1) result.Add(new Vector2(coord.x + 1, coord.y));
        if (coord.y > 0) result.Add(new Vector2(coord.x, coord.y - 1));
        if (coord.y < mapHeight -1) result.Add(new Vector2(coord.x, coord.y + 1));
        return result;
    }

    // include diagonals
    public HashSet<Vector2> getNeighbours8(int x, int y) {
        return getNeighbours8(new Vector2(x,y));
    }

    public HashSet<Vector2> getNeighbours8(Vector2 coord) {
        HashSet<Vector2> result = new HashSet<Vector2>();
        for (int x = Mathf.Max ((int) coord.x - 1, 0); x <= Mathf.Min(mapWidth - 1, (int) coord.x + 1); x++) {
            for (int y = Mathf.Max ((int) coord.y - 1, 0); y <= Mathf.Min(mapHeight - 1, (int) coord.y + 1); y++) {
                result.Add(new Vector2(x,y));
            }
        }
        result.Remove (coord);
        return result;
    }
}
