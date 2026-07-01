using System.Collections.Concurrent;

namespace discord_bot.Services
{
    public class CommandQueue
    {
        private readonly ConcurrentQueue<CommandItem> _queue = new();
        private readonly object _lock = new object();

        public void Enqueue(string command, Dictionary<string, string> parameters)
        {
            var item = new CommandItem(command, parameters);
            _queue.Enqueue(item);
            Console.WriteLine($"[QUEUE] Enqueued: {command} with params: {string.Join(", ", parameters.Select(kv => $"{kv.Key}={kv.Value}"))}");
            Console.WriteLine($"[QUEUE] Queue size: {_queue.Count}");
        }

        public bool TryDequeue(out CommandItem item)
        {
            var result = _queue.TryDequeue(out item);
            if (result && item != null)
            {
                Console.WriteLine($"[QUEUE] Dequeued: {item.Command}");
                Console.WriteLine($"[QUEUE] Queue size: {_queue.Count}");
            }
            return result;
        }

        public bool HasCommands => !_queue.IsEmpty;
        public int Count => _queue.Count;
    }

    public class CommandItem
    {
        public string Command { get; }
        public Dictionary<string, string> Parameters { get; }

        public CommandItem(string command, Dictionary<string, string> parameters)
        {
            Command = command;
            Parameters = parameters;
        }
    }
}