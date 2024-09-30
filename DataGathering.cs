
using Perfy.Testing;
using System.Text.Json;

namespace Perfy.DataGathering
{
    interface IDataSource
    {
        public TestCase? GetNextTest();
        public event Action? EndOfData;
        public IDataSource Clone();
        public int ItemsLeft();
    }
    class InvalidSourceException(string filename, string message) : Exception
    {
        readonly string Filename = filename;
        readonly string ErrorMessage = message;
        public override string ToString()
        {
            return $"Error encountered with file \"{Filename}\":\"{ErrorMessage}\". Developer info:\n{base.ToString()}";
        }
    }
    class SingularSource(TestCase test) : IDataSource
    {
        TestCase Test = test;
        bool hasReturned = false;
        public event Action? EndOfData;
        public TestCase? GetNextTest()
        {
            if (!hasReturned)
            {
                hasReturned = true;
                EndOfData?.Invoke();
                return Test;
            }
            else
                return null;
        }
        public int ItemsLeft() => 1;
        public IDataSource Clone()
        {
            return new SingularSource(Test);
        }
    }
    class QueueSource(Queue<TestCase> data) : IDataSource
    {
        readonly Queue<TestCase> Data = data;
        public event Action? EndOfData;
        public TestCase? GetNextTest()
        {
            if (Data.Count == 0)
                return null;
            TestCase next = Data.Dequeue();
            if (Data.Count == 0)
                EndOfData?.Invoke();
            return Data.Dequeue();
        }
        public IDataSource Clone()
        {
            Queue<TestCase> recollect = [];
            TestCase[] read = [.. Data];
            for (int i = 0; i < Data.Count; i++)
                recollect.Enqueue(read[i]);
            return new QueueSource(recollect);
        }
        public int ItemsLeft()
        {
            return Data.Count;
        }
    }
    class JSDataFile : IDataSource
    {
        readonly Queue<TestCase> Data;
        public event Action? EndOfData;

        public JSDataFile(string filePath)
        {
            if(!File.Exists(filePath))
                throw new InvalidSourceException(filePath, "File doesn't exist");
            Data = JsonSerializer.Deserialize<Queue<TestCase>>(File.ReadAllText(filePath)) ?? throw new InvalidSourceException(filePath, "Invalid JSON syntax");
        }
        public TestCase? GetNextTest()
        {
            if(Data.Count == 0)
                return null;
            TestCase next = Data.Dequeue();
            if (Data.Count == 0)
                EndOfData?.Invoke();
            return Data.Dequeue();
        }
        public IDataSource Clone()
        {
            Queue<TestCase> recollect = [];
            TestCase[] read = [.. Data];
            for (int i = 0; i < Data.Count; i++)
                recollect.Enqueue(read[i]);
            return new QueueSource(recollect);
        }
        public int ItemsLeft()
        {
            return Data.Count;
        }
    }
}