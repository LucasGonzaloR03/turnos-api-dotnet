using Microsoft.EntityFrameworkCore;
using Turnos.Domain.Entities;

namespace Turnos.Infrastructure.Persistence;

public class TurnosDbContext(DbContextOptions<TurnosDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Profesional> Profesionales => Set<Profesional>();
    public DbSet<Servicio> Servicios => Set<Servicio>();
    public DbSet<HorarioAtencion> HorariosAtencion => Set<HorarioAtencion>();
    public DbSet<Turno> Turnos => Set<Turno>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("Usuarios");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(255);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.Rol).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<Profesional>(entity =>
        {
            entity.ToTable("Profesionales");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(p => p.Especialidad).IsRequired().HasMaxLength(100);

            entity.HasMany(p => p.Servicios)
                  .WithMany(s => s.Profesionales)
                  .UsingEntity<Dictionary<string, object>>(
                      "ProfesionalServicio",
                      join => join.HasOne<Servicio>().WithMany().HasForeignKey("ServicioId").OnDelete(DeleteBehavior.Cascade),
                      join => join.HasOne<Profesional>().WithMany().HasForeignKey("ProfesionalId").OnDelete(DeleteBehavior.Cascade),
                      join => join.HasKey("ProfesionalId", "ServicioId"));
        });

        modelBuilder.Entity<Servicio>(entity =>
        {
            entity.ToTable("Servicios");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(s => s.DuracionMinutos).IsRequired();
            entity.Property(s => s.Precio).HasColumnType("numeric(10,2)").IsRequired();
            entity.Property(s => s.HorasAnticipoCancelacion).IsRequired();
        });

        modelBuilder.Entity<HorarioAtencion>(entity =>
        {
            entity.ToTable("HorariosAtencion");
            entity.HasKey(h => h.Id);
            entity.Property(h => h.DiaSemana).HasConversion<string>().HasMaxLength(10);
            entity.Property(h => h.HoraInicio).HasColumnType("time without time zone");
            entity.Property(h => h.HoraFin).HasColumnType("time without time zone");

            entity.HasOne(h => h.Profesional)
                  .WithMany(p => p.Horarios)
                  .HasForeignKey(h => h.ProfesionalId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Turno>(entity =>
        {
            entity.ToTable("Turnos");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Inicio).HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(t => t.Fin).HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(t => t.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();

            entity.HasOne(t => t.Usuario)
                  .WithMany(u => u.Turnos)
                  .HasForeignKey(t => t.UsuarioId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.Profesional)
                  .WithMany(p => p.Turnos)
                  .HasForeignKey(t => t.ProfesionalId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.Servicio)
                  .WithMany(s => s.Turnos)
                  .HasForeignKey(t => t.ServicioId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(t => new { t.ProfesionalId, t.Inicio });
            entity.HasIndex(t => t.UsuarioId);
            entity.HasIndex(t => t.ServicioId);
        });
    }
}
