using Perfy.DataGathering;
using Perfy.Display;
using System.Numerics;

namespace Perfy
{
    static class Interpreter
    {
        public static T SafeInterpret<T>(string input, string exitMessage, Func<T, bool>? validator = null) where T : IParsable<T>
        {
            if(T.TryParse(input, null, out T? result) && (validator == null || validator(result)))
                return result;
            else
            {
                DisplayMethods.PrintError(exitMessage);
                Environment.Exit(0);
                return default;
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
            { "-time", "timeoutms" },
            { "-timeout", "timeoutms" },
            { "-d", "datasource" },
            { "-data", "datasource" },
            { "-batch", "batch" },
            { "-input", "onecase" },
            { "-onecase", "onecase" }
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
                    if (sides.Length > 2)
                        DisplayMethods.PrintError($"Definition \"{def}\" contains multiple colons");
                    else
                        DisplayMethods.PrintError($"Error at \"{s}\", expected dictionary (ex. a:3;b:4)");
                    Environment.Exit(0);
                }
                dict[sides[0]] = sides[1];
            }
            return dict;
        }
    }
}