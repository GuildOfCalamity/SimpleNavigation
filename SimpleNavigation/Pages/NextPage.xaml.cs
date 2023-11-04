using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace SimpleNavigation;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class NextPage : Page
{
    ObservableCollection<string> ocMessages = new();

    /// <summary>
    /// An event that the main page can subscribe to.
    /// </summary>
    public static event EventHandler<Message>? PostMessageEvent;

    public NextPage()
    {
        Debug.WriteLine($"{MethodBase.GetCurrentMethod()?.DeclaringType?.Name}__{MethodBase.GetCurrentMethod()?.Name} [{DateTime.Now.ToString("hh:mm:ss.fff tt")}]");
        this.InitializeComponent();
        this.Loaded += NextPage_Loaded;
        #region [Config behavior using code-behind instead of XAML]
        //var behaviors = Microsoft.Xaml.Interactivity.Interaction.GetBehaviors(asbName);
        //var userStoppedTyping = new Behaviors.TypingPauseBehavior { MinimumDelay = 500, MinimumCharacters = 1 };
        //userStoppedTyping.TypingPaused += AutoSuggestBox_TypingPaused;
        //behaviors.Add(userStoppedTyping);
        #endregion
    }

    async void NextPage_Loaded(object sender, RoutedEventArgs e)
    {
        ocMessages.Clear();
        await Task.Run(async () => 
        {
            InsertMessage($"Started test at {DateTime.Now.ToLongTimeString()}");
            await CustomEventTesting();
            await Task.Delay(1600);
            TestCancellableTaskClass();
        });
    }

    void asbName_GotFocus(object sender, RoutedEventArgs e) => ttAutoSuggest.IsOpen = true;

    void InsertMessage(string msg)
    {
        DispatcherQueue?.TryEnqueue(() => { ocMessages.Insert(0, $"⇒ {msg}"); });
    }

    /// <summary>
    /// Handle any parameter passed.
    /// </summary>
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter != null && e.Parameter is SystemState sys)
        {
            Debug.WriteLine($"You sent '{sys.Title}'");
            landing.Text = $"I'm on page {sys.Title}";
            PostMessageEvent?.Invoke(this, new Message
            {
                Content = $"OnNavigatedTo ⇨ {sys.Title}",
                Severity = InfoBarSeverity.Informational,
            });
        }
        else
        {
            Debug.WriteLine($"Parameter is not of type '{nameof(SystemState)}'");
            landing.Text = $"Parameter is not of type '{nameof(SystemState)}'";
        }
        base.OnNavigatedTo(e);
    }

    #region [Behavior Testing]
    void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        DisplaySuggestions(sender);
    }
    
    void AutoSuggestBox_TypingPaused(object sender, EventArgs e)
    {
        DisplaySuggestions(sender as AutoSuggestBox);
    }

    void DisplaySuggestions(AutoSuggestBox? sender)
    {
        if (sender == null)
            return;

        var suitableItems = new List<string>();
        var splitText = sender.Text.ToLower().Split(" ", StringSplitOptions.RemoveEmptyEntries);
        foreach (var name in TopNames)
        {
            // LINQ "splitText.All(Func<string, bool>)"
            var found = splitText.All((key) => { return name.Contains(key, StringComparison.OrdinalIgnoreCase); });
            if (found)
                suitableItems.Add(name);
        }

        if (suitableItems.Count == 0)
            suitableItems.Add("No results found");

        sender.ItemsSource = suitableItems;
    }

    // I've added more names, but the original list started from  https://www.ssa.gov/oact/babynames/decades/century.html
    readonly List<string> TopNames = new() {
        "Aaliyah",        "Aaron",        "Abigail",        "Adam",        "Addison",
        "Adrian",        "Aiden",        "Alan",        "Albert",        "Alex",
        "Alexander",        "Alexis",        "Alice",        "Amanda",        "Amara",
        "Amber",        "Amelia",        "Amir",        "Amy",        "Andrea",
        "Andrew",        "Angela",        "Ann",        "Anna",        "Anthony",
        "Aria",        "Ariana",        "Arthur",        "Arya",        "Asher",
        "Ashley",        "Athena",        "Aubrey",        "Audrey",        "Aurora",
        "Austin",        "Autumn",        "Ava",        "Avery",        "Ayla",
        "Barbara",        "Bart",        "Beau",        "Bella",        "Benjamin",
        "Betty",        "Beverly",        "Billy",        "Bobby",        "Bradley",
        "Brandon",        "Brenda",        "Brian",        "Brielle",        "Brittany",
        "Brooklyn",        "Bruce",        "Bryan",        "Caleb",        "Cameron",
        "Camila",        "Carl",        "Carol",        "Carolyn",        "Carson",
        "Carter",        "Catherine",        "Charles",        "Charlie",        "Charlotte",
        "Cheryl",        "Chloe",        "Christian",        "Christina",        "Christine",
        "Christopher",        "Claire",        "Colton",        "Cooper",        "Cynthia",
        "Daniel",        "Danielle",        "David",        "Deborah",        "Debra",
        "Declan",        "Delilah",        "Denise",        "Dennis",        "Diana",
        "Diane",        "Dominic",        "Donald",        "Donna",        "Doris",
        "Dorothy",        "Douglas",        "Dylan",        "Easton",        "Edward",
        "Eleanor",        "Elena",        "Eli",        "Eliana",        "Elias",
        "Elijah",        "Elizabeth",        "Ella",        "Ellie",        "Emery",
        "Emilia",        "Emily",        "Emma",        "Eric",        "Ethan",
        "Eugene",        "Eva",        "Evelyn",        "Everett",        "Everleigh",
        "Everly",        "Ezekiel",        "Ezra",        "Frances",        "Frank",
        "Gabriel",        "Gabriella",        "Gary",        "George",        "Gerald",
        "Gianna",        "Gloria",        "Grace",        "Grayson",        "Gregory",
        "Greyson",        "Hailey",        "Hannah",        "Harold",        "Harper",
        "Hazel",        "Heather",        "Helen",        "Henry",        "Hudson",
        "Hunter",        "Iris",        "Isaac",        "Isabella",        "Isaiah",
        "Isla",        "Ivy",        "Jace",        "Jack",        "Jackson",
        "Jacob",        "Jacqueline",        "Jade",        "James",        "Jameson",
        "Janet",        "Janice",        "Jason",        "Jaxon",        "Jaxson",
        "Jayden",        "Jean",        "Jeffrey",        "Jennifer",        "Jeremiah",
        "Jeremy",        "Jerry",        "Jesse",        "Jessica",        "Joan",
        "Joe",        "John",        "Jonathan",        "Jordan",        "Jose",
        "Joseph",        "Joshua",        "Josiah",        "Joyce",        "Juan",
        "Judith",        "Judy",        "Julia",        "Julian",        "Julie",
        "Justin",        "Kai",        "Karen",        "Katherine",        "Kathleen",
        "Kathryn",        "Kayden",        "Kayla",        "Keith",        "Kelly",
        "Kennedy",        "Kenneth",        "Kevin",        "Kimberly",        "Kingston",
        "Kinsley",        "Kyle",        "Landon",        "Larry",        "Laura",
        "Lauren",        "Lawrence",        "Layla",        "Leah",        "Leilani",
        "Leo",        "Levi",        "Liam",        "Lillian",        "Lily",
        "Lincoln",        "Linda",        "Lisa",        "Logan",        "Lori",
        "Luca",        "Lucas",        "Lucy",        "Luke",        "Luna",
        "Madelyn",        "Madison",        "Margaret",        "Maria",        "Marie",
        "Marilyn",        "Mark",        "Martha",        "Mary",        "Mason",
        "Mateo",        "Matthew",        "Maverick",        "Maya",        "Megan",
        "Melissa",        "Melody",        "Mia",        "Micah",        "Michael",
        "Michelle",        "Mila",        "Miles",        "Muhammad",        "Nancy",
        "Naomi",        "Natalia",        "Natalie",        "Nathan",        "Nevaeh",
        "Nicholas",        "Nicole",        "Noah",        "Nolan",        "Nora",
        "Nova",        "Oliver",        "Olivia",        "Owen",        "Paisley",
        "Pamela",        "Patricia",        "Patrick",        "Paul",        "Penelope",
        "Peter",        "Philip",        "Piper",        "Quinn",        "Rachel",
        "Raelynn",        "Ralph",        "Randy",        "Raymond",        "Rebecca",
        "Richard",        "Riley",        "Robert",        "Roger",        "Roman",
        "Ronald",        "Rowan",        "Roy",        "Ruby",        "Russell",
        "Ruth",        "Ryan",        "Ryder",        "Rylee",        "Ryleigh",
        "Sadie",        "Samantha",        "Samuel",        "Sandra",        "Santiago",
        "Sara",        "Sarah",        "Savannah",        "Scarlett",        "Scott",
        "Sean",        "Sebastian",        "Serenity",        "Sharon",        "Shirley",
        "Silas",        "Skylar",        "Sofia",        "Sophia",        "Sophie",
        "Stella",        "Stephanie",        "Stephen",        "Steven",        "Susan",
        "Teresa",        "Terry",        "Theo",        "Theodore",        "Theresa",
        "Thomas",        "Timothy",        "Tyler",        "Valentina",        "Victoria",
        "Vincent",        "Violet",        "Virginia",        "Walter",        "Waylon",
        "Wayne",        "Weston",        "William",        "Willie",        "Willow",
        "Wyatt",        "Xavier",        "Zachary",        "Zoe",        "Zoey",
    };

    #endregion

    #region [TCS Testing]
    CancellationTokenSource cts = new CancellationTokenSource();
    Dictionary<string, TaskCompletionSource<string>> tasks = new();
    async void Button_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            if (tasks.Count == 0)
            {
                var result = await CreateTaskCompletionSource("1", async () =>
                {
                    Debug.WriteLine("starting work...");
                    await Task.Delay(10000);
                    Debug.WriteLine("...finished work");
                }, cts.Token);
            }
            else
            {
                ListTaskIds();
                CheckTaskAndSetResult("1", "2", false);
                ListTaskIds();
            }
        }
        catch (Exception ex) { Debug.WriteLine($"TaskCompletionSource Test: {ex.Message}"); }
    }
    async Task<string> CreateTaskCompletionSource(string taskId, Action work, CancellationToken token)
    {
        var tcs = new TaskCompletionSource<string>();
        if (token.CanBeCanceled)
        {
            token.Register(() =>
            {
                tcs.TrySetCanceled();
                if (tasks.ContainsKey(taskId))
                    tasks.Remove(taskId);
            });

            if (token.IsCancellationRequested)
                token.ThrowIfCancellationRequested();
        }

        tasks.Add(taskId, tcs);

        work();

        return taskId;
    }
    void CheckTaskAndSetResult(string taskId, string result, bool remove = true)
    {
        if (!string.IsNullOrEmpty(taskId) && tasks.ContainsKey(taskId))
        {
            var task = tasks[taskId];

            if (remove)
                tasks.Remove(taskId);

            task.TrySetResult(result);
        }
    }
    void ListTaskIds()
    {
        Debug.WriteLine($"Listing task IDs...");

        foreach (var t in tasks)
        {
            Debug.WriteLine($"> TaskID: {t.Key}");
            try
            {
                if (t.Value.Task.Status == TaskStatus.RanToCompletion)
                    Debug.WriteLine($" - Value: {t.Value.Task.Result}");
                else
                    Debug.WriteLine($" - Status: {t.Value.Task.Status}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ListTaskIds: {ex.Message}");
            }
        }
    }
    #endregion

    #region [Cancellable Task Class]
    /// <summary>
    /// This is a test method to show the power of our <see cref="TaskWithCTS"/> class.
    /// Another option is to use the WithCancellation() extension method with a task, but
    /// our <see cref="TaskWithCTS"/> provides more useful feedback information.
    /// </summary>
    /// <param name="totalTasks">pick more than 10 to show the oversubscription solution</param>
    /// <param name="addMonitorThread">creates a thread to announce changes during the test</param>
    void TestCancellableTaskClass(int totalTasks = 12, bool addMonitorThread = true)
    {
        Guid? targetID = null;
        List<TaskWithCTS> cancellableTasks = new List<TaskWithCTS>();
        for (int i = 0; i < totalTasks; i++)
        {
            //Task.Yield(); // fork the continuation into a separate work item

            // We need to copy the counter state since we'll be
            // inside a lambda and not using foreach enumerator...
            int idx = i; // <-- avoid the captured variable trap

            // We'll need to create the CTS outside of the Task object so that we
            // can monitor it inside the Action and take the appropriate steps.
            CancellationTokenSource _cts = new CancellationTokenSource(new TimeSpan(0, 1, 0));

            TaskWithCTS task = new TaskWithCTS(() =>
            {
                CancellationTokenRegistration ctr = _cts.Token.Register(() => 
                {
                    InsertMessage($"Token registration invoked for task index {idx}");
                });

                InsertMessage($"Task index {idx} started.");
                // The work function of this task; we're going to pause for a few
                // seconds and during this time we'll see if we've been canceled.
                for (int j = 0; j < 30; j++)
                {
                    Task.Delay(Random.Shared.Next(10, 120)).Wait(); // place-holder for work code

                    if (_cts.IsCancellationRequested)
                    {
                        InsertMessage($"Task index {idx} cancelled!");
                        break;
                    }
                }

                if (!_cts.IsCancellationRequested)
                {
                    InsertMessage($"Task index {idx} finished.");
                }

                // If the registration was never used we'll need to
                // let go so it doesn't hang around in memory forever.
                ctr.Dispose();

            }, _cts);

            cancellableTasks.Add(task);

            // Randomly pick a task to cancel...
            if (targetID == null && Random.Shared.Next(1, 10) > 7)
                targetID = task?._GID; // this will be the task we're going to cancel
        }

        // Find our randomly selected task and call it's Cancel() method...
        TaskWithCTS? canTask = cancellableTasks.FirstOrDefault(x => x._GID == targetID);
        if (canTask != null)
        {
            InsertMessage($"Cancelling {canTask._TID} (created at {canTask._Started})");
            canTask.Cancel();
        }

        if (addMonitorThread)
        {
            // Spin up another thread to watch the tasks so the current thread can resume...
            ThreadPool.QueueUserWorkItem((object? o) =>
            {
                // We'll wait here for all cancellable tasks to complete...
                while (cancellableTasks.Any(x => x.IsRunning() == true))
                    Thread.Sleep(50);

                // We can check for any internal TaskWithCTS object exceptions...
                TaskWithCTS? exTask = cancellableTasks.FirstOrDefault(x => x._Error != null);
                if (exTask != null)
                {
                    InsertMessage($"{exTask._GID} error: {exTask._Error.Message}");
                }

                // We can get a listing of the cancellable tasks and when they ended...
                var finishedTasks = cancellableTasks.Select(x => x).Where(x => x._Ended.HasValue);
                // Use our extension method to action through each one...
                finishedTasks.ForEach(et =>
                {
                    //TimeSpan? tsStart = et._Started?.TimeOfDay;
                    //TimeSpan? tsEnd = et._Ended?.TimeOfDay;
                    TimeSpan? diff = et._Ended - et._Started;
                    InsertMessage($"[{et._GID?.ToString().Substring(0, 8)}] StartTime: {et._Started?.ToLongTimeString()}, FinishTime: {et._Ended?.ToLongTimeString()} ({diff.ToTimeString()})");
                });

                // We can check IsCanceled() on the tasks...
                var isCanTasks = cancellableTasks.Select(x => x).Where(x => x.IsCanceled() == true);
                InsertMessage($"Number of CTs with IsCanceled true: {isCanTasks.Count()}");
            });
        }
    }
    #endregion

    #region [Custom EventArgs Testing]
    async Task CustomEventTesting()
    {
        EventProcessModule pm = new EventProcessModule();
        
        // Register the custom ProcessEventArgs.
        pm.ProcessStarted += PM_ProcessStarted;
        pm.ProcessCompleted += PM_ProcessCompleted;
        pm.ProcessCanceled += PM_ProcessCanceled;

        // Call the object methods.
        await pm.StartProcessAsync();
        await Task.Delay(1000);
        await pm.StopProcessAsync();
    }

    void PM_ProcessStarted(object? sender, ProcessEventArgs e)
    {
        InsertMessage($"{e.Result} ID: {e.Id}");
    }

    void PM_ProcessCompleted(object? sender, ProcessEventArgs e)
    {
        var epm = sender as EventProcessModule;
        if (epm != null) { InsertMessage($"IsRunning? {epm.IsRunning}"); }
        InsertMessage($"{(e.Error == null ? "Success: " + e.Result : "Failed: " + e.Error.Message)}");
        InsertMessage($"Completion Time: {e.CompletionTime.ToLongTimeString()}");
    }
    void PM_ProcessCanceled(object? sender, ProcessEventArgs e)
    {
        var epm = sender as EventProcessModule;
        if (epm != null) { InsertMessage($"⇒ IsRunning? {epm.IsRunning}"); }
        InsertMessage($"{(e.Error == null ? "Success: " + e.Result : "Failed: " + e.Error.Message)}");
        InsertMessage($"Cancel Time: {e.CompletionTime.ToLongTimeString()}");
    }
    #endregion
}
