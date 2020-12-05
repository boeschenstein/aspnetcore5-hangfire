using System;

namespace MyHFTest
{
    public class CustomHelloWorld
    {
        public void LogThis(string info)
        {
            Console.WriteLine(info + $" {DateTime.Now}");
        }
    }
}