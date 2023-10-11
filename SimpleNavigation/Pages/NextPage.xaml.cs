using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace SimpleNavigation;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class NextPage : Page
{
    /// <summary>
    /// An event that the main page can subscribe to.
    /// </summary>
    public static event EventHandler<Message>? PostMessageEvent;

    public NextPage()
    {
        Debug.WriteLine($"{MethodBase.GetCurrentMethod()?.DeclaringType?.Name}__{MethodBase.GetCurrentMethod()?.Name} [{DateTime.Now.ToString("hh:mm:ss.fff tt")}]");
        this.InitializeComponent();
        this.Loaded += (_, _) => { ttAutoSuggest.IsOpen = true; };
        #region [Config behavior using code-behind instead of XAML]
        //var behaviors = Microsoft.Xaml.Interactivity.Interaction.GetBehaviors(asbName);
        //var userStoppedTyping = new Behaviors.TypingPauseBehavior { MinimumDelay = 500, MinimumCharacters = 1 };
        //userStoppedTyping.TypingPaused += AutoSuggestBox_TypingPaused;
        //behaviors.Add(userStoppedTyping);
        #endregion
    }

    /// <summary>
    /// Handle any parameter passed.
    /// </summary>
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter != null && e.Parameter is SystemState sys)
        {
            // ⇦ ⇨ ⇧ ⇩  🡐 🡒 🡑 🡓  🡄 🡆 🡅 🡇  http://xahlee.info/comp/unicode_arrows.html
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

}
