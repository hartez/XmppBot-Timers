using System;
using System.Linq;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NUnit.Framework;
using XmppBot.Common;
using XmppBot_Timers;

namespace Timers.Tests
{
    [TestFixture]
    public class CountdownTests : ReactiveTest
    {
        [Test]
        public void count_down_from_three_seconds()
        {
            var scheduler = new TestScheduler();

            var plugin = new CountdownTimer(scheduler);

            var pl = new ParsedLine("!countdown -d 3 -i 1", "Bob");

            long oneSecond = TimeSpan.FromSeconds(1).Ticks;

            ITestableObserver<string> obs = scheduler.Start(() => plugin.Evaluate(pl), 11 * oneSecond);

            obs.Messages.AssertEqual(
                OnNext(201, "3 seconds remaining..."),
                OnNext(10000201, "2 seconds remaining..."),
                OnNext(20000201, "1 second remaining..."),
                OnNext(30000202, "Finished!"),
                OnCompleted<string>(30000202)
                );
        }

        [Test]
        public void finish_with_message()
        {
            var scheduler = new TestScheduler();

            var plugin = new CountdownTimer(scheduler);

            var pl = new ParsedLine("!countdown --duration=3 -i 1 -f I will say this when I'm done", "Bob");

            long oneSecond = TimeSpan.FromSeconds(1).Ticks;

            ITestableObserver<string> obs = scheduler.Start(() => plugin.Evaluate(pl), 11 * oneSecond);

            obs.Messages.AssertEqual(
                OnNext(201, "3 seconds remaining..."),
                OnNext(10000201, "2 seconds remaining..."),
                OnNext(20000201, "1 second remaining..."),
                OnNext(30000202, "I will say this when I'm done"),
                OnCompleted<string>(30000202)
                );
        }

        [Test]
        public void five_minutes_wrap_up_reminder_at_three_and_four()
        {
            var scheduler = new TestScheduler();

            var plugin = new CountdownTimer(scheduler);

            var pl = new ParsedLine("!countdown --duration=5m -i 1m -e 4m 1 minute warning 3m 2 minute warning", "Bob");

            long oneMinute = TimeSpan.FromSeconds(60).Ticks;

            ITestableObserver<string> obs = scheduler.Start(() => plugin.Evaluate(pl), 6 * oneMinute);

            obs.Messages.AssertEqual(
                OnNext(201, "5 minutes remaining..."),
                OnNext(600000201, "4 minutes remaining..."),
                OnNext(1200000201, "3 minutes remaining..."),
                OnNext(1800000201, "2 minutes remaining..."),
                OnNext(1810000201, "2 minute warning"),
                OnNext(2400000201, "1 minute remaining..."),
                OnNext(2410000201, "1 minute warning"),
                OnNext(3010000202, "Finished!"),
                OnCompleted<string>(3010000202)
                );
        }

        [Test]
        public void encoded_urls_in_events_are_decoded()
        {
            var scheduler = new TestScheduler();

            var plugin = new CountdownTimer(scheduler);

            var pl = new ParsedLine("!countdown --duration=5m -i 1m -e 4m http%3A%2F%2Fi.imgur.com%2FpMIpQ46.jpg", "Bob");

            long oneMinute = TimeSpan.FromSeconds(60).Ticks;

            ITestableObserver<string> obs = scheduler.Start(() => plugin.Evaluate(pl), 6 * oneMinute);

            obs.Messages.AssertEqual(
                OnNext(201, "5 minutes remaining..."),
                OnNext(600000201, "4 minutes remaining..."),
                OnNext(1200000201, "3 minutes remaining..."),
                OnNext(1800000201, "2 minutes remaining..."),
                OnNext(2400000201, "1 minute remaining..."),
                OnNext(2410000201, "http://i.imgur.com/pMIpQ46.jpg"),
                OnNext(3010000202, "Finished!"),
                OnCompleted<string>(3010000202)
                );
        }

        [Test]
        public void no_options_returns_help()
        {
            var scheduler = new TestScheduler();

            var plugin = new CountdownTimer(scheduler);

            var pl = new ParsedLine("!countdown", "Bob");

            IObservable<string> results = plugin.Evaluate(pl);

            ITestableObserver<string> obs = scheduler.Start(() => results);

            obs.Messages.First().Value.Value.Should().Contain("!countdown -d <duration> -i <interval> [options]");
        }

        [Test]
        public void help_returns_help()
        {
            var scheduler = new TestScheduler();

            var plugin = new CountdownTimer(scheduler);

            var pl = new ParsedLine("!countdown --help", "Bob");

            IObservable<string> results = plugin.Evaluate(pl);

            ITestableObserver<string> obs = scheduler.Start(() => results);

            obs.Messages.First().Value.Value.Should().Contain("!countdown -d <duration> -i <interval> [options]");
        }

        [Test]
        public void h_returns_help()
        {
            var scheduler = new TestScheduler();

            var plugin = new CountdownTimer(scheduler);

            var pl = new ParsedLine("!countdown -h", "Bob");

            IObservable<string> results = plugin.Evaluate(pl);

            ITestableObserver<string> obs = scheduler.Start(() => results);

            obs.Messages.First().Value.Value.Should().Contain("!countdown -d <duration> -i <interval> [options]");
        }

        [Test]
        public void finish_help_formats_default()
        {
            var scheduler = new TestScheduler();

            var plugin = new CountdownTimer(scheduler);

            var pl = new ParsedLine("!countdown", "Bob");

            IObservable<string> results = plugin.Evaluate(pl);

            ITestableObserver<string> obs = scheduler.Start(() => results);

            obs.Messages.First().Value.Value.Contains("System.String[]")
                .Should().BeFalse("the formatting of the default value shouldn't be System.String[]");

            obs.Messages.First().Value.Value.Contains("(Default: Finished!)")
               .Should().BeTrue();
        }
    }
}