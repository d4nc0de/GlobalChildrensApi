using GlobalChildrensApi.Controllers;
using GlobalChildrensApi.Models;
using Microsoft.EntityFrameworkCore;

namespace GlobalChildrensApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Sede> sede => Set<Sede>();
        public DbSet<Aula> aula => Set<Aula>();
        public DbSet<AsistenciaEstudiante> asistenciaestudiante => Set<AsistenciaEstudiante>();
        public DbSet<SesionClase> sesionclase => Set<SesionClase>();
        public DbSet<Estudiante> estudiante => Set<Estudiante>();
        public DbSet<Nota> nota => Set<Nota>();
        public DbSet<ComponenteNota> componentenota => Set<ComponenteNota>();
        public DbSet<Tutor> tutor => Set<Tutor>();
        public DbSet<CalendarioSemanalPrograma> calendariosemanalprograma => Set<CalendarioSemanalPrograma>();
        public DbSet<PeriodoEvaluacion> periodoevaluacion => Set<PeriodoEvaluacion>();
        public DbSet<AulaTutorHistorico> aulatutorhistorico => Set<AulaTutorHistorico>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasDefaultSchema("public");

            //modelBuilder.Entity<Sede>(entity =>
            //{
            //    entity.ToTable("sede");          // nombre de la tabla en Supabase

            //    entity.HasKey(e => e.Id_Sede);

            //    entity.Property(e => e.Id_Sede)
            //          .HasColumnName("id_sede");

            //    entity.Property(e => e.Nombre)
            //          .HasColumnName("nombre");

            //    entity.Property(e => e.Direccion)
            //          .HasColumnName("direccion");

            //    entity.Property(e => e.Es_Principal)
            //          .HasColumnName("es_principal");

            //    entity.Property(e => e.Estado)
            //          .HasColumnName("estado");

            //    entity.Property(e => e.Id_Institucion)
            //          .HasColumnName("id_institucion");

            //    //entity.Property(e => e.CreatedAt)
            //    //      .HasColumnName("created_at");
            //});
        }
    }
}
