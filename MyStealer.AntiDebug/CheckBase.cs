using MyStealer.Shared;

namespace MyStealer.AntiDebug
{
    public abstract class CheckBase : ModuleBase
    {
        /// <summary>
        /// Take action to prevent from being debugged.
        /// Only single execution of this function on the startup of the application is enough.
        /// </summary>
        /// <returns><c>true</c> if the action is successfully applied, <c>false</c> otherwise.</returns>
        public virtual bool PreventPassive() => true;

        /// <summary>
        /// Take action to prevent from being debugged.
        /// This function should be executed every seconds to have such effects.
        /// </summary>
        /// <returns><c>true</c> if the action is successfully applied, <c>false</c> otherwise.</returns>
        public virtual bool PreventActive() => true;

        /// <summary>
        /// Check whether a debugger or similar behavior is present.
        /// Only single execution of this function on the startup of the application is enough.
        /// </summary>
        /// <returns><c>true</c> if debugging action is present, <c>false</c> otherwise.</returns>
        public virtual bool CheckPassive() => true;

        /// <summary>
        /// Check whether a debugger or similar behavior is present.
        /// This function should be executed every seconds to have such effects.
        /// </summary>
        /// <returns><c>true</c> if debugging action is present, <c>false</c> otherwise.</returns>
        public virtual bool CheckActive() => true;
    }
}
