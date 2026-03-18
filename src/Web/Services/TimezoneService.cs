namespace Web.Services;

public class TimezoneService
{
    private string _timezone = "America/Argentina/Buenos_Aires";
    public string CurrentTimezone => _timezone;
    public void SetTimezone(string tz) { _timezone = tz; }

    public static List<(string Id, string Label)> GetTimezones() => new()
    {
        ("America/Argentina/Buenos_Aires", "Buenos Aires (UTC-3)"),
        ("America/Sao_Paulo", "São Paulo (UTC-3)"),
        ("America/Montevideo", "Montevideo (UTC-3)"),
        ("America/Santiago", "Santiago (UTC-4)"),
        ("America/Bogota", "Bogotá (UTC-5)"),
        ("America/Lima", "Lima (UTC-5)"),
        ("America/New_York", "New York (UTC-5)"),
        ("America/Mexico_City", "Ciudad de México (UTC-6)"),
        ("America/Chicago", "Chicago (UTC-6)"),
        ("America/Denver", "Denver (UTC-7)"),
        ("America/Los_Angeles", "Los Angeles (UTC-8)"),
        ("Europe/London", "Londres (UTC+0)"),
        ("Europe/Madrid", "Madrid (UTC+1)"),
        ("Europe/Paris", "París (UTC+1)"),
        ("Europe/Berlin", "Berlín (UTC+1)"),
        ("UTC", "UTC"),
    };

    public string GetTimezoneLabel()
    {
        var tz = GetTimezones().FirstOrDefault(t => t.Id == _timezone);
        return tz.Label ?? _timezone;
    }
}
