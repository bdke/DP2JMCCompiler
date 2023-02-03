using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JMCCompiler
{
    internal struct Arguments
    {
        public ArgType ArgType;
        public string? Value;
        public bool isArg;
    }

    internal enum ArgType
    {
        Compile, CompilePath, OutputPath, Namespace, MainFile, OutputMode,
        Trim
    }
}
