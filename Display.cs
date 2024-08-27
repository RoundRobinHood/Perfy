
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
        void PrintComparison(int caseId)
        {
            Console.WriteLine();
            for(int i = 0;i < Results.GetLength(0); i++)
                Console.Write("Accuracy: {0}%, Time taken: {1}ms\t", Results[i, caseId].Accuracy * 100, Results[i, caseId].ElapsedMs);
            Console.WriteLine();
        }
        void PrintResults()
        {
            Console.WriteLine();
            for(int i = 0;i < EndArgs.Length;i++)
                Console.Write("Average Accuracy: {0}%, Time taken: {1}ms\t", EndArgs[i].AverageAccuracy * 100, EndArgs[i].TotalElapsedMs);
        }
    }
}