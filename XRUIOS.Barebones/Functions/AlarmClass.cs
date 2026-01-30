using Hangfire;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using static Pariah_Cybersecurity.DataHandler;
using static XRUIOS.Barebones.XRUIOS;

namespace XRUIOS.Barebones
{
    //Later I should make it so notifications appearing is optional; that way people can use it on the backend

    public class AlarmClass
    {

        public record Alarm
        {
            public string AlarmName;
            public DateTime AlarmTime;
            public bool IsRecurring;
            public List<DayOfWeek> RecurringDays;
            public FileRecord SoundFilePath;
            public int Volume;
            public bool IsEnabled;
            public Alarm() { }
            public Alarm(string alarmName, DateTime alarmTime, bool isRecurring, List<DayOfWeek> recurringDays, FileRecord soundFilePath, int volume, bool isEnabled)
            {
                AlarmName = alarmName;
                AlarmTime = alarmTime;
                IsRecurring = isRecurring;
                RecurringDays = recurringDays;
                SoundFilePath = soundFilePath;
                Volume = volume;
                IsEnabled = isEnabled;
            }
        }

   
        public static ObservableCollection<Alarm> Alarms = new ObservableCollection<Alarm>();

        //C
        public static async Task AddAlarm(Alarm newAlarm)
        {
            Alarms.Add(newAlarm);

            var directoryPath = Path.Combine(DataPath, "Alarms");
            var manager = new Yuuko.Bindings.DirectoryManager(directoryPath);

            var alarmsFile = await JSONDataHandler.LoadJsonFile("Alarms", directoryPath);
            var alarms = (ObservableCollection<Alarm>) await JSONDataHandler.GetVariable<ObservableCollection<Alarm>>(alarmsFile, "Data", encryptionKey);

            alarms.Add(newAlarm);

            var editedJSON = await JSONDataHandler.UpdateJson<ObservableCollection<Alarm>>(alarmsFile, "Data", alarms, encryptionKey);

            await JSONDataHandler.SaveJson(editedJSON);

            AlarmScheduler.ScheduleAlarm(newAlarm);
        }

        //R
        public static async Task LoadAlarms()
        {
            var directoryPath = Path.Combine(DataPath, "Alarms");
            var manager = new Yuuko.Bindings.DirectoryManager(directoryPath);

            var alarmsFile = await JSONDataHandler.LoadJsonFile("Alarms", directoryPath);
            var alarms = (ObservableCollection<Alarm>)await JSONDataHandler.GetVariable<ObservableCollection<Alarm>>(alarmsFile, "Data", encryptionKey);
            Alarms = alarms;

            AlarmScheduler.ScheduleAllAlarms();

        }

        //U (Lowk headache trying to do this one)
        public static async Task<Alarm?> UpdateAlarm(Alarm existingAlarm, Action<Alarm> updateAction)
        {
            if (!Alarms.Contains(existingAlarm))
                return null;

            // Cancel any scheduled jobs
            BackgroundJob.Delete($"alarm:{existingAlarm.AlarmName}:*");

            // Apply caller updates
            updateAction(existingAlarm);

            var directoryPath = Path.Combine(DataPath, "Alarms");
            var manager = new Yuuko.Bindings.DirectoryManager(directoryPath);

            var alarmsFile = await JSONDataHandler.LoadJsonFile("Alarms", directoryPath);
            var alarms = (ObservableCollection<Alarm>)await JSONDataHandler.GetVariable<ObservableCollection<Alarm>>(alarmsFile, "Data", encryptionKey);

            // Replace the old alarm object with the updated one
            var index = alarms.IndexOf(alarms.First(a => a.AlarmName == existingAlarm.AlarmName && a.AlarmTime == existingAlarm.AlarmTime));
            if (index >= 0)
                alarms[index] = existingAlarm;

            var editedJSON = await JSONDataHandler.UpdateJson<ObservableCollection<Alarm>>(alarmsFile, "Data", alarms, encryptionKey);
            await JSONDataHandler.SaveJson(editedJSON);

            // Reschedule the updated alarm
            AlarmScheduler.ScheduleAlarm(existingAlarm);

            return existingAlarm;
        }

