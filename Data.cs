namespace JMCCompiler
{
    internal struct Arguments
    {
        public ArgType ArgType { get; set; }
        public string? Value { get; set; }
        public bool isArg { get; set; }
    }

    internal enum ArgType
    {
        Compile, CompilePath, OutputPath, Namespace, MainFile, OutputMode,
        Trim
    }
}
