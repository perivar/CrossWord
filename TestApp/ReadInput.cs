using System;

namespace CrossWord.TestApp
{
    public class ReadInput
    {
        readonly CommandStore commandStore;
        bool shouldStop;

        public ReadInput(CommandStore commandStore)
        {
            this.commandStore = commandStore;
        }

        public bool ShouldStop
        {
            set { shouldStop = value; }
        }

        public void Run()
        {
            while (! shouldStop)
            {
                string command = Console.ReadLine();
                commandStore.AddCommand(command);
            }
        }
    }
}