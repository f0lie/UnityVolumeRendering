using System.Collections;
using System.Collections.Generic;

public class BrainSection
{
    public float r { get; set; }
    public float g { get; set; }
    public float b { get; set; }
    public float a { get; set; }

    public string label { get; set; }
    public float intensity { get; set; }

    public BrainSection() {}
    public BrainSection(float _r, float _g, float _b, float _a, string _label, float _intensity)
    {
        r = _r;
        g = _g;
        b = _b;
        a = _a;
        label = _label;
        intensity = _intensity;
    }
}
