
using Perfy.Testing;

namespace Perfy.Display
{
    static class DisplayMethods
    {
        public static void StrikeThrough(char c)
        {
            string s = "";
            for (int i = 0; i < Console.WindowWidth; i++)
                s += c;
            Console.WriteLine(s);
        }
        public static void PrintInColor(string message, ConsoleColor color)
        {
            ConsoleColor currentColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(message);
            Console.ForegroundColor = currentColor;
        }
        public static void PrintWarning(string message) => PrintInColor(message + "\n", ConsoleColor.Yellow);
        public static void PrintError(string message) => PrintInColor(message + "\n", ConsoleColor.Red);
    }
    abstract class DisplayHandler(Tester[] testers, int caseCount)
    {
        protected Tester[] Testers = testers;
        protected int CaseCount = caseCount;
        public abstract void Listen();
    }
    class Racer : DisplayHandler
    {
        readonly TestResult[,] Results;

        readonly TesterEndArgs[] EndArgs;
        int PrintedResults = 0;
        readonly int[] ReceivedResults;
        bool[] TestingEnded;
        volatile bool Process = false;
        public Racer(Tester[] testers, int caseCount) : base(testers, caseCount)
        {
            TestingEnded = new bool[Testers.Length];
            ReceivedResults = new int[Testers.Length];
            Results = new TestResult[Testers.Length,CaseCount];
            EndArgs = new TesterEndArgs[Testers.Length];
            for(int i = 0;i < Testers.Length;i++)
            {
                int localIndex = i;
                Testers[i].OnTestCaseReturned += (test) => { Results[localIndex, ReceivedResults[localIndex]++] = test; Process = true; };
                Testers[i].TestingEnded += (self, args) => { EndArgs[localIndex] = args; Process = true; TestingEnded[localIndex] = true; };
            }
        }
        bool Finished()
        {
            foreach (bool flag in TestingEnded)
                if (!flag)
                    return false;
            return true;
        }
        public override void Listen()
        {
            while(!Finished())
            {
                if (Process)
                {
                    while (ReceivedResults.Min() > PrintedResults)
                        PrintComparison(PrintedResults++);
                    Process = false;
                }
                Thread.Sleep(100);
            }
            PrintResults();
        }
        List<(int, int)> ErrorCases = [];
        void PrintComparison(int caseId)
        {
            Console.WriteLine();

            DisplayMethods.StrikeThrough('=');
            bool[] isAccurate = new bool[Testers.Length];
            for (int i = 0; i < Testers.Length; i++)
                isAccurate[i] = Results[i, caseId].Accuracy > 0.5 && Results[i, caseId].Errors.Length == 0;
            int minIndex = -1;
            string[] messages = new string[Testers.Length];
            for(int i = 0;i < Results.GetLength(0); i++)
            {
                if (isAccurate[i] && (minIndex == -1 || Results[i, caseId].ElapsedMs < Results[minIndex, caseId].ElapsedMs))
                {
                    minIndex = i;
                }
                messages[i] = $"Accuracy: {Results[i, caseId].Accuracy * 100}%, Time taken: {Results[i, caseId].ElapsedMs}ms\t";
            }
            
            for (int i = 0; i < Testers.Length; i++)
                if (!isAccurate[i])
                {
                    DisplayMethods.PrintInColor(messages[i], ConsoleColor.Red);
                    ErrorCases.Add((i, caseId));
                }
                else if (i == minIndex)
                    DisplayMethods.PrintInColor(messages[i], ConsoleColor.Green);
                else
                    Console.Write(messages[i]);
            Console.WriteLine();
            DisplayMethods.StrikeThrough('=');
        }
        void PrintResults()
        {
            while (ReceivedResults.Min() > PrintedResults)
                PrintComparison(PrintedResults++);
            Console.WriteLine();
            DisplayMethods.StrikeThrough('=');
            Console.WriteLine("\nTesting ended.\n");
            
            Console.WriteLine();
            int minIndex = -1;
            string[] messages = new string[Testers.Length];
            for (int i = 0; i < Testers.Length; i++)
            {
                if (minIndex == -1 || EndArgs[i].TotalElapsedMs < EndArgs[minIndex].TotalElapsedMs)
                {
                    minIndex = i;
                }
                messages[i] = $"Average Accuracy: {EndArgs[i].AverageAccuracy * 100}%, Time taken: {EndArgs[i].TotalElapsedMs}ms";
            }
            for (int i = 0;i < EndArgs.Length;i++)
                if(i == minIndex)
                    DisplayMethods.PrintInColor(messages[i] + ": fastest\t", ConsoleColor.Green);
                else
                    Console.Write(messages[i] + ": {0:F2}% slower\t", (EndArgs[i].TotalElapsedMs / (double)EndArgs[minIndex].TotalElapsedMs) * 100 - 100);
            Console.WriteLine("\n");
            DisplayMethods.StrikeThrough('=');
            if (ErrorCases.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                DisplayMethods.StrikeThrough('=');
                Console.WriteLine("Issues found in some test cases:\n");
                foreach((int, int) index in ErrorCases)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("Test result {0} from script {1}", index.Item2 + 1, index.Item1 + 1);
                    if (Results[index.Item1, index.Item2].Errors.Length > 0)
                    {
                        Console.WriteLine("Errors were thrown:");
                        DisplayMethods.PrintError(Results[index.Item1, index.Item2].Errors);
                    }
                    else
                    {
                        Console.WriteLine("Wrong output returned (Accuracy {0}%)", Results[index.Item1, index.Item2].Accuracy * 100);
                        Console.WriteLine("Script input: {0}\nExpected output: {1}\n", String.Join(',', Results[index.Item1, index.Item2].Test.Inputs), String.Join(',', Results[index.Item1, index.Item2].Test.Outputs));
                        DisplayMethods.PrintError($"Script output: {String.Join(',', Results[index.Item1, index.Item2].ScriptOutputs)}");
                    }
                }
            }
        }
    }
}