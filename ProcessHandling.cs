
using Perfy.Testing;
using System.Diagnostics;

namespace Perfy.ProcessHandling
{
    class ProcessHandler
    {
        public ProcessHandler(string cmd, int timeout = 10000)
        {
            string[] wordList = cmd.Split(' ');
            ApplicationName = wordList[0];
            Arguments = String.Join(' ', wordList.Skip(1));
            Timeout = timeout;
        }
        public string ApplicationName { get; private set; }
        public string Arguments { get; private set; }
        public int Timeout { get; private set; }
        public TestResult HandleTestCase(TestCase Test)
        {
            // 
            Process process = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ApplicationName,
                    Arguments = Arguments.Replace(":inputs", String.Join(' ', Test.Inputs)),
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                },
                EnableRaisingEvents = true,
            };
            TaskCompletionSource<bool> waitingTaskSrc = new();
            List<string> outputs = [];
            List<string> errors = [];

            process.Exited += (sender, args) => waitingTaskSrc.SetResult(true);
            process.OutputDataReceived += (sender, args) => { if (args.Data != null) outputs.Add(args.Data); };
            process.ErrorDataReceived += (sender, args) => { if (args.Data != null) errors.Add(args.Data); };

            process.Start();
            process.BeginOutputReadLine(); process.BeginErrorReadLine();
            Task RaceResult = Task.WhenAny(waitingTaskSrc.Task, Task.Delay(Timeout)).GetAwaiter().GetResult();

            TestResult result;
            if (RaceResult == waitingTaskSrc.Task)
            {
                process.WaitForExit();
                bool passed = true;
                if (outputs.Count != Test.Outputs.Length)
                {
                    passed = false;
                }
                else
                    for (int i = 0; i < outputs.Count; i++)
                    {
                        if (outputs[i] != Test.Outputs[i])
                        {
                            passed = false;
                            break;
                        }
                    }
                if (process.ExitCode != 0)
                {
                    errors.Add($"Perfy: test case returned exit code {process.ExitCode}");
                }
                result = new TestResult(Test, passed ? 1 : 0, (long)(process.ExitTime - process.StartTime).TotalMilliseconds, String.Join('\n', errors), [.. outputs]);
            }
            else
            {
                process.Kill();
                errors.Add($"Perfy: timeout of {Timeout}ms exceeded\n");
                result = new TestResult(Test, 0, Timeout, String.Join('\n', errors), [.. outputs]);
            }
            process.Dispose();

            return result;
        }
    }
}