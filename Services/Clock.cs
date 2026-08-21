namespace LibraryAppPrototype.Services;

// Satu-satunya abstraksi yang dipertahankan di proyek ini (PRD 11.3).
// BR-06, BR-08, dan BR-19 semuanya bergantung pada "hari ini".
public interface IClock
{
    DateOnly Today { get; }
    DateTime UtcNow { get; }
}

// SATU-SATUNYA tempat DateTime.Now / DateTime.UtcNow boleh muncul di seluruh project.
public class SystemClock : IClock
{
    public DateOnly Today => DateOnly.FromDateTime(DateTime.Now);
    public DateTime UtcNow => DateTime.UtcNow;
}
