using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Steering : MonoBehaviour
{
	public bool canTurn = true;

	protected float MAX_V = 5f;
	protected float ACCEL = 20f;
	protected float TURN_RATE = 900f;
	protected float forceRemaining;

	private static float dt;

	private static float MAXPREDICTIONTIME = 2.5f; 

	private Rigidbody2D rb;

	/*
	 * Utility functions
	 */

	private Vector2 scaled(float n, Vector2 v) {
		return n * v.normalized;
	}
	
	private float angleDiff(float a1, float a2) {
		float diff = a2 - a1;
		// (diff + 180 mod 360) - 180
		if (diff > 180) {
			return diff - 360;
		} else if (diff < -180) {
			return diff + 360;
		} else {
			return diff;
		}
	}
	
	// spaceship operator to an interval
	private int intervalComp(float f, float intervalMin, float intervalMax) {
		if (f < intervalMin) {
			return -1;
		} else if (f <= intervalMax) {
			return 0;
		} else {
			return 1;
		}
	}
	
	public void turnToward(float angle2) {
		float angle1 = transform.localEulerAngles.z;
		float diff = angleDiff(angle1, angle2);
		float rot = TURN_RATE * Time.fixedDeltaTime;
		if (diff > rot) {
			transform.localEulerAngles = new Vector3(0, 0, angle1 + rot);
		} else if (diff < -rot) {
			transform.localEulerAngles = new Vector3(0, 0, angle1 - rot);
		} else {
			transform.localEulerAngles = new Vector3(0, 0, angle2);
		}
	}

	private bool sameDir(Vector2 v1, Vector2 v2) {
		return Vector2.Dot(v1, v2) >= 0f;
	}

	/*
	 * Unity Events
	 */

	public virtual void Start() {
		rb = GetComponent<Rigidbody2D>();
		dt = Time.fixedDeltaTime;
	}

	public virtual void FixedUpdate () {
		// this should happen after actions are performed
		float vv = rb.velocity.sqrMagnitude;
		if (vv > MAX_V * MAX_V) {
			rb.velocity = MAX_V * rb.velocity.normalized;
		}
		if (canTurn && vv > 0.5) {
			float angle = Mathf.Rad2Deg * Mathf.Atan2(rb.velocity.y, rb.velocity.x);
			turnToward(angle);
		}
		forceRemaining = ACCEL;
	}
	
	/*
	 * Steering forces
	 * I assume that each of these will be applied immediately and use all remainingForce available
	 */
	
	// force to accel toward a point used by seek/arrival/flee
	private Vector2 seekForce(Vector2 dest, float maxforce) {
		Vector2 desiredV = scaled(MAX_V, dest - (Vector2) transform.position);
		Vector2 deltaV = desiredV - rb.velocity;
		float dvmagn = deltaV.magnitude;
		if (dvmagn > maxforce * maxforce * dt * dt) {
			forceRemaining -= maxforce;
			return scaled(maxforce, deltaV);
		} else {
			forceRemaining -= dvmagn;
			return deltaV;
		}
	}
	
	// force to accel toward a unit used by pursue/evade
	private Vector2 pursueForce(Steering other, float maxforce) {
		Vector2 offset = other.transform.position - transform.position;
		float dist = offset.magnitude;
		float directtime = dist/MAX_V;

		Vector2 unitV = rb.velocity.normalized;
		Vector2 otherV = other.rb.velocity;

		float parallelness = Vector2.Dot(unitV, otherV.normalized);
		float forwardness = Vector2.Dot (unitV, offset/dist);

		float halfsqrt2 = 0.707f;
		int f = intervalComp(forwardness, -halfsqrt2, halfsqrt2);
		int p = intervalComp(parallelness, -halfsqrt2, halfsqrt2);

		// approximate how far to lead the target
		float timeFactor = 1f;
		// case logic based on (ahead, aside, behind) X (parallel, perp, anti-parallel)
		switch (f) {
		case 1: //target is ahead
			switch (p) {
			case 1:
				timeFactor = 4f;
				break;
			case 0:
				timeFactor = 1.8f;
				break;
			case -1:
				timeFactor = 0.85f;
				break;
			}
			break;
		case 0: //target is aside
			switch (p) {
			case 1:
				timeFactor = 1f;
				break;
			case 0:
				timeFactor = 0.8f;
				break;
			case -1:
				timeFactor = 4f;
				break;
			}
			break;
		case -1: //target is behind
			switch (p) {
			case 1:
				timeFactor = 0.5f;
				break;
			case 0:
				timeFactor = 2f;
				break;
			case -1:
				timeFactor = 2f;
				break;
			}
			break;
		}

		float estTime = Mathf.Min (MAXPREDICTIONTIME, directtime * timeFactor);
		Vector2 estPos = (Vector2) other.transform.position + estTime * otherV;

		return seekForce(estPos, maxforce);
	}
	
	/*
	 * Steering behaviours
	 */
	
    public void brake() {
		Vector2 deltaV = - rb.velocity;
		float dvmagn = deltaV.magnitude;
		if (dvmagn > forceRemaining * forceRemaining * dt * dt) {
			rb.AddForce(scaled(forceRemaining, deltaV));
			forceRemaining = 0f;
		} else {
			rb.AddForce(deltaV);
			forceRemaining -= dvmagn;
		}
	}
	
    public void pursue(Steering other) {
		Vector2 force = pursueForce(other, forceRemaining);
		rb.AddForce(force);
	}
	
    public void seek(Vector2 dest) {
		Vector2 force = seekForce(dest, forceRemaining);
		rb.AddForce(force);
	}
	
    public void arrival(Vector2 dest) {
		// can approximate with (if too close : break)
		// alternatively stopdist from current pos and current speed
		// or expected stopdist after the current frame assuming accel wont change the current frame much

		// for the duration it takes to go from maxV to 0
		//stoptime = v/ACCEL;
		//stopdist = stoptime * v/2;
		//dist = desiredv * desiredV/(2 * accel) 
		//desiredv = sqrt(dist * 2 * accel) 

		Vector2 offset = dest - ((Vector2) transform.position + (dt * rb.velocity));

		float dist = offset.magnitude;
		float stopdist = MAX_V * MAX_V / (2 * ACCEL);
		float desiredV = dist < stopdist ? Mathf.Sqrt(dist * 2 * ACCEL) : MAX_V;
		Vector2 deltaV = scaled (desiredV, offset) - rb.velocity;
		float dvmagn = deltaV.magnitude;
		if (dvmagn > forceRemaining * forceRemaining * dt * dt) {
			rb.AddForce(scaled(forceRemaining, deltaV));
			forceRemaining = 0f;
		} else {
			rb.AddForce(deltaV);
			forceRemaining -= dvmagn;
		}
	}

    public void separate(Neighbours<Unit> neighbours) {
		float TOO_CLOSE = 0.8f; // radius is 0.
		Vector2 steer = new Vector2(0f, 0f);
		float totalforce = 0f;
		// steer away from each object that is too close with a weight of up to 0.5 for each
		foreach (Tuple<float, Unit> tuple in neighbours) {
			if (tuple.First > TOO_CLOSE * TOO_CLOSE) {
				break;
			}
			Unit other = tuple.Second;
			Vector2 offset = other.transform.position - transform.position;
			float d = Mathf.Sqrt(tuple.First);
			if (d == 0f) {
				// Units spawn with identical position.
				offset = new Vector2(0.01f, 0f);
				d = 0.01f;
			}
			// only prioritize separation if the objects are moving toward each other
			float importance = 0.5f;//(sameDir(v1, v2) || !sameDir(v1, offset)) ? 0.3f : 0.6f;
			// force of 0.5 per other
			float force = importance * (TOO_CLOSE - d)/TOO_CLOSE;
			totalforce += force;
			steer += (- force / d) * offset;
			break;
		}
		float m = steer.magnitude * ACCEL;
		if (m >= forceRemaining) {
			steer = scaled(forceRemaining, steer);
			forceRemaining = 0f;
		} else {
			steer = ACCEL * steer;
			forceRemaining -= m;
		}
		rb.AddForce(steer);
	}
	
	// avoid individual static objects
    public void avoid(List<Vector2> obstacles) {
		
	}

	// avoid edges of the pathable areas (large walls)
    public void containment(List<Rect> walls) {
		
	}
}

