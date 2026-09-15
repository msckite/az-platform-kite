using System;
using System.Management.Automation;

namespace MSCKite.Azure.Platform.Commands.Common
{
    [Cmdlet(VerbsCommon.New, "RandomUniqueId")]
    [OutputType(typeof(string))]
    public class NewRandomUniqueId : PSCmdlet
    {
        private const string Digits = "0123456789";
        private const string Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        // Digits are included twice to double their odds relative to any single letter.
        private const string AlphanumericCharacters = Digits + Digits + Letters;
        private static readonly Random Random = new Random();
        private static readonly object RandomLock = new object();

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ValidateRange(4, 12)]
        public short Length { get; set; } = 8;

        protected override void ProcessRecord()
        {
            var characters = new char[Length];

            lock (RandomLock)
            {
                for (var index = 0; index < characters.Length; index++)
                {
                    characters[index] = AlphanumericCharacters[Random.Next(AlphanumericCharacters.Length)];
                }
            }

            WriteObject(new string(characters));
        }
    }
}
