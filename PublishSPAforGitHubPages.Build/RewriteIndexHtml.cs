using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;

namespace PublishSPAforGHPages
{
    public class RewriteIndexHtml : Microsoft.Build.Utilities.Task
    {
        [Required]
        public string File { get; set; }

        [Required]
        public string BaseHref { get; set; }

        private struct State
        {
            public bool HasChanged;
            public bool RewitedBaseHref;
        }

        public override bool Execute()
        {
            var state = new State();
            var lines = System.IO.File.ReadLines(File);
            var rewritedLines = new List<string>();

            foreach (var line in lines)
            {
                // rewrite "base href"
                if (!state.RewitedBaseHref)
                {
                    var m = Regex.Match(line, "(<base[ ]+href=\")([^\"]*)(\"[ ]*/>)");//, m => m.Groups[1].Value + BaseHref + m.Groups[3].Value)
                    if (m.Success)
                    {
                        state.RewitedBaseHref = true;
                        var rewritedLine = m.Groups[1].Value + BaseHref + m.Groups[3].Value;
                        if (line != rewritedLine)
                        {
                            state.HasChanged = true;
                            rewritedLines.Add(rewritedLine);
                            continue;
                        }
                    }
                }

                rewritedLines.Add(line);
            }

            if (state.HasChanged)
            {
                System.IO.File.WriteAllLines(File, rewritedLines);
            }

            return true;
        }
    }
}
