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
- La sesion se hace por Google Meet (se genera un link unico por reserva)

## Sistema de packs / tokens

- Los packs son bloques de horas (ej: 10 clases)
- El Admin los carga al usuario (el pago se maneja por fuera por ahora)
- El usuario ve cuantos tokens le quedan
- Se pueden comprar packs extras

## Invitaciones

- Solo se entra por invitacion (email)
- El Admin invita tanto usuarios como instructores
- El invitado recibe un link para registrarse (expira en 7 dias)

## Datos de cada usuario

- **Todos**: nombre, email, telefono, rol
- **Usuario**: + VPS asignado + tokens disponibles
- **Instructor**: + disponibilidad horaria

---

## Roadmap

### Fase 1 - Base de datos y roles
- [x] Redisenar init.sql con tablas: Users, Invitations, TokenPacks, Availabilities, Bookings, AppSettings
- [x] Adaptar AppDbContext.cs y crear modelos C#
- [x] Adaptar login para 3 roles (Admin, Instructor, Usuario)
- [x] Registro por invitacion unicamente

### Fase 2 - Sistema de invitaciones
- [x] Backend: InvitationService + InvitationsController
- [x] Frontend: Pagina de invitaciones (admin)
- [x] Pagina de registro publico por token

### Fase 3 - Panel de Admin
- [x] Listado de usuarios/instructores
- [x] Editar datos de cualquier usuario (VpsInfo incluido)
- [x] Cargar packs de tokens a un usuario
- [x] Configuraciones generales (CancellationHours, BrandName)
- [x] Vista de todas las reservas

### Fase 4 - Calendario del Instructor
- [x] Vista de calendario semanal (8-21h x 7 dias)
- [x] Marcar/desmarcar bloques de 1 hora
- [x] Ver reservas confirmadas

### Fase 5 - Reservas del Usuario
- [x] Ver tokens disponibles en dashboard
- [x] Ver horarios disponibles de instructores
- [x] Reservar un bloque (descuenta 1 token)
- [x] Cancelar reserva (devuelve token si cumple anticipacion)
- [x] Mis Reservas con historial

### Fase 6 - Integracion Google Meet
- [x] Generar link unico de Google Meet al crear cada reserva
- [x] Mostrar link a usuario e instructor en Mis Reservas

### Fase 7 - Pulir y publicar
- [x] Verificar que API compila correctamente
- [x] Verificar que frontend compila correctamente
- [x] Verificar login y endpoints funcionan en desarrollo
- [x] Corregir endpoints del dashboard
- [x] Agregar VpsInfo a modelos frontend
- [ ] Publicar en produccion (cuando el usuario lo pida)
