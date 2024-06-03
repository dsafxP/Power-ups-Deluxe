private static readonly Random _rng = new Random();

public class ManaShield : Powerup {
  private const string centerobj = "InvisibleBlockNoCollision";

  private const int MAX_TIME = 25000;
  private const int RADIUS = 25;
  private const int X_OFFSET = 0;
  private const int Y_OFFSET = 10;

  private const byte COLOR_R = 123;
  private const byte COLOR_G = 244;
  private const byte COLOR_B = 244;

  private readonly List < IObject > allItems = new List < IObject > ();
  private readonly IObjectText[] effects = new IObjectText[8];
  private readonly Events.CallbackDelegate[] handlers = new Events.CallbackDelegate[2];

  private IObject bird;

  private Vector2 offset;

  private float health = 100;
  private float preservedHealth;

  private bool queueDisable = false;
  private bool delayUpdate = true;

  public override string Name {
    get {
      return "MANA SHIELD";
    }
  }

  public override string Author {
    get {
      return "Danger Ross";
    }
  }

  public ManaShield(IPlayer player) : base(player) {
    Time = MAX_TIME;
  }

  protected override void Activate() {
    offset = new Vector2(X_OFFSET, Y_OFFSET);

    Game.PlaySound("StrengthBoostStart", Player.GetWorldPosition(), 5);

    PlayerModifiers modify = Player.GetModifiers();

    modify.MeleeStunImmunity = 1;

    preservedHealth = modify.CurrentHealth;

    Player.SetModifiers(modify);

    IObjectWeldJoint weld1 = (IObjectWeldJoint) Game.CreateObject("WeldJoint", Player.GetWorldPosition() + offset); //Direct attachment to player by center1
    IObjectWeldJoint weld2 = (IObjectWeldJoint) Game.CreateObject("WeldJoint", Player.GetWorldPosition() + offset); //Rotating attachments around center1, by center2
    IObjectWeldJoint weld3 = (IObjectWeldJoint) Game.CreateObject("WeldJoint", Player.GetWorldPosition() + offset); //attached to player by proxy through center1

    allItems.Add(weld1);
    allItems.Add(weld2);
    allItems.Add(weld3);

    IObject center1 = (IObject) Game.CreateObject(centerobj, Player.GetWorldPosition() + offset); //HINGE FOR ROTATING PART TO ATTACH TO, WELDED ONTO PLAYER
    center1.SetBodyType(BodyType.Dynamic);
    center1.SetMass(0.0001f);
    weld1.AddTargetObject(center1);
    allItems.Add(center1);

    IObjectPullJoint force = (IObjectPullJoint) Game.CreateObject("PullJoint", center1.GetWorldPosition() + new Vector2(0, 200));
    //force.SetLineVisual(LineVisual.DJRope);
    force.SetForcePerDistance(0.01f);
    allItems.Add(force);

    bird = Game.CreateObject(centerobj, center1.GetWorldPosition() + new Vector2(0, 200));
    force.SetTargetObject(bird);
    allItems.Add(bird);

    IObjectTargetObjectJoint target = (IObjectTargetObjectJoint) Game.CreateObject("TargetObjectJoint", center1.GetWorldPosition());
    target.SetTargetObject(center1);
    force.SetTargetObjectJoint(target);
    allItems.Add(target);

    IObject center2 = (IObject) Game.CreateObject(centerobj, Player.GetWorldPosition() + offset);
    center2.SetBodyType(BodyType.Dynamic);
    center2.SetMass(0.001f);
    weld2.AddTargetObject(center2);
    weld2.AddTargetObject(Player);
    allItems.Add(center2);

    IObjectRevoluteJoint revolute = (IObjectRevoluteJoint) Game.CreateObject("RevoluteJoint", Player.GetWorldPosition() + offset);
    revolute.SetTargetObjectA(center2);
    revolute.SetTargetObjectB(center1);
    revolute.SetMotorEnabled(true);
    revolute.SetMotorSpeed(0.7f);
    allItems.Add(revolute);

    //revolute.SetBodyType(BodyType.Dynamic);
    //revolute.SetMass(0.0001f);

    for (int i = 0; i < 4; i++) {
      IObjectText obj = (IObjectText) Game.CreateObject("Text", center1.GetWorldPosition() + Vector2Helper.Rotated(new Vector2(-22, 2), MathHelper.PIOver2 * i));
      obj.SetTextColor(new Color(COLOR_R, COLOR_G, COLOR_B));
      obj.SetTextScale(4);
      obj.SetText("(");
      obj.CustomID = "(";
      obj.SetAngle(MathHelper.PIOver2 * i);
      obj.SetBodyType(BodyType.Dynamic);
      obj.SetMass(0.000001f);
      weld1.AddTargetObject(obj);
      allItems.Add(obj);
      effects[i] = obj;
    }

    for (int i = 0; i < 4; i++) {
      IObjectText obj = (IObjectText) Game.CreateObject("Text", center1.GetWorldPosition() + Vector2Helper.Rotated(new Vector2(-22, 2), MathHelper.PIOver2 * i));
      obj.SetTextColor(Color.White);
      obj.SetTextScale(4);
      obj.SetText("{");
      obj.CustomID = "{";
      obj.SetAngle(MathHelper.PIOver2 * i);
      obj.SetBodyType(BodyType.Dynamic);
      obj.SetMass(0.000001f);
      weld1.AddTargetObject(obj);
      allItems.Add(obj);
      effects[i + 4] = obj;
    }

    CollisionFilter filter = new CollisionFilter();
    filter.ProjectileHit = true;
    filter.AbsorbProjectile = false;
    filter.BlockFire = true;

    IObject deflector = Game.CreateObject("InvisibleBlockNoCollision", Player.GetWorldPosition() + new Vector2(-17, 2.3f));

    deflector.CustomID = "deflector";
    deflector.SetBodyType(BodyType.Dynamic);
    deflector.SetCollisionFilter(filter);
    deflector.SetAngle(MathHelper.PIOver4);
    deflector.SetSizeFactor(new Point(4, 4)); //setmass doesnt come into effect if called too early
    deflector.SetMass(0.000001f);
    weld2.AddTargetObject(deflector);
    allItems.Add(deflector);

    IObjectText shine = (IObjectText) Game.CreateObject("Text", new Vector2(-5, -1) + center1.GetWorldPosition());
    shine.SetTextColor(Color.White);
    shine.SetTextScale(3);
    shine.SetText(",");
    shine.SetAngle(MathHelper.PIOver2);
    shine.SetBodyType(BodyType.Dynamic);
    shine.SetMass(0.000001f);
    weld2.AddTargetObject(shine);
    allItems.Add(shine);

    IObjectText crack1 = (IObjectText) Game.CreateObject("Text", center1.GetWorldPosition() + new Vector2(-8.6f, 14.5f));
    crack1.SetTextColor(Color.White);
    crack1.SetTextScale(3);
    crack1.SetText("");
    crack1.SetAngle(5.22f);
    crack1.SetBodyType(BodyType.Dynamic);
    crack1.SetMass(0.000001f);
    weld2.AddTargetObject(crack1);
    allItems.Add(crack1);

    IObjectText crack2 = (IObjectText) Game.CreateObject("Text", center1.GetWorldPosition() + new Vector2(12.8f, 6.9f));
    crack2.SetTextColor(Color.White);
    crack2.SetTextScale(3);
    crack2.SetText("");
    crack2.SetAngle(4.45f);
    crack2.SetBodyType(BodyType.Dynamic);
    crack2.SetMass(0.000001f);
    weld2.AddTargetObject(crack2);
    allItems.Add(crack2);

    IObjectText crack3 = (IObjectText) Game.CreateObject("Text", center1.GetWorldPosition() + new Vector2(6f, -5.3f));
    crack3.SetTextColor(Color.White);
    crack3.SetTextScale(3);
    crack3.SetText("");
    crack3.SetAngle(3.93f);
    crack3.SetBodyType(BodyType.Dynamic);
    crack3.SetMass(0.000001f);
    weld2.AddTargetObject(crack3);
    allItems.Add(crack3);

    //Player.SetCollisionFilter(filter);

    Events.ProjectileHitCallback onHit = null;

    onHit = Events.ProjectileHitCallback.Start((projectile, args) => {
      if (Game.GetObject(args.HitObjectID).CustomID == "deflector") { //remove getobject
        Vector2 normal = Vector2Helper.Rotated(Vector2.Normalize(projectile.Position - center1.GetWorldPosition()), MathHelper.PI);

        double angleDifference = Math.Abs(Vector2Helper.AngleTo(normal, projectile.Velocity)); //Math.Abs(Vector2Helper.Angle(normal) - Vector2Helper.Angle(projectile.Velocity));

        if (angleDifference < MathHelper.PIOver2) {

          if (projectile.ProjectileItem == ProjectileItem.GRENADE_LAUNCHER || projectile.ProjectileItem == ProjectileItem.BAZOOKA || projectile.ProjectileItem == ProjectileItem.FLAKCANNON) {
            Game.TriggerExplosion(projectile.Position);
            projectile.FlagForRemoval();
            return;
          }

          Game.PlaySound("GrenadeBounce", projectile.Position);
          Game.PlayEffect(EffectName.Sparks, projectile.Position);

          projectile.Velocity = Vector2Helper.Bounce(projectile.Velocity, normal);
          projectile.Position += normal * 2;

          health -= (projectile.GetProperties().ObjectDamage * (float)(angleDifference / MathHelper.PI)) + (projectile.GetProperties().ObjectDamage) / 3;

          if (health > 50 && health < 75) {
            crack1.SetText("X");
            Game.PlaySound("ImpactGlass", crack1.GetWorldPosition(), 5);
          } else if (health > 25 && health < 50) {
            crack2.SetText("X");
            Game.PlaySound("ImpactGlass", crack2.GetWorldPosition(), 5);
          } else if (health > 0 && health < 25) {
            crack3.SetText("X");
            Game.PlaySound("ImpactGlass", crack3.GetWorldPosition(), 5);
          } else if (health <= 0) {
            queueDisable = true;
          }
          //onHeadshot.Stop();
          //return;
        }

      }
    });

    handlers[0] = onHit;

    Events.PlayerDamageCallback onDamage = null;

    onDamage = Events.PlayerDamageCallback.Start((IPlayer hitPlayer, PlayerDamageArgs args) => {
      if (args.DamageType == PlayerDamageEventType.Fire) {
        preservedHealth = Player.GetModifiers().CurrentHealth;
        if (preservedHealth > 0) return;
      }

      if (hitPlayer.UniqueID == Player.UniqueID) {
        PlayerModifiers modhp = Player.GetModifiers();
        if (modhp.CurrentHealth == 0)
          modhp.CurrentHealth = preservedHealth;
        else
          modhp.CurrentHealth += args.Damage; //THIS DOESNT BLOCK ALL DAMAGE
        Player.SetModifiers(modhp);
        queueDisable = true;
      }
    });
    handlers[1] = onDamage;

    weld2.AddTargetObject(Player);
  }

