using Perfy.DataGathering;
using Perfy.Display;

namespace Perfy
{
    static class Interpreter
    {
        public static int SafeInterpretPositiveInt(string input, string exitMessage)
        {
            if(int.TryParse(input, out int result) && result > 0)
                return result;
            else
            {
                DisplayMethods.PrintError(exitMessage);
                Environment.Exit(0);
                return -1;
            }
        }
        public struct ArgDictionary(Dictionary<string, string> flags, List<string> unnamedArgs)
        {
            public Dictionary<string, string> Flags = flags;
            public List<string> UnnamedArgs = unnamedArgs;
        }
        public static readonly Dictionary<string, string> FlagNames = new()
        {
            { "-rcmd", "runCommand" },
            { "-runCommand", "runCommand" },
            { "-time", "timeScript" },
            { "-d", "datasource" },
            { "-data", "datasource" },
            { "-batch", "batch" }
        };
        public static ArgDictionary GrabArgs(string[] args)
        {
            Dictionary<string, string> flags = [];
            List<string> unnamedArgs = [];
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i][0] != '-')
                {
                    unnamedArgs.Add(args[i]);
                    continue;
                }
                if (i + 1 >= args.Length)
                {
                    DisplayMethods.PrintError($"Flag {args[i]} doesn't have a value");
                    Environment.Exit(0);
                }
                if (args[i + 1][0] == '-')
                {
                    DisplayMethods.PrintError($"Flag {args[i]} value cannot be another flag name");
                    Environment.Exit(0);
                }
                if (FlagNames.TryGetValue(args[i], out string? value))
                    flags[value] = args[i + 1];
                else
                    DisplayMethods.PrintWarning($"Warning: Unknown flag \"{args[i]}\"");
                i++;
            }
            return new ArgDictionary(flags, unnamedArgs);
        }
        public static Dictionary<string, string> ColonString(string s)
        {
            string[] definitions = s.Split(';');
            Dictionary<string, string> dict = [];
            foreach(string def in definitions)
            {
                string[] sides = def.Split(':');
                if(sides.Length != 2)
                {
                    DisplayMethods.PrintError($"Definition \"{def}\" contains multiple colons");
                    Environment.Exit(0);
                }
                dict[sides[0]] = sides[1];
            }
            return dict;
        }
    }
}