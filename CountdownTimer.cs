using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using CommandLine;
using XmppBot.Common;

namespace XmppBot_Timers
{
    [Export(typeof(IXmppBotSequencePlugin))]
    public class CountdownTimer : IXmppBotSequencePlugin
    {
        private readonly IScheduler _scheduler;

        public CountdownTimer(IScheduler scheduler)
        {
            _scheduler = scheduler;
        }

        public CountdownTimer()
        {
        }

        public IObservable<string> Evaluate(ParsedLine line)
        {
            if(!line.IsCommand || line.Command.ToLower() != "countdown")
            {
                return null;
            }

            var options = new CountdownTimerOptions();

            if(!Parser.Default.ParseArguments(line.Args, options))
            {
                return Return(options.GetUsage());
            }

            try
            {
                int seconds = options.DurationSeconds;
                int interval = options.IntervalSeconds;

                var unitLabels = options.ParseUnitLabels(options.IntervalString);

                // Create an interval sequence that fires off a value every [interval] seconds
                IObservable<string> seq = Interval(TimeSpan.FromSeconds(interval))

                    // Run that seq until the total time has exceeded the [seconds] value
                    .TakeWhile(l => ((l + 1) * interval) < seconds)

                    // Project each element in the sequence to a human-readable time value
                    .Select(
                        l =>
                        String.Format("{0} remaining...",
                            FormatRemaining(seconds - ((l + 1) * interval), unitLabels)));

                // If there are any other events configured, merge them into the sequence
                if(options.Events.Any())
                {
                    var events = options.Events.OrderBy(@event => @event.Target);

                    var eventTimes = Interval(TimeSpan.FromSeconds(1))
                        .TakeWhile(l => l < seconds)
                        .Where(l => events.Any(@event => @event.Target == l))
                        .Select(l => events.First(@event => @event.Target == l).Message);

                    seq = seq.Merge(eventTimes);
                }

                // Add a start and end message
                return Return(String.Format("{0} remaining...", FormatRemaining(seconds, options.ParseUnitLabels(options.DurationString))))
                    .Concat(seq)
                    .Concat(Return(String.Join(" ", options.FinishedMessage)));
            }
            catch(ArgumentException)
            {
                return Return(options.GetUsage());
            }
        }

        public string Name
        {
            get { return "CountdownTimer"; }
        }

        public IObservable<String> Return(String message)
        {
            if(_scheduler == null)
            {
                return Observable.Return(message);
            }

            return Observable.Return(message, _scheduler);
        }

        public IObservable<long> Interval(TimeSpan timeSpan)
        {
            if(_scheduler == null)
            {
                return Observable.Interval(timeSpan);
            }

            return Observable.Interval(timeSpan, _scheduler);
        }

        private string FormatRemaining(long remaining, Tuple<string, string> units)
        {
            if(units.Item1 == "minute")
            {
                remaining /= 60;
            }
            else if(units.Item1 == "hour")
            {
                remaining /= 3600;
            }

            return string.Format("{0} {1}", remaining, (remaining == 1 ? units.Item1 : units.Item2));
        }
    }
}