namespace Ttw.Core.Activities;

/// <summary>
/// The activity types the watch reports and their human readable descriptions.
/// </summary>
public static class ActivityTypes
{
    /// <summary>Running.</summary>
    public const int Running = 0;

    /// <summary>Cycling.</summary>
    public const int Cycling = 1;

    /// <summary>Swimming.</summary>
    public const int Swimming = 2;

    /// <summary>Treadmill.</summary>
    public const int Treadmill = 7;

    /// <summary>Freestyle.</summary>
    public const int Freestyle = 8;

    /// <summary>Gym.</summary>
    public const int Gym = 9;

    /// <summary>Hiking.</summary>
    public const int Hiking = 10;

    /// <summary>Indoor cycling.</summary>
    public const int IndoorCycling = 11;

    /// <summary>Trail running.</summary>
    public const int TrailRunning = 14;

    /// <summary>Skiing.</summary>
    public const int Skiing = 15;

    /// <summary>Snowboarding.</summary>
    public const int Snowboarding = 16;

    /// <summary>
    /// Returns the description of an activity type, or "Unknown" for a type the
    /// watch does not report.
    /// </summary>
    public static string Describe(int activityType) => activityType switch
    {
        Running => "Running",
        Cycling => "Cycling",
        Swimming => "Swimming",
        Treadmill => "Treadmill",
        Freestyle => "Freestyle",
        Gym => "Gym",
        Hiking => "Hiking",
        IndoorCycling => "IndoorCycling",
        TrailRunning => "TrailRunning",
        Skiing => "Skiing",
        Snowboarding => "Snowboarding",
        _ => "Unknown",
    };
}
