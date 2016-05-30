using System.Diagnostics;
using System.Threading;
using BitcoinUtilities.GUI.Models;
using NUnit.Framework;
using TestUtilities;

namespace Test.BitcoinUtilities.GUI.Models
{
    [TestFixture]
    public class TestEventManager
    {
        [Test]
        public void TestSmoke()
        {
            MessageLog log = new MessageLog();
            ActionPerformer performer = new ActionPerformer(log);
            EventManager eventManager = new EventManager();

            const string eventType1 = "ET1";
            const string eventType2 = "ET2";

            eventManager.Watch(eventType1, performer.Action1);
            eventManager.Watch(eventType2, performer.Action2);

            eventManager.Start();

            log.Log($"Fired {eventType1} event.");
            eventManager.Notify(eventType1);

            Thread.Sleep(200);

            log.Log($"Fired {eventType2} event.");
            eventManager.Notify(eventType2);

            Thread.Sleep(200);

            Assert.That(log.GetLog(), Is.EqualTo(new string[]
            {
                $"Fired {eventType1} event.",
                $"Action1 invoked.",
                $"Fired {eventType2} event.",
                $"Action2 invoked."
            }));

            // testing that attempt to remove non-existent event listeners does not affect existing watchers
            eventManager.Unwatch(eventType1, performer.Action2);
            eventManager.Unwatch(eventType2, performer.Action1);

            log.Clear();
            log.Log("Attempted to remove non-existent event listeners.");

            log.Log($"Fired {eventType1} event.");
            eventManager.Notify(eventType1);

            Thread.Sleep(200);

            log.Log($"Fired {eventType2} event.");
            eventManager.Notify(eventType2);

            Thread.Sleep(200);

            Assert.That(log.GetLog(), Is.EqualTo(new string[]
            {
                $"Attempted to remove non-existent event listeners.",
                $"Fired {eventType1} event.",
                $"Action1 invoked.",
                $"Fired {eventType2} event.",
                $"Action2 invoked."
            }));

            eventManager.Unwatch(eventType1, performer.Action1);

            log.Clear();
            log.Log($"Removed event-listener for {eventType1}.");

            log.Log($"Fired {eventType1} event.");
            eventManager.Notify(eventType1);

            log.Log($"Fired {eventType2} event.");
            eventManager.Notify(eventType2);

            Thread.Sleep(200);

            Assert.That(log.GetLog(), Is.EqualTo(new string[]
            {
                $"Removed event-listener for {eventType1}.",
                $"Fired {eventType1} event.",
                $"Fired {eventType2} event.",
                $"Action2 invoked."
            }));
        }

        [Test]
        public void TestSlowListener()
        {
            MessageLog log = new MessageLog();
            ActionPerformer performer = new ActionPerformer(log);
            EventManager eventManager = new EventManager();

            const string eventType = "ET";

            eventManager.Start();

            eventManager.Watch(eventType, performer.SlowAction);

            log.Log("Event fired.");
            eventManager.Notify(eventType);

            Thread.Sleep(150);

            const int eventsCount = 10000;

            Stopwatch sw = Stopwatch.StartNew();
            log.Log($"Starting to fire a series of {eventsCount} events.");
            for (int i = 0; i < eventsCount; i++)
            {
                eventManager.Notify(eventType);
            }
            log.Log($"Finished to fire a series of {eventsCount} events.");
            Assert.That(sw.ElapsedMilliseconds, Is.LessThan(100));

            Thread.Sleep(1000);

            eventManager.Stop();

            Assert.That(log.GetLog(), Is.EqualTo(new string[]
            {
                "Event fired.",
                "SlowAction started.",
                $"Starting to fire a series of {eventsCount} events.",
                $"Finished to fire a series of {eventsCount} events.",
                "SlowAction completed.",
                "SlowAction started.",
                "SlowAction completed."
            }));
        }

        [Test]
        public void TestBrokenListener()
        {
            MessageLog log = new MessageLog();
            ActionPerformer performer1 = new ActionPerformer(log);
            ActionPerformer performer2 = new ActionPerformer(log);
            EventManager eventManager = new EventManager();

            const string eventType = "ET";

            eventManager.Start();

            eventManager.Watch(eventType, performer1.BrokenAction);
            eventManager.Watch(eventType, performer2.BrokenAction);

            log.Log("Event fired.");
            eventManager.Notify(eventType);

            Thread.Sleep(200);

            Assert.That(log.GetLog(), Is.EqualTo(new string[]
            {
                "Event fired.",
                "BrokenAction invoked.",
                "BrokenAction invoked."
            }));

            log.Log("Event fired.");
            eventManager.Notify(eventType);

            Thread.Sleep(200);

            Assert.That(log.GetLog(), Is.EqualTo(new string[]
            {
                "Event fired.",
                "BrokenAction invoked.",
                "BrokenAction invoked.",
                "Event fired.",
                "BrokenAction invoked.",
                "BrokenAction invoked."
            }));
        }

        private class ActionPerformer
        {
            private readonly MessageLog log;

            public ActionPerformer(MessageLog log)
            {
                this.log = log;
            }

            public void Action1()
            {
                log.Log("Action1 invoked.");
            }

            public void Action2()
            {
                log.Log("Action2 invoked.");
            }

            public void SlowAction()
            {
                log.Log("SlowAction started.");
                Thread.Sleep(500);
                log.Log("SlowAction completed.");
            }

            public void BrokenAction()
            {
                log.Log("BrokenAction invoked.");
            }
        }
    }
}