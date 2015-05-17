using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Unit : Steering, Attackable {
	//TODO: call getComponent less often
	private int ATTACK = 0;
	private float range = 1f;

	public Sprite[] sprites;
	public float radius {get; set;}
	public GameObject bulletObj;

	private bool selected = false;

	// movement
    private bool hasDest;
    private Path path;
	private float destRadius;

	private float targetDir; // if this object is casting an ability in a direction, turn toward this)

	private UnitGroup group = null;

	private int maxHealth = 10;
	private int health;
	private float hpPercent = 1f;

	private StatusMap statusMap;
	private ActionMap actionMap;
	public Neighbours<Unit> neighbours;
	public Neighbours<Building> nearbyBuildings;

	public bool dead {get; set;}
	public Player owner;
	public UnitType type;

	// trading
	public float carrying;
	public Resource resource;
	public float capacity = 1f;
	public Building tradeDest;

	private SpriteRenderer teamColor;
	private ParticleSystem fireEmitter;
	
	public void init(Player p, UnitType unitType) {
		setOwner(p);
		type = unitType;
		transform.FindChild("resource").GetComponent<Renderer>().enabled = false;
		switch (type) {
		case UnitType.MERCHANT:
			capacity = 25f;
			ACCEL = 40f;
			MAX_V = 2f;
			ACCEL = 5;
			break;
		default:
			MAX_V = 1.5f;
			ACCEL = 5;
			range = 3f;
			break;
		}
		GetComponent<SpriteRenderer>().sprite = UnitData.getImage(type);
		teamColor = transform.FindChild("team-color").GetComponent<SpriteRenderer>();
		teamColor.sprite = UnitData.getTeamImage(type);
		if (owner != null) {
			teamColor.color = owner.color;
		}
		GetComponent<LineRenderer>().enabled = false;
		fireEmitter = transform.FindChild("Fire").GetComponent<ParticleSystem>();
		fireEmitter.enableEmission = false;

		radius = 0.25f;
		dead = false;
	}

	/*
	 * Unity Events
	 */

	void Awake() {
		neighbours = new Neighbours<Unit>();
		nearbyBuildings = new Neighbours<Building>();
		statusMap = new StatusMap(this);
		actionMap = new ActionMap(this);
		actionMap.add(0, new Ability(0.1f));
	}

	// Use this for initialization
	override public void Start () {
		base.Start();
		if (sprites != null && sprites.Length > 0) {
			Sprite spr = sprites[Random.Range(0, sprites.Length)];
			SpriteRenderer renderer = GetComponent<SpriteRenderer>();
			renderer.sprite = spr;
		}

		health = maxHealth;
	}
	
	// Update is called once per frame
	void Update () {
		//Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
		//Vector2 offset = ray.origin - transform.position;
		//float angle = Mathf.Rad2Deg * Mathf.Atan2(offset.y, offset.x);
		//float angle = Vector2.Angle(Vector2.right, );
		//transform.localEulerAngles = new Vector3(0, 0, angle);
		if (hpPercent * maxHealth > health) {
			hpPercent -= 0.4f * Time.deltaTime;
		}
	}

	override public void FixedUpdate () {
		if (!canMove()) {
			brake ();
		}
		// containment (walls)
		// avoid (obstacles)
		// separate from 'too close' others
		separate(neighbours);
		// align with others in the same group
		// move to destination (queue if necessary)
		if (hasDest && canMove ()) {
            path.followPath(this);
		}
		statusMap.update(Time.fixedDeltaTime);
		actionMap.update(Time.fixedDeltaTime);
		float maxdd = range * range;
		if (canAttack()) {
			foreach (Tuple<float, Unit> tuple in neighbours) {
				if (tuple.First > maxdd ) {
					break;
				}
				Unit other = tuple.Second;
				if (other.owner != owner) {
					cast(ATTACK, other);
					// look at
					Vector2 offset = other.transform.position - transform.position;
					targetDir = Mathf.Rad2Deg * Mathf.Atan2(offset.y, offset.x);
					break;
				}
			}
		}
		if (canAttack()) {
			foreach (Tuple<float, Building> tuple in nearbyBuildings) {
				if (tuple.First > maxdd ) {
					break;
				}
				Building other = tuple.Second;
				if (other.owner != owner) {
					cast(ATTACK, other);
					// look at
					Vector2 offset = other.transform.position - transform.position;
					targetDir = Mathf.Rad2Deg * Mathf.Atan2(offset.y, offset.x);
					break;
				}
			}
		}
		if (canTrade()) {
			bool isIdle = false;

			if (hasDest && path.arrived) {
				//decide what to do next
				Vector2 dropoff = owner.getBase().getDock();
				if (tradeDest != null && path.goal == tradeDest.getDock()) {
					// pick up
					resource = tradeDest.resource;
					carrying = tradeDest.collect(capacity);
					followPath(owner.getReturnRoute(tradeDest));
					transform.FindChild("resource").GetComponent<Renderer>().enabled = true;
					float sz = 0.25f + 0.75f * carrying / capacity;
					transform.FindChild("resource").localScale = new Vector3(sz,sz,1f);
				} else if (path.goal == dropoff) {
					// drop off
					owner.collect(resource, carrying);
					carrying = 0f;
					transform.FindChild("resource").GetComponent<Renderer>().enabled = false;
					float maxProfit = -1f;
					foreach (Building building in owner.tradeWith) {
						Path tradeRoute = owner.getTradeRoute(building);
						float expectedProfit = Mathf.Max(building.expectedProfit(tradeRoute.length / MAX_V)
														 - capacity * owner.getTraders(building).Count, 0f);
						expectedProfit = MAX_V * expectedProfit / (2 * tradeRoute.length);
						if (expectedProfit > maxProfit) {
							maxProfit = expectedProfit;
							setTradeDest(building);
							followPath(tradeRoute);
						}
					}
				} else if (carrying > 0f) {
					moveTo(dropoff, radius);
				} else {
					isIdle = true;
				}
			}
			if (isIdle || !hasDest) {
				// stopped in middle of nowhere
				float maxProfit = -1f;
				foreach (Building building in owner.tradeWith) {
					Path tradeRoute = Pathing.findPath(transform.position, building.getDock(), radius);
					float expectedProfit = Mathf.Max(building.expectedProfit(tradeRoute.length / MAX_V)
					                                 - capacity * owner.getTraders(building).Count, 0f);	
					float tripDuration = tradeRoute.length / MAX_V + owner.getReturnRoute(building).length / MAX_V;
					expectedProfit = expectedProfit / tripDuration;
					if (expectedProfit > maxProfit) {
						maxProfit = expectedProfit;
						setTradeDest(building);
						followPath(tradeRoute);
					}
				}
			}
		}
		// repairs

		base.FixedUpdate();
	}

	// called after (fixed)update => after objects move
	void LateUpdate() {
		GameObject HPbar = transform.FindChild("HP").gameObject;
		HPbar.transform.rotation = Quaternion.identity;
		Renderer r = HPbar.GetComponent<Renderer>();
		r.material.color = Color.green;
		r.material.SetFloat("_Cutoff", 1f - health / (2f * maxHealth));
		GameObject HPbar2 = transform.FindChild("HPred").gameObject;
		HPbar2.transform.rotation = Quaternion.identity;
		Renderer r2 = HPbar2.GetComponent<Renderer>();
		r2.material.color = Color.red;
		r2.material.SetFloat("_Cutoff", 1f - hpPercent/2f);
		
		LineRenderer linerender = GetComponent<LineRenderer>();
		if (hasDest) {
			linerender.enabled = true;
			linerender.SetVertexCount(path.points.Count + 1);
			linerender.SetPosition(0, transform.position);
			for (int i=0; i<path.points.Count; ++i) {
				linerender.SetPosition(i + 1, path.points[i]);
			}
		} else {
			linerender.enabled = false;
		}
	}

	/*
	 * Unit actions
	 */
	
	public void followPath(Path newPath) {
		path = newPath;
		hasDest = true;
		if (tradeDest != null && path.goal != tradeDest.getDock()) {
			setTradeDest(null);
		}
	}

	public void moveTo(Vector2 point, float radius) {
		Path newPath = Pathing.findPath(transform.position, point, radius);
		followPath(newPath);
	}

	public void stop() {
		hasDest = false;
		setTradeDest(null);
		path = null;
	}

	public void setGroup(UnitGroup newGroup) {
		if (group != null) {
			group.Remove(this);
			group = null;
		}
		if (newGroup != null) {
			newGroup.Add(this);
			group = newGroup;
		}
	}

	public void cast(int action, object target) {
		actionMap.use(action, target);
		statusMap.add(new Status(State.ANIMATION), actionMap.getCastTime(action));
	}

	public void select() {
		GetComponent<Renderer>().material.color = new Color(0.7f, 1f, 0.7f);
		selected = true;
	}

	public void deselect() {
		GetComponent<Renderer>().material.color = Color.white;
		selected = false;
	}

	public void damage(Player attacker, int amount) {
		health = Mathf.Max (0, health - amount);
		if (health == 0) {
			dead = true;setTradeDest(null);
		}
	}

	public void fire(Attackable target) {
		if (target.dead) return;
		GameObject gun = transform.FindChild("Gun").gameObject;
		Shot shot = Instantiate(bulletObj).GetComponent<Shot>();
		shot.owner = owner;
		float r = 0.25f;
		shot.transform.position = gun.transform.position + new Vector3(Random.Range(-r, r), Random.Range(-r,r),0);
		Vector2 offset = target.transform.position - gun.transform.position;
		float angle = Mathf.Rad2Deg * Mathf.Atan2(offset.y, offset.x);
		shot.transform.localEulerAngles = new Vector3(0,0,angle);
		//shot.transform.rotation = gun.transform.rotation;
		shot.GetComponent<Rigidbody2D>().velocity = 20 * shot.transform.right;
		shot.setTarget(target);
	}
	
	/*
	 * State logic
	 */
	
	private bool canMove() {
		return true;// !statusMap.has(State.ANIMATION);
	}
	
	private bool canCast() {
		return !statusMap.has(State.ANIMATION);
	}
	
	private bool canAttack() {
		return type != UnitType.MERCHANT && actionMap.ready(ATTACK) && !statusMap.has(State.ANIMATION);
	}
	
	private bool canTrade() {
		return type == UnitType.MERCHANT;
	}

	public void setOwner(Player p) {
		owner = p;
		p.units.Add(this);
		if (teamColor != null) {
			teamColor.color = owner.color;
		}
	}

	public void setTradeDest(Building building) {
		if (tradeDest != null) {
			owner.noLongerHeadingTo(this, tradeDest);
		}
		if (building != null) {
			owner.headingTo(this, building);
		}
		tradeDest = building;
	}
}
