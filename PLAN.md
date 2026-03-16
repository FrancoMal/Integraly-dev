# Plan de desarrollo - Integraly.dev

## Descripcion

Integraly.dev es una plataforma de gestion de VPS + tutorias 1 a 1 con instructores.

## Roles

| | Admin | Instructor | Usuario |
|---|---|---|---|
| **Ve** | Todo: usuarios, instructores, reservas, packs | Su calendario, sus reservas | Su VPS, sus horas disponibles, sus reservas |
| **Hace** | Invitar usuarios/instructores, cargar packs de tokens, configurar reglas | Marca horarios disponibles | Reserva horarios, cancela con anticipacion |

## Sistema de tutorias

- El instructor arma su disponibilidad semanal en un calendario
- El usuario ve los horarios libres y reserva (1 hora = 1 clase)
- Cada reserva descuenta 1 token del pack del usuario
- Si cancela con anticipacion (configurable, default 24hs), se le devuelve el token
- La sesion se hace por Google Meet (se genera o asigna un link)

## Sistema de packs / tokens

- Los packs son bloques de horas (ej: 10 clases)
- El Admin los carga al usuario (el pago se maneja por fuera por ahora)
- El usuario ve cuantos tokens le quedan
- Se pueden comprar packs extras

## Invitaciones

- Solo se entra por invitacion (email)
- El Admin invita tanto usuarios como instructores
- El invitado recibe un mail con un link para registrarse

## Datos de cada usuario

- **Todos**: nombre, email, telefono, rol
- **Usuario**: + VPS asignado + tokens disponibles
- **Instructor**: + disponibilidad horaria

---

## Roadmap

### Fase 1 - Base de datos y roles
- [x] Redisenar init.sql con tablas: Users, Invitations, Packs, Availability, Bookings, Settings
- [x] Adaptar AppDbContext.cs y crear modelos C#
- [x] Adaptar login para 3 roles (Admin, Instructor, Usuario)

### Fase 2 - Sistema de invitaciones
- [ ] Panel del Admin para crear invitaciones
- [ ] Envio de email con link de registro
- [ ] Pagina de registro para invitados

### Fase 3 - Panel de Admin
- [ ] Listado de usuarios/instructores con filtros
- [ ] Editar datos de cualquier usuario
- [ ] Cargar packs de tokens a un usuario
- [ ] Configuraciones generales
- [ ] Vista de todas las reservas

### Fase 4 - Calendario del Instructor
- [ ] Vista de calendario semanal
- [ ] Marcar/desmarcar bloques de 1 hora
- [ ] Ver reservas confirmadas

### Fase 5 - Reservas del Usuario
- [ ] Ver tokens disponibles
- [ ] Ver horarios disponibles de instructores
- [ ] Reservar un bloque (descuenta 1 token)
- [ ] Cancelar reserva (devuelve token si cumple anticipacion)
- [ ] Ver info de VPS

### Fase 6 - Integracion Google Meet
- [ ] Generar o asignar link de Google Meet a cada reserva
- [ ] Mostrar link a usuario e instructor

### Fase 7 - Pulir y publicar
- [ ] Revisar diseno de todas las pantallas
- [ ] Probar flujos completos
- [ ] Publicar en produccion
