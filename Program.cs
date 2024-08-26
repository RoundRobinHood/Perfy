using Perfy.DataGathering;
using Perfy.Display;
using Perfy.ProcessHandling;
using Perfy.Testing;

namespace Perfy
{
    internal class Program
    {
        static void EchoTest(TestResult result)
        {
            if(result.Accuracy < 0.5)
            {
                DisplayMethods.PrintError($"Test case failed: {result.Accuracy * 100}% accuracy\n");
                DisplayMethods.PrintError($"Inputs:\t{String.Join(' ', result.Test.Inputs)}");
                DisplayMethods.PrintError($"Expected Output:\t{String.Join(',', result.Test.Outputs)}");
                DisplayMethods.PrintError($"Returned Output:\t{String.Join(',', result.ScriptOutputs)}");
            }
            else
            {
                Console.WriteLine($"\nTest case succeeded: {result.Accuracy * 100}% accuracy, {result.ElapsedMs}ms");
                Console.WriteLine($"Inputs:\t{String.Join(' ', result.Test.Inputs)}");
            }
        }
        static void Main(string[] args)
        {
            Interpreter.ArgDictionary argDictionary = Interpreter.GrabArgs(args);
            string? rcmd = null;
            if(!argDictionary.Flags.TryGetValue("runCommand", out rcmd))
            {
                DisplayMethods.PrintError("Please define the run command for script execution.");
                return;
            }
            if (argDictionary.UnnamedArgs.Count == 0)
                DisplayMethods.PrintWarning("No scripts provided to test.");
            foreach(string script in argDictionary.UnnamedArgs)
            {
                IDataSource dataSource;
                if(argDictionary.Flags.TryGetValue("datasource", out string? data) && Interpreter.ColonString(data).TryGetValue("jsfile", out string? filePath))
                    dataSource = new JSDataFile(filePath);
                else
                {
                    DisplayMethods.PrintError("Please provide a data source.");
                    Environment.Exit(0);
                    return;
                }

                ProcessHandler processHandler;
                if (argDictionary.Flags.TryGetValue("timeoutms", out string? timeout))
                    processHandler = new ProcessHandler(rcmd.Replace(":script", script), Interpreter.SafeInterpretPositiveInt(timeout, "Timeout value must be a positive integer"));
                else
                    processHandler = new ProcessHandler(rcmd.Replace(":script", script));

                Tester tester;
                if (argDictionary.Flags.TryGetValue("batch", out string? batch))
                    tester = new BatchTester(dataSource, processHandler, Interpreter.SafeInterpretPositiveInt(batch, "Batch size must be a positive integer"));
                else
                    tester = new SingleProcessTester(dataSource, processHandler);

                tester.OnTestCaseReturned += EchoTest;
                tester.TestingEnded += (self, results) =>
                {
                    Console.WriteLine("\nOverall Statistics:");
                    Console.WriteLine("Accuracy: {0}%\tTime: {1}ms", results.AverageAccuracy * 100, results.TotalElapsedMs);
                };
                tester.Start();
            }
        }

        
    }
}
