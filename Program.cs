namespace JMCCompiler
{
    internal class Program
    {
        static string CompilePath = ".";
        static string OutputPath = ".";
        static string Namespace = "main";
        static string Mainfile = "main";

        const string Version = "v0.0.1";
        static bool IsSingle = true;
        static bool IsTrim = false;

        static List<Arguments> Arguments = new();
        // -h | compile | -p <files> | -o <path> | -ms | -mm | -n <namespace> | -r <filename> | -v | -t
        static async Task<int> Main(string[] args)
        {
            if (args.Length == 0)
            {
                WriteError("[ERROR] Missing command");
            }

            if (args.Contains("-h") || 
                args.Contains("--h") ||
                args.Contains("-help") ||
                args.Contains("--help"))
            {
                Help();
                return 0;
            }

            if (args.Contains("-v") || 
                args.Contains("--v") || 
                args.Contains("--version") || 
                args.Contains("-version"))
            {
                Console.WriteLine(Version);
                return 0;
            }

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (arg.StartsWith('-') || arg.StartsWith("--"))
                {
                    switch (arg)
                    {
                        case "-p":
                            if (args.Length < i + 2)
                                WriteError("[ERROR] Missing input <path>");
                            Arguments.Add(new Arguments
                            {
                                Value = args[i + 1],
                                ArgType = ArgType.CompilePath,
                                isArg = true
                            });
                            i++;
                            continue;
                        case "-o":
                            if (args.Length < i + 2)
                                WriteError("[ERROR] Missing input <path>");
                            Arguments.Add(new Arguments
                            {
                                Value = args[i + 1],
                                ArgType = ArgType.OutputPath,
                                isArg = true
                            });
                            i++;
                            continue;
                        case "-n":
                            if (args.Length < i + 2)
                                WriteError("[ERROR] Missing input <namespace>");
                            Arguments.Add(new Arguments
                            {
                                Value = args[i + 1],
                                ArgType = ArgType.Namespace,
                                isArg = true
                            });
                            i++;
                            continue;
                        case "-f":
                            if (args.Length < i + 2)
                                WriteError("[ERROR] Missing input <filename>");
                            Arguments.Add(new Arguments
                            {
                                Value = args[i + 1],
                                ArgType = ArgType.MainFile,
                                isArg = true
                            });
                            i++;
                            continue;
                        case "-ms":
                            Arguments.Add(new Arguments
                            {
                                Value = "single",
                                ArgType = ArgType.OutputMode,
                                isArg = true
                            });
                            continue;
                        case "-mm":
                            Arguments.Add(new Arguments
                            {
                                Value = "multiple",
                                ArgType = ArgType.OutputMode,
                                isArg = true
                            });
                            continue;
                        case "-t":
                            Arguments.Add(new Arguments
                            {
                                ArgType = ArgType.Trim,
                                isArg = true
                            });
                            continue;
                        default:
                            WriteError($"[ERROR] Unspported argument '{arg}'");
                            break;
                    }
                }
                else
                {
                    switch (arg)
                    {
                        case "compile":
                            Arguments.Add(new Arguments()
                            {
                                ArgType = ArgType.Compile,
                                isArg = false
                            });
                            continue;
                        default: 
                            WriteError($"[ERROR] Unknown Command '{arg}'");
                            break;
                    }
                }
            }

            var arguments = from i in Arguments where i.isArg select i;
            var commands = from i in Arguments where !i.isArg select i;

            foreach (var arg in arguments)
            {
                switch (arg.ArgType) 
                {
                    case ArgType.CompilePath:
                        CompilePath = arg.Value;
                        continue;
                    case ArgType.OutputPath:
                        OutputPath = arg.Value;
                        continue;
                    case ArgType.MainFile:
                        Mainfile = arg.Value;
                        continue;
                    case ArgType.OutputMode:
                        IsSingle = arg.Value == "single";
                        continue;
                    case ArgType.Trim:
                        IsTrim = true;
                        continue;
                    default:
                        continue;
                }
            }

            foreach(var command in commands)
            {
                switch (command.ArgType)
                {
                    case ArgType.Compile:
                        await Task.Run(Compile);
                        continue;
                    default:
                        continue;
                }
            }
            return 0;
        }

        static void Help()
        {
            var s = 
            """
            Usage:
                <command> [args]
            Argments:
                -v, -version:
                    The version of the compiler.
                -h, -help:
                    Shows this message.
                -p <path>:
                    Specify the compile path (with pack.mcmeta).
                -n <namespace>:
                    Change the output folder namespace.
                -f <filename>:
                    Specify main file name (default is 'main').
                -o <path>:
                    Specify the output folder path.
                -ms, -mm:
                    Use Single File mode for -ms, Use Multiple File mode for -mm.
                -t:
                    Trim all lines of the function.
            Commands:
                compile:
                    Compile the datapack to JMC.
            """;
            Console.WriteLine(s);
        }

        static async Task Compile()
        {
            if (!Path.Exists(Path.Combine(CompilePath, "pack.mcmeta")))
                WriteError($"[ERROR] Path does not contain 'pack.mcmeta'");

            var files = DirSearch(CompilePath);
            var functionsPath = from i in files where i.EndsWith(".mcfunction") select i;
            var mcfunctions = new Dictionary<string, string[]>() { };

            foreach (var functionPath in functionsPath)
            {
                var edited = functionPath.Remove(0, CompilePath.Length + 1).Split("\\")[3..];
                var directory = string.Join('\\',edited);
                var data = (await File.ReadAllTextAsync(functionPath)).Split('\n').ToArray();
                mcfunctions.Add(directory, data);
            }

            if (Directory.Exists(Namespace))
                Directory.Delete(Namespace, true);
            Directory.CreateDirectory(Namespace);

            foreach (var mcfunction in mcfunctions)
            {
                var path = mcfunction.Key.Split("\\").ToList();
                var last = path.Last();
                var fullpath = string.Join('.', path);

                if (path.Count == 1 || IsSingle)
                {
                    var value = (from i in mcfunction.Value
                                 select (i.Trim() != string.Empty && !i.StartsWith("#")) 
                                 ? "\t" + i.Trim() + ";" 
                                 : "\t" + i.Replace("#","//"))
                                 .ToList();
                    if (IsTrim)
                        value = (from i in value where i.Trim() != string.Empty select "\t" + i.Trim()).ToList();

                    value.Insert(0, "function " + fullpath.Remove(fullpath.Length - 11) + "() {");
                    value.Add("}");
                    value.Add("");

                    var mainPath = Path.Combine(OutputPath ,Namespace, $"{Mainfile}.jmc");

                    if (!Path.Exists(mainPath))
                        await File.WriteAllLinesAsync(mainPath, value);
                    else
                        await File.AppendAllLinesAsync(mainPath, value);
                }
                else
                {
                    var value = (from i in mcfunction.Value
                                 select (i.Trim() != string.Empty && !i.StartsWith("#")) 
                                 ? "\t" + i.Trim() + ";"
                                 : "\t" + i.Replace("#", "//"))
                                 .ToList();

                    value.Insert(0, "function " + fullpath.Remove(fullpath.Length - 11) + "() {");
                    value.Add("}");
                    value.Add("");
                    var stringPath = new List<string>
                    {
                        OutputPath,
                        Namespace,
                        $"{string.Join('.',path.GetRange(0, path.Count - 1))}.jmc"
                    };
                    var filePath = Path.Combine(stringPath.ToArray());

                    if (!Path.Exists(filePath))
                        await File.WriteAllLinesAsync(filePath, value);
                    else
                        await File.AppendAllLinesAsync(filePath, value);
                }
            }
        }

        static void WriteError(object msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(msg);
            Console.ResetColor();
            Environment.Exit(1);
        }

        static string[] DirSearch(string sDir)
        {
            List<string> files = new();
            try
            {
                foreach (string f in Directory.GetFiles(sDir))
                {
                    files.Add(f);
                }
                foreach (string d in Directory.GetDirectories(sDir))
                {
                    files.AddRange(DirSearch(d));
                }
            }
            catch (Exception excpt)
            {
                WriteError(excpt.Message);
            }

            return files.ToArray();
        }
    }
}