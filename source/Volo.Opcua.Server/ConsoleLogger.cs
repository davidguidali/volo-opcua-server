using LibUA;
using System;
using System.Collections.Generic;
using System.Text;

namespace Volo.Opcua.Server
{
    public class ConsoleLogger : ILogger
    {
        public bool HasLevel(LogLevel level)
        {
            switch(level)
            {
                case LogLevel.None:
                case LogLevel.Info:
                    return true;
                default: 
                    return false;
            }
        }

        public void LevelSet(LogLevel mask)
        {
            // TODO: Implement level set.
        }

        public void Log(LogLevel level, string str)
        {
            if(level.Equals(LogLevel.Info))
            {
                Console.WriteLine(str);
            }
        }
    }
}
