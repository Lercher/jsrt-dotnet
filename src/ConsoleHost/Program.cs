﻿using Microsoft.Scripting.JavaScript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConsoleHost
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            using (var runtime = new JavaScriptRuntime())
            using (var engine = runtime.CreateEngine())
            {
                engine.SetGlobalFunction("echo", Echo);
                var pt = new Point { X = 18, Y = 27 };
                engine.SetGlobalVariable("pt", engine.Converter.FromObject(pt));

                var fn = engine.EvaluateScriptText(@"(function() {
    echo('{0}, {1}!', 'Hello', 'world');
    //echo(pt.X);
    //echo(pt.Y);
    echo(pt.ToString());
})();");
                fn.Invoke(Enumerable.Empty<JavaScriptValue>());

                dynamic fnAsDynamic = fn;
                fnAsDynamic.foo = 24;
                dynamic global = engine.GlobalObject;
                global.echo("{0}, {1}, via dynamic!", "Hey there", "world");

                dynamic echo = global.echo;
                echo("Whoa, {0}, that {1} {2}???", "world", "really", "worked");

                foreach (dynamic name in global.Object.getOwnPropertyNames(global))
                {
                    echo(name);
                }
            }
            Console.ReadLine();
        }

        static JavaScriptValue Echo(JavaScriptEngine engine, bool construct, JavaScriptValue thisValue, IEnumerable<JavaScriptValue> arguments)
        {
            Console.WriteLine(arguments.First().ToString(), (object[])arguments.Skip(1).ToArray());
            return engine.UndefinedValue;
        }
    }

    public class Point
    {
        public int X
        {
            get;
            set;
        }
        
        public int Y
        {
            get;
            set;
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }
    }
}