/*
 * steering.lua
 * 
 * 
 * --acceleration for objects with a velocity property v and position p
--both objects and destinations/targets also have p and may have v or may simply be a point and have no properties

--seek and flee a point
--pursue and evade another object
--arrive at a point

--objects should have a direction vector to use even when stopped and can only accelerate within 90 degrees of direction

--these all return unit force vectors
MAXPREDICTIONTIME = 2.5 --for pursue, predict time of impact no larger than 5 because he might have changed direction after a few seconds

NEIGHBORRANGE = 50
--cohesion min range
TOOFAR = 50
--seperation max range
--TOOCLOSE = 40
--TOOCLOSE = 24
--object radius is 8, so 18 is 2pixels larger than the radii
TOOCLOSE1 = 17
TOOCLOSE1 = 15
--tooclose2 is for unaligned avoidance, tooclose1 is for separation
TOOCLOSE2 = 20 --this doesn't count unit radii
--sort of like the spaceship operator

--    ACCEL = 1100
--    MAXSPEED = 180 --speed after which unable to accelerate by running. could be ignored and use the breakeven point of friction instead
--    ACCEL = 1100
--    MAXSPEED = 330 --speed after which unable to accelerate by running. could be ignored and use the breakeven point of friction instead
--    MAXSPEED = 160
--    MAXSPEED = 80


Object = {t=OBJECT,p=P(0,0),v=P(0,0),f=P(0,0),r=8,maxf = 1100,remainingf = 1100,n={},maxv = 130}
--if object has the path property it will follow it
--if object has the "area" property it will try to stay in the area
--area is given by {p=centre,r=radius}

-- n is list of neighbors
function Object:new(o)
    o = o or {}
    o.p = o.p or P(0,0)
    o.v = o.v or P(0,0)
    o.f = o.f or P(0,0) --force (acceleration)
    setmetatable(o,self)
    self.__index = self
    return o
end



--any force function will return a force with magnitude between 0 and remaining force
--maybe have an additional braking force though, since it is not really steering
function Object:seek(p,dt)
    if self.remainingf < 1 then return end
    local f,m = seekforce(self,p,dt)
    self.remainingf = self.remainingf - m
    self.f = Vadd(self.f,f)
end

function Object:stopat(p,dt)
    if self.remainingf < 1 then return end
    --vi = self.v vf = 0, a = ACCEL or remainingf, want distance or time to stop
    --vi/2 * t = d, t = vi/accel
    local v = Vmagn(self.v)
    local stopdistance = math.max(v*v / (2*self.remainingf),1) --1 is an approximation of "close enough"
    if Vinrange(self.p,p,stopdistance) then
        self:brake(dt)
    else
        local f,m = seekforce(self,p,dt)
        self.remainingf = self.remainingf - m
        self.f = Vadd(self.f,f)
    end
end

function Object:flee(p,dt)
    if self.remainingf < 1 then return end
    local f,m = seekforce(self,p,dt)
    self.remainingf = self.remainingf - m
    self.f = Vsub(self.f,f) --flee is opposite of seek
end

function Object:pursue(t,dt,dbg)
    if self.remainingf < 1 then return end
    local f,m = pursueforce(self,t,dt,dbg)
    self.remainingf = self.remainingf - m
    self.f = Vadd(self.f,f)
end

function Object:evade(t,dt)
    if self.remainingf < 1 then return end
    local f,m = pursueforce(self,t,dt)
    self.remainingf = self.remainingf - m
    self.f = Vsub(self.f,f) --evade is opposite of pursue
end

function Object:separate(dt)
    if self.remainingf < 1 then return end
    local f,m = separationforce(self,dt)
    self.remainingf = self.remainingf - m
    self.f = Vadd(self.f,f)
end

function Object:avoidneighbors(dt)
    if self.remainingf < 1 then return end
    local f,m = unalignedavoidforce(self,dt)
    self.remainingf = self.remainingf - m
    self.f = Vadd(self.f,f)
end

function Object:brake(dt)
    if self.remainingf < 1 then return end
    local f,m = brakeforce(self,dt)
    self.remainingf = self.remainingf - m
    self.f = Vadd(self.f,f)
end

function Object:avoid(objects,dt)
    if self.remainingf < 1 then return end
    local f,m = avoidforce(self,objects,dt)
    self.remainingf = self.remainingf - m
    self.f = Vadd(self.f,f)
end

function Object:followpath(dt)
    if self.remainingf < 1 then return end
    local f,m = followforce(self,self.path,dt)
    self.remainingf = self.remainingf - m
    self.f = Vadd(self.f,f)
end

--all of the stuff should (optionally) add to the force vector which is (optionally) scaled to maxforce
function intervalComparison(x,rangemin,rangemax)
    if x < rangemin then return -1
    elseif x <= rangemax then return 0
    else return 1 end
end


function accelerationforce(u,dt)
    --consider max speed
    local m = math.min(u.remainingf,(u.maxv - Vmagn(u.v))/dt)
	return Vscale(m,u.v),m
end

function brakeforce(u,dt)
    --consider min speed (0)
    --do not want to brake more than current speed
    local m = math.min(u.remainingf,Vmagn(u.v)/dt)
	return Vscale(-m,u.v),m
end

function seekforce(u,p,dt)
	--current velocity is u.vx,u.vy
	--desired velocity is difference in desired position and cirrent position scaled to unit max speed
	local diffp = Vscale(u.maxv,Vsub(p,u.p))
	--force is difference in desired v - current v
    local diffv = Vsub(diffp,u.v)
    local m = math.min(u.remainingf,Vmagn(diffv)/dt) --desired speed change / dt will give the desired acceleration to create the exact speed after this frame
	return Vscale(m,diffv),m
end

function pursueforce(u,t,dt,dbg)
    local offset = Vsub(t.p,u.p)
    local dist = Vmagn(offset)
    local directtime = dist/u.maxv --estimate time to reach target, maybe consider the fact that the two units have different maxv
    
    --openSteer prediction estimate
    local uforward, tforward = unitV(u.v),unitV(t.v)
    local uoffset = Vmult(1/dist,offset)
    

    -- how parallel are the paths of "this" and the quarry
    -- (1 means parallel, 0 is pependicular, -1 is anti-parallel)
    local parallelness = Vdot(uforward,tforward)
    
    -- how "forward" is the direction to the quarry
    -- (1 means dead ahead, 0 is directly to the side, -1 is straight back)
    local forwardness = Vdot(uforward, uoffset)
    
    local f = intervalComparison (forwardness,  -0.707, 0.707) --0.707 is sqrt2/2
    local p = intervalComparison (parallelness, -0.707, 0.707)

    
    local timeFactor = 0 -- to be filled in below
    local color
    --
    -- Break the pursuit into nine cases, the cross product of the
    -- quarry being [ahead, aside, or behind] us and heading
    -- [parallel, perpendicular, or anti-parallel] to us.
    if f == 1 then
        if p == 1 then -- ahead, parallel
            timeFactor = 4
--            color = {0,0,0}
        elseif p == 0 then -- ahead, perpendicular
            timeFactor = 1.8
--            color = {128,128,128}
        elseif p == -1 then -- ahead, anti-parallel
            timeFactor = 0.85
--            color = {64,64,64}
        end
    elseif f == 0 then
        if p == 1 then -- aside, parallel
            timeFactor = 1
--            color = {255,0,0}
        elseif p == 0 then -- aside, perpendicular
            timeFactor = 0.8
--            color = {255,255,0}
        elseif p == -1 then -- aside, anti-parallel
            timeFactor = 4
--            color = {0,255,0}
        end
    elseif f == -1 then
        if p == 1 then -- behind, parallel
            timeFactor = 0.5
--            color = {0,255,255}
        elseif p == 0 then -- behind, perpendicular
            timeFactor = 2
--            color = {255,0,0}
        elseif p == -1 then -- behind, anti-parallel
            timeFactor = 2
--            color = {255,0,255}
        end
    end
    
    local et = math.min(MAXPREDICTIONTIME,directtime * timeFactor) --time estimate
    local p = Vadd(t.p,Vmult(et,t.v))
    if dbg then
        debugline(u.p,p,dGray1)
        debugcircle(p,6,dGray2)
    end
    --seek p
    if DEBUG then
--        table.insert(debugshapes,{p=p,r=6,col={32,32,32}}) --circle
----        table.insert(debugshapes,{p1=u.p,p2=p,col=color})
--        table.insert(debugshapes,{p1=u.p,p2=p,col={255,128,0}}) --line
    end
    return seekforce(u,p,dt)
end

function arrive(u,t)
    --like seek but slow down when approaching the destination
end

--steer to average position of neighbors
function cohesion(u,neighbors)

end

--steer to average direction of neighbors
function alignment(u,neighbors)

end

--steer to avoid crowding neighbors
--ideally this has a smaller radius than cohesion so that they can be used together
--prioiritize the combination seperation > cohesion == alignment
function separationforce(u,dt)
    local totalforce = 0
    local steer = P(0,0)
    local m = 0
    for u2,d in pairs(u.n) do
        --local d = Vmagn(diff)
        if d < TOOCLOSE1 then
            --only separate if you are moving toward each other
            --if both moving in same direction or both moving away from each other, don't worry about it
            --this might behave strangely for V(0,0) (changed vsamedir)
            if (not Vsamedir(u.v,u2.v)) and (not Vsamedir(u.v, Vsub(u2.p,u.p))) then
                local diff = Vsub(u.p,u2.p)
                --d < tooclose so tooclose - d > 0 and tooclose > 0 so tooclose-d / tooclose > 0
                local force = 0.5*(TOOCLOSE1-d)/TOOCLOSE1
                totalforce = totalforce + force
                steer = Vadd(steer,Vscale(force,diff)) --0 is max range, 1 is min range, scaling away from neighbour instead of toward            
            end
        end
    end
    m = Vmagn(steer)
    if m > 1 then
    --if totalforce > 0 then
        m = u.remainingf
        steer = Vscale(m, steer) --weighted average of steering forces, each one from 0 to 1
        --vmult 1/totalforce
        --so this is from 0 to 1 
    
     --this is at most magnitude 1, probably lower
        --steer = Vmult(1/totalforce,steer)
        --steer = unitV(steer)
    else
        m = m * u.remainingf
        steer = Vmult(u.remainingf,steer)
    end
    return steer,m
    
    --http://www.altdevblogaday.com/2011/04/14/steering-youre-doing-it-wrong/
end

REACTIONTIME = 1 --0.4
REACTIONTIME = 0.4
function unalignedavoidforce(u,dt)
    local steer = P(0,0)
    for u2,d in pairs(u.n) do
        --local d = Vmagn(diff)
        if d < TOOCLOSE2 then
            local diff = Vsub(u2.p,u.p) --toward u2
            --relative velocity is difference in velocities
            local v = Vproj(Vsub(u.v,u2.v),diff) --how u is moving with respect to u2
            local relspeed = Vmagn(v)
            if Vsamedir(v,diff) and relspeed > 0 then --u is moving toward u2 with respect to u2
                --urgency is 0 not moving toward each other or 1 moving at 2 * maxspeed or 1 TOO CLOSE
                local t = (Vmagn(diff)-u.r-u2.r)/relspeed
                if t < 0 then urgency = 1 end
                urgency = math.max(0,REACTIONTIME-t)/REACTIONTIME --urgency is 0 if unit is 1s or more away
                --urgency = (Vmagn(v)/Vmagn(diff))
                if urgency > 0 then
                    steer = Vadd(steer,Vmult(-urgency,diff))
                end
            end
            --urgency is component of these vectors that is toward each other
            --will be negative if they are moving away from each other, <= 0 is ignore, 1 is USE MAXFORCE
            
            --local force = (TOOCLOSE-d)/TOOCLOSE
            --steer = Vadd(steer,Vscale(diff,force)) --0 is max range, 1 is min range, scaling away from neighbour instead of toward            
        end
    end
    local m = Vmagn(steer)
    if m > 1 then
        steer = Vscale(u.remainingf,steer)
        m = u.remainingf
    else
        steer = Vmult(u.remainingf,steer)
        m = m * u.remainingf
    end
    return steer,m
end

--optimize this to use something like a 2 dimensional pigeonhole sort
--or to memoize previous values
--currently only compares each unit with the units that come after it in the list to half the computation cost
function getneighbors(units)
    local rr = NEIGHBORRANGE * NEIGHBORRANGE
    for i,u in ipairs(units) do
        u.n = {}
    end
    for i,u in ipairs(units) do
        for i2 = i+1,#units do
            local u2 = units[i2]
            local dd = Vdd(Vsub(u2.p,u.p))
            if dd < rr then
                u.n[u2] = math.sqrt(dd)-u.r-u2.r
                u2.n[u] = u.n[u2]
            end
            --[[
            local uforward = unitV(u.v)
            local uoffset = Vmult(1/dist,offset)
            theta = math.acos(Vdot(uforward,uoffset))
            if theta > math.pi then theta = math.abs(theta - 2 * math.pi) end
            if theta < math.pi * 2/3 then
                --120 to -120 degree angle for neighbor selection?
            ]]
            --math.max(0,dd-r)
            --distance and angle
            --can use dot product between u.forward and offset
            --u2.n[u]
            --n[u2] = dd
            --end
            --if Vinrange(u.p,u2.p,u.r+u2.r) then
            --    u.hit = true
            --end
        end
    end
end

--check for collision with neighbor units and all obstacle
--set component of velocity toward object to 0
function hittest(u,obs)
    u.hit = false
    for u2,d in pairs(u.n) do
        if d < 0 then
            u.hit = true
            local diff = Vsub(u.p,u2.p)
            --if units had variable size this would need to be u.r + u2.r / 2
            local r = (u.r + u2.r) / 2
            local c = Vavg(u.p,u2.p)
            u.p = Vadd(c,Vscale(r+1,diff))
            u2.p = Vadd(c,Vscale(-r-1,diff))
        end
    end
    for i,o in ipairs(obs) do
        if Vinrange(u.p,o.p,u.r+o.r) then
            u.hit = true
            local diff = Vsub(u.p,o.p)
            --if units had variable size this would need to be u.r + u2.r / 2
            local r = u.r + o.r
            u.p = Vadd(o.p,Vscale(r+1,diff))
        end
    end
end
--seperation, cohesion, and alignment create flocking
--can be useful for group path following

--each object is a circle
function avoidforce(u,obs,dt)
    local f = P(0,0)
    local m = 0
    if samepoint(u.v,P(0,0)) then return f,m end
    local factor = 0.6

    local ray = Vadd(u.p,Vmult(factor,u.v)) --random arc
--    table.insert(debugshapes,{p1=u.p,p2=ray,col={0,0,0}})
--    local range = math.pi/16
--    local angle = math.random()*2*range - range
--    local ray = Vadd(u.p,Vmult(factor,Vrotate(u.v,math.cos(angle),math.sin(angle)))) --random arc

    for i,o in ipairs(obs) do
        if Vinrange(u.p,o.p,o.r+u.maxv) then --quick estimation of relevance of sphere
            if pointdistance(o.p,S(u.p,ray)) < (o.r+u.r)*(o.r+u.r) then
    --        if Vinrange(ray,o.p,o.r+u.r) then
                --steer laterally away from object's center
                --and brake
                --need an "urgency" estimate
                f = unitV(Vproj(Vsub(ray,o.p),Vnorm(u.v)))
                --mult by urgency
                local m = u.remainingf
                return Vmult(m,f),m
            end
        end
    end
    return f,m
end

--each object is a rectangle, represented as {p1=minp,p2=maxp}
--doesnt work too well for small objects since it might get back of object as a normal
function containment(u,obs,dt)
    local f = P(0,0)
    local m = 0
    if samepoint(u.v,P(0,0)) then return f,0 end
    local range = math.pi/16
    local angle = math.random()*2*range - range
    --time to stop from max speed?
    --maybe it should be distance to stop from max speed / maxspeed
    local factor = 0.6
    local size = 30
    local ray = Vadd(u.p,Vmult(factor,Vrotate(u.v,math.cos(angle),math.sin(angle)))) --random arc
--    local ray = Vadd(u.p,Vscale(Vrotate(u.v,math.cos(angle),math.sin(angle)),size)) --random arc
    for i,o in ipairs(obs) do
        if inrectangle(ray,o.p1,o.p2) then
            --find nearest point
            local closest,normal
            local w,h = o.p2[1]-o.p1[1],o.p2[2]-o.p1[2]
            local dx1,dx2 = ray[1] - o.p1[1],o.p2[1]-ray[1]
            local dy1,dy2 = ray[2] - o.p1[2],o.p2[2]-ray[2]
            local mind = math.min(dx1,dx2,dy1,dy2)
            if mind == dx1 then
                closest = P(o.p1[1],ray[2])
                normal = P(-1,0)
            elseif mind == dx2 then
                closest = P(o.p2[1],ray[2])
                normal = P(1,0)
            elseif mind == dy1 then
                closest = P(ray[1],o.p1[2])
                normal = P(0,-1)
            elseif mind == dy2 then
                closest = P(ray[1],o.p2[2])
                normal = P(0,1)
            end
            --component of normal that is perpindicular to velocity
            --this could be scaled base on "urgency"
            f = Vadd(f,unitV(Vproj(normal,Vnorm(u.v))))
            --ray = closest
            --and set f instead of adding to f
            debugline(u.p,closest,dBlack)
        end
    end
    if not samepoint(f,P(0,0)) then
        f = Vscale(u.remainingf,f)
        m = u.remainingf
    end

    return f,m
end

function arrival(u,p)

end

--[[
ALL STEERING FUNCTIONS SHOULD BE METHODS FOR AN OBJECT
TAKE AN AVAILABLE FORCE PARAMETER
AND RETURN "FORCE USED"

so seek doesn't use maxforce if it is already very very close to ideal velocity
and braking doesnt overcommit

maybe use AVAILABLFORCE parameter to scale to number other than 1 and consider the <maxforce scenarios
]]

--unit has path property
--path has number of followers as a property,
--unit has pathprogress, 1 for just started, i for nextpoint in path, #path + 1 for final dest, #path + 2 for done and then some
function followforce(u,path,dt)
	local f,m = P(0,0),0
    local prevp = path.start
    local nextp = path.dest
    local width = path.size
    if u.pathprogress <= #path then
        nextp = path[u.pathprogress].p
        width = Vdist(nextp,path[u.pathprogress].p1)
        local n = Vnorm(Vsub(path[u.pathprogress].p2,path[u.pathprogress].p1))
        local diff = Vsub(u.p,nextp)
        if Vsamedir(n,diff) then
            path[u.pathprogress].followers = path[u.pathprogress].followers - 1
            u.pathprogress = u.pathprogress + 1
            --restart this function
            return followforce(u,path,dt)
        end
    elseif path.editing then
        u.pathprogress = math.min(#path+1,u.pathprogress) -- change "done" units to "barely done" in case a part of the path was removed
        --finished the path but it is still being drawn, so just chill here for now
        if not Vinrange(u.p,path.dest,path.size) then
            f,m = seekforce(u,path.dest,dt)
        end
        return f,m
    else
        path.followers = path.followers - 1
        u.area = {p=path.dest,r=path.size}
        if path.followers == 0 then
            removepath(u.path)
        end
        u.pathprogress = nil
        u.path = nil
        return f,m
    end
    if u.pathprogress > 1 then
        prevp = path[u.pathprogress - 1].p
    end
    
    --debugline(u.p,nextp)

	--first check if done this line segment.
	--if dd(u.x,u.y,path[u.pathprogress+1][1],path[u.pathprogress+1][2]) < 2*path[u.pathprogress+1].width^2 then
	--	u.pathprogress = math.min(u.pathprogress+1,#path-1) 
	--end
	--local x1,y1,x2,y2 = path[u.pathprogress][1],path[u.pathprogress][2],path[u.pathprogress+1][1],path[u.pathprogress+1][2]
	--local dx,dy = x2-x1,y2-y1
	
	--predicted position is 
	local np = Vadd(u.p, Vmult(width/u.maxv,u.v))--where this guy will be in a few seconds.
	--figure out distance of predicted position from current line segment of path.
	local closest = pointclosest(np,S(prevp,nextp))
    local dd = Vdd(Vsub(np,closest))
    --local dd1 = distancefromlinesegment(nx,ny,x1,y1,x2,y2)
	--local dd2,nx2,ny2 = distancefromlinesegment(u.x,u.y,x1,y1,x2,y2)
	--if distance is greater than margin for error, corrective steering is required
	if dd > width*width then
    --if dd1 > path[u.pathprogress+1].width^2 or dd2 > path[u.pathprogress+1].width^2 then --correct steering
		--convert unit's position to a point on the path and seek the unit to the point that is path.width further along the path
		local dist = width + Vdist(prevp,closest)
        --maybe this should be width - vdist
		local steerto = path.dest
		for i = u.pathprogress,#path-1 do
			local diff2 = Vsub(path[i+1].p,path[i].p)
            local diff2size = Vmagn(diff2)
			if dist > diff2size then
				dist = dist - diff2size
			else 
				steerto = Vadd(path[i].p,Vscale(dist,diff2))
				break
			end
		end
		f,m = seekforce(u,steerto,dt)
	--elseif u.pathprogress == #path+1 then --no need to continue if already at the end of the path.
		--do nothing, already at the end of the path
        --u.path.following --
        --u.path = nil
	else
		--move in direction of current line segment of path
		--f = scaleto(dx,dy,1)
        --force is difference in desired v - current v
        local diffv = Vsub(Vscale(u.maxv,Vsub(nextp,prevp)),u.v)
        m = math.min(u.remainingf,Vmagn(diffv)/dt) --desired speed change / dt will give the desired acceleration to create the exact speed after this frame
        f = Vscale(m,diffv)
	end
	return f,m
end

 */

