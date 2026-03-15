namespace Web.Services;

public class BrandSettingsService
{
    public string BrandName { get; private set; } = "Tu Marca";
    public string BrandIcon { get; private set; } = "Brand";
    public string PrimaryColor { get; private set; } = "#10b981";
    public string SidebarBgColor { get; private set; } = "#1a1a2e";

    public event Action? OnChange;

    public void Update(string key, string value)
    {
        switch (key)
        {
            case "BrandName": BrandName = value; break;
            case "BrandIcon": BrandIcon = value; break;
            case "PrimaryColor": PrimaryColor = value; break;
            case "SidebarBgColor": SidebarBgColor = value; break;
        }
        OnChange?.Invoke();
    }

    public void LoadFromSettings(Dictionary<string, string> settings)
    {
        if (settings.TryGetValue("BrandName", out var name)) BrandName = name;
        if (settings.TryGetValue("BrandIcon", out var icon)) BrandIcon = icon;
        if (settings.TryGetValue("PrimaryColor", out var color)) PrimaryColor = color;
        if (settings.TryGetValue("SidebarBgColor", out var bg)) SidebarBgColor = bg;
        OnChange?.Invoke();
    }
}
