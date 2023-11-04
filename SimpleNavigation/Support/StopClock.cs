using System;
using System.Diagnostics;
using System.Text;

namespace SimpleNavigation;

/* How to use...

    using (StopClock sc = new StopClock("Some Process"))
    {
        //some process that takes some time
    }

 */
public class StopClock : IDisposable
{
    private Stopwatch? m_watch;
    private string m_title;
    private bool m_console;

    public StopClock(string title = "Stopwatch", bool console = false)
    {
        m_watch = Stopwatch.StartNew();
        m_title = title;
        m_console = console;
    }

    public Stopwatch? Stop()
    {
        if (m_watch != null)
            m_watch.Stop();

        return m_watch;
    }

    public void Print()
    {
        if (m_watch != null)
        {
            if (m_console)
            {
                if (Console.OutputEncoding == Encoding.UTF7 || Console.OutputEncoding == Encoding.ASCII)
                    Console.OutputEncoding = Encoding.UTF8;

                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine($"⇒ {m_title}: Execution lasted {m_watch.ElapsedMilliseconds} ms ({m_watch.ElapsedMilliseconds / 1000} sec)");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            else
            {
                Debug.WriteLine($"======================================================================================");
                Debug.WriteLine($"⇒ {m_title}: Execution lasted {m_watch.ElapsedMilliseconds} m ({m_watch.ElapsedMilliseconds / 1000} sec)");
                Debug.WriteLine($"======================================================================================");
            }
        }
    }

    public void Dispose()
    {
        Stop();
        Print();
        m_watch = null;
    }
}