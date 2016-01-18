using System;

namespace ConsoleHost
{
    class SampleClass
    {
        public string Name { get; set; } = "My name is John Doe";
        public int Number { get; set; } = 42;
        public SampleClass2 Inner { get; set; } = new SampleClass2();
    }

    class SampleClass2
    {
        public string SecondName { get; set; } = "My 2nd name is Robert the 3rd";
    }
}
