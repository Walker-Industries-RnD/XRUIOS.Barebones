using EclipseProject;
using Hangfire;
using static XRUIOS.Barebones.Interfaces.TimerManagerClass;

namespace XRUIOS.Barebones
{
    public class TimerManagerClass 
    {
         
        public static readonly TimerManagerClass Instance = new();
        private TimerManagerClass() { }

        public static Dictionary<string, TimerRecord> Timers = new Dictionary<string, TimerRecord>();

        // Start or restart timer
        [SeaOfDirac("TimerManagerClass.StartTimer", new[] { "timer" }, typeof(void), typeof(TimerRecord))]
        public static void StartTimer(TimerRecord timer)
        {
            if (Timers.ContainsKey(timer.TimerName))
                CancelTimer(timer.TimerName);

            timer.EndTime = DateTime.Now + timer.Duration;
            timer.IsRunning = true;
            Timers[timer.TimerName] = timer;

            ScheduleTimerJob(timer);
        }

        // Add time to running timer
        [SeaOfDirac("TimerManagerClass.AddTime", new[] { "timerName", "extra" }, typeof(void), typeof(string), typeof(TimeSpan))]
        public static void AddTime(string timerName, TimeSpan extra)
        {
            if (!Timers.TryGetValue(timerName, out var timer)) return;

            if (timer.IsRunning)
            {
                timer.EndTime += extra;
            }
            else
            {
                timer.Duration += extra;
            }

            // Cancel previous Hangfire job if it exists
            if (!string.IsNullOrEmpty(timer.HangfireJobId))
            {
                BackgroundJob.Delete(timer.HangfireJobId);
            }

            ScheduleTimerJob(timer);
        }

        // Cancel timer
        [SeaOfDirac("TimerManagerClass.CancelTimer", new[] { "timerName" }, typeof(void), typeof(string))]
        public static void CancelTimer(string timerName)
        {
            if (!Timers.TryGetValue(timerName, out var timer)) return;

            timer.IsRunning = false;

            if (!string.IsNullOrEmpty(timer.HangfireJobId))
            {
                BackgroundJob.Delete(timer.HangfireJobId);
            }

            Timers.Remove(timerName);
        }

        // Schedule Hangfire job
        private static void ScheduleTimerJob(TimerRecord timer)
        {
            var delay = timer.EndTime - DateTime.Now;
            if (delay <= TimeSpan.Zero)
            {
                FireTimer(timer.TimerName);
                return;
            }

            // Schedule a real Hangfire job and store the job ID
            timer.HangfireJobId = BackgroundJob.Schedule(() => FireTimer(timer.TimerName), delay);
        }

        // Create and immediately start a timer
        [SeaOfDirac("TimerManagerClass.CreateTimer", new[] { "name", "duration", "onFinish" }, typeof(void), typeof(string), typeof(TimeSpan), typeof(Action))]
        public static void CreateTimer(string name, TimeSpan duration, Action? onFinish = null)
        {
            var timer = new TimerRecord(name, duration, onFinish);
            StartTimer(timer);
        }

        // Pause a running timer, preserving remaining time
        [SeaOfDirac("TimerManagerClass.PauseTimer", new[] { "timerName" }, typeof(void), typeof(string))]
        public static void PauseTimer(string timerName)
        {
            if (!Timers.TryGetValue(timerName, out var timer) || !timer.IsRunning) return;

            var remaining = timer.EndTime - DateTime.Now;
            timer.PausedRemaining = remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;

            if (!string.IsNullOrEmpty(timer.HangfireJobId))
                BackgroundJob.Delete(timer.HangfireJobId);

            timer.IsRunning = false;
        }

        // Resume a paused timer from where it left off
        [SeaOfDirac("TimerManagerClass.ResumeTimer", new[] { "timerName" }, typeof(void), typeof(string))]
        public static void ResumeTimer(string timerName)
        {
            if (!Timers.TryGetValue(timerName, out var timer) || timer.IsRunning || timer.PausedRemaining is null) return;

            timer.EndTime = DateTime.Now + timer.PausedRemaining.Value;
            timer.PausedRemaining = null;
            timer.IsRunning = true;

            ScheduleTimerJob(timer);
        }

        // Check if a timer is currently running
        [SeaOfDirac("TimerManagerClass.IsTimerRunning", new[] { "timerName" }, typeof(bool), typeof(string))]
        public static bool IsTimerRunning(string timerName)
        {
            return Timers.TryGetValue(timerName, out var timer) && timer.IsRunning;
        }

        // Fire timer callback
        [SeaOfDirac("TimerManagerClass.FireTimer", new[] { "timerName" }, typeof(void), typeof(string))]
        public static void FireTimer(string timerName)
        {
            if (!Timers.TryGetValue(timerName, out var timer) || !timer.IsRunning) return;

            timer.IsRunning = false;
            timer.OnFinish?.Invoke();
            Timers.Remove(timerName);
        }
    }
}