  public override void Update(float dlt, float dltSecs) {

    if (queueDisable) {
      if (!delayUpdate) {
        Enabled = false;
        return;
      }
      delayUpdate = false;
    }

    if (Time > MAX_TIME - 1200 || Time < 2000) {
      if ((Time < MAX_TIME - 1000 && Time > 2000)) {
        TurnEffect(); //setting to effect at the last sec
      } else {
        ToggleEffect(); //blinking
      }
    }

    if (Time % 50 == 0) {
      if (_rng.Next(0, 6) == 1) {
        Game.PlayEffect(EffectName.ItemGleam, RandomPoint(RADIUS - 6) + Player.GetWorldPosition() + offset);
      }
    }

    bird.SetWorldPosition(Player.GetWorldPosition() + new Vector2(0, 202));
  }

  public override void OnEnabled(bool enabled) {
    PlayerModifiers modify = Player.GetModifiers();
    modify.MeleeStunImmunity = 0;
    Player.SetModifiers(modify);

    if (!enabled) {
      foreach(IObject obj in allItems) {
        obj.Remove();
      }

      for (int i = 0; i < handlers.Length; i++) {
        handlers[i].Stop();
      }

      if (Time > 0) {
        BreakShield();
      }
    }
  }

  public void ToggleEffect() {
    for (int i = 0; i < effects.Length; i++) {
      if (effects[i].GetText() == "") {
        effects[i].SetText(effects[i].CustomID);
      } else {
        effects[i].SetText("");
      }
    }
  }

