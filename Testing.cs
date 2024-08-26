
using Perfy.DataGathering;
using Perfy.ProcessHandling;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks.Dataflow;

namespace Perfy.Testing
{
    struct TestCase
    {
        public string[] Inputs { get; set; }
        public string[] Outputs { get; set; }
    }
    struct TestResult(TestCase test, double accuracy, long elapsedMs, string errors, string[] scriptOutputs)
    {
        public TestCase Test = test;
        public string[] ScriptOutputs = scriptOutputs;
        public double Accuracy = accuracy;
        public long ElapsedMs = elapsedMs;
        public string Errors = errors;
    }
    delegate void testCaseReturned(TestResult result);
    class TesterEndArgs(double averageAccuracy, long totalElapsedMs) : EventArgs
    {
        public double AverageAccuracy = averageAccuracy;
        public long TotalElapsedMs = totalElapsedMs;
    }
    abstract class Tester(IDataSource dataSource, ProcessHandler handler)
    {
        protected readonly IDataSource DataSource = dataSource;
        protected readonly ProcessHandler Handler = handler;
        public abstract void Start();
        public event testCaseReturned? OnTestCaseReturned;
        public event EventHandler<TesterEndArgs>? TestingEnded;

        protected void CompleteTestCase(TestResult result) => OnTestCaseReturned?.Invoke(result);
        protected void EndTesting(TesterEndArgs testerEndArgs) => TestingEnded?.Invoke(this, testerEndArgs);
    }
    class BatchTester(IDataSource dataSource, ProcessHandler handler, int batchSize) : Tester(dataSource, handler)
    {
        public readonly int BatchSize = batchSize;
        long TotalElapsedMs = 0;
        int TestCaseCount = 0;
        double TotalAccuracy = 0;

        void RefHandler(TestCase test, ref TestResult result)
        {
            result = Handler.HandleTestCase(test);
        }

        public override void Start()
        {
            if (BatchSize <= 0)
                throw new InvalidOperationException("Batch size for batch testing must be > 0");
            while(true)
            {
                List<TestCase> batch = [];
                TestCase? t;
                while ((t = DataSource.GetNextTest()) != null && batch.Count < BatchSize)
                    batch.Add(t.Value);
                Thread[] threads = new Thread[batch.Count];
                TestResult[] results = new TestResult[batch.Count];
                TestCase[] batchArray = [.. batch];
                for(int i = 0;i < batch.Count; i++)
                {
                    int localIndex = i;
                    threads[i] = new Thread(() => results[localIndex] = Handler.HandleTestCase(batchArray[localIndex]));
                }
                foreach(Thread thread in threads)
                    thread.Start();
                for(int i = 0;i < batchArray.Length; i++)
                {
                    threads[i].Join();
                    TotalElapsedMs += results[i].ElapsedMs;
                    TotalAccuracy += results[i].Accuracy;
                    CompleteTestCase(results[i]);
                }
                TestCaseCount += batch.Count;
                if (t == null)
                    break;
            }
            EndTesting(new TesterEndArgs(TotalAccuracy / TestCaseCount, TotalElapsedMs));
        }
        
    }
    class SingleProcessTester(IDataSource dataSource, ProcessHandler handler) : Tester(dataSource, handler)
    {
        long TotalElapsedMs = 0;
        int TestCaseCount = 0;
        double TotalAccuracy = 0;
        

        public override void Start()
        {
            TestCase? t;
            
            while ((t = DataSource.GetNextTest()) != null)
            {
                TestResult result = Handler.HandleTestCase(t.Value);

                TotalAccuracy += result.Accuracy;
                TotalElapsedMs += result.ElapsedMs;
                TestCaseCount++;
                
                CompleteTestCase(result);
            }
            EndTesting(new TesterEndArgs(TotalAccuracy / TestCaseCount, TotalElapsedMs));
        }

    }
}