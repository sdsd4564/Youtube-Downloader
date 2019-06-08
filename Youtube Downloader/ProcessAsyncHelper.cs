using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Youtube_Downloader;

// https://gist.github.com/AlexMAS/276eed492bc989e13dcce7c78b9e179d 참조
public static class ProcessAsyncHelper
{
    public static async Task<ProcessResult> RunProcessAsync(string command, string arguments, int timeout)
    {
        var result = new ProcessResult();
        var dialog = ProgressDialog.Instance;

        using (var process = new Process())
        {
            process.StartInfo.FileName = command;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;

            var outputBuilder = new StringBuilder();
            var outputCloseEvent = new TaskCompletionSource<bool>();

            process.OutputDataReceived += (s, e) =>
            {
                if (e.Data == null)
                {
                    outputCloseEvent.SetResult(true);
                }
                else
                {
                    if (dialog != null)
                    {
                        dialog.Dispatcher.Invoke(() =>
                        {
                            dialog.tbxProcess.Text += e.Data + "\t\n";
                            dialog.scvScroll.ScrollToEnd();
                        });
                    }
                    outputBuilder.AppendLine(e.Data);
                }
            };

            var isStarted = process.Start();
            if (!isStarted)
            {
                result.ExitCode = process.ExitCode;
                return result;
            }

            // Reads the output stream first and then waits because deadlocks are possible
            process.BeginOutputReadLine();

            // Creates task to wait for process exit using timeout
            var waitForExit = WaitForExitAsync(process, timeout);

            // Create task to wait for process exit and closing all output streams
            var processTask = Task.WhenAll(waitForExit, outputCloseEvent.Task);

            // Waits process completion and then checks it was not completed by timeout
            if (await Task.WhenAny(Task.Delay(timeout), processTask) == processTask && waitForExit.Result)
            {
                result.ExitCode = process.ExitCode;
                result.Output = outputBuilder.ToString();
                if (dialog != null)
                    dialog.tbxProcess.Text += "완료";
            }
            else
            {
                try
                {
                    // Kill hung process
                    process.Kill();
                }
                catch
                {
                    // ignored
                }
            }
        }

        return result;
    }

    private static Task<bool> WaitForExitAsync(Process process, int timeout)
    {
        return Task.Run(() => process.WaitForExit(timeout));
    }

    public struct ProcessResult
    {
        public int? ExitCode;
        public string Output;
    }
}