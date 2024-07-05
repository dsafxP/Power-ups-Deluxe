using SFDGameScriptInterface;

namespace PowerupsDeluxe {
  public partial class GameScript : GameScriptInterface {
    // HUNGRY - Danger Ross
    public class HUNGRY : Powerup {
      private const float FORCE_DISTANCE = 62;
      private const float JOINT_MASS = 0.0001f;
      private const float DMG = 22;
      private const string SOLID = "InvisibleBlock";
      private const string NON_SOLID = "InvisibleBlockNoCollision";
      private const int JUMP_COOLDOWN = 1200;
      private const int MAX_CELLS = 14;
      private const int TIME = 19000;
    
      private static readonly VirtualKey[] _inputKeys = {VirtualKey.AIM_RUN_LEFT,
                                                         VirtualKey.AIM_RUN_RIGHT};
    
      private static int _cradleSlot = 0;
    
      private readonly List<IObject> _allItems = new List<IObject>();
      private readonly List<Tendril> tendrils = new List<Tendril>();
    
      private readonly IObject[] _walls = new IObject[4];
      private readonly IObject[] _cells = new IObject[MAX_CELLS];
      private readonly float[] _cradlePos = {-200, 260};
    
      private int _cellCount = 2;
      private int _jumpCooldown = 0;
      private float slowUpdateTime = 0;
      private bool _screamed = false;
    
      private IObject _forcePoint;
      private IObject _body;
      private IObject _skull;
      private IObjectAlterCollisionTile _cellCollision;
      private IObjectAlterCollisionTile _collisionGroup;
      private IObjectPullJoint _force;
      private IObjectTargetObjectJoint _cellTarget;
    
      public override string Name {
        get { return "HUNGRY"; }
      }
    
      public override string Author {
        get { return "Danger Ross"; }
      }
    
      public HUNGRY(IPlayer player) : base(player) {
        Time = TIME;
    
        _cradlePos[0] = _cradlePos[0] + Game.GetCameraMaxArea().Left;
        _cradlePos[0] = _cradlePos[0] + (-40 * _cradleSlot);
        _cradleSlot += 1;
      }

