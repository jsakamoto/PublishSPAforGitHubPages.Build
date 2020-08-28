using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;

namespace PublishSPAforGHPages
{
    public class RewriteBaseHref : Microsoft.Build.Utilities.Task
    {
        [Required]
        public string File { get; set; }

        [Required]
        public string BaseHref { get; set; }

        public override bool Execute()
        {
            var lines = System.IO.File.ReadAllLines(File);
            var rewritedLines = lines
                .Select(line => Regex.Replace(line, "(<base[ ]+href=\")([^\"]*)(\"[ ]*/>)", m => m.Groups[1].Value + BaseHref + m.Groups[3].Value))
                .ToArray();
            if (!Enumerable.SequenceEqual(lines, rewritedLines))
            {
                System.IO.File.WriteAllLines(File, rewritedLines);
            }
            return true;
        }
    }
}
