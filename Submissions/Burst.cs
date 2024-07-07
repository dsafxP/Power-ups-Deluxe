using SFDGameScriptInterface;

namespace PowerupsDeluxe {
  public partial class GameScript : GameScriptInterface {
    // BURST - Danger Ross
    public class Burst : Powerup {
      private const int BATCH_SIZE = 8;
      private const float BURST_COOLDOWN = 500;

      private readonly List<BParticle> _followingParticles = new List<BParticle>();

      private Events.PlayerKeyInputCallback _playerKeyInputEvent = null;

      private float _power = 0;
      private float _elapsed = 0;
      private float _nextParticleTime;

      public override string Name {
        get {
          return "BURST";
        }
      }

      public override string Author {
        get {
          return "Danger Ross";
        }
      }

      public bool CanBurst {
        get {
          return _elapsed <= 0;
        }
      }

      public Burst(IPlayer player) : base(player) {
        Time = 20000; // 20 s
      }

      protected override void Activate() {
        _nextParticleTime = _rng.Next(30) * 50;

        _playerKeyInputEvent = Events.PlayerKeyInputCallback.Start(OnPlayerKeyInput);
      }

      public void OnPlayerKeyInput(IPlayer player, VirtualKeyInfo[] keyEvents) {
        if (player.UserIdentifier == Player.UserIdentifier && keyEvents[0].Key == VirtualKey.ATTACK && 
          (player.IsMeleeAttacking || player.IsKicking) && CanBurst) {
          PlayerModifiers mods = player.GetModifiers();
          PlayerModifiers original = player.GetModifiers();

          float damageMult = 1 + (_power / 5000);

          mods.MeleeDamageDealtModifier *= damageMult;
          mods.MeleeStunImmunity = 1;
          mods.MeleeForceModifier *= 1 + damageMult * 3f;

          Game.PlayEffect("CFTXT", Player.GetWorldPosition() + new Vector2(0, 5), "x" + ((int)(damageMult * 100) / 100f));

          Area effectArea = new Area(14, -12, -10, 12);
          effectArea.Move(Player.GetWorldPosition());

          for (int i = 0; i < 5 + _rng.Next(5) * (int)damageMult; i++)
            PointShape.Random(v => Game.PlayEffect("S_P", v), effectArea, _rng);

          Game.PlayEffect("CAM_S", Player.GetWorldPosition(), 2f, 500f, false);
          Game.PlaySound("StrengthBoostStart", Player.GetWorldPosition());

          player.SetModifiers(mods);

          //Enabled = false;

          _elapsed = BURST_COOLDOWN;

          Events.PlayerMeleeActionCallback finder = null;

          finder = Events.PlayerMeleeActionCallback.Start((IPlayer attacker, PlayerMeleeHitArg[] args) => {
            if (attacker.UniqueID == Player.UniqueID) {
              foreach (PlayerMeleeHitArg arg in args) {
                if (arg.HitObject is IPlayer) {
                  IPlayer vic = (IPlayer)arg.HitObject;
                  vic.SetWorldPosition(vic.GetWorldPosition() + Vector2Helper.Up);
                  vic.SetLinearVelocity(vic.GetLinearVelocity() + new Vector2(5 * Player.FacingDirection * damageMult, 6));
                  if (arg.HitDamage <= 0) {
                    vic.DealDamage(8 * damageMult);
                  }
                }
              }
            }
          });

          Events.UpdateCallback delay = null;

          delay = Events.UpdateCallback.Start(e => {
            player.SetModifiers(original);
            delay.Stop();
            finder.Stop();
            _power = 0;
          }, 500);
        }
      }

      public override void Update(float dlt, float dltSecs) {
        _elapsed = Math.Max(_elapsed - dlt, 0);

        float powerMult = 1;

        if (Player.IsCrouching)
          powerMult += 0.5f;

        if (Math.Abs(Player.GetLinearVelocity().X) + Math.Abs(Player.GetLinearVelocity().Y) > 4)
          powerMult -= 0.5f;

        _power += dlt * powerMult;

        //spawning particles
        _nextParticleTime -= dlt;

        if (_nextParticleTime <= 0) { //replace with while
          _followingParticles.Add(BParticle.GetBParticle(Player, Player.GetWorldPosition() +
            Vector2Helper.Rotated(Vector2Helper.Right, (float)(_rng.NextDouble() * 2 * Math.PI)) *
            ((float)(_rng.NextDouble() * 30f) + 10f), ((float)_rng.NextDouble() * 4f + 2)));

          _nextParticleTime += _rng.Next((int)(5 / (powerMult))) * 50 - 50;
        }

        //updating particles
        if (Time % 100 == 0)
          for (int i = _followingParticles.Count - 1; i >= 0; i--) {
            BParticle particle = _followingParticles[i];
            particle.Update();
            if (!particle.GetActive()) {
              _followingParticles.RemoveAt(i);
            }
          }
      }

