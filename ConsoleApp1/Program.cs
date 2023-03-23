using System.Globalization;

namespace HttpTrafficMonitor;

class Program
{
    private static string _logFilePath = "C:/Users/arsen/source/repos/ConsoleApp1/ConsoleApp1/bin/Debug/net6.0/access.log" ;
    private static int _alertThreshold = 10;
    private static TimeSpan _alertInterval = TimeSpan.FromMinutes(2);

    static void Main(string[] args)
    {
        if (args.Length > 0)
        {
            _logFilePath = args[0];
        }

        Console.WriteLine($"Monitoring HTTP traffic from log file: {_logFilePath}");
        string[] parts = Array.Empty<string>();
        while (true)
        {
            var logEntries = File.ReadLines(_logFilePath)
             .Where(line => line.StartsWith("127.0.0.1"))
             .Select(line =>
             {
                   parts = line.Split(' ');
                 return new
                 {
                     Section = GetSection(parts[6]),
                     StatusCode = int.Parse(parts[8]),
                     Timestamp = DateTime.ParseExact(parts[3].Substring(1), "dd/MMM/yyyy:HH:mm:ss", CultureInfo.InvariantCulture)
                 };
             });

            var sectionStats = logEntries.GroupBy(entry => entry.Section)
                .Select(group => new
                {
                    Section = group.Key,
                    Hits = group.Count(),
                    Successes = group.Count(entry => entry.StatusCode >= 200 && entry.StatusCode < 300),
                    Errors = group.Count(entry => entry.StatusCode >= 400 && entry.StatusCode < 600)
                });

            var totalStats = new
            {
                Hits = sectionStats.Sum(section => section.Hits),
                Successes = sectionStats.Sum(section => section.Successes),
                Errors = sectionStats.Sum(section => section.Errors)
            };

            Console.WriteLine($"Section\tHits\tSuccesses\tErrors");
            foreach (var sectionStat in sectionStats.OrderByDescending(section => section.Hits))
            {
                Console.WriteLine($"{sectionStat.Section}\t{sectionStat.Hits}\t{sectionStat.Successes}\t{sectionStat.Errors}");
            }

            Console.WriteLine($"Total\t{totalStats.Hits}\t{totalStats.Successes}\t{totalStats.Errors}");

            var windowStart = DateTime.Now - _alertInterval;
            var hitsInWindow = logEntries.Count(entry => entry.StatusCode >= 200 && entry.StatusCode < 600 && DateTime.ParseExact(parts[3].Substring(1), "dd/MMM/yyyy:HH:mm:ss", CultureInfo.InvariantCulture).ToLocalTime() > windowStart);
            var hitsPerSecond = (double)hitsInWindow / _alertInterval.TotalSeconds;
            if (hitsPerSecond > _alertThreshold)
            {
                Console.WriteLine($"High traffic generated an alert - hits = {hitsPerSecond}, triggered at {DateTime.Now.ToLocalTime()}");
            }

            Thread.Sleep(TimeSpan.FromSeconds(10));
        }
    }

    private static string GetSection(string resource)
    {
        var secondSlashIndex = resource.IndexOf('/', resource.IndexOf('/') + 1);
        if (secondSlashIndex > 0)
        {
            return resource.Substring(0, secondSlashIndex);
        }
        else
        {
            return resource;
        }
    }
}
