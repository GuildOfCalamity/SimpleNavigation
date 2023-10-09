using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System;

namespace SimpleNavigation;

public class KeyboardInput
{
    public Windows.System.VirtualKey? virtualKey { get; set; }
    public Windows.System.VirtualKeyModifiers? virtualKeyModifiers { get; set; }
    public bool? handled { get; set; }
}
