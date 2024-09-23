using System.Net.Mail;
using System.Net.Mime;
using System.Net;
using System.Text.RegularExpressions;

namespace ConsoleApp1
{
    public class ToDoTask
    {
        public Guid Id { get; set; }
        public string Description { get; set; }
        public TaskPriority Priority { get; set; }
        public TaskStatus Status { get; set; }
        public DateTime CreationDate { get; set; }
        public string Email { get; set; }

        public ToDoTask(string description, TaskPriority priority, string email)
        {
            Id = Guid.NewGuid();
            Description = description;
            Priority = priority;
            Status = TaskStatus.Pending;
            CreationDate = DateTime.Now;
            Email = email;
        }
    }
    public enum TaskStatus
    {
        Pending,
        Completed
    }
    public enum TaskPriority
    {
        low,
        medium,
        high
    }
    public class RemindMe
    {
        private readonly List<ToDoTask> tasks;
        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly Dictionary<TaskPriority, TimeSpan> priorityIntervals = new Dictionary<TaskPriority, TimeSpan>
        {
            { TaskPriority.high, TimeSpan.FromMinutes(1) },
            { TaskPriority.medium, TimeSpan.FromMinutes(5) },
            { TaskPriority.low, TimeSpan.FromMinutes(10) }
        };
        private readonly TimeSpan priorityCheckInterval = TimeSpan.FromDays(1);

        public RemindMe(List<ToDoTask> tasks)
        {
            this.tasks = tasks;
            cancellationTokenSource = new CancellationTokenSource();
        }
        
        public void Start()
        {
            Task.Run(() => CheckHighPriorityTask(cancellationTokenSource.Token));
            Task.Run(() => CheckMediumPriorityTask(cancellationTokenSource.Token));
            Task.Run(() => CheckLowPriorityTasks(cancellationTokenSource.Token));
            Task.Run(() => CheckPriorityUpdates(cancellationTokenSource.Token));
        }

