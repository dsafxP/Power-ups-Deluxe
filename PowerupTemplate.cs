using SFDGameScriptInterface;

namespace PowerupsDeluxe {
  public partial class GameScript : GameScriptInterface {
    public static class Template {
      //==================================================================//
      //======================< YOUR SUBMISSION FORM >====================//
      //=====================< EDIT THE SECTIONS BELOW >==================//
      //==================================================================//

      /// <summary>
      /// Represents a base power-up that can be activated and updated over time.
      /// </summary>
      public class MY_POWERUP_CHANGE_THIS : Powerup { //1: CHANGE YOUR CLASS' NAMESPACE

        public override string Name {
          get {
            return "My Powerup Name";
          } //2: CHANGE THE TEXT TO YOUR POWERUP NAME
        }

        public override string Author {
          get {
            return "My Name";
          } //3: CHANGE THE TEXT TO YOUR NAME
        }

        // Interval for the main update callback event

        public MY_POWERUP_CHANGE_THIS(IPlayer player) : base(player) { //4: CHANGE YOUR CONSTRUCTOR TO MATCH THE CLASS NAME
          Time = 12000; //5: CHANGE THE DURATION OF THE EFFECT TO WHAT YOU WANT
        }

        /// <summary>
        /// Virtual method for actions upon activating the power-up.
        /// </summary>
        protected override void Activate() {
          //6: ADD CODE ON ACTIVATION

          Game.RunCommand(string.Format("/msg {0} power activated on {1}", Name, Player.Name));

          // Implement in derived classes
        }

        /// <summary>
        /// Virtual method for updating the power-up.
        /// </summary>
        /// <param name="dlt">The time delta since the last update.</param>
        /// <param name="dltSecs">The time delta in seconds since the last update.</param>
        public override void Update(float dlt, float dltSecs) {
          //7: ADD CODE FOR THE EFFECT DURING ITS INFLUENCE

          // Implement in derived classes

          //EXAMPLE EFFECT
          if (Time % 50 == 0)
            PointShape.Swirl((v => Game.PlayEffect("GLM", Vector2Helper.Rotated(v - Player.GetWorldPosition(),
            (float)(Time % 1500 * (MathHelper.TwoPI / 1500))) + Player.GetWorldPosition() + new Vector2(0, 4))),
              Player.GetWorldPosition(), //Center Position
              5f, //Initial Radius
              50f, //End Radius
              2 //Rotations
            );
        }

        /// <summary>
        /// Virtual method called when the power-up times out.
        /// </summary>
        public override void TimeOut() {
          //8: ADD CODE ON EFFECT END
          Game.RunCommand(string.Format("/msg {0} power deactivated on {1}", Name, Player.Name));
          // Implement in derived classes
        }

        /// <summary>
        /// Virtual method called when the power-up is enabled or disabled. Called by the constructor and on timeout.
        /// </summary>
        public override void OnEnabled(bool enabled) {
          // Implement in derived classes
        }
      }

      //==================================================================//
      //==================< DO NOT CHANGE ANYTHING BELOW >================//
      //==============< IF YOU'RE NOT SURE WHAT YOU'RE DOING >============//
      //==================================================================//
    }

    public void AfterStartup() {
      Events.UserMessageCallback.Start(OnUserMessage);
    }

    //		   USE  >>>> /test <player>  <<<< TO TEST YOUR SUBMISSION

    private void OnUserMessage(UserMessageCallbackArgs args) {
      if (args.IsCommand && args.User.IsModerator) {
        if (args.Command == "TEST") {
          string[] argsPieces = args.CommandArguments.ToLower()
          .Split(' ');

          IUser reciever;

          if (argsPieces.Length > 0 && argsPieces[0].Length > 0) {
            reciever = GetUser(argsPieces[0]);
          } else {
            reciever = args.User;
          }

          if (reciever != null && reciever.GetPlayer() != null) {
            IPlayer ply = reciever.GetPlayer();

            Activator.CreateInstance(GetPowerup(), ply);

            Game.ShowChatMessage(string.Format("{0} recieved ability", ply.Name), new Color(34, 134, 34));
          }
        }
      }
    }

    public static Type GetPowerup() {
      Type[] nestedPowerups = typeof(Template).GetNestedTypes();

      Type[] instantiableTypes = nestedPowerups
        .Where(t =>
          // t.BaseType == typeof(Powerup) &&
          t.GetConstructors().Any(c => c.GetParameters().Length == 1 &&
            c.GetParameters()[0].ParameterType ==
            typeof(IPlayer)))
        .ToArray();

      if (instantiableTypes.Length == 0)
        throw new InvalidOperationException("No instantiable types found.");

      return instantiableTypes[0];
    }
  }
}