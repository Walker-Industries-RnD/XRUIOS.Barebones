using Hangfire;
using System;
using System.Collections.Generic;
using System.Text;

namespace XRUIOS.Barebones
{
    public static class TimerManagerClass
    {
        public record TimerRecord
        {
            public string TimerName;
            public DateTime EndTime;
            public bool IsRunning;
            public TimeSpan Duration;
            public Action? OnFinish;

            public TimerRecord(string timerName, TimeSpan duration, Action? onFinish = null)
            {
                TimerName = timerName;
                Duration = duration;
                EndTime = DateTime.Now + duration;
                IsRunning = false;
                OnFinish = onFinish;
            }
        }

        public static Dictionary<string, TimerRecord> Timers = new Dictionary<string, TimerRecord>();

        //C
        public static void StartTimer(TimerRecord timer)
        {
            if (Timers.ContainsKey(timer.TimerName))
                CancelTimer(timer.TimerName);

            timer.EndTime = DateTime.Now + timer.Duration;
            timer.IsRunning = true;
            Timers[timer.TimerName] = timer;

            ScheduleTimerJob(timer);
        }


        //U
        public static void AddTime(string timerName, TimeSpan extra)
        {
            if (!Timers.TryGetValue(timerName, out var timer)) return;

            if (timer.IsRunning)
                timer.EndTime += extra;
            else
                timer.Duration += extra;

            ScheduleTimerJob(timer);
        }


        //D
        public static void CancelTimer(string timerName)
        {
            BackgroundJob.Delete($"timer:{timerName}:*");
            if (Timers.TryGetValue(timerName, out var timer))
                timer.IsRunning = false;
        }

        private static void ScheduleTimerJob(TimerRecord timer)
        {
            var delay = timer.EndTime - DateTime.Now;
            if (delay <= TimeSpan.Zero)
            {
                FireTimer(timer.TimerName);
                return;
            }

            var jobId = $"timer:{timer.TimerName}:{timer.EndTime:O}";
            BackgroundJob.Schedule(jobId, () => FireTimer(timer.TimerName), delay);
        }

        //Notification TEMPORARY
        public static void FireTimer(string timerName)
        {
            if (!Timers.TryGetValue(timerName, out var timer) || !timer.IsRunning) return;

            timer.IsRunning = false;
            timer.OnFinish?.Invoke();
            Timers.Remove(timerName);
        }
    }


}
