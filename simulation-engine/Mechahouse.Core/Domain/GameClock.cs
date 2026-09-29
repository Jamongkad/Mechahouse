namespace Mechahouse.Core.Domain;

public sealed class GameClock
{
    private const double MinutesPerHour = 60;
    private const double MinutesPerDay = 24 * MinutesPerHour;

    public double ElapsedMinutes { get; private set; }

    public long DayNumber => (long)Math.Floor(ElapsedMinutes / MinutesPerDay) + 1;

    public int HourOfDay => (int)(ElapsedMinutes % MinutesPerDay / MinutesPerHour);

    public int MinuteOfHour => (int)(ElapsedMinutes % MinutesPerHour);

    internal void Advance(double minutes)
    {
        if (!double.IsFinite(minutes) || minutes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minutes), "Game time must advance by a finite, non-negative number of minutes.");
        }

        ElapsedMinutes += minutes;
    }
}
