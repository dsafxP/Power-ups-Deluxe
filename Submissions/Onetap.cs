// ONE-TAP - Danila015
public class Onetap : Powerup {
  private const float EFFECT_COOLDOWN = 50;
  private const float EFFECT_SEPARATION = 50;
  private const float EFFECT_SIZE = 40;
  private const float SHAKE_TIME = 1000;
  private const float SHAKE_INTENSITY = 4;
  private const float BLOCK_TIME_PENALTY = 3000;

  private const float EFFECT_RADIUS = EFFECT_SIZE / 2;

  private Events.PlayerMeleeActionCallback _meleeActionCallback = null;

  public override string Name {
    get {
      return "ONE-TAP";
    }
  }

  public override string Author {
    get {
      return "Danila015";
    }
  }

  public Onetap(IPlayer player) : base(player) {
    Time = 10000;
  }

  protected override void Activate() {}

  public override void Update(float dlt, float dltSecs) {
    if (Time % EFFECT_COOLDOWN == 0) {
      Draw(Player.GetWorldPosition());
    }
  }

  public override void TimeOut() {
    Game.PlaySound("StrengthBoostStop", Vector2.Zero);

    Game.PlayEffect(EffectName.Gib, Player.GetWorldPosition());
  }

  public override void OnEnabled(bool enabled) {
    if (enabled) {
      _meleeActionCallback = Events.PlayerMeleeActionCallback.Start(OnPlayerMeleeAction);
    } else {
      _meleeActionCallback.Stop();

      _meleeActionCallback = null;
    }
  }

  private void OnPlayerMeleeAction(IPlayer player, PlayerMeleeHitArg[] args) {
    if (player != Player)
      return;

    foreach(PlayerMeleeHitArg arg in args) {
      if (!arg.IsPlayer)
        continue;

      IPlayer hit = (IPlayer) arg.HitObject;

      if (hit.IsBlocking) {
        Time -= BLOCK_TIME_PENALTY;

        continue;
      }

      hit.Gib();

      Game.PlayEffect(EffectName.CameraShaker, Vector2.Zero,
        SHAKE_INTENSITY, SHAKE_TIME, true);

      Time = 0;
    }
  }

  private void Draw(Vector2 pos) {
    PointShape.Circle(v => {
      Game.PlayEffect(EffectName.BloodTrail, Vector2Helper.Rotated(v - pos,
          (float)(Time % 1500 * (MathHelper.TwoPI / 1500))) +
        pos);
    }, pos, EFFECT_RADIUS, EFFECT_SEPARATION);
  }
}