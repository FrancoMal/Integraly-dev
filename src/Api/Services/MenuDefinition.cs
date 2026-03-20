namespace Api.Services;

public static class MenuDefinition
{
    public static readonly List<MenuGroup> MenuTree = new()
    {
        new MenuGroup("principal", "Principal", new[]
        {
            new MenuItem("dashboard", "Dashboard", "/")
        }),
        new MenuGroup("tutorias", "Tutorias", new[]
        {
            new MenuItem("calendario", "Calendario", "/calendario"),
            new MenuItem("reservar", "Reservar", "/reservar"),
            new MenuItem("mis-reservas", "Mis Reservas", "/mis-reservas")
        }),
        new MenuGroup("administracion", "Administracion", new[]
        {
            new MenuItem("usuarios", "Usuarios", "/usuarios"),
            new MenuItem("invitaciones", "Invitaciones", "/invitaciones"),
            new MenuItem("packs", "Packs", "/packs"),
            new MenuItem("todas-reservas", "Todas las Reservas", "/todas-reservas"),
            new MenuItem("auditoria", "Auditoria", "/auditoria"),
            new MenuItem("config", "Configuracion", "/config"),
            new MenuItem("webinar", "Webinar", "/webinar")
        }),
        new MenuGroup("cuenta", "Cuenta", new[]
        {
            new MenuItem("perfil", "Mi Perfil", "/perfil")
        })
    };

    public static readonly List<string> AllMenuKeys =
        MenuTree.SelectMany(g => g.Items.Select(i => i.Key)).ToList();
}

public record MenuGroup(string GroupKey, string Label, MenuItem[] Items);
public record MenuItem(string Key, string Label, string Route);