      public override void OnEnabled(bool enabled) {
        // Implement in derived classes
        if (!enabled) {
          _playerKeyInputEvent.Stop();

          //free all particles
          for (int i = _followingParticles.Count - 1; i >= 0; i--) {
            BParticle particle = _followingParticles[i];
            particle.Disable();
            _followingParticles.RemoveAt(i);
          }
        }
      }

      private class BParticle {
        private readonly IObjectText graphic;
        private readonly IObjectElevatorPathJoint path1;
        private readonly IObjectElevatorPathJoint path2;
        private readonly IObjectElevatorAttachmentJoint elevatorAttachment;

        //private int index; //should match the particles position in the global list

        private float size;

        //the class has a list of all instantiated particles for memory management
        private static BParticle[] particles = new BParticle[0];
        //particle index loops around the list in a circle, represents the next available particle for use
        private static int particleIndex = 0;
        //the count of used particles must correspond to the last usable index. particles must be freed in the order they were consumed
        private static int usedParticles = 0;
        //equivalent to particles.Length
        private static int particleCount = 0;

        public static BParticle GetBParticle(IPlayer following, Vector2 start, float size) {
          InitializeParticles();
          BParticle particle = particles[particleIndex];

          Vector2 offset1 = new Vector2(-8f, 8f);
          Vector2 offset2 = new Vector2(0, 6);

          particleIndex = (particleIndex + 1) % particleCount;
          usedParticles++;

          particle.size = size;

          particle.path1.SetWorldPosition(start + offset1);

          particle.path2.SetWorldPosition(following.GetWorldPosition() + offset2 + following.GetLinearVelocity() * 5f);

          // particle.UpdatePath2();

          particle.graphic.SetWorldPosition(start);
          particle.graphic.SetText(".");
          particle.graphic.SetTextScale(size);
          particle.graphic.SetBodyType(BodyType.Dynamic);

          particle.elevatorAttachment.SetWorldPosition(start);
          particle.elevatorAttachment.SetMotorSpeed(200);



          return particle;
        }
        private BParticle() {
          //the text particle
          graphic = (IObjectText)Game.CreateObject("Text");

          //the rails
          path2 = (IObjectElevatorPathJoint)Game.CreateObject("ElevatorPathJoint");

          path1 = (IObjectElevatorPathJoint)Game.CreateObject("ElevatorPathJoint");
          path1.SetNextPathJoint(path2);
          //path1.SetLineVisual(LineVisual.DJRope);

          //the rail attachment
          elevatorAttachment = (IObjectElevatorAttachmentJoint)Game.CreateObject("ElevatorAttachmentJoint");
          elevatorAttachment.SetTargetObject(graphic);
          elevatorAttachment.SetElevatorPathJoint(path1);
          elevatorAttachment.SetMaxMotorTorque(200);
          elevatorAttachment.SetAccelerationDistance(2000);
          elevatorAttachment.SetMotorSpeed(10);

        }

        public void Update() {
          if (!GetActive()) return;
          //UpdatePath2();

          float distanceToTarget = Vector2.Distance(path1.GetWorldPosition(), path2.GetWorldPosition()) - 3f;
          float distanceToTravel = Vector2.Distance(graphic.GetWorldPosition(), path2.GetWorldPosition()) - 3f;

          if (distanceToTravel < 3f) {
            Disable();
            return;
          }

          float sizeFactor = (distanceToTravel / distanceToTarget);

          if (graphic.GetTextScale() > size * sizeFactor)
            graphic.SetTextScale(size * sizeFactor);
        }

        public bool GetActive() {
          return graphic.GetText() != string.Empty || graphic.GetTextScale() < 0.1f;
        }

        public void Disable() {
          graphic.SetText(string.Empty);
          elevatorAttachment.SetMotorSpeed(0);
        }

        private static void InitializeParticles() {
          if (usedParticles > 0) {
            int backIndex = (particleCount + particleIndex - usedParticles) % particleCount;
            while (usedParticles > 0 && !particles[backIndex].GetActive()) {
              particles[backIndex].Disable();
              usedParticles--;
              backIndex = (backIndex + 1) % particleCount;
            }
          }

          if (usedParticles >= particleCount) {
            particleCount += BATCH_SIZE;

            BParticle[] newParticlesList = new BParticle[particleCount];

            for (int i = 0; i < particleCount; i++) {
              int length = particles.Length;
              if (length == 0) length = 1;

              //iterating backwards
              int index = (particleCount + particleIndex - i) % particleCount;
              int oldIndex = (length + particleIndex - i) % length;

              //inserting old items
              if (i < usedParticles) {
                newParticlesList[index] = particles[oldIndex];
              }

              //creating new (unused) particles
              else {
                newParticlesList[index] = new BParticle();
              }
            }
            particles = newParticlesList;

          } else return;
        }
      }
    }
  }
}
