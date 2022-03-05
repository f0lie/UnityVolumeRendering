using System.Collections;
using System.Collections.Generic;

public class BrainSection
{
    public int r { get; set; }
    public int g { get; set; }
    public int b { get; set; }
    public int a { get; set; }

    public string label { get; set; }

    public BrainSection() {}
    public BrainSection(int _r, int _g, int _b, int _a, string _label)
    {
        r = _r;
        g = _g;
        b = _b;
        a = _a;
        label = _label;
    }
}
