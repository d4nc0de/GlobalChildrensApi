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
    public class SesionesClaseController : ControllerBase
    {
        private readonly AppDbContext _db;

        public SesionesClaseController(AppDbContext db)
        {
            _db = db;
        }

        // CRUD de sesiones de clase

        // Obtener todas las sesiones de clase activas
        [HttpGet("GetAllSesionClase")]
        public async Task<ActionResult<IEnumerable<SesionClase>>> GetAllSesionClase()
        {
            try
            {
                var sesiones = await _db.sesionclase
                    .Where(s => s.estado == "ACT")
                    .OrderByDescending(s => s.fecha_real)
                    .ThenBy(s => s.hora_inicio_programada)
                    .ToListAsync();

                return Ok(sesiones);
            }
            catch (Exception ex)
            {
                return StatusCode(503, new
                {
                    error = "ERROR_SERVIDOR",
                    message = "No fue posible comunicarse con la base de datos. Intenta de nuevo.",
                    details = ex.Message
                });
            }
        }

        [HttpGet("ObtenerSesionClase/{id:long}")]
        public async Task<ActionResult<SesionClase>> GetSesionClaseById(long id)
        {
            try
            {
                //verificar si la sesion existe y está activa
                var sesion = await _db.sesionclase.FindAsync(id);
                if (sesion == null || sesion.estado == "INA")
                {
                    return NotFound($"No existe sesión de clase activa con id {id}.");
                }

                return Ok(sesion);
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

        [HttpGet("ObtenerSesionClaseReposicion/{idsesion:long}")]
        public async Task<ActionResult<SesionClase>> GetSesionClaseReposicionById(long idsesion)
        {
            try
            {
                //verificar si la sesion original existe y está activa
                var sesion = await _db.sesionclase.FindAsync(idsesion);
                if (sesion == null || sesion.estado == "INA")
                {
                    return NotFound($"No existe sesión de clase activa con id {idsesion}.");
                }

                // verificar si tiene reposición asociada
                if (sesion.sesionrepuestaid == null || sesion.es_reposicion == true)
                {
                    return NotFound($"La sesión de clase con id {idsesion} no tiene una sesión de reposición asociada o es una reposición.");
                }

                // obtener la sesión de reposición
                var sesionReposicion = await _db.sesionclase.FindAsync(sesion.sesionrepuestaid);
                if (sesionReposicion == null || sesionReposicion.estado == "INA")
                {
                    return NotFound($"No existe sesión de clase de reposición activa con id {sesion.sesionrepuestaid}.");
                }

                return Ok(sesionReposicion);
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


        [HttpPost("CrearSesionClase")]
        public async Task<ActionResult<SesionClase>> CreateSesionClase([FromBody] SesionClase sesion)
        {
            try
            {
                // Validar que la sesion no sea nula
                if (sesion == null)
                {
                    return BadRequest("El objeto de asistencia no puede ser nulo.");
                }

                // Validar fechas
                if (sesion.fecha_real == default(DateTime) || sesion.fecha_real > DateTime.UtcNow)
                {
                    return BadRequest("Error con la fecha");
                }

                if (sesion.es_reposicion)
                {
                    // Validar que la sesionrepuestaid exista
                    if (sesion.sesionrepuestaid == null)
                    {
                        return BadRequest("La sesión de reposición debe tener un id de sesión original asociado.");
                    }
                    var sesionOriginal = await _db.sesionclase.FindAsync(sesion.sesionrepuestaid);
                    if (sesionOriginal == null || sesionOriginal.estado == "INA")
                    {
                        return BadRequest("La sesión original asociada no existe o está inactiva.");
                    }
                }

                // Rellenar camos por defecto
                if (string.IsNullOrWhiteSpace(sesion.estado))
                    sesion.estado = "ACT";
                if (string.IsNullOrWhiteSpace(sesion.fecha_creacion.ToString()))
                    sesion.fecha_creacion = DateTime.UtcNow;


                _db.sesionclase.Add(sesion);
                await _db.SaveChangesAsync();

                return CreatedAtAction(nameof(GetSesionClaseById), new { id = sesion.sesionclaseid }, sesion);
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

        [HttpPut("ActualizarSesionClase/{idsesion:long}")]
        public async Task<IActionResult> UpdateSesionClase(long idsesion, [FromBody] SesionClase sesion)
        {
            try
            {
                // Validar que el id en la URL coincida con el id en el cuerpo
                if (idsesion != sesion.sesionclaseid)
                    return BadRequest("El id de la URL no coincide con el id del cuerpo.");

                // Validaciones
                if (sesion.estado != "ACT" && sesion.estado != "INA" && sesion.estado == null)
                {
                    return BadRequest("El estado debe ser 'ACT' o 'INA'.");
                }


                var existing = await _db.sesionclase.FindAsync(idsesion);
                if (existing == null)
                    return NotFound($"No existe un asistencia con el id {idsesion}.");

                if (existing.es_reposicion)
                {
                    // Validar que la sesionrepuestaid exista
                    if (sesion.sesionrepuestaid == null)
                    {
                        return BadRequest("La sesión de reposición debe tener un id de sesión original asociado.");
                    }
                    var sesionOriginal = await _db.sesionclase.FindAsync(sesion.sesionrepuestaid);
                    if (sesionOriginal == null || sesionOriginal.estado == "INA")
                    {
                        return BadRequest("La sesión original asociada no existe o está inactiva.");
                    }
                }

                // Actualizamos campos
                existing.fecha_real = sesion.fecha_real;
                existing.dia_semana = sesion.dia_semana;
                existing.hora_inicio_programada = sesion.hora_inicio_programada;
                existing.hora_fin_programada = sesion.hora_fin_programada;
                existing.minutos_dictados = sesion.minutos_dictados;
                existing.clase_dictada = sesion.clase_dictada;
                existing.es_reposicion = sesion.es_reposicion;
                existing.estado = sesion.estado;
                // Actualizamos los FKs
                existing.tutorid = sesion.tutorid;
                existing.aulaid = sesion.aulaid;
                existing.calendariosemanalprogramaid = sesion.calendariosemanalprogramaid;
                existing.motivonoclaseid = sesion.motivonoclaseid;
                existing.festivoid = sesion.festivoid;
                existing.sesionrepuestaid = sesion.sesionrepuestaid;

                await _db.SaveChangesAsync();

                return Ok(existing);
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
        [HttpDelete("InactivarSesionClase/{id:long}")]
        public async Task<IActionResult> InactivateSesionClase(long id)
        {
            try
            {
                var sesion = await _db.sesionclase.FindAsync(id);
                if (sesion == null || sesion.estado == "INA")
                    return NotFound($"No existe una sesion de clase activa con el id {id}.");

                sesion.estado = "INA";
                await _db.SaveChangesAsync();

                return Ok(sesion);
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

        [HttpDelete("BorrarSesionClase/{id:long}")]
        public async Task<IActionResult> DeleteSesionClase(long id)
        {
            try
            {
                var sesion = await _db.sesionclase.FindAsync(id);
                if (sesion == null)
                    return NotFound($"No existe una sesion de clase con el id {id}.");

                _db.sesionclase.Remove(sesion);
                await _db.SaveChangesAsync();
                return Ok(sesion);
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

