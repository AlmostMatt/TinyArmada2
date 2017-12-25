using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Unit : MonoBehaviour, Attackable, Actor, ObjectWithPosition {
	//TODO: call getComponent less often
	private int ATTACK = 0;
	private float range = 1f;

	public Sprite[] sprites;
	public float radius {get; set;}
	public GameObject bulletObj; // The prefab to spawn

	private bool selected = false;

	// Movement related
    private bool hasDest;
    private Path path;
	private float destRadius;
	private Seek seekBehaviour = new Seek(Vector2.zero);
	private Arrival arrivalBehaviour = new Arrival(Vector2.zero);
	private Brake brakeBehaviour = new Brake();

	private float targetDir; // if this object is casting an ability in a direction, turn toward this)
	
	[HideInInspector]
	public UnitGroup group = null;

	private int maxHealth = 10;
	private int health;
	private float hpPercent = 1f;

	// Actor properties
	public StatusMap statusMap { get; set; }
	private ActionMap actionMap;

	[HideInInspector]
	// TODO: make this <Unit, Unit> and figure out a different way to share it with SteeringBehaviours.
	public Neighbours<Steering, Steering> nearbyUnits;
	[HideInInspector]
	public Neighbours<Unit, Building> nearbyBuildings;
	
	[HideInInspector]
	public bool dead {get; set;}
	[HideInInspector]
	public Player owner;
	public int playerNumber {get {return owner.number;} }
	[HideInInspector]
	public UnitType type;

	// trading
	[HideInInspector]
	public float carrying;
	[HideInInspector]
	public Resource resource;
	[HideInInspector]
	public float capacity = 1f;
	[HideInInspector]
	public Building tradeDest;

	private GameObject unitModel;
	private GameObject resourceObject; // the child object of type resource
	private ParticleSystem fireEmitter;
	
	public void init(Player p, UnitType unitType) {
		type = unitType;
		resourceObject = transform.FindChild("resource").gameObject;
		resourceObject.SetActive(false);
		switch (type) {
		case UnitType.MERCHANT:
		case UnitType.TRADER:
			capacity = 25f;
			GetComponent<Steering>().setSpeed(2f, 5f);
			break;
		default:
			GetComponent<Steering>().setSpeed(1.5f, 5f);
			range = 3f;
			break;
		}
		unitModel = Instantiate(UnitData.getModel(type));
		unitModel.transform.parent = transform;
		//float modelScale = 0.01f;
		//unitModel.transform.localScale = new Vector3(modelScale,modelScale,modelScale);
		//unitModel.transform.localEulerAngles = new Vector3(0f, 180f, 0f);
		unitModel.transform.localPosition = new Vector3();
		GetComponent<LineRenderer>().enabled = false;
		fireEmitter = transform.FindChild("Fire").GetComponent<ParticleSystem>();
		fireEmitter.enableEmission = false;

		radius = 0.25f;
		GetComponent<Steering>().setSize(radius);
		dead = false;
		setOwner(p);
	}

	/*
	 * Unity Events
	 */

	void Awake() {
		nearbyUnits = new Neighbours<Steering, Steering>(GetComponent<Steering>());
		nearbyBuildings = new Neighbours<Unit, Building>(this);
		statusMap = new StatusMap(this);
		actionMap = new ActionMap(this);
		actionMap.add(0, new Ability(fireAbility, 0.1f));
	}

	// Use this for initialization
	public void Start () {
		if (sprites != null && sprites.Length > 0) {
			Sprite spr = sprites[Random.Range(0, sprites.Length)];
			SpriteRenderer renderer = GetComponent<SpriteRenderer>();
			renderer.sprite = spr;
		}

		health = maxHealth;
		Steering steering = GetComponent<Steering>();
		steering.addBehaviour(0f, seekBehaviour);
		steering.addBehaviour(0f, arrivalBehaviour);
		steering.addBehaviour(0f, brakeBehaviour);
		//steering.addBehaviour(0.5f, new Separate(nearbyUnits, 0.7f));
		steering.addBehaviour(1.2f, new UnalignedCollisionAvoidance(nearbyUnits));
		steering.addBehaviour(0.3f, new WallAvoidance(LayerMask.GetMask("Walls")));
		// TODO: queueing (for other boats visiting the same dock)
		// TODO: align with others in the same group
	}
	
	// Update is called once per frame
	void Update () {
		// Updates HP indicators over time
		if (hpPercent * maxHealth > health) {
			hpPercent -= 0.4f * Time.deltaTime;
		}
	}

	public void FixedUpdate () {
		nearbyUnits.Update();
		nearbyBuildings.Update();

		Steering steering = GetComponent<Steering>();
		float maxSpeed = steering.getMaxSpeed();
		// move to destination (queue if necessary)
		if (group != null) {
			if (!hasDest || (path.goal - group.getDest()).sqrMagnitude > group.radius * group.radius) {
				moveTo(group.getDest(), group.radius);
			}
		}
		if (hasDest && canMove ()) {
			path.followPath(steering, seekBehaviour, arrivalBehaviour, brakeBehaviour);
		} else {
			steering.updateWeight(seekBehaviour, 0f);
			steering.updateWeight(arrivalBehaviour, 0f);
			steering.updateWeight(brakeBehaviour, 2f);
		}

		statusMap.update(Time.fixedDeltaTime);
		actionMap.update(Time.fixedDeltaTime);
		float maxdd = range * range;
		if (canAttack()) {
			foreach (Neighbour<Steering> otherUnitNeighbour in nearbyUnits) {
				Unit otherUnit = otherUnitNeighbour.obj.GetComponent<Unit>();
				if (otherUnitNeighbour.dd > maxdd ) {
					break;
				}
				if (otherUnit.owner != owner) {
					actionMap.use(ATTACK, otherUnit);
					// look at
					Vector2 offset = otherUnit.transform.position - transform.position;
					targetDir = Mathf.Rad2Deg * Mathf.Atan2(offset.y, offset.x);
					break;
				}
			}
		}
		if (canAttack()) {
			foreach (Neighbour<Building> building in nearbyBuildings) {
				if (building.dd > maxdd ) {
					break;
				}
				Building other = building.obj;
				if (other.owner != owner) {
					actionMap.use(ATTACK, other);
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
					resourceObject.SetActive(true);
					float sz = 0.25f + 0.75f * carrying / capacity;
					resourceObject.transform.localScale = new Vector3(sz,sz,sz);
				} else if (path.goal == dropoff) {
					// drop off
					owner.collect(resource, carrying);
					carrying = 0f;
					resourceObject.SetActive(false);
					float maxProfit = -1f;
					foreach (Building building in owner.tradeWith) {
						Path tradeRoute = owner.getTradeRoute(building);
						float expectedProfit = Mathf.Max(building.expectedProfit(tradeRoute.length / maxSpeed)
														 - capacity * owner.getTraders(building).Count, 0f);
						expectedProfit = maxSpeed * expectedProfit / (2 * tradeRoute.length);
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
					float expectedProfit = Mathf.Max(building.expectedProfit(tradeRoute.length / maxSpeed)
					                                 - capacity * owner.getTraders(building).Count, 0f);	
					float tripDuration = tradeRoute.length / maxSpeed + owner.getReturnRoute(building).length / maxSpeed;
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

	public void select() {
		GetComponent<Renderer>().material.color = new Color(0.7f, 1f, 0.7f);
		selected = true;
	}

	public void deselect() {
		GetComponent<Renderer>().material.color = Color.white;
		selected = false;
	}

	public void damage(Actor attacker, int amount) {
		health = Mathf.Max (0, health - amount);
		if (health == 0) {
			setTradeDest(null);
			dead = true;
		}
		fireEmitter.enableEmission = true;
		fireEmitter.startSize = 1.5f * (1f - health/maxHealth);
		fireEmitter.emissionRate = 50f * (1f - health/maxHealth);
	}

	public void fireAbility(AbilityTarget abilityTarget) {
		Attackable target = abilityTarget.getAttackableTarget();
		if (target.dead) return;
		GameObject gun = transform.FindChild("Gun").gameObject;
		Shot shot = Instantiate(bulletObj).GetComponent<Shot>();
		shot.attacker = this;
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
	
	public bool canAttack() {
		return !canTrade() && actionMap.ready(ATTACK) && !statusMap.has(State.ANIMATION);
	}
	
	public bool canTrade() {
		return type == UnitType.MERCHANT || type == UnitType.TRADER;
	}

	public void setOwner(Player p) {
		owner = p;
		p.units.Add(this);
		// Sets the team color to be used by the TeamColorAlphaMask shader
		unitModel.GetComponent<Renderer>().material.SetColor("_TeamColor", p.color);
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

	/* 
	 * ObjectWithPosition
	 */

	public Vector2 getPosition() {
		return transform.position;
	}
}
