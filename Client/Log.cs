using System;

namespace Client
{
    /// <summary>
    /// Used to quickly disable all debug WriteLine statements.
    /// </summary>
    class Log
    {
        public static void Debug(string s)
        {
            //Console.WriteLine(s);
        }

        public static void Info(string s)
        {
            Console.WriteLine(s);
        }
    }
}