        //D I should really be putting more comments LOLLLL
        //Me looking at this in like a month
        public static async Task DeleteAlarm(Alarm alarm)
        {
            // Cancel any scheduled jobs
            BackgroundJob.Delete($"alarm:{alarm.AlarmName}:*");

            // Remove locally
            Alarms.Remove(alarm);

            var directoryPath = Path.Combine(DataPath, "Alarms");
            var manager = new Yuuko.Bindings.DirectoryManager(directoryPath);

            var alarmsFile = await JSONDataHandler.LoadJsonFile("Alarms", directoryPath);
            var alarms = (ObservableCollection<Alarm>)await JSONDataHandler.GetVariable<ObservableCollection<Alarm>>(alarmsFile, "Data", encryptionKey);

            alarms.Remove(alarm);

            var editedJSON = await JSONDataHandler.UpdateJson<ObservableCollection<Alarm>>(alarmsFile, "Data", alarm, encryptionKey);

            await JSONDataHandler.SaveJson(editedJSON);
        }

        public static class AlarmScheduler
        {
            public static void ScheduleAlarm(Alarm alarm)
            {
                // Cancel any existing jobs
                BackgroundJob.Delete($"alarm:{alarm.AlarmName}:*");

                if (!alarm.IsEnabled)
                    return;

                if (alarm.IsRecurring)
                {
                    // Schedule for the next 7 days from now
                    var now = DateTime.Now;
                    var endWindow = now.AddDays(7);

                    for (var day = now.Date; day <= endWindow.Date; day = day.AddDays(1))
                    {
                        if (!alarm.RecurringDays.Contains(day.DayOfWeek))
                            continue;

                        var alarmTime = new DateTime(
                            day.Year, day.Month, day.Day,
                            alarm.AlarmTime.Hour, alarm.AlarmTime.Minute, alarm.AlarmTime.Second
                        );

                        var delay = alarmTime - now;
                        var jobId = $"alarm:{alarm.AlarmName}:{alarmTime:O}";

                        if (delay <= TimeSpan.Zero)
                            BackgroundJob.Enqueue(jobId, () => FireAlarm(alarm));
                        else
                            BackgroundJob.Schedule(jobId, () => FireAlarm(alarm), delay);
                    }
                }
                else
                {
                    var delay = alarm.AlarmTime - DateTime.Now;
                    var jobId = $"alarm:{alarm.AlarmName}:{alarm.AlarmTime:O}";

                    if (delay <= TimeSpan.Zero)
                        BackgroundJob.Enqueue(jobId, () => FireAlarm(alarm));
                    else
                        BackgroundJob.Schedule(jobId, () => FireAlarm(alarm), delay);
                }
            }

            private static void FireAlarm(Alarm alarm)
            {
                // Trigger the alarm (play sound, show notification, etc.)
                Console.WriteLine($"Alarm Triggered: {alarm.AlarmName} at {DateTime.Now}");
                // Example: PlaySound(alarm.SoundFilePath, alarm.Volume);

                // If recurring, reschedule itself for the next occurrence
                if (alarm.IsRecurring)
                {
                    ScheduleNextOccurrence(alarm);
                }
            }

            private static void ScheduleNextOccurrence(Alarm alarm)
            {
                var now = DateTime.Now;

                // Find the next recurring day
                for (int i = 1; i <= 7; i++) // look up to a week ahead
                {
                    var nextDay = now.AddDays(i);
                    if (alarm.RecurringDays.Contains(nextDay.DayOfWeek))
                    {
                        var nextAlarmTime = new DateTime(
                            nextDay.Year, nextDay.Month, nextDay.Day,
                            alarm.AlarmTime.Hour, alarm.AlarmTime.Minute, alarm.AlarmTime.Second
                        );

                        var delay = nextAlarmTime - now;
                        var jobId = $"alarm:{alarm.AlarmName}:{nextAlarmTime:O}";

                        BackgroundJob.Schedule(jobId, () => FireAlarm(alarm), delay);
                        break;
                    }
                }
            }

            public static void ScheduleAllAlarms()
            {
                foreach (var alarm in Alarms)
                {
                    ScheduleAlarm(alarm);
                }
            }

        }



    }

}
