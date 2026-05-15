using System.Collections.Generic;

public static class Options 
{
    internal const string SELECTED_FPS_KEY = "SelectedFPS";

    private const int FPS_30_VALUE = 30;
    private const int FPS_60_VALUE = 60;
    private const int FPS_75_VALUE = 75;
    private const int FPS_120_VALUE = 120;
    private const int FPS_144_VALUE = 144;

    private const string FPS_30_KEY = "30 FPS";
    private const string FPS_60_KEY = "60 FPS";
    private const string FPS_75_KEY = "75 FPS";
    private const string FPS_120_KEY = "120 FPS";
    private const string FPS_144_KEY = "144 FPS";

    internal static readonly Dictionary<string, int> FPS = new()
    {
        { FPS_30_KEY, FPS_30_VALUE },
        { FPS_60_KEY, FPS_60_VALUE },
        { FPS_75_KEY, FPS_75_VALUE },
        { FPS_120_KEY, FPS_120_VALUE },
        { FPS_144_KEY, FPS_144_VALUE }
    };

}
