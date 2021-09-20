using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace WebBloatScore.Models
{
    public class SlimerExecutor
    {
        private const string PageTitle = "title";
        private const string PageTitlePlaceholder = "(?<" + PageTitle + ">([^\t]*))";
        private const string PageCount = "count";
        private const string PageCountPlaceholder = "(?<" + PageCount + ">([\\d]+))";
        private const string PageSize = "size";
        private const string PageSizePlaceholder = "(?<" + PageSize + ">([\\d]+))";

        private const string OutputPattern =
            "PageTitle=" + PageTitlePlaceholder + "\t" +
            "PageCount=" + PageCountPlaceholder + "\t" +
            "PageSize=" + PageSizePlaceholder;

        private static readonly string ArgumentsFormat =
            "-user-agent \"" + Utilities.SlimerUserAgent + "\" \"" + Utilities.SlimerSource + "\" \"{0}\" \"{1}\" " +
            "\"" + OutputPattern + "\" " +
            "\"" + PageTitlePlaceholder + "\" " +
            "\"" + PageCountPlaceholder + "\" " +
            "\"" + PageSizePlaceholder + "\"";

        private static readonly Regex OutputRegex = new Regex(OutputPattern, RegexOptions.Compiled);

        private readonly int slimmerWaitForExit;
        private readonly int pngQuantWaitForExit;

        public SlimerExecutor(int slimmerWaitForExit, int pngQuantWaitForExit)
        {
            this.slimmerWaitForExit = slimmerWaitForExit;
            this.pngQuantWaitForExit = pngQuantWaitForExit;
        }

        // first executes the slimer which creates the screenshot and returns the size of the page
        // after that PngQuant is used to optimize the screenshot
        // in the end CaptureResult is returned that contains all the values necessary to calculate WebBS
        public CaptureResult CalculateResult(string url)
        {
            string screenshotName = Guid.NewGuid() + ".png";
            string screenshotPath = Path.Combine(Utilities.ScreenshotsPath, screenshotName);
            string slimerArguments = string.Format(CultureInfo.InvariantCulture, ArgumentsFormat, url, screenshotPath);

            string output = this.ExecuteSlimer(slimerArguments, out ExitCode exit);

            if (exit == ExitCode.Success || exit == ExitCode.SuccessTimeout)
            {
                CaptureResult result = GetCaptureResult(output, url, screenshotPath, Path.Combine(Utilities.ScreenshotsRelativePath, screenshotName), exit);
                if (result != null)
                {
                    Logger.Info("Execute => URL:" + url + "\r\n" + output);
                    return result;
                }
                else
                    exit = ExitCode.Fail;
            }

            Logger.Error("Execute => URL:" + url + "\r\n" + output);
            return new CaptureResult() { Exit = exit };
        }

        // executes slimer that creates the screenshot and returns the size of the page
        private string ExecuteSlimer(string arguments, out ExitCode exit)
        {
            var processInfo = new ProcessStartInfo(Utilities.SlimerExecutable, arguments);
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardOutput = true;
            processInfo.StandardOutputEncoding = Encoding.UTF8;

            var slimerOutput = new StringBuilder();
            DataReceivedEventHandler handler = (sender, e) => slimerOutput.AppendLine(e.Data);

            var process = Process.Start(processInfo);
            process.OutputDataReceived += handler;
            process.BeginOutputReadLine();

            exit = process.WaitForExit(this.slimmerWaitForExit) ? (ExitCode)process.ExitCode : ExitCode.FailTimeout;

            process.OutputDataReceived -= handler;
            process.Close();

            return slimerOutput.ToString();
        }

        // optimizes the screenshot and creates the result from slimer output
        private CaptureResult GetCaptureResult(string slimerOutput, string url, string screenshotPath, string screenshotUrl, ExitCode exit)
        {
            if (!File.Exists(screenshotPath))
                return null;

            // regular expression is used to parse the output of the slimer
            Match result = OutputRegex.Match(slimerOutput);
            if (!result.Success)
                return null;

            // Screenshot over 20MB will timeout anyway so I'll not even try them.
            long screenshotLength = new FileInfo(screenshotPath).Length;
            if (screenshotLength < 20000000 && OptimizeScreenshot(screenshotPath))
                screenshotLength = new FileInfo(screenshotPath).Length;
            else if (exit != ExitCode.SuccessTimeout)
                exit = ExitCode.SuccessNotOptimized;
            
            return new CaptureResult()
            {
                Page = url,
                PageTitle = result.Groups[PageTitle].Value,
                PageCount = int.Parse(result.Groups[PageCount].Value, CultureInfo.InvariantCulture),
                PageSize = long.Parse(result.Groups[PageSize].Value, CultureInfo.InvariantCulture),
                Image = screenshotUrl,
                ImageSize = screenshotLength,
                Exit = exit,
                DetailsPath = Path.GetFileNameWithoutExtension(screenshotPath),
                ImagePath = Path.GetFileName(screenshotPath),
            };
        }

        // executes PngQuant that optimizes the screenshot
        private bool OptimizeScreenshot(string screenshotPath)
        {
            var processInfo = new ProcessStartInfo(Utilities.PngQuantExecutable,
                    string.Format("{0} \"{1}\"", Utilities.PngQuantArguments, screenshotPath));
            processInfo.UseShellExecute = false;

            var process = Process.Start(processInfo);
            var result = process.WaitForExit(this.pngQuantWaitForExit);

            process.Close();
            return result;
        }
    }
}