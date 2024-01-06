using CommandLine;

namespace MyStealerToolbox
{
    public class ConfigOptions
    {
        [Option('i', "input", HelpText = "The path of stealer executable file to process.")]
        public string Input { get; set; }

        [Option('e', "encryption-key", Required = false, HelpText = "The unique encryption key to encrypt all sensitive informations.")]
        public string EncryptionKey { get; set; }

        [Option('w', "webhook-url", Required = false, HelpText = "The webhook url to dump the information to.")]
        public string WebhookUrl { get; set; }
    }

    public class DecryptOptions
    {
        [Option('i', "input", HelpText = "The path of encrypted file to decrypt.")]
        public string Input { get; set; }

        [Option('e', "encryption-key", Required = false, HelpText = "The unique encryption key to decrypt the file.")]
        public string EncryptionKey { get; set; }

        [Option('o', "output", Required = false, HelpText = "The path where decrypted file will be stored.")]
        public string Output { get; set; }
    }
}
