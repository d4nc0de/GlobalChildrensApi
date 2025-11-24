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
    public class AsistenciaNotasController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AsistenciaNotasController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("ObtenerAsistenciaSesion/{idsesion:long}")]
        public async Task<ActionResult<IEnumerable<AsistenciaEstudiante>>> GetAsistenciaSesion(long idsesion)
        {
            try
            {
                var asistencias = await _db.asistenciaestudiante
                .Where(ae => ae.estado == "ACT" && ae.sesionclaseid == idsesion)
                .OrderBy(ae => ae.estudianteid)
                .ToListAsync();

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

        [HttpGet("ObtenerAsistenciaEstudiante/{idestudiante:long}")]
        public async Task<ActionResult<Aula>> GetById(long idestudiante)
        {
            try
            {
                var asistencias = await _db.asistenciaestudiante
                .Where(ae => ae.estado == "ACT" && ae.estudianteid == idestudiante)
                .OrderBy(ae => ae.sesionclaseid)
                .ToListAsync();

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


        //CRUD de AsistenciaEstudiante
        [HttpPost("CrearAsistenciaEstudiante")]
        public async Task<ActionResult<AsistenciaEstudiante>> Create([FromBody] AsistenciaEstudiante asistencia)
        {
            try
            {
                // Validar que el estudiante exista
                var estudiante = await _db.estudiante.FindAsync(asistencia.estudianteid);
                if (estudiante == null)
                {
                    return BadRequest($"El estudiante con ID {asistencia.estudianteid} no existe.");
                }

                if (!estudiante.activo || estudiante.estado != "ACT")
                {
                    return BadRequest($"El estudiante no está activo.");
                }

                // Verificar que no exista ya una asistencia para este estudiante en esta sesión
                var existeAsistencia = await _db.asistenciaestudiante
                    .AnyAsync(a => a.sesionclaseid == asistencia.sesionclaseid
                                && a.estudianteid == asistencia.estudianteid);

                if (existeAsistencia)
                {
                    return BadRequest("Ya existe un registro de asistencia para este estudiante en esta sesión de clase.");
                }

                // Si no asistió y tiene motivo de inasistencia, debe estar justificada
                if (!asistencia.asistio && asistencia.motivoinasistenciaestudianteid.HasValue && !asistencia.justificada)
                {
                    asistencia.justificada = true; // Automáticamente se marca como justificada si tiene motivo
                }

                // Si asistió, no puede tener motivo de inasistencia ni estar justificada
                if (asistencia.asistio)
                {
                    asistencia.motivoinasistenciaestudianteid = null;
                    asistencia.justificada = false;
                }

                // Establecer valores por defecto
                if (string.IsNullOrWhiteSpace(asistencia.estado))
                    asistencia.estado = "ACT";

                _db.asistenciaestudiante.Add(asistencia);
                await _db.SaveChangesAsync();

                return CreatedAtAction(nameof(GetById), new { id = asistencia.asistenciaestudianteid }, asistencia);
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

        [HttpPut("ActualizarAsistencia/{idasistencia:long}")]
        public async Task<IActionResult> Update(long idasistencia, [FromBody] AsistenciaEstudiante asistencia)
        {
            try
            {
                // Validar que el id en la URL coincida con el id en el cuerpo
                if (idasistencia != asistencia.asistenciaestudianteid)
                    return BadRequest("El id de la URL no coincide con el id del cuerpo.");

                // Validaciones
                if (asistencia.estado != "ACT" && asistencia.estado != "INA" && asistencia.estado == null)
                {
                    return BadRequest("El estado debe ser 'ACT' o 'INA'.");
                }

                var existing = await _db.asistenciaestudiante.FindAsync(idasistencia);
                if (existing == null)
                    return NotFound($"No existe un asistencia con el id {idasistencia}.");

                // Actualizamos campos
                existing.asistio = asistencia.asistio;
                existing.observacion = asistencia.observacion;
                existing.justificada = asistencia.justificada;
                existing.estado = asistencia.estado;
                existing.sesionclaseid = asistencia.sesionclaseid;
                existing.estudianteid = asistencia.estudianteid;
                existing.motivoinasistenciaestudianteid = asistencia.motivoinasistenciaestudianteid;

                await _db.SaveChangesAsync();

                return Ok("Asistencia de estudiante actualizada correctamente");
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

        //Cambia el estado de la entidad a INA y activo a false
        [HttpDelete("InactivarAsistencia/{id:long}")]
        public async Task<IActionResult> Inactivate(long id)
        {
            try
            {
                var asistencia = await _db.asistenciaestudiante.FindAsync(id);
                if (asistencia == null || asistencia.estado == "INA")
                    return NotFound($"No existe una asistencia activa con el id {id}.");

                asistencia.estado = "INA";
                await _db.SaveChangesAsync();

                return Ok("Asistencia inactivada correctamente");
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

