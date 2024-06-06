// VAMPIRISM - Danila015
public class Vampirism : Powerup {
  private const float EFFECT_COOLDOWN = 50;
  private const float HEAL_MULT = 0.77 f;
  private const float BLEED_DMG = 2;
  private const float BLEED_COOLDOWN = 250;
  private const float CORPSE_DMG_MULT = 3;
  private const uint BLEED_TIME = 6000;

  private static readonly Vector2 _offset = new Vector2(0, 12);

  private readonly List < IPlayer > _bleeding = new List < IPlayer > ();

  private Events.PlayerMeleeActionCallback _meleeActionCallback = null;

  private IPlayer[] Bleeding {
    get {
      _bleeding.RemoveAll(p => p == null || p.IsRemoved || p.IsDead);

      return _bleeding.ToArray();
    }
  }

  public override string Name {
    get {
      return "VAMPIRISM";
    }
  }

  public override string Author {
    get {
      return "Danila015";
    }
  }

  public Vampirism(IPlayer player) : base(player) {
    Time = 21000; // 21 s
  }

  protected override void Activate() {}

  public override void Update(float dlt, float dltSecs) {
    if (Time % EFFECT_COOLDOWN == 0)
      Game.PlayEffect(EffectName.BloodTrail, Player.GetWorldPosition() + _offset);

    if (Time % BLEED_COOLDOWN == 0)
      foreach(IPlayer bleeding in Bleeding) {
        bleeding.DealDamage(BLEED_DMG);

        Game.PlayEffect(EffectName.Blood, bleeding.GetWorldPosition());
        Game.PlaySound("ImpactFlesh", Vector2.Zero, 0.5 f);
      }
  }
  
  public override void TimeOut() {
    Game.PlaySound("StrengthBoostStop", Vector2.Zero);
  }

  public override void OnEnabled(bool enabled) {
    if (enabled) {
      _meleeActionCallback = Events.PlayerMeleeActionCallback.Start(OnPlayerMeleeAction);
    } else {
      _meleeActionCallback.Stop();

      _meleeActionCallback = null;

      _bleeding.Clear();
    }
  }

  private void OnPlayerMeleeAction(IPlayer player, PlayerMeleeHitArg[] args) {
    if (player != Player)
      return;

    foreach(PlayerMeleeHitArg arg in args
      .Where(a => a.IsPlayer)) {
      float dmg = arg.HitDamage;

      Player.SetHealth(Player.GetHealth() + dmg * HEAL_MULT);

      IPlayer hit = (IPlayer) arg.HitObject;

      if (hit.IsDead) {
        hit.SetHealth(hit.GetHealth() + dmg); // Null dmg so it isn't dealt twice

        hit.DealDamage(dmg * CORPSE_DMG_MULT);
      }

      if (!_bleeding.Contains(hit)) {
        _bleeding.Add(hit);

        Events.UpdateCallback.Start((float _dlt) => {
          _bleeding.Remove(hit);
        }, BLEED_TIME, 1);
      }
    }
  }
}