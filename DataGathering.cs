
using Perfy.Testing;
using System.Text.Json;

namespace Perfy.DataGathering
{
    interface IDataSource
    {
        public abstract TestCase? GetNextTest();
        public event Action? EndOfData;
    }
    class JSDataFile : IDataSource
    {
        readonly Queue<TestCase> data;
        public event Action? EndOfData;

        public JSDataFile(string filePath)
        {
            if(!File.Exists(filePath))
                throw new FileNotFoundException(filePath);
            data = JsonSerializer.Deserialize<Queue<TestCase>>(File.ReadAllText(filePath)) ?? throw new Exception("JSON interpretation failed");
        }
        public TestCase? GetNextTest()
        {
            if(data.Count == 0)
                return null;
            TestCase next = data.Dequeue();
            if (data.Count == 0)
                EndOfData?.Invoke();
            return data.Dequeue();
        }
    }
}