      protected override void Activate() {
        Player.SetInputMode(PlayerInputMode.ReadOnly);

        Game.PlayEffect("CAM_S", Vector2.Zero, 2f, 2000f, false);

        Game.PlaySound("Wilhelm", Vector2.Zero, 1f);

        Game.PlaySound("Madness", Vector2.Zero, 10f);
        Game.PlaySound("Madness", Vector2.Zero, 10f);
        Game.PlaySound("Madness", Vector2.Zero, 10f);
        Game.PlaySound("Madness", Vector2.Zero, 10f);

        Game.PlayEffect("GIB", Player.GetWorldPosition());

        _playerKeyInputEvent = Events.PlayerKeyInputCallback.Start(OnPlayerKeyInput);
        _ObjectTerminatedEvent = Events.ObjectTerminatedCallback.Start(OnObjectDestroyed);

        Player.SetNametagVisible(false);
        Player.SetStatusBarsVisible(false);

        Player.SetCameraSecondaryFocusMode(CameraFocusMode.Ignore);

        Vector2 centerPos = Player.GetWorldPosition() + new Vector2(0, 5f);

        //       SETTING UP JOINTS AND CHARACTER POSITIONS
        IObject floor = Game.CreateObject(SOLID, new Vector2(_cradlePos[0] - 16, _cradlePos[1] - 8));
        floor.SetSizeFactor(new Point(4, 1));
        floor.SetBodyType(BodyType.Static);
        _walls[0] = floor;


        IObject wall1 = Game.CreateObject(SOLID, new Vector2(_cradlePos[0] - 16, _cradlePos[1] + 16));
        wall1.SetSizeFactor(new Point(1, 4));
        wall1.SetBodyType(BodyType.Static);
        _walls[1] = wall1;

        IObject wall2 = Game.CreateObject(SOLID, new Vector2(_cradlePos[0] + 16, _cradlePos[1] + 16));
        wall2.SetSizeFactor(new Point(1, 4));
        wall2.SetBodyType(BodyType.Static);
        _walls[2] = wall2;


        IObject ceiling = Game.CreateObject(SOLID, new Vector2(_cradlePos[0] - 16, _cradlePos[1] + 24));
        ceiling.SetSizeFactor(new Point(5, 1));
        ceiling.SetBodyType(BodyType.Static);
        _walls[3] = ceiling;

        _allItems.Add(floor);
        _allItems.Add(wall1);
        _allItems.Add(wall2);
        _allItems.Add(ceiling);

        _collisionGroup = (IObjectAlterCollisionTile)Game.CreateObject("AlterCollisionTile");
        _collisionGroup.SetDisableCollisionTargetObjects(true);
        _allItems.Add(_collisionGroup);

        _cellCollision = (IObjectAlterCollisionTile)Game.CreateObject("AlterCollisionTile");
        _cellCollision.SetDisabledCategoryBits(65535);
        //_cellCollision.SetDisabledMaskBits(2);//2
        _cellCollision.SetDisabledAboveBits(65535);
        _allItems.Add(_cellCollision);

        _body = Game.CreateObject(SOLID, centerPos);//Game.CreateObject(SOLID, Player.GetWorldPosition());
        _body.SetSizeFactor(new Point(1, 2));
        _body.SetBodyType(BodyType.Dynamic);
        _body.SetMass(0.04f);
        SetPlayerCollision(_body);
        _collisionGroup.AddTargetObject(_body);
        _allItems.Add(_body);

        _cellTarget = (IObjectTargetObjectJoint)Game.CreateObject("TargetObjectJoint", centerPos);
        _cellTarget.SetTargetObject(_body);
        _allItems.Add(_cellTarget);

        _forcePoint = Game.CreateObject(NON_SOLID, centerPos + new Vector2(0, FORCE_DISTANCE));
        SetNoCollision(_forcePoint);
        AddNoProjectileFilter(_forcePoint);
        _allItems.Add(_forcePoint);

        IObjectTargetObjectJoint connection = (IObjectTargetObjectJoint)Game.CreateObject("TargetObjectJoint", centerPos + new Vector2(0, 7));
        connection.SetTargetObject(_body);
        connection.SetMass(JOINT_MASS);
        _allItems.Add(connection);

        _force = (IObjectPullJoint)Game.CreateObject("PullJoint", centerPos + new Vector2(0, FORCE_DISTANCE));
        _force.SetForce(0f);
        _force.SetForcePerDistance(0.03f); //0.8 for 20
                                           //_force.SetLineVisual(LineVisual.DJRope);
        _force.SetTargetObject(_forcePoint);
        _force.SetTargetObjectJoint(connection);
        _allItems.Add(_force);


        _skull = Game.CreateObject("Giblet04", centerPos + new Vector2(0, 25));
        _skull.SetMass(0.00001f);
        SetNoCollision(_skull);
        _collisionGroup.AddTargetObject(_skull);
        _allItems.Add(_skull);

        IObjectTargetObjectJoint sConnection = (IObjectTargetObjectJoint)Game.CreateObject("TargetObjectJoint", _skull.GetWorldPosition());
        sConnection.SetTargetObject(_skull); //SKULL CONNECTOR
        _allItems.Add(sConnection);


        IObjectDistanceJoint sRope = (IObjectDistanceJoint)Game.CreateObject("DistanceJoint", _forcePoint.GetWorldPosition());
        sRope.SetLengthType(DistanceJointLengthType.Elastic);
        sRope.SetTargetObject(_forcePoint);// ROPE GOES FROM POINT TO SKULL
                                           //sRope.SetLineVisual(LineVisual.DJRope);
        sRope.SetTargetObjectJoint(sConnection);
        _allItems.Add(sRope);

        IObjectPullJoint sForce = (IObjectPullJoint)Game.CreateObject("PullJoint", centerPos);
        sForce.SetForce(0f);
        sForce.SetForcePerDistance(0.0008f);
        sForce.SetLineVisual(LineVisual.DJRope);
        sForce.SetTargetObject(_body); //FORCE IS FROM BODY TO SKULL
        sForce.SetTargetObjectJoint(sConnection);
        sForce.SetMass(JOINT_MASS);
        _allItems.Add(sForce);

        for (int i = 0; i < MAX_CELLS / 2; i++)
          SpawnCell();

        _skull.SetWorldPosition(Player.GetWorldPosition());

        Player.SetWorldPosition(new Vector2(_cradlePos[0], _cradlePos[1]));
        Player.ClearFire();
      }

      private Events.PlayerKeyInputCallback _playerKeyInputEvent = null;

      private Events.ObjectTerminatedCallback _ObjectTerminatedEvent = null;

      public void OnObjectDestroyed(IObject[] destroyed) {
        if (!Enabled) {
          _ObjectTerminatedEvent.Stop();

          return;
        }

        foreach (IObject obj in destroyed) {
          if (obj == _skull) {
            //Game.RunCommand("/msg skull destroyed");
            Enabled = false;
            Player.Gib();
            return;
          }

          if (obj.CustomID == "__cell__") {
            Player.DealDamage(DMG);
            if (Player.IsDead) {
              Enabled = false;
              Player.Gib();
            }
            _cellCount--;
          }

          if (obj.UniqueID == _body.UniqueID) {
            Player.Gib();
            Enabled = false;
            return;
          }
        }
      }

      private void OnPlayerKeyInput(IPlayer player, VirtualKeyInfo[] keyEvents) {
        if (Player != player)
          return;

        // player key event registered
        for (int i = 0; i < keyEvents.Length; i++) {
          if (keyEvents[i].Event == VirtualKeyEvent.Pressed) {
            if (keyEvents[i].Key == VirtualKey.ATTACK) {
              if (Player.KeyPressed(VirtualKey.AIM_CLIMB_UP)) {
                tendrils.Add(new Tendril(new Vector2(_skull.GetFaceDirection() * 3f, 10f), _body));
              } else if (Player.KeyPressed(VirtualKey.AIM_CLIMB_DOWN)) {
                tendrils.Add(new Tendril(new Vector2(_skull.GetFaceDirection() * 5f, -5f), _body));
              } else {
                tendrils.Add(new Tendril(new Vector2(_skull.GetFaceDirection() * 10f, 3f), _body));
                //Game.RunCommand("/msg spawning tendril");
              }
            } else if (keyEvents[i].Key == VirtualKey.AIM_CLIMB_DOWN) {
              if (TouchingGround()) {
                RayCastInput input = new RayCastInput {
                  ClosestHitOnly = true,
                  BlockExplosions = RayCastFilterMode.True
                };

                RayCastResult output = Game.RayCast(_body.GetWorldPosition() + new Vector2(0, -12f),
                  _body.GetWorldPosition() + new Vector2(0, -13f), input)[0];

                if (!output.Hit)
                  _body.SetWorldPosition(_body.GetWorldPosition() + new Vector2(0, -12f));
                //else Game.RunCommand("/msg blocked");
              }
            }
          }
        }
      }

      private void Bite(IPlayer victim, PlayerModifiers hpmod) {

        for (int i = (MAX_CELLS / 2); i < MAX_CELLS; i++) {
          if (_cells[i] == null || _cells[i].IsRemoved) continue;
          if (i > (MAX_CELLS / 4)) {
            _cells[i].SetWorldPosition(victim.GetWorldPosition() + new Vector2((float)(_rng.NextDouble() - 0.5f) * 24f, 24f));
            _cells[i].SetAngle((float)(Math.PI * (7 / 4)));
          } else {
            _cells[i].SetWorldPosition(victim.GetWorldPosition() + new Vector2((float)(_rng.NextDouble() - 0.5f) * 24f, -12f));
            _cells[i].SetAngle((float)(Math.PI * (3 / 4)));
          }
        }

        victim.DealDamage(DMG);
        hpmod.CurrentHealth += 1f;

        Time += 200;

        Game.PlaySound("MeleeHitSharp", victim.GetWorldPosition(), 2f);
        if (victim.IsDead) {
          victim.DealDamage(DMG);
          //victim.Gib();//maybe remove for feeding portion?
          Time += 1500;
          return;
        }
      }

      private void SpawnCell() {
        for (int i = 0; i < MAX_CELLS; i++) {
          if (_cells[i] == null || _cells[i].IsRemoved) {
            Vector2 randPos = new Vector2((float)((_rng.NextDouble() - 0.5f) * 14f), (float)((_rng.NextDouble() - 0.5f) * 16f - 8f));
            Vector2 pos = _body.GetWorldPosition() + randPos - new Vector2(0, -8);
            if (i < (int)(MAX_CELLS / 2)) {
              _cells[i] = Game.CreateObject("Giblet0" + _rng.Next(2), pos, (float)(Math.PI * 2 * _rng.NextDouble()));
            } else {
              _cells[i] = Game.CreateObject("Giblet02", pos, (float)(Math.PI * 2 * _rng.NextDouble()));
            }
            _cells[i].CustomID = "__cell__";
            _cells[i].SetMass(0.00001f);
            CollisionFilter filter = _cells[i].GetCollisionFilter();//setPlayerCollision(_cells[i]);
            filter.MaskBits = 73; //9 + 64
            filter.CategoryBits = 64; //4 + 64
            _cells[i].SetCollisionFilter(filter);
            //_cellCollision.AddTargetObject(_cells[i]);

            //_collisionGroup.AddTargetObject(_cells[i]);

            //IObjectTargetObjectJoint targetObject = (IObjectTargetObjectJoint)Game.CreateObject("TargetObjectJoint", pos);
            //targetObject.SetTargetObject(_body);
            //targetObject.SetMass(JOINT_MASS);
            //_allItems.Add(targetObject);


            IObjectPullJoint pullJoint = (IObjectPullJoint)Game.CreateObject("PullJoint", pos);
            //pullJoint.SetForce(0.02f);
            pullJoint.SetForcePerDistance(0.001f);
            pullJoint.SetTargetObject(_cells[i]);
            pullJoint.SetTargetObjectJoint(_cellTarget);
            //pullJoint.SetTargetObjectJoint(targetObject);
            _allItems.Add(pullJoint);

            _cells[i].SetLinearVelocity(randPos * 3f);

            _cellCount += 1;
            _force.SetForcePerDistance(0.06f + 0.021f * _cellCount);
            return;
          }
        }
      }

      public static CollisionFilter AddNoProjectileFilter(IObject obj) {
        CollisionFilter filter = obj.GetCollisionFilter();
        filter.AbsorbProjectile = false;
        filter.ProjectileHit = false;
        return filter;
      }

      public static CollisionFilter SetPlayerCollision(IObject obj) {
        CollisionFilter filter = new CollisionFilter {
          CategoryBits = 4,
          MaskBits = 11,
          AbsorbProjectile = true,
          ProjectileHit = true
        };

        obj.SetCollisionFilter(filter);
        return filter;
      }

      public static CollisionFilter SetNoCollision(IObject obj) {
        CollisionFilter filter = new CollisionFilter {
          CategoryBits = 0,
          MaskBits = 0,
          AbsorbProjectile = true,
          ProjectileHit = true
        };

        obj.SetCollisionFilter(filter);
        return filter;
      }

      private bool TouchingGround() {
        Vector2 starting = _body.GetWorldPosition();
        RayCastInput input = new RayCastInput(true) {
          ClosestHitOnly = true,
          ProjectileHit = RayCastFilterMode.True,
          IncludeOverlap = false,
          MaskBits = 1
        };

        RayCastResult result = Game.RayCast(starting, starting + new Vector2(0, -17), input)[0];
        if (result.Hit) return true;

        return false;
      }

      public override void Update(float dlt, float dltSecs) {
        // Implement in derived classes
        Vector2 newPos = _body.GetWorldPosition() + new Vector2(0, FORCE_DISTANCE);

        if (_jumpCooldown > 0) {
          float eq = ((float)Math.Floor(Math.Pow(_jumpCooldown - JUMP_COOLDOWN, 2) / (JUMP_COOLDOWN / 3)) - 1500) / (-50);
          //if (eq < 0) eq = 0;
          //Game.RunCommand("/msg " + eq);
          newPos += new Vector2(0, eq);
          _jumpCooldown -= (int)dlt;
        } else if (Player.KeyPressed(VirtualKey.JUMP)) {
          if (TouchingGround()) {
            _jumpCooldown = JUMP_COOLDOWN;

            newPos += new Vector2(0, ((float)Math.Floor(Math.Pow(_jumpCooldown - JUMP_COOLDOWN, 2) / 500) - 1500f) / -50);
          }
        }

        int facingDirection = Player.KeyPressed(VirtualKey.AIM_RUN_RIGHT) ? 1 : -1;

        if (_inputKeys.Any(k => Player.KeyPressed(k))) {
          // Calculate offset
          if (TouchingGround()) {
            newPos += new Vector2((13f * Player.GetModifiers().RunSpeedModifier) * facingDirection, FORCE_DISTANCE / 20f);
          } else {
            newPos += new Vector2((10f * Player.GetModifiers().RunSpeedModifier) * facingDirection, 0f);
          }
        }

        if (Player.KeyPressed(VirtualKey.AIM_CLIMB_DOWN)) {
          newPos += new Vector2(0, -10);
        }

        if (_body.GetLinearVelocity().Y < -1f) {
          newPos += new Vector2(0, -10f);
        }

        _skull.SetFaceDirection(facingDirection);
        _skull.SetAngularVelocity(0f);
        _skull.SetAngle(0f);

        _forcePoint.SetWorldPosition(newPos);

        slowUpdateTime += dlt;
        PlayerModifiers hpmod = Player.GetModifiers();

        while (slowUpdateTime > 200) {
          slowUpdateTime -= 200;
          //SPAWN CELLS
          if (_cellCount < MAX_CELLS) {
            SpawnCell();
          }

          for (int i = tendrils.Count - 1; i >= 0; i--) {
            tendrils[i].Update();
            if (tendrils[i].Removed) {
              tendrils.RemoveAt(i);
            }
          }

          //add player damage
          bool foundPlayer = false;

          foreach (IPlayer ply in Game.GetPlayers()) {
            Vector2 plyPos = ply.GetWorldPosition();
            if (Math.Abs(plyPos.X - _body.GetWorldPosition().X) < 15f && Math.Abs(plyPos.Y - _body.GetWorldPosition().Y) < 30f) {

              PointShape.Random(e => { Game.PlayEffect("BLD", e); },
              new Area(plyPos.Y + 12f, plyPos.X - 8f, plyPos.Y - 4f, plyPos.X + 8f),
              _rng);

              Bite(ply, hpmod);

              if (ply.IsRemoved) {
                hpmod.CurrentHealth = hpmod.MaxHealth;
              }

              if (!_screamed) {
                Game.PlaySound("CartoonScream", ply.GetWorldPosition(), 0.7f);
                _screamed = true;
              }

              foundPlayer = true;

              break;
            }
          }

          if (!foundPlayer)
            _screamed = false;
        }
        Player.SetModifiers(hpmod);
      }

      public override void OnEnabled(bool enabled) {
        if (enabled)
          return;

        _cradleSlot -= 1;

        if (Player != null) {
          Player.SetWorldPosition(_body.GetWorldPosition());
          Player.SetLinearVelocity(_body.GetLinearVelocity());
          Player.SetNametagVisible(true);
          Player.SetStatusBarsVisible(true);
          Player.SetCameraSecondaryFocusMode(CameraFocusMode.Focus);
          Player.SetInputEnabled(true);

          Game.PlayEffect(EffectName.TraceSpawner, Vector2.Zero,
            Player.UniqueID, EffectName.Blood, 2.5f);
        }

        foreach (IObject obj in _allItems) {
          obj.Destroy();
        }

        for (int i = tendrils.Count - 1; i >= 0; i--) {
          tendrils[i].Destroy();
          tendrils.RemoveAt(i);
        }

        _playerKeyInputEvent.Stop();
        _playerKeyInputEvent.Stop();
        _ObjectTerminatedEvent.Stop();
      }

      private class Tendril {
        private const int DURATION = 1000;
        private float _expiration;
        private bool _grabbing = false;

        private readonly IObject _grabber;

        private readonly IObjectTargetObjectJoint _toGrabber;
        private readonly IObject _forceSolid;
        private readonly IObjectPullJoint _force;
        private readonly IObjectPullJoint _arm;
        private readonly IObjectWeldJoint _weld;

        public bool Removed { get; private set; }

