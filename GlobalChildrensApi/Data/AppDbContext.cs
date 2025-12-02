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
        public DbSet<Institucion> institucion => Set<Institucion>();
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
        public DbSet<Persona> persona => Set<Persona>();
        public DbSet<AuthUser> AuthUsers => Set<AuthUser>();
        public DbSet<Festivo> festivo => Set<Festivo>();
        public DbSet<AulaHorarioHistorico> aulahorariohistorico => Set<AulaHorarioHistorico>();
        public DbSet<Programa> programa => Set<Programa>();
        public DbSet<Jornada> jornada => Set<Jornada>();
        public DbSet<TipoDocumento> tipodocumento => Set<TipoDocumento>();
        public DbSet<MotivoInasistenciaEstudiante> motivoinasistenciaestudiante => Set<MotivoInasistenciaEstudiante>();
        public DbSet<MotivoNoClase> motivonoclase => Set<MotivoNoClase>();
        public DbSet<Horario> horario => Set<Horario>();
        public DbSet<HorarioDetalle> horariodetalle => Set<HorarioDetalle>();
        public DbSet<Institucion> institucion => Set<Institucion>();

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

            modelBuilder.Entity<AuthUser>(entity =>
            {
                entity.ToTable("users", "auth"); 

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Instance_Id).HasColumnName("instance_id");
                entity.Property(e => e.Aud).HasColumnName("aud");
                entity.Property(e => e.Role).HasColumnName("role");
                entity.Property(e => e.Email).HasColumnName("email");
                entity.Property(e => e.Encrypted_Password).HasColumnName("encrypted_password");
                entity.Property(e => e.Email_Confirmed_At).HasColumnName("email_confirmed_at");
                entity.Property(e => e.Invited_At).HasColumnName("invited_at");
                entity.Property(e => e.Confirmation_Token).HasColumnName("confirmation_token");
                entity.Property(e => e.Confirmation_Sent_At).HasColumnName("confirmation_sent_at");
                entity.Property(e => e.Recovery_Token).HasColumnName("recovery_token");
                entity.Property(e => e.Recovery_Sent_At).HasColumnName("recovery_sent_at");
                entity.Property(e => e.Email_Change_Token_New).HasColumnName("email_change_token_new");
                entity.Property(e => e.Email_Change).HasColumnName("email_change");
                entity.Property(e => e.Email_Change_Sent_At).HasColumnName("email_change_sent_at");
                entity.Property(e => e.Last_Sign_In_At).HasColumnName("last_sign_in_at");
                entity.Property(e => e.Raw_App_Meta_Data).HasColumnName("raw_app_meta_data");
                entity.Property(e => e.Raw_User_Meta_Data).HasColumnName("raw_user_meta_data");
                entity.Property(e => e.Is_Super_Admin).HasColumnName("is_super_admin");
                entity.Property(e => e.Created_At).HasColumnName("created_at");
                entity.Property(e => e.Updated_At).HasColumnName("updated_at");
                entity.Property(e => e.Phone).HasColumnName("phone");
                entity.Property(e => e.Phone_Confirmed_At).HasColumnName("phone_confirmed_at");
                entity.Property(e => e.Phone_Change).HasColumnName("phone_change");
                entity.Property(e => e.Phone_Change_Token).HasColumnName("phone_change_token");
                entity.Property(e => e.Phone_Change_Sent_At).HasColumnName("phone_change_sent_at");
                entity.Property(e => e.Confirmed_At).HasColumnName("confirmed_at");
                entity.Property(e => e.Email_Change_Token_Current).HasColumnName("email_change_token_current");
                entity.Property(e => e.Email_Change_Confirm_Status).HasColumnName("email_change_confirm_status");
                entity.Property(e => e.Banned_Until).HasColumnName("banned_until");
                entity.Property(e => e.Reauthentication_Token).HasColumnName("reauthentication_token");
                entity.Property(e => e.Reauthentication_Sent_At).HasColumnName("reauthentication_sent_at");
                entity.Property(e => e.Is_Sso_User).HasColumnName("is_sso_user");
                entity.Property(e => e.Deleted_At).HasColumnName("deleted_at");
                entity.Property(e => e.Is_Anonymous).HasColumnName("is_anonymous");
            });

        }
    }
}
