using Microsoft.Scripting.JavaScript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConsoleHost
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Color codes: Black - C# Console output, Blue - JS Echo callback, Red - JS Runtime Exception");
            Console.WriteLine();
            var runtime = new JavaScriptRuntime();
            var engine = runtime.CreateEngine();
            using (var context = engine.AcquireContext())
            {
                engine.SetGlobalFunction("echo", Echo);
                engine.AddTypeToGlobal<Point3D>();
                engine.AddTypeToGlobal<Point>();
                engine.AddTypeToGlobal<Toaster>();
                engine.AddTypeToGlobal<ToasterOven>();
                var pt = new Point3D { X = 18, Y = 27, Z = -1 };
                engine.SetGlobalVariable("pt", engine.Converter.FromObject(pt));
                engine.RuntimeExceptionRaised += (sender, e) =>
                {
                    dynamic error = engine.GetAndClearException();
                    dynamic glob = engine.GlobalObject;
                    var color = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    var err = glob.JSON.stringify(error);
                    if ((string)err == "{}")
                        err = engine.Converter.ToString(error);
                    Console.WriteLine("Script error occurred: {0}", (string)err);
                    Console.ForegroundColor = color;
                };

                var fn = engine.EvaluateScriptText(@"
echo('Hello Toaster outer');
(function() {
    echo('----- Point3D');
    var o = new Point3D(1, 2, 3);
    echo('o.toString={0}', o.ToString());
    o.X = 254;
    echo('o.X={0}', o.X);
    o.Y = 189;
    o.Z = -254.341;
    echo('o after mutation? {0}', o.ToString());
    echo('Hello, world? -> {0}, {1}!', 'Hello', 'world');
    echo('pt.X={0}', pt.X);
    echo('pt.Y={0}', pt.Y);
    echo('pt.toString={0}', pt.ToString());
    pt.Y = 207;
    echo('pt.toString={0}', pt.ToString());

/*
    echo('----- Toaster');
    var tb = new Toaster();
    tb.addEventListener('toastcompleted', function(e) {
        echo('Direct Toaster Toast is done!');
        echo('{0}', JSON.stringify(e));
    });
    tb.StartToasting(); 
*/

    echo('----- ToasterOven');
    var t = new ToasterOven();
    t.addEventListener('toastcompleted2', function(e) {
        echo('Derived ToasterOven Toast is done!');
        echo('{0}', JSON.stringify(e));
    });
    t.addEventListener('loaftoasted', function(e) {
        echo('Loaf is done!');
        echo('{0}', JSON.stringify(e.e));
        echo('Cooked {0} pieces', e.e.PiecesCooked);
    });
    t.addEventListener('toastcompleted', function(e) {
        echo('Base Toaster Toast is done!');
        echo('{0}', JSON.stringify(e));
    });
    t.StartToasting();

    echo('----- script done');
})();");
                fn.Invoke(Enumerable.Empty<JavaScriptValue>());
                var obj = engine.CreateObject();

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
            var c = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(fmt, args);
            Console.ForegroundColor = c;
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