        public Tendril(Vector2 velocity, IObject body) {
          _expiration = Game.TotalElapsedGameTime + DURATION;

          _grabber = Game.CreateObject("Giblet03",
          body.GetWorldPosition() + (Vector2.Normalize(velocity) * 8f), //position
          0f, //angle
          velocity + body.GetLinearVelocity(), //linearvelocity
          0f, //angularvelocity
          velocity.X > 0 ? 1 : -1 //direction
          );
          _grabber.TrackAsMissile(true);
          _grabber.SetHealth(10f);
          _grabber.CustomID = "__tendril__";

          _toGrabber = (IObjectTargetObjectJoint)Game.CreateObject("TargetObjectJoint", _grabber.GetWorldPosition());
          _toGrabber.SetTargetObject(_grabber);

          Vector2 forcePos = body.GetWorldPosition() + velocity * 5f + new Vector2(0, 20f);

          _forceSolid = Game.CreateObject("InvisibleBlockNoCollision", forcePos);

          _force = (IObjectPullJoint)Game.CreateObject("PullJoint", forcePos);
          _force.SetTargetObject(_forceSolid);
          _force.SetTargetObjectJoint(_toGrabber);
          _force.SetForcePerDistance(0.02f);
          _force.SetForce(0f);

          //_force.SetLineVisual(LineVisual.DJRope); //TEMPORARY

          _arm = (IObjectPullJoint)Game.CreateObject("PullJoint", body.GetWorldPosition() + new Vector2(0, 5f));
          _arm.SetLineVisual(LineVisual.DJRope);
          _arm.SetTargetObject(body);
          _arm.SetTargetObjectJoint(_toGrabber);
          _arm.SetForce(0f);
          _arm.SetForcePerDistance(0f);

          _weld = (IObjectWeldJoint)Game.CreateObject("WeldJoint");
          _weld.AddTargetObject(_grabber);

        }

        public bool MatchTendril(IObject obj) {
          return MatchTendril(obj.UniqueID);
        }

        public bool MatchTendril(int id) {
          return _grabber.UniqueID == id;
        }

        private void CheckGrab() {
          if (Math.Floor(_grabber.GetAngularVelocity() * 1000) != 0f || Math.Floor(_grabber.GetAngle() * 1000) != 0f || _grabber.GetHealth() < 10f) {

            //Game.RunCommand("/msg angle " + _grabber.GetAngle());
            //Game.RunCommand("/msg AngularVelocity " + _grabber.GetAngularVelocity());

            RayCastInput input = new RayCastInput(true) {
              ClosestHitOnly = true,
              ProjectileHit = RayCastFilterMode.True,
              IncludeOverlap = false
            };

            Vector2 pos = _grabber.GetWorldPosition();
            RayCastResult result = new RayCastResult(false, 0, null, false, 0, Vector2.Zero, Vector2.Zero);
            for (int i = 0; i < 4; i++) {
              result = Game.RayCast(pos, pos + Vector2Helper.Rotated(new Vector2(5f * (_grabber.GetFaceDirection() < 0 ? -1 : 1), 0), (float)Math.PI / 2 * i), input)[0];

              if (result.Hit) {
                if (result.HitObject.Name.Substring(0, 3) == "Gib") {
                  result = new RayCastResult(false, 0, null, false, 0, Vector2.Zero, Vector2.Zero);
                  continue;
                }
                break;
              }
            }
            if (result.Hit && result.ObjectID != _arm.GetTargetObject().UniqueID) {
              Grab(result.HitObject);
            } else Destroy();

          }
        }

        public void Grab(IObject obj) {
          _expiration += 1000f;
          _grabbing = true;
          _weld.AddTargetObject(obj);
          _arm.SetForcePerDistance(0.05f);
          _arm.SetForce(0.1f);
          //Game.RunCommand("/msg grabbed " + obj.Name);
          obj.DealDamage(DMG);
          if (obj is IPlayer) {
            _arm.SetForcePerDistance(0.2f); //also knock them down?
            _expiration += 1000f;
            //Time += 300f;
          }
        }

        public void Update() {
          if (_grabber == null || Removed) {
            Destroy();
            return;
          }

          if (!_grabbing) {
            CheckGrab();
          }

          if (Game.TotalElapsedGameTime > _expiration) {
            Destroy();
          }
        }
        public void Destroy() {
          if (!Removed) {
            Removed = true;

            if (_grabber != null)
              _grabber.Destroy();

            _toGrabber.Remove();
            _forceSolid.Remove();
            _force.Remove();
            _arm.Remove();
            _weld.Remove();
          }
        }
      }
    }
  }
}
