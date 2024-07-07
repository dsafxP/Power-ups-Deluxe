using SFDGameScriptInterface;

namespace PowerupsDeluxe {
  public partial class GameScript : GameScriptInterface {
    // RECONSTRUCTOR - Danila015
    public class Reconstructor : Powerup {
      private const float EFFECT_COOLDOWN = 250;
      private const float HEAL_MULT = 0.24f;
      private const float OBJ_DMG_MULT = 7;
      private const float MAGNET_AREA_SIZE = 75;
      private const float MAGNET_FORCE = 2;
      private const float MAGNET_ANGULAR_SPEED = 1.5f;
      private const PlayerHitEffect HIT_EFFECT = PlayerHitEffect.Metal;

      private static readonly string[] _debrisNames = {
        "Debris",
        "Giblet"
      };

      public override string Author {
        get {
          return "Danila015";
        }
      }

      public override string Name {
        get {
          return "RECONSTRUCTOR";
        }
      }

      private Events.PlayerMeleeActionCallback _meleeActionCallback = null;

      private IProfile _profile;
      private PlayerModifiers _modifiers;
      private PlayerHitEffect _hitEffect;

      private Area MagnetArea {
        get {
          Area playerArea = Player.GetAABB();

          playerArea.SetDimensions(MAGNET_AREA_SIZE, MAGNET_AREA_SIZE);

          return playerArea;
        }
      }

      private IObject[] DebrisInMagnet {
        get {
          return Game.GetObjectsByArea(MagnetArea)
            .Where(o => _debrisNames.Any(d => o.Name.Contains(d)) &&
            o.Destructable)
            .ToArray();
        }
      }

      private IObject[] EatDebris {
        get {
          return Game.GetObjectsByArea(Player.GetAABB())
            .Where(o => _debrisNames.Any(d => o.Name.Contains(d)) &&
            o.Destructable)
            .ToArray();
        }
      }

      private IProfile MetalProfile {
        get {
          IProfile playerProfile = Player.GetProfile();

          playerProfile.Skin = new IProfileClothingItem(string.Format("Normal{0}",
          playerProfile.Gender == Gender.Male ? string.Empty : "_fem"), "Skin5");

          return ColorProfile(playerProfile, "ClothingLightGray", "ClothingLightGray");
        }
      }

      public Reconstructor(IPlayer player) : base(player) {
        Time = 24000; // 24 s
      }

      protected override void Activate() {
        _profile = Player.GetProfile();
        _hitEffect = Player.GetHitEffect();
        _modifiers = Player.GetModifiers();

        _modifiers.CurrentHealth = -1;
        _modifiers.CurrentEnergy = -1;

        Player.SetProfile(MetalProfile);
        Player.SetHitEffect(HIT_EFFECT);
      }

      public override void Update(float dlt, float dltSecs) {
        bool effect = Time % EFFECT_COOLDOWN == 0;

        if (effect) {
          PointShape.Random(Draw, Player.GetAABB(), _rng);
        }

        foreach (IObject deb in EatDebris) {
          float heal = deb.GetHealth() * HEAL_MULT;
          float healed = Player.GetHealth() + heal;

          if (healed > Player.GetMaxHealth()) {
            PlayerModifiers mod = Player.GetModifiers();

            mod.CurrentHealth += heal;
            mod.MaxHealth += (int) heal;

            Player.SetModifiers(mod);
          } else {
            Player.SetHealth(healed);
          }

          deb.Destroy();
        }

        foreach (IObject deb in DebrisInMagnet) {
          Vector2 debPos = deb.GetWorldPosition();

          deb.SetLinearVelocity(Vector2Helper.DirectionTo(debPos,
            Player.GetWorldPosition()) * MAGNET_FORCE);

          deb.SetAngularVelocity(MAGNET_ANGULAR_SPEED);

          if (effect)
            Game.PlayEffect(EffectName.ItemGleam, debPos);
        }
      }

      public override void TimeOut() {
        Game.PlaySound("DestroyMetal", Vector2.Zero);
        Game.PlayEffect(EffectName.Sparks, Player.GetWorldPosition());
      }

      public override void OnEnabled(bool enabled) {
        if (enabled) {
          _meleeActionCallback = Events.PlayerMeleeActionCallback.Start(OnPlayerMeleeAction);
        } else {
          Player.SetProfile(_profile);
          Player.SetHitEffect(_hitEffect);
          Player.SetModifiers(_modifiers);

          _meleeActionCallback.Stop();

          _meleeActionCallback = null;
        }
      }

      private static void Draw(Vector2 v) {
        Game.PlayEffect(EffectName.ItemGleam, v);
      }

      private static IProfile ColorProfile(IProfile pr, string col, string colI) {
        if (pr.Accesory != null)
          pr.Accesory = new IProfileClothingItem(pr.Accessory.Name, col, colI);

        if (pr.ChestOver != null)
          pr.ChestOver = new IProfileClothingItem(pr.ChestOver.Name, col, colI);

        if (pr.ChestUnder != null)
          pr.ChestUnder = new IProfileClothingItem(pr.ChestUnder.Name, col, colI);

        if (pr.Feet != null)
          pr.Feet = new IProfileClothingItem(pr.Feet.Name, col, colI);

        if (pr.Hands != null)
          pr.Hands = new IProfileClothingItem(pr.Hands.Name, col, colI);

        if (pr.Head != null)
          pr.Head = new IProfileClothingItem(pr.Head.Name, col, colI);

        if (pr.Legs != null)
          pr.Legs = new IProfileClothingItem(pr.Legs.Name, col, colI);

        if (pr.Waist != null)
          pr.Waist = new IProfileClothingItem(pr.Waist.Name, col, colI);

        return pr;
      }

      private void OnPlayerMeleeAction(IPlayer player, PlayerMeleeHitArg[] args) {
        if (player != Player)
          return;

        foreach (PlayerMeleeHitArg arg in args) {
          if (arg.IsPlayer)
            continue;

          IObject hit = arg.HitObject;
          float dmg = arg.HitDamage;

          hit.SetHealth(hit.GetHealth() + dmg); // Null dmg so it isn't dealt twice

          hit.DealDamage(dmg * OBJ_DMG_MULT);

          hit.SetAngularVelocity(MAGNET_ANGULAR_SPEED);

          Game.PlaySound("ChainsawStartup", Vector2.Zero, 0.6f);
        }
      }
    }
  }
}