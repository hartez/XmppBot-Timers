using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CommandLine;
using CommandLine.Text;

namespace XmppBot_Timers
{
    public class Options
    {
        public int DurationSeconds
        {
            get { return ParseSeconds(DurationString); }
        }

        public int IntervalSeconds
        {
            get { return ParseSeconds(IntervalString); }
        }

        [Option('d', "duration", Required = true, HelpText = "The countdown duration (e.g., 1m, 2h, 45s)")]
        public string DurationString { get; set; }

        [Option('i', "interval", Required = true, HelpText = "The countdown interval (e.g., 2m, 1h, 5s)")]
        public string IntervalString { get; set; }

        [OptionArray('f', "finish", DefaultValue = new []{"Finished!"}, Required = false, HelpText = "What to say when the countdown is over.")]
        public String[] FinishedMessage { get; set; }

        [OptionArray('e', "events", Required = false,
            HelpText =
                "Things to say after specific amounts of time have passed (in the format [time] [message]. For instance, you can say -e 4m Wrap it up!"
            )]
        public String[] EventStrings { get; set; }

        public Tuple<string, string> ParseUnitLabels(string timespan)
        {
            if(Char.IsLetter(timespan.Last()))
            {
                switch(timespan.Last())
                {
                    case ('h'):
                        return new Tuple<string, string>("hour", "hours");
                    case ('m'):
                        return new Tuple<string, string>("minute", "minutes");
                }
            }

            return new Tuple<string, string>("second", "seconds");
        }

        private static int ParseSeconds(String timespan)
        {
            const string format = "Timespan must be in the format <int>[unit] where unit is s, m, h";

            if(String.IsNullOrEmpty(timespan))
            {
                throw new ArgumentException(format);
            }

            int multiplier = 1;

            if(Char.IsLetter(timespan.Last()))
            {
                var units = new[] {'s', 'h', 'm'};

                if(!units.Any(s => s == timespan.Last()))
                {
                    throw new ArgumentException(format);
                }

                switch(timespan.Last())
                {
                    case ('h'):
                        multiplier = 60 * 60;
                        break;
                    case ('m'):
                        multiplier = 60;
                        break;
                }
            }

            string amountString = timespan;

            if(Char.IsLetter(timespan.Last()))
            {
                amountString = timespan.Remove(timespan.Length - 1);
            }

            int amount;

            if(!int.TryParse(amountString, out amount))
            {
                throw new ArgumentException(format);
            }

            return amount * multiplier;
        }

        [HelpOption]
        public string GetUsage()
        {
            var help = new HelpText
                {
                    AddDashesToOption = true
                };

            help.AddPreOptionsLine("!countdown -d=<duration> -i=<interval> [options]");
            help.AddOptions(this);
            return help;
        }

        private List<Event> _events;

        public IEnumerable<Event> Events
        {
            get
            {
                if(_events == null)
                {
                    ParseEvents();
                }
                return _events;
            }
        }

        private void ParseEvents()
        {
            _events = new List<Event>();

            if(EventStrings == null || !EventStrings.Any())
            {
                return;
            }

            Event currentEvent = null;

            foreach(string eventString in EventStrings)
            {
                if(TimeSpanRegex.IsMatch(eventString))
                {
                    if(currentEvent != null)
                    {
                        _events.Add(currentEvent);
                    }

                    currentEvent = new Event();
                    currentEvent.Target = ParseSeconds(eventString);
                }
                else
                {
                    if(currentEvent != null)
                    {
                        currentEvent.Message += (String.IsNullOrEmpty(currentEvent.Message) ? "" : " ") + eventString;
                    }
                }
            }

            if (currentEvent != null)
            {
                _events.Add(currentEvent);
            }
        }

        public Regex TimeSpanRegex = new Regex("[0-9]+[smh]");
    }

    public class Event
    {
        public long Target { get; set; }
        public string Message { get; set; }
    }
}