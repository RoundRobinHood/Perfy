using Perfy.DataGathering;
using Perfy.Display;
using Perfy.ProcessHandling;
using Perfy.Testing;

namespace Perfy
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Interpreter.ArgDictionary argDictionary = Interpreter.GrabArgs(args);
            if (!argDictionary.Flags.TryGetValue("runCommand", out string? rcmd))
                rcmd = ":script :inputs";

            if (argDictionary.UnnamedArgs.Count == 0)
                DisplayMethods.PrintWarning("No scripts provided to test.");
            List<Tester> testers = [];
            IDataSource dataSource;
            if (argDictionary.Flags.TryGetValue("onecase", out string? singleCase))
            {
                Dictionary<string, string> colonVals = Interpreter.ColonString(singleCase);
                if(colonVals.Count != 1)
                {
                    DisplayMethods.PrintError($"Invalid onecase value {singleCase}");
                    Environment.Exit(0);
                }
                string key = colonVals.Keys.ToArray()[0];
                dataSource = new SingularSource(new TestCase()
                {
                    Inputs = key.Split(','),
                    Outputs = colonVals[key].Split(',')
                });

            }
            else if (argDictionary.Flags.TryGetValue("datasource", out string? data))
            {
                Dictionary<string, string> sourceConfig = Interpreter.ColonString(data);
                if(sourceConfig.TryGetValue("jsfile", out string? filePath))
                    dataSource = new JSDataFile(filePath);
                else
                {
                    DisplayMethods.PrintError("Invalid data flag value.");
                    Environment.Exit(0);
                    return;
                }
                //else if(sourceConfig.TryGetValue("case", out string? Input))
                
            }
            else
            {
                dataSource = new SingularSource(new TestCase()
                {
                    Inputs = [],
                    Outputs = []
                });
            }

            foreach (string script in argDictionary.UnnamedArgs)
            {

                ProcessHandler processHandler;
                if (argDictionary.Flags.TryGetValue("timeoutms", out string? timeout))
                    processHandler = new ProcessHandler(rcmd.Replace(":script", script), Interpreter.SafeInterpret<int>(timeout, "Timeout value must be a positive integer", (int i) => i > 0));
                else
                    processHandler = new ProcessHandler(rcmd.Replace(":script", script));

                Tester tester;
                if (argDictionary.Flags.TryGetValue("batch", out string? batch))
                    tester = new BatchTester(dataSource.Clone(), processHandler, Interpreter.SafeInterpret<int>(batch, "Batch size must be a positive integer", (int i) => i > 0));
                else
                    tester = new SingleProcessTester(dataSource.Clone(), processHandler);

                //tester.OnTestCaseReturned += EchoTest;
                //tester.TestingEnded += (self, results) =>
                //{
                //    Console.WriteLine("\nOverall Statistics:");
                //    Console.WriteLine("Accuracy: {0}%\tTime: {1}ms", results.AverageAccuracy * 100, results.TotalElapsedMs);
                //};
                testers.Add(tester);
            }
            DisplayHandler displayHandler = new Racer([..testers], 400);
            Thread displayThread = new Thread(displayHandler.Listen);
            Thread[] threads = new Thread[testers.Count];
            for(int i = 0;i < threads.Length;i++)
                threads[i] = new Thread(testers[i].Start);
            displayThread.Start();
            foreach (Thread t in threads)
                t.Start();
            foreach (Thread t in threads)
                t.Join();
            displayThread.Join();
        }

        
    }
}
