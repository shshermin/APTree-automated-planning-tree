using System;
using System.Collections.Generic;
using System.Linq;

namespace BehaviorTreeMainProject.Log
{
    /// <summary>
    /// Centralized statistics tracking for all loggers
    /// </summary>
    public class LogStatistics
    {
        private readonly Dictionary<string, int> counters = new Dictionary<string, int>();
        private readonly Dictionary<string, TimeSpan> timings = new Dictionary<string, TimeSpan>();
        private readonly Dictionary<string, DateTime> startTimes = new Dictionary<string, DateTime>();
        private readonly object lockObject = new object();

        public void Increment(string key)
        {
            lock (lockObject)
            {
                if (!counters.ContainsKey(key))
                    counters[key] = 0;
                counters[key]++;
            }
        }

        public void StartTiming(string key)
        {
            lock (lockObject)
            {
                startTimes[key] = DateTime.Now;
            }
        }

        public void EndTiming(string key)
        {
            lock (lockObject)
            {
                if (startTimes.ContainsKey(key))
                {
                    var duration = DateTime.Now - startTimes[key];
                    timings[key] = duration;
                    startTimes.Remove(key);
                }
            }
        }

        public int GetCount(string key)
        {
            lock (lockObject)
            {
                return counters.ContainsKey(key) ? counters[key] : 0;
            }
        }

        public TimeSpan GetTiming(string key)
        {
            lock (lockObject)
            {
                return timings.ContainsKey(key) ? timings[key] : TimeSpan.Zero;
            }
        }

        public Dictionary<string, int> GetAllCounters()
        {
            lock (lockObject)
            {
                return new Dictionary<string, int>(counters);
            }
        }

        public Dictionary<string, TimeSpan> GetAllTimings()
        {
            lock (lockObject)
            {
                return new Dictionary<string, TimeSpan>(timings);
            }
        }

        public void Clear()
        {
            lock (lockObject)
            {
                counters.Clear();
                timings.Clear();
                startTimes.Clear();
            }
        }

        public void ClearCounter(string key)
        {
            lock (lockObject)
            {
                if (counters.ContainsKey(key))
                    counters.Remove(key);
            }
        }

        public void ClearTiming(string key)
        {
            lock (lockObject)
            {
                if (timings.ContainsKey(key))
                    timings.Remove(key);
            }
        }
    }
}
