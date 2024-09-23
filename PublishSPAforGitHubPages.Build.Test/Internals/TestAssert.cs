using NUnit.Framework;
using Toolbelt.Diagnostics;

namespace PublishSPAforGitHubPages.Build.Test.Internals;

internal static class TestAssert
{
    internal static async Task RunAsync(string command, string args, string workDir)
    {
        using var process = XProcess.Start(command, args, workDir);
        await process.WaitForExitAsync();
        process.ExitCode.Is(0, message: process.Output);
    }

    internal static void FilesAreEquals(
        string actualFilePath,
        string expectedFilePath,
        Func<(string TargetContentLine, string ExpectedContentLine), bool>? filter = null)
    {
        var actualLines = File.ReadAllLines(actualFilePath).ToList();
        var expectedLines = File.ReadAllLines(expectedFilePath).ToList();
        var maxLineCount = Math.Max(actualLines.Count, expectedLines.Count);
        actualLines.AddRange(Enumerable.Repeat("", maxLineCount - actualLines.Count));
        expectedLines.AddRange(Enumerable.Repeat("", maxLineCount - expectedLines.Count));

        var unmatchLineIndex = -1;
        for (var i = 0; i < maxLineCount; i++)
        {
            var actualLine = actualLines[i];
            var expectedLine = expectedLines[i];
            if (filter != null && filter((actualLine, expectedLine)) == false) continue;

            if (actualLine != expectedLine)
            {
                unmatchLineIndex = i;
                break;
            }
        }

        if (unmatchLineIndex != -1)
        {
            var lineNumberWidth = maxLineCount.ToString().Length;
            var details = actualLines
                .Select((content, index) => $"{index + 1}".PadLeft(lineNumberWidth) + " " + content)
                .Select((content, index) => (unmatchLineIndex == index ? "> " : "  ") + content)
                .ToList();
            var expectedContentLine = expectedLines[unmatchLineIndex];
            details.Insert(unmatchLineIndex + 1, "<-".PadRight(lineNumberWidth + 3) + expectedContentLine);
            var detail = details.Aggregate((current, next) => current + "\r\n" + next);

            Assert.Fail(message:
                $"The file content of \"{Path.GetFileName(actualFilePath)}\", line number {unmatchLineIndex + 1} is not as expected.\r\n\r\n" +
                detail);
        }
    }
}
