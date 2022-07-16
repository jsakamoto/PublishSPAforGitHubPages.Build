﻿using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;

namespace PublishSPAforGHPages
{
    public class RewriteIndexHtml : Microsoft.Build.Utilities.Task
    {
        [Required]
        public string File { get; set; }

        public bool InjectBrotliLoader { get; set; } = true;

        [Required]
        public string BaseHref { get; set; }

        public bool Recursive { get; set; }

        private struct State
        {
            public bool HasChanged;
            public bool RewitedBaseHref;
            public bool InjectedBrotliLoader;
            public bool DisabledAutoStartOfBlazorWasmLoader;
        }

        public override bool Execute()
        {
            var fileName = Path.GetFileName(this.File);
            var baseDir = Path.GetDirectoryName(this.File);
            var searchOption = this.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var targetFiles = Directory.GetFiles(baseDir, fileName, searchOption);
            foreach (var targeFile in targetFiles)
            {
                this.Rewrite(targeFile);
            }
            return true;
        }

        private void Rewrite(string file)
        {
            var state = new State();
            var lines = System.IO.File.ReadLines(file);
            var rewritedLines = new List<string>();

            foreach (var line in lines)
            {
                // rewrite "base href"
                if (this.RewritedBaseHref(ref state, rewritedLines, line)) continue;

                // set autostart of the blazor.webassembly.js to false
                if (this.DisabledAutoStart(ref state, rewritedLines, line)) continue;

                // inject brotli loader
                if (this.InjectedBrotliLoader(ref state, rewritedLines, line)) continue;

                rewritedLines.Add(line);
            }

            if (state.HasChanged)
            {
                System.IO.File.WriteAllLines(file, rewritedLines);
            }
        }

        private bool RewritedBaseHref(ref State state, List<string> rewritedLines, string line)
        {
            if (state.RewitedBaseHref) return false;

            var m = Regex.Match(line, "(<base[ ]+href=\")([^\"]*)(\"[ ]*/>.*)");
            if (m.Success)
            {
                state.RewitedBaseHref = true;
                var rewritedLine = line.Substring(0, m.Index) + m.Groups[1].Value + this.BaseHref + m.Groups[3].Value;
                if (line != rewritedLine)
                {
                    state.HasChanged = true;
                    rewritedLines.Add(rewritedLine);
                    return true;
                }
            }

            return false;
        }

        private bool DisabledAutoStart(ref State state, List<string> rewritedLines, string line)
        {
            if (!this.InjectBrotliLoader) return false;
            if (state.DisabledAutoStartOfBlazorWasmLoader) return false;

            var m = Regex.Match(line, @"(<script[^>]+src=""_framework/blazor.webassembly.js""[^>]*)(></script>.*)");
            if (m.Success)
            {
                state.DisabledAutoStartOfBlazorWasmLoader = true;
                var part1 = m.Groups[1].Value;
                var part2 = m.Groups[2].Value;
                var m2 = Regex.Match(part1, @"autostart="".+""");
                if (m2.Success)
                    part1 = part1.Substring(0, m2.Index) + @"autostart=""false""" + part1.Substring(m2.Index + m2.Length);
                else
                    part1 += @" autostart=""false""";

                var rewritedLine = line.Substring(0, m.Index) + part1 + part2;
                if (line != rewritedLine)
                {
                    state.HasChanged = true;
                    rewritedLines.Add(rewritedLine);
                    return true;
                }
            }

            return false;
        }

        private bool InjectedBrotliLoader(ref State state, List<string> rewritedLines, string line)
        {
            if (!this.InjectBrotliLoader) return false;
            if (state.InjectedBrotliLoader) return false;

            if (line.TrimStart().StartsWith(@"<script src=""decode.min.js"""))
            {
                state.InjectedBrotliLoader = true;
                return false;
            }

            if (line.TrimStart().StartsWith("</body>"))
            {
                if (state.DisabledAutoStartOfBlazorWasmLoader)
                {
                    rewritedLines.Add(@"    <script src=""decode.min.js""></script>");
                    rewritedLines.Add(@"    <script src=""brotliloader.min.js""></script>");
                    rewritedLines.Add(line); // line is "</body>"
                    state.InjectedBrotliLoader = true;
                    state.HasChanged = true;
                    return true;
                }
            }
            return false;
        }
    }
}
