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
                engine.AddTypeToGlobal<Point3D>();
                var pt = new Point3D { X = 18, Y = 27, Z = -1 };
                //engine.SetGlobalVariable("pt", engine.Converter.FromObject(pt));
                engine.RuntimeExceptionRaised += (sender, e) =>
                {
                    var error = engine.GetAndClearException();
                    dynamic glob = engine.GlobalObject;
                    var color = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Script error occurred: {0}", glob.JSON.stringify(error));
                    Console.ForegroundColor = color;
                };

                var fn = engine.EvaluateScriptText(@"(function() {
    var o = new Point3D(1, 2, 3);
    echo(o.toString());
    o.X = 254;
    echo('{0}', o.X);
    o.Y = 189;
    o.Z = -254.341;
    echo(o.ToString());
    echo('{0}, {1}!', 'Hello', 'world');
    //echo('{0}', pt.X);
    //echo('{0}', pt.Y);
    //echo('{0}', pt.ToString());
    //pt.Y = 207;
    //echo('{0}', pt.ToString());
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
            string fmt = arguments.First().ToString();
            object[] args = (object[])arguments.Skip(1).ToArray();
            Console.WriteLine(fmt, args);
            return engine.UndefinedValue;
        }
    }

    public class Point
    {
        public double X
        {
            get;
            set;
        }
        
        public double Y
        {
            get;
            set;
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }
    }

    public class Point3D : Point
    {
        public double Z
        {
            get;
            set;
        }

        public override string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }
    }
}
