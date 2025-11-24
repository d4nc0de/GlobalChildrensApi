using GlobalChildrensApi.Data;
using GlobalChildrensApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GlobalChildrensApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize] // Usa el JWT de Supabase
    public class ReporteTutorController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ReporteTutorController(AppDbContext db)
        {
            _db = db;
        }

        //GET de reportes para tutores
        [HttpGet("ObtenerAsistenciaEstudianteAulaSemana/{numsemana:long}-{idaula:long}")]
        public async Task<ActionResult<AsistenciaEstudiante>> GetAsistenciaEstudianteAulaBySemana(long numsemana, long idaula)
        {
            try
            {
                //verificar que el aula existe y esta activa
                var aula = await _db.aula.FindAsync(idaula);
                if (aula == null || aula.estado == "INA")
                {
                    return NotFound($"No existe aula con id{idaula}");
                }

                // Verificar que el numero de semana existe y está activa
                var semana = await _db.calendariosemanalprograma
                    .FirstOrDefaultAsync(csp => csp.numero_semana == numsemana && csp.estado == "ACT");
                if (semana == null)
                {
                    return NotFound($"No existe una semana activa con el numero {numsemana}.");
                }

                // Obtener las sesiones de clase activas
                var sesiones = await _db.sesionclase
                    .Where(sc => sc.estado == "ACT" && sc.aulaid == idaula && sc.calendariosemanalprogramaid == semana.calendariosemanalprogramaid)
                    .Select(sc => sc.sesionclaseid)
                    .ToListAsync();
                if (sesiones.Count == 0)
                {
                    return NotFound($"No existen sesiones activas para el aula {idaula} en la semana {numsemana}.");
                }

                //Obtener las asistencias de estudiantes para las sesiones de clase obtenidas
                var asistencias = await _db.asistenciaestudiante
                    .Where(ae => ae.estado == "ACT" && sesiones.Contains(ae.sesionclaseid))
                    .OrderBy(ae => ae.sesionclaseid)
                    .ToListAsync();
                if (asistencias.Count() == 0)
                {
                    return NotFound($"No existen asistencias activas para estas sesiones de clase.");
                }
                return Ok(asistencias);
            }
            catch (Exception ex)
            {
                return StatusCode(503, new
                {
                    message = "No fue posible comunicarse con la base de datos. Intenta de nuevo.",
                    error = ex.Message
                });
            }
        }

        [HttpGet("ObtenerAsistenciaEstudianteAulaFecha/{fechainicio:datetime}_{fechafin:datetime}_{idaula:long}")]
        public async Task<ActionResult<System.Collections.Generic.IEnumerable<AsistenciaEstudiante>>> GetAsistenciaEstudianteByFecha(DateTime fechainicio, DateTime fechafin, long idaula)
        {
            try
            {
                fechainicio = DateTime.SpecifyKind(fechainicio, DateTimeKind.Utc);
                fechafin = DateTime.SpecifyKind(fechafin, DateTimeKind.Utc);

                // Validación de fechas
                if (fechainicio > fechafin || fechainicio > DateTime.UtcNow || fechafin > DateTime.UtcNow)
                {
                    return BadRequest("Fechas inválidas: asegúrate que el inicio sea anterior al fin y que no estén en el futuro.");
                }

                // Validar que el aula existe y está activa
                var aula = await _db.aula.FindAsync(idaula);
                if (aula == null || aula.estado == "INA")
                {
                    return NotFound($"No existe aula activa con id {idaula}.");
                }

                // Obtener sesiones activas del aula dentro del rango de fechas
                var sesiones = await _db.sesionclase
                    .Where(sc => sc.estado == "ACT"
                                 && sc.aulaid == idaula
                                 && sc.fecha_real >= fechainicio
                                 && sc.fecha_real <= fechafin)
                    .Select(sc => sc.sesionclaseid)
                    .ToListAsync();

                if (sesiones == null || !sesiones.Any())
                {
                    return NotFound($"No existen sesiones activas para el aula {idaula} en el rango {fechainicio:yyyy-MM-dd} - {fechafin:yyyy-MM-dd}.");
                }

                // Obtener asistencias activas para las sesiones encontradas
                var asistencias = await _db.asistenciaestudiante
                    .Where(ae => ae.estado == "ACT" && sesiones.Contains(ae.sesionclaseid))
                    .OrderBy(ae => ae.sesionclaseid)
                    .ToListAsync();

                if (asistencias == null || !asistencias.Any())
                {
                    return NotFound("No existen asistencias activas para las sesiones encontradas.");
                }

                return Ok(asistencias);
            }
            catch (Exception ex)
            {
                return StatusCode(503, new
                {
                    message = "No fue posible comunicarse con la base de datos. Intenta de nuevo.",
                    error = ex.Message
                });
            }
        }

        [HttpGet("ObtenerNotasAulaFecha/{fechainicio:datetime}_{fechafin:datetime}_{idaula:long}")]
        public async Task<ActionResult<System.Collections.Generic.IEnumerable<AsistenciaEstudiante>>> GetNotaAulaByFecha(DateTime fechainicio, DateTime fechafin, long idaula)
        {
            try
            {
                fechainicio = DateTime.SpecifyKind(fechainicio, DateTimeKind.Utc);
                fechafin = DateTime.SpecifyKind(fechafin, DateTimeKind.Utc);

                // Validación de fechas
                if (fechainicio > fechafin || fechainicio > DateTime.UtcNow || fechafin > DateTime.UtcNow)
                {
                    return BadRequest("Fechas inválidas: asegúrate que el inicio sea anterior al fin y que no estén en el futuro.");
                }

                // Validar que el aula existe y está activa
                var aula = await _db.aula.FindAsync(idaula);
                if (aula == null || aula.estado == "INA")
                {
                    return NotFound($"No existe aula activa con id {idaula}.");
                }

                // Obtener estudiantes activos en el aula
                var estudiantes = await _db.estudiante
                    .Where(e => e.estado == "ACT" && e.aulaid == idaula)
                    .Select(e => e.estudianteid)
                    .ToListAsync();
                if (estudiantes == null || !estudiantes.Any())
                {
                    return NotFound($"No existen estudiantes activos en el aula con id {idaula}.");
                }

                // Obtener periodos de evaluacion activos dentro del rango de fechas
                var periodos = await _db.periodoevaluacion
                    .Where(pe => pe.estado == "ACT"
                                    && pe.fecha_inicio >= fechainicio
                                    && pe.fecha_fin <= fechafin)
                    .Select(pe => pe.periodoevaluacionid)
                    .ToListAsync();

                if (periodos == null || !periodos.Any())
                {
                    return NotFound($"No existen periodos de evaluacion en el rango {fechainicio:yyyy-MM-dd} - {fechafin:yyyy-MM-dd}.");
                }

                // Obtener notas activas de los estiantes del aula para los periodos encontrados
                var notas = await _db.nota
                    .Where(n => n.estado == "ACT"
                            && periodos.Contains(n.periodoevaluacionid)
                            && estudiantes.Contains(n.estudianteid))
                    .OrderBy(n => n.notaid)
                    .ToListAsync();

                if (notas == null || !notas.Any())
                {
                    return NotFound("No existen notas activas para los estudiantes del aula en el periodo seleccionado.");
                }

                return Ok(notas);
            }
            catch (Exception ex)
            {
                return StatusCode(503, new
                {
                    message = "No fue posible comunicarse con la base de datos. Intenta de nuevo.",
                    error = ex.Message
                });
            }
        }

    }
}

