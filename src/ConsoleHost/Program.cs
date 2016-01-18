using Microsoft.Scripting.JavaScript;
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
                var fortytwo = engine.Converter.FromInt32(42);
                engine.SetGlobalVariable(nameof(fortytwo), fortytwo);
                var val = engine.GetGlobalVariable(nameof(fortytwo));
                var n = engine.Converter.ToInt32(val);
                System.Diagnostics.Debug.Assert(n == 42, "n should be 42");

                var sample = new SampleClass();
                sample.Number = 42;
                var sampleJS = engine.CreateExternalObject(sample, (data) => {});
                engine.SetGlobalVariable(nameof(sample), sampleJS);
                var s = engine.GetGlobalVariable(nameof(sample));
                // no way to get the external object back from s

                engine.SetGlobalFunction("echo", Echo);
                var fn = engine.EvaluateScriptText(@"(function() {
    echo('{0}, {1}!', 'Hello', 'world');
    fortytwo += 7;
    // fortytwo = sample.Number + 7; // wont work
    /* 
    echo('{0}!', fortytwo);
    sample.Name = 'that is not my Name';
    sample.name = 'that is not my name';
    echo('Hello Sample. {0}', sample);
    echo('Hello Sample. {0}', sample.Name);
    echo('Hello Sample. {0}', sample.name);
    */
})();");
                fn.Invoke(Enumerable.Empty<JavaScriptValue>());

                var n7 = engine.Converter.ToInt32(engine.GetGlobalVariable(nameof(fortytwo)));
                System.Diagnostics.Debug.Assert(n7 == 42+7, "n should be 49");

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
}
