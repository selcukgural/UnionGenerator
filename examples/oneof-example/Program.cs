using System;
using OneOf;
using UnionGenerator.OneOfExtensions;

namespace OneOfExample
{
    internal class Program
    {
        static void Main()
        {
            OneOf<string,string> one = OneOf<string,string>.FromT0("hello");
            var generated = one.ToGeneratedResult<object, string, string>();
            Console.WriteLine(generated?.ToString());
        }
    }
}

