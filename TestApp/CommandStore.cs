using System.Collections.Generic;

namespace CrossWord.TestApp
{
    public class CommandStore
    {
        readonly IList<string> commandQueue;
        readonly object lockObject = new();
        readonly object outLockObject = new();
        int count;

        public CommandStore()
        {
            commandQueue = new List<string>();
            count = 0;
        }

        public int Count
        {
            get { return count; }
        }

        public void AddCommand(string aCommand)
        {
            lock (lockObject)
            {
                commandQueue.Add(aCommand);
                count++;
            }
        }

        public string PopCommand()
        {
            string result = null;
            lock (lockObject)
            {
                if (commandQueue.Count > 0)
                {
                    result = commandQueue[0];
                    commandQueue.RemoveAt(0);
                    count--;
                }
            }
            return result;
        }

        public object Lock
        {
            get { return outLockObject; }
        }
    }
}