  public void TurnEffect() {
    for (int i = 0; i < effects.Length; i++) {
      effects[i].SetText(effects[i].CustomID); //setting to effect at the last sec
    }
  }

  private void BreakShield() {
    List < IObject > toFade = new List < IObject > ();

    Game.PlaySound("BreakGlass", Player.GetWorldPosition(), 5);
    Game.PlaySound("BreakGlass", Player.GetWorldPosition(), 5);
    Game.PlaySound("BreakGlass", Player.GetWorldPosition(), 5);

    for (int i = 0; i < 50; i++) {
      Vector2 dir = RandomPoint(RADIUS);

      if (_rng.Next(0, 2) == 0) {
        IObject debris = Game.CreateObject("GlassShard00A", Player.GetWorldPosition() + new Vector2(X_OFFSET, Y_OFFSET) + dir);
        debris.SetHealth(1);
        debris.SetLinearVelocity(dir * 0.3f + new Vector2(0, 4));
        debris.SetAngle((float)(_rng.NextDouble() * MathHelper.TwoPI));
        debris.SetAngularVelocity(((float) _rng.NextDouble() - 0.5f) * 20);
        toFade.Add(debris);
      } else {
        Game.PlayEffect(EffectName.DestroyGlass, dir + Player.GetWorldPosition());
      }
    }

    Events.UpdateCallback cleanUp = null;

    cleanUp = Events.UpdateCallback.Start(_dlt => {
      if (toFade.Count() > 0) {
        Game.PlaySound("GlassShard", toFade[toFade.Count() - 1].GetWorldPosition(), 5);
        toFade[toFade.Count() - 1].Remove();
        toFade.RemoveAt(toFade.Count() - 1);
      } else {
        cleanUp.Stop();
      }
    }, 100);
  }

  private Vector2 RandomPoint(float radius) {
    float distance = (float) Math.Pow(_rng.NextDouble(), 0.25) * radius;

    return Vector2Helper.Rotated(new Vector2(distance, 0), (float)(_rng.NextDouble() * MathHelper.TwoPI));
  }
}