
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DiscordRichPresenceForVisualStudio
{
    /// <summary>
    /// Represents path validation logic like in UNIX glob system.
    /// </summary>
    public class FilePatternMatcher
    {
        private readonly List<Regex> validators = new List<Regex>();
        
        private Regex BuildValidator(string pattern)
        {
            if (char.IsLetter(pattern[0]) || char.IsDigit(pattern[0])) pattern = "**" + pattern;
            // If the input string has a RegExp template, it tries to complie it as a regex.
            if (pattern.StartsWith("^") && pattern.EndsWith("$"))
            {
                try
                {
                    return new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                }
                catch
                {

                }
            }
            if (pattern[pattern.Length - 1] == '/' || pattern.Substring(pattern.Length - 2) == @"\\")
            {
                pattern += "**";
            }
            pattern = Regex.Escape(pattern).Replace(@"\*\*", ".*").Replace(@"\*", @"[^\\\.]*").Replace(@"\?", ".");
            return new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        }

        /// <summary>
        /// Adds new exclusion validator to path.
        /// </summary>
        /// <param name="exclude">String to filter the path.</param>
        public void AddExclude(string exclude)
        {
            if (string.IsNullOrWhiteSpace(exclude)) return;
            // They'll be like comments
            if (exclude.StartsWith("#")) return;
            validators.Add(BuildValidator(exclude));
        }

        /// <summary>
        /// Adds a collection of exlusion strings.
        /// </summary>
        /// <param name="exludes">Strings to filter the path.</param>
        public void AddExcludes(IEnumerable<string> exludes)
        {
            foreach (var exclude in exludes)
            {
                AddExclude(exclude);
            }
        }

        /// <summary>
        /// Validates the path with added excluders.
        /// </summary>
        /// <param name="test">File path to test.</param>
        /// <returns><see langword="true"/> if the path is not filtered.</returns>
        public bool ValidatePath(string test)
        {
            test = Path.GetFullPath(test).Replace("\\", "/");
            if (string.IsNullOrWhiteSpace(test)) return false;
            if (validators.Any(x => x.IsMatch(test))) return false;
            return true;
        }
    }
}