        public void Stop()
        {
            cancellationTokenSource.Cancel();
        }
        private async Task CheckLowPriorityTasks(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.Now;

                    var overdueTasks = tasks
                        .Where(task => task.Priority == TaskPriority.low && task.Status != TaskStatus.Completed)
                        .Where(task => now - task.CreationDate > TimeSpan.FromMinutes(10))
                        .ToList();

                    foreach (var task in overdueTasks)
                    {
                        Console.WriteLine($"Reminder: Task ID: {task.Id}, Description: {task.Description}, Priority: {task.Priority}, Status: {task.Status}");
                    }
                    await Task.Delay(priorityIntervals[TaskPriority.low], token);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Low Priority Task checking is canceled.");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in CheckLowPriorityTasks: {ex.Message}");
                }
            }
        }
        private async Task CheckHighPriorityTask(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.Now;
                    var overdueTasks = tasks
                        .Where(task => task.Priority == TaskPriority.high && task.Status != TaskStatus.Completed)
                        .Where(task => now - task.CreationDate > TimeSpan.FromMinutes(1))
                        .ToList();

                    foreach (var task in overdueTasks)
                    {
                        Console.WriteLine($"Reminder: Task ID: {task.Id}, Description: {task.Description}, Priority: {task.Priority}, Status: {task.Status}");
                    }

                    await Task.Delay(priorityIntervals[TaskPriority.high], token);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("High Priority Task Checking is canceled.");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in CheckHighPriorityTask: {ex.Message}");
                }
            }
        }

        private async Task CheckMediumPriorityTask(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.Now;

                    var overdueTasks = tasks
                        .Where(task => task.Priority == TaskPriority.medium && task.Status != TaskStatus.Completed)
                        .Where(task => now - task.CreationDate > TimeSpan.FromMinutes(5))
                        .ToList();

                    foreach (var task in overdueTasks)
                    {
                        Console.WriteLine($"Reminder: Task ID: {task.Id}, Description: {task.Description}, Priority: {task.Priority}, Status: {task.Status}");

                    }

                    await Task.Delay(priorityIntervals[TaskPriority.medium], token);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Medium Priority Task Checking is canceled.");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in CheckMediumPriorityTask: {ex.Message}");
                }
            }
        }

        

        private async Task CheckPriorityUpdates(CancellationToken token)
        {

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.Now;

                    foreach (var task in tasks.Where(task => task.Status != TaskStatus.Completed))
                    {
                        var age = now - task.CreationDate;

                        if (age > TimeSpan.FromDays(10) && task.Priority != TaskPriority.high)
                        {
                            task.Priority = TaskPriority.high;
                            Console.WriteLine($"Task ID: {task.Id} priority updated to High.");
                        }
                        else if (age > TimeSpan.FromDays(5) && task.Priority == TaskPriority.low)
                        {
                            task.Priority = TaskPriority.medium;
                            Console.WriteLine($"Task ID: {task.Id} priority updated to Medium.");
                        }
                    }
                    await Task.Delay(priorityCheckInterval, token);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Task Priority Update Service was canceled.");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in CheckPriorityUpdates: {ex.Message}");
                }
            }

            Console.WriteLine("Task Priority Updated ....");
        }
    }


    class Program
    {
        private static readonly string logFilePath = "ShubhamTask_log.txt";
        private static readonly List<string> logList = new List<string>();
        private static readonly object logLock = new object();
        private static bool isRunning = true;

        static Program()
        {
            Thread logThread = new Thread(SaveLogToFile);
            logThread.IsBackground = true;
            logThread.Start();
        }

        private static void SaveLogToFile()
        {
            while (isRunning || logList.Count > 0)
            {
                List<string> logsToWrite;

                lock (logLock)
                {
                    logsToWrite = new List<string>(logList);
                    logList.Clear();
                }

                if (logsToWrite.Count > 0)
                {
                    using StreamWriter sw = new StreamWriter(logFilePath, true);
                    foreach (string logEntry in logsToWrite)
                    {
                        sw.WriteLine(logEntry);
                    }
                }

                Thread.Sleep(10000);
            }
        }

        public static void StopLogging()
        {
            isRunning = false;
        }

        private static void LogOperation(string operationType, ToDoTask? task = null, Guid? taskId = null)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string logEntry = $"[{timestamp}] Operation: {operationType}\n";

            if (task != null)
            {
                logEntry += $" Task Details\n ID: {task.Id}\n Description: {task.Description}, Priority: {task.Priority}, Status: {task.Status}, " +
                    $"CreationDate: {task.CreationDate}, Email: {task.Email}\n" +
                    $"                    -----------------------------------------------                               \n";
            }
            else if (taskId.HasValue)
            {
                logEntry += $" Task ID: {taskId.Value}\n" +
                    $"                    -----------------------------------------------                               \n";
            }

            lock (logLock)
            {
                logList.Add(logEntry);
            }
        }

        public static bool TryGetPriority(string input, out TaskPriority priority)
        {
            return Enum.TryParse(input, true, out priority) && Enum.IsDefined(typeof(TaskPriority), priority);
        }

        public static bool TryGetStatus(string input, out TaskStatus status)
        {
            return Enum.TryParse(input, true, out status) && Enum.IsDefined(typeof(TaskStatus), status);
        }

        public static bool ValidDescription(string input)
        {
            string patternDesc = @"^(?!\s)(?!.*\s{2,})(?!.*\s$).{5,}$";
            Regex regexDesc = new Regex(patternDesc);
            return regexDesc.IsMatch(input);

        }

        public static bool ValidEmail(string input)
        {
            string patternEmail = @"^[^\s@]+@[^\s@]+\.[^\s@]+$";
            Regex regexEmail = new Regex(patternEmail);
            return regexEmail.IsMatch(input);

        }


        public static void CreateTask(List<ToDoTask> tasks)
        {
            Console.Write("Enter Task Description (Minimum 5 Character length) ");
            var description = Console.ReadLine()!.Trim();
            if (description.Length < 5)
            {
                Console.WriteLine("Description must contain 5 characters");
                return;
            }
            else if (!ValidDescription(description))
            {
                Console.WriteLine("Description must be in vaid format");
                return;
            }
            Console.Write("Enter Task Priority (Low, Medium, High): ");
            var priorityInput = Console.ReadLine()!.Trim();
            if (!TryGetPriority(priorityInput!, out TaskPriority priority))
            {
                Console.WriteLine("Invalid priority value. Please enter one of the following: Low, Medium, High.");
                return;
            }
            Console.Write("Enter Your Email: ");
            var email = Console.ReadLine()!.Trim();
            if (!ValidEmail(email))
            {
                Console.WriteLine("Invalid Email Id Please Give Valid Email Id: ");
                return;
            }

            ToDoTask newTask = new ToDoTask(description!, priority!, email!);
            tasks.Add(newTask);

            LogOperation("Create", task: newTask);

            Console.WriteLine("Task Added Successfully.....");
        }

        public static void ViewTasks(List<ToDoTask> tasks, TaskPriority? priority = null, TaskStatus? status = null)
        {
            var query = from task in tasks
                        where (!priority.HasValue || task.Priority == priority.Value) &&
                        (!status.HasValue || task.Status == status.Value)
                        orderby task.Priority descending, task.CreationDate descending
                        select task;

            var filteredTasks = query.ToList();

            if (filteredTasks.Count == 0)
            {
                Console.WriteLine("No tasks found.");
            }
            else
            {
                foreach (var task in filteredTasks)
                {

                    Console.WriteLine($"ID: {task.Id}");
                    Console.WriteLine($"Description: {task.Description}");
                    Console.WriteLine($"Priority: {task.Priority}");
                    Console.WriteLine($"Status: {task.Status}");
                    Console.WriteLine($"Creation Date: {task.CreationDate}");
                    Console.WriteLine($"Email: {task.Email}");
                    Console.WriteLine("----------------------------");
                    LogOperation("View", task: task);
                }
            }
        }

        public static void UpdateTask(List<ToDoTask> tasks, Guid taskId)
        {
            bool IsUpdated = true;

            var task = (from t in tasks
                        where t.Id == taskId
                        select t).FirstOrDefault();

            if (task != null)
            {
                Console.Write("Enter New Description (Leave blank to skip): ");
                var description = Console.ReadLine()!.Trim();
                if (description.Length > 0 && description.Length < 5)
                {
                    Console.WriteLine("Description must be in valid format.");
                    IsUpdated = false;
                    return;
                }
                Console.Write("Enter New Priority (Low, Medium, High) or Leave blank to skip: ");
                var priorityInput = Console.ReadLine();
                if (!string.IsNullOrEmpty(priorityInput))
                    if (TryGetPriority(priorityInput, out TaskPriority priority))
                    {
                        task.Priority = priority;
                    }
                    else
                    {
                        Console.WriteLine("Invalid priority value. Please enter one of the following: Low, Medium, High.");
                        IsUpdated = false;
                        return;
                    }

                Console.Write("Enter New Status (Pending, Completed) or Leave blank to skip: ");
                var statusInput = Console.ReadLine();
                if (!string.IsNullOrEmpty(statusInput))
                    if (TryGetStatus(statusInput, out TaskStatus status))
                    {
                        task.Status = status;
                    }
                    else
                    {
                        Console.WriteLine("Invalid status value. Please enter one of the following: Pending, Completed.");
                        IsUpdated = false;
                        return;
                    }
                if (IsUpdated)
                {
                    task.Description = description;
                    LogOperation("Update", task: task);
                    Console.WriteLine("Task Updated Successfully.....");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Task Updation Failed!");
                    Console.ResetColor();
                }
            }
            else
            {
                Console.WriteLine("Task not found.");
            }
        }

        public static void DeleteTask(List<ToDoTask> tasks, Guid taskId)
        {
            var task = (from t in tasks
                        where t.Id == taskId
                        select t).FirstOrDefault();
            if (task != null)
            {
                tasks.Remove(task);
                LogOperation("Delete", taskId: taskId);
                Console.WriteLine("Task Deleted Successfully!");
            }
            else
            {
                Console.WriteLine("Task not found.");
            }
        }


        public static void SendLogFileByEmail(string recipientMail)
        {
            try
            {
                Console.WriteLine("Email Sending is under progress please wait for some Time......");

                var username = "sy335740@gmail.com";
                var password = "befy kuzp uvli iawv";
                var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential(username, password),
                    EnableSsl = true,
                };

                MailMessage mail = new()
                {
                    From = new MailAddress(username),
                    Subject = "Task Log File Created At Revocept",
                    Body = "<h1>This is the Task Log file Created  </h1>",
                    IsBodyHtml = true,// to include our logfilesss

                };
                mail.To.Add(recipientMail);
                var attachement = new Attachment("ShubhamTask_log.txt", MediaTypeNames.Text.Plain);
                mail.Attachments.Add(attachement);
                smtpClient.Send(mail);
                Console.WriteLine("Email sent successfully.");

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);

            }
        }

        static void Main()
        {
            List<ToDoTask> tasks = new List<ToDoTask>();
            RemindMe taskReminder = new RemindMe(tasks);
            taskReminder.Start();

            Console.WriteLine("Starting log operation in background...");

            Console.WriteLine("Welcome to the Console Application..............");

            while (true)
            {
                Console.WriteLine("\nChoose an option:");
                Console.WriteLine("1. Add Task");
                Console.WriteLine("2. View Tasks");
                Console.WriteLine("3. View Tasks By Priority :");
                Console.WriteLine("4. View Tasks By Status :");
                Console.WriteLine("5. Update Task :");
                Console.WriteLine("6. Delete Task :");
                Console.WriteLine("7. Send E-mail :");
                Console.WriteLine("8. Exit");
                Console.WriteLine("---------------------------------------------------");

                string input = Console.ReadLine()!;
                if (int.TryParse(input, out int choice))
                {
                    try
                    {
                        switch (choice)
                        {
                            case 1:
                                CreateTask(tasks);
                                break;

                            case 2:
                                ViewTasks(tasks);
                                break;

                            case 3:
                                Console.Write("Enter Task Priority to Filter (Low, Medium, High): ");
                                var pr = Console.ReadLine();
                                if (TryGetPriority(pr!, out TaskPriority priority))
                                {
                                    ViewTasks(tasks, priority: priority);
                                    break;
                                }
                                else
                                {
                                    Console.WriteLine("Invalid priority value. Please enter one of the following: Low, Medium, High.");
                                }
                                break;

                            case 4:
                                Console.Write("Enter Task Status to Filter (Pending, Completed): ");
                                var st = Console.ReadLine();
                                if (TryGetStatus(st!, out TaskStatus status))
                                {
                                    ViewTasks(tasks, status: status);
                                    break;
                                }
                                else
                                {
                                    Console.WriteLine("Invalid Status value. Please enter one of the following: Pending,Completed.");
                                }
                                break;

                            case 5:
                                Console.Write("Enter Task ID to Update: ");
                                Guid updateId = Guid.Parse(Console.ReadLine()!);
                                UpdateTask(tasks, updateId);
                                break;

                            case 6:
                                Console.Write("Enter Task ID to Delete: ");
                                Guid deleteId = Guid.Parse(Console.ReadLine()!);
                                DeleteTask(tasks, deleteId);
                                break;

                            case 7:
                                Console.WriteLine("Enter Recipient Email Id ");
                                var recipientEmail = Console.ReadLine()!.Trim();
                                if (!ValidEmail(recipientEmail))
                                {
                                    Console.WriteLine("Invalid Email Id");
                                    Console.ResetColor();
                                    break;
                                }
                                else
                                {
                                    SendLogFileByEmail(recipientEmail);
                                }
                                break;

                            case 8:
                                taskReminder.Stop();
                                StopLogging();
                                return;

                            default:
                                Console.WriteLine("Invalid choice. Please select a valid option.");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"An error occurred {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("Invalid input. Please enter a valid integer.");
                }
            }

        }
    }
}
