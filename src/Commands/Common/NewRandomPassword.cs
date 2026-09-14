using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Security;
using System.Security.Cryptography;
using MSCKite.Azure.Platform.Models;

namespace MSCKite.Azure.Platform.Commands.Common
{
    [Cmdlet(VerbsCommon.New, "RandomPassword")]
    [OutputType(typeof(RandomPasswordResult), typeof(SecureString))]
    public class NewRandomPassword : PSCmdlet
    {
        private const string UppercaseCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string LowercaseCharacters = "abcdefghijklmnopqrstuvwxyz";
        private const string NumberCharacters = "0123456789";
        private const string DefaultSpecialCharacters = "!@#$%^&*()-_=+[]{}:,.?";
        private const string AmbiguousCharacters = "0Oo1lLiI5Ss8Bb";

        private readonly RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create();

        [Parameter]
        [ValidateRange(8, 256)]
        public short Length { get; set; } = 16;

        [Parameter]
        public SwitchParameter Uppercase { get; set; } = new SwitchParameter(true);

        [Parameter]
        public SwitchParameter Lowercase { get; set; } = new SwitchParameter(true);

        [Parameter]
        public SwitchParameter Numbers { get; set; } = new SwitchParameter(true);

        [Parameter]
        public SwitchParameter SpecialCharacters { get; set; } = new SwitchParameter(true);

        [Parameter]
        [ValidateNotNullOrEmpty]
        public string SpecialCharacterSet { get; set; } = DefaultSpecialCharacters;

        [Parameter]
        public SwitchParameter AsSecureString { get; set; }

        [Parameter]
        public string ExcludeCharacters { get; set; }

        [Parameter]
        public SwitchParameter NoAmbiguousCharacters { get; set; }

        [Parameter]
        [ValidateRange(1, int.MaxValue)]
        public int Count { get; set; } = 1;

        protected override void ProcessRecord()
        {
            var characterSets = GetEnabledCharacterSets();
            if (characterSets.Count == 0)
            {
                ThrowTerminatingError(CreateValidationError("At least one character set must be selected."));
            }

            if (Length < characterSets.Count)
            {
                ThrowTerminatingError(CreateValidationError("Length must be at least the number of enabled character sets."));
            }

            var generatedPasswords = new HashSet<string>(StringComparer.Ordinal);
            var attemptsRemaining = Math.Max(1000, Count * 10);

            while (generatedPasswords.Count < Count)
            {
                if (attemptsRemaining-- == 0)
                {
                    ThrowTerminatingError(CreateValidationError("Unable to generate the requested number of unique passwords with the selected character pool."));
                }

                var password = GeneratePassword(characterSets);
                if (!generatedPasswords.Add(password))
                {
                    continue;
                }

                if (AsSecureString.IsPresent)
                {
                    WriteObject(ConvertToSecureString(password));
                    continue;
                }

                WriteObject(new RandomPasswordResult
                {
                    Password = password,
                    Length = Length,
                    Complexity = GetComplexity(characterSets.Count)
                });
            }
        }

        protected override void EndProcessing()
        {
            randomNumberGenerator.Dispose();
        }

        private List<char[]> GetEnabledCharacterSets()
        {
            var excludedCharacters = new HashSet<char>((ExcludeCharacters ?? string.Empty).ToCharArray());
            if (NoAmbiguousCharacters.IsPresent)
            {
                foreach (var character in AmbiguousCharacters)
                {
                    excludedCharacters.Add(character);
                }
            }

            var characterSets = new List<char[]>();
            AddCharacterSet(characterSets, Uppercase.IsPresent, UppercaseCharacters, excludedCharacters, "Uppercase");
            AddCharacterSet(characterSets, Lowercase.IsPresent, LowercaseCharacters, excludedCharacters, "Lowercase");
            AddCharacterSet(characterSets, Numbers.IsPresent, NumberCharacters, excludedCharacters, "Numbers");
            AddCharacterSet(characterSets, SpecialCharacters.IsPresent, SpecialCharacterSet, excludedCharacters, "SpecialCharacters");

            return characterSets;
        }

        private void AddCharacterSet(List<char[]> characterSets, bool enabled, string characters, HashSet<char> excludedCharacters, string name)
        {
            if (!enabled)
            {
                return;
            }

            var usableCharacters = characters
                .Where(character => !excludedCharacters.Contains(character))
                .Distinct()
                .ToArray();

            if (usableCharacters.Length == 0)
            {
                ThrowTerminatingError(CreateValidationError($"The {name} character set is empty after exclusions."));
            }

            characterSets.Add(usableCharacters);
        }

        private string GeneratePassword(List<char[]> characterSets)
        {
            var passwordCharacters = new List<char>(Length);

            foreach (var characterSet in characterSets)
            {
                passwordCharacters.Add(characterSet[GetRandomInt32(characterSet.Length)]);
            }

            var characterPool = characterSets.SelectMany(characterSet => characterSet).ToArray();
            while (passwordCharacters.Count < Length)
            {
                passwordCharacters.Add(characterPool[GetRandomInt32(characterPool.Length)]);
            }

            Shuffle(passwordCharacters);
            return new string(passwordCharacters.ToArray());
        }

        private void Shuffle(IList<char> characters)
        {
            for (var index = characters.Count - 1; index > 0; index--)
            {
                var swapIndex = GetRandomInt32(index + 1);
                var current = characters[index];
                characters[index] = characters[swapIndex];
                characters[swapIndex] = current;
            }
        }

        private int GetRandomInt32(int exclusiveUpperBound)
        {
            if (exclusiveUpperBound <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(exclusiveUpperBound));
            }

            var randomBytes = new byte[4];
            var maxValue = uint.MaxValue - (uint.MaxValue % (uint)exclusiveUpperBound);
            uint value;

            do
            {
                randomNumberGenerator.GetBytes(randomBytes);
                value = BitConverter.ToUInt32(randomBytes, 0);
            }
            while (value >= maxValue);

            return (int)(value % (uint)exclusiveUpperBound);
        }

        private SecureString ConvertToSecureString(string value)
        {
            var secureString = new SecureString();
            foreach (var character in value)
            {
                secureString.AppendChar(character);
            }

            secureString.MakeReadOnly();
            return secureString;
        }

        private string GetComplexity(int characterSetCount)
        {
            if (Length >= 16 && characterSetCount >= 4)
            {
                return "High";
            }

            if (Length >= 12 && characterSetCount >= 3)
            {
                return "Medium";
            }

            return "Basic";
        }

        private ErrorRecord CreateValidationError(string message)
        {
            return new ErrorRecord(
                new PSArgumentException(message),
                "RandomPasswordValidationFailed",
                ErrorCategory.InvalidArgument,
                null);
        }
    }
}
