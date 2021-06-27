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
            // Output disabled for performance reasons.
            //Console.WriteLine(s);
        }

        public static void Info(string s)
        {
            Console.WriteLine(s);
        }
    }
}
