using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AILogic
{
	private Map map;

	private float timeSinceHeatmapUpdate = 0f;
	private List<List<int>> heatmap;
	private RandomSet<Vector2> walkableTiles;
	private RandomSet<Vector2> playerInfluencedTiles;
	private RandomSet<Vector2> enemyInfluencedTiles;

	private static UnitType[] AI_BUILD_ORDER = {
		//UnitType.MERCHANT, 
		//UnitType.MERCHANT, 
		UnitType.MERCHANT, 
		UnitType.GALLEY,
		UnitType.MERCHANT,
		UnitType.MERCHANT,
		UnitType.GALLEY,
		UnitType.GALLEY,
		UnitType.MERCHANT, 
		UnitType.MERCHANT, 
		UnitType.MERCHANT, 
	};

	private Player aiPlayer;

	public AILogic (Player aiPlayer)
	{
		this.aiPlayer = aiPlayer;
		this.map = Scene.getMap();
	}

	// The update function for AI logic.
	public void think() {
		if (!Player.humanHasMoved) {
			return;
		}
		updateHeatmap();

		DefaultDict<UnitType, int> currentUnitCounts = new DefaultDict<UnitType, int>(0);
		foreach (Unit unit in aiPlayer.units) {
			currentUnitCounts[unit.type] += 1;
		}
		aiPlayer.tradeWithNClosest(Mathf.Min(8, 1 + currentUnitCounts[UnitType.MERCHANT]));
		bool isMaxBuild = true;
		foreach (UnitType desiredUnitType in AI_BUILD_ORDER) {
			currentUnitCounts[desiredUnitType] -= 1;
			if (currentUnitCounts[desiredUnitType] < 0) {
				isMaxBuild = false;
				if (aiPlayer.has(UnitData.getCost(desiredUnitType))) {
					aiPlayer.getBase().trainUnit((int) desiredUnitType);
				} else {
					break;
				}
			}
		}
		// Spend excess money on galleys
		if (isMaxBuild && aiPlayer.has(UnitData.getCost(UnitType.GALLEY))) {
			aiPlayer.getBase().trainUnit((int) UnitType.GALLEY);
		}

		/* Warship AI:
		 * Compute influence heatmap. Move towards threatening enemy warships or weak enemies.	
		 */
		List<Unit> weakEnemies = findWeakEnemies();
		// TODO: score weak enemies and other objectives based on distance, influence, etc.
		if (weakEnemies.Count > 0) {
			foreach(Unit u in aiPlayer.units) {
				if (u.capableOfAttack()) {
					u.moveTo(weakEnemies[0].getPosition(), u.attackRange);
				}
			}
		}
	}

	private void initializeHeatmap() {
		heatmap = new List<List<int>>();
		walkableTiles = new RandomSet<Vector2>();
		playerInfluencedTiles = new RandomSet<Vector2>();
		enemyInfluencedTiles = new RandomSet<Vector2>();
		for (int x = 0; x < map.mapWidth; ++x) {
			heatmap.Add(new List<int>());
			for (int y = 0; y < map.mapHeight; ++y) {
				heatmap[x].Add(0);
				if (map.isWalkable(map.getTile(x,y))) {
					walkableTiles.Add(new Vector2(x,y));
				}
			}
		}
	}

	private void updateHeatmap() {
		if (heatmap == null && map.hasBeenGenerated) {
			initializeHeatmap();
		}
		// Construct a heatmap. Each tile is hot if near a friendly warship and cold if near an enemy warship.
		timeSinceHeatmapUpdate += Time.deltaTime;
		if (timeSinceHeatmapUpdate < 0.4f) {
			return;
		}
		for (int x = 0; x < map.mapWidth; ++x) {
			for (int y = 0; y < map.mapHeight; ++y) {
				heatmap[x][y] = 0;
			}
		}
		playerInfluencedTiles.Clear();
		enemyInfluencedTiles.Clear();
		RandomSet<Vector2> allInfluencedTiles = new RandomSet<Vector2>();
		int influenceDepth = 4;
		foreach (Unit u in aiPlayer.units) {
			if (!u.capableOfAttack()) {
				continue;
			}
			foreach (TileAndDepth tileAndDepth in iterateTilesNearUnit(u, influenceDepth)) {
				updateHeatmapTile(tileAndDepth.tile, influenceDepth - tileAndDepth.depth);
			}
		}
		foreach (Unit u in iterateEnemyUnits(false, true)) {
			foreach (TileAndDepth tileAndDepth in iterateTilesNearUnit(u, influenceDepth)) {
				updateHeatmapTile(tileAndDepth.tile, - (influenceDepth - tileAndDepth.depth));
			}
		}
	}

	private void updateHeatmapTile(Vector2 tile, int incrementAmount) {
		// TODO: improve out of bounds handling)
		int x = (int)tile.x;
		int y = (int)tile.y;
		if (heatmap.Count > x && x >= 0 && heatmap[x].Count > y && y >= 0) {
			heatmap[x][y] += incrementAmount;
		}
	}

	private IEnumerable<Unit> iterateEnemyUnits(bool includeMerchants, bool includeCombatUnits) {
		foreach (Player otherPlayer in Scene.getPlayers()) {
			if (otherPlayer == aiPlayer) {
				continue;
			}
			foreach (Unit u in otherPlayer.units) {
				if ((u.canTrade() && includeMerchants) || (u.capableOfAttack() && includeCombatUnits)) {
					yield return u;
				}
			}
		}
	}

	private class TileAndDepth
	{
		public Vector2 tile { get; set; }
		public int depth { get; set; }
		internal TileAndDepth(Vector2 tile, int depth)
		{
			this.tile = tile;
			this.depth = depth;
		}
	}
		
	private IEnumerable<TileAndDepth> iterateTilesNearUnit(Unit unit, int maxDepth) {
		Vector2 mapPos = map.gameToMap(unit.getPosition());
		// TODO: handle out-of-bounds units
		yield return new TileAndDepth(mapPos, 0);
		RandomSet<Vector2> tilesInfluencedByThisUnit = new RandomSet<Vector2>();
		RandomSet<Vector2> previousIterationTiles = new RandomSet<Vector2>();
		tilesInfluencedByThisUnit.Add(mapPos);
		previousIterationTiles.Add(mapPos);
		for (int iteration=1; iteration <= maxDepth; iteration++) {
			RandomSet<Vector2> currentIterationTiles = new RandomSet<Vector2>();
			foreach (Vector2 tile in previousIterationTiles) {
				currentIterationTiles.UnionWith(map.getValidNeighbour4(tile, walkableTiles));
			}
			currentIterationTiles.DifferenceWith(tilesInfluencedByThisUnit);
			tilesInfluencedByThisUnit.UnionWith(currentIterationTiles);
			playerInfluencedTiles.UnionWith(currentIterationTiles);
			foreach (Vector2 tile in currentIterationTiles) {
				yield return new TileAndDepth(tile, iteration);
			}
			previousIterationTiles = currentIterationTiles;
		}
	}

	private List<Building> findUsefulBuildings() {
		// TODO: return Colonies that are between allies and enemies.
		return new List<Building>();
	}

	private List<Unit> findEnemyThreats() {
		// TODO: return Enemy warships near friendly units.
		return new List<Unit>();
	}

	private List<Unit> findWeakEnemies() {
		List<Unit> weakUnits = new List<Unit>();
		// Enemy units that are not guarded by warships (or are outnumbered)
		foreach (Unit u in iterateEnemyUnits(true, true)) {
			Vector2 mapPos = map.gameToMap(u.getPosition());
			int influence = heatmap[(int)mapPos.x][(int)mapPos.y];
			if (influence <= 0) {
				weakUnits.Add(u);
			}
		}
		return weakUnits;
	}
}
