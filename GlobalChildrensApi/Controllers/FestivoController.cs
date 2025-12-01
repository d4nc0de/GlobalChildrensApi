using GlobalChildrensApi.Data;
using GlobalChildrensApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GlobalChildrensApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize] // Usa el JWT de Supabase
    public class FestivoController : ControllerBase
    {
        private readonly AppDbContext _db;

        public FestivoController(AppDbContext db)
        {
            _db = db;
        }

        // Obtener todos los festivos activos
        [HttpGet("GetAllFestivo")]
        public async Task<ActionResult<IEnumerable<Festivo>>> GetAllFestivo()
        {
            try
            {
                var festivos = await _db.festivo
                    .Where(f => f.estado == "ACT")
                    .OrderBy(f => f.fecha)
                    .ToListAsync();

                return Ok(new
                {
                    total = festivos.Count,
                    festivos = festivos
                });
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

        // Obtener festivo por ID
        [HttpGet("GetFestivoPorId/{id:long}")]
        public async Task<ActionResult<Festivo>> GetById(long id)
        {
            try
            {
                var festivo = await _db.festivo.FirstOrDefaultAsync(f => f.festivoid == id);

                if (festivo == null)
                    return NotFound(new
                    {
                        error = "FESTIVO_NO_ENCONTRADO",
                        message = $"No existe un festivo con el id {id}."
                    });

                if (festivo.estado != "ACT")
                    return BadRequest(new
                    {
                        error = "FESTIVO_INACTIVO",
                        message = $"El festivo '{festivo.nombre}' existe pero no está activo (Estado = {festivo.estado})."
                    });

                return Ok(festivo);
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

        // Obtener festivos por año
        [HttpGet("GetFestivosPorAnio/{anio:int}")]
        public async Task<ActionResult<IEnumerable<Festivo>>> GetByYear(int anio)
        {
            try
            {
                var festivos = await _db.festivo
                    .Where(f => f.fecha.Year == anio && f.estado == "ACT")
                    .OrderBy(f => f.fecha)
                    .ToListAsync();

                return Ok(new
                {
                    anio = anio,
                    total = festivos.Count,
                    festivos = festivos
                });
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

        // Obtener festivos por rango de fechas
        [HttpGet("GetFestivosPorRango")]
        public async Task<ActionResult<IEnumerable<Festivo>>> GetByDateRange(
            [FromQuery] DateTime fechaInicio,
            [FromQuery] DateTime fechaFin)
        {
            try
            {
                if (fechaFin < fechaInicio)
                {
                    return BadRequest(new
                    {
                        error = "RANGO_INVALIDO",
                        message = "La fecha fin no puede ser menor que la fecha inicio."
                    });
                }

                var festivos = await _db.festivo
                    .Where(f => f.fecha >= fechaInicio.Date && f.fecha <= fechaFin.Date && f.estado == "ACT")
                    .OrderBy(f => f.fecha)
                    .ToListAsync();

                return Ok(new
                {
                    fecha_inicio = fechaInicio.Date,
                    fecha_fin = fechaFin.Date,
                    total = festivos.Count,
                    festivos = festivos
                });
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

        [HttpPost("CreateFestivo")]
        public async Task<ActionResult<Festivo>> CreateFestivo([FromBody] Festivo festivo)
        {
            try
            {
                // VALIDACIÓN 1: Nombre no puede estar vacío
                if (string.IsNullOrWhiteSpace(festivo.nombre))
                {
                    return BadRequest(new
                    {
                        error = "NOMBRE_REQUERIDO",
                        message = "El nombre del festivo es requerido y no puede estar vacío."
                    });
                }

                // VALIDACIÓN 2: Fecha no puede ser en el pasado
                if (festivo.fecha.Date < DateTime.UtcNow.Date)
                {
                    return BadRequest(new
                    {
                        error = "FECHA_PASADA",
                        message = $"La fecha del festivo ({festivo.fecha:yyyy-MM-dd}) no puede ser una fecha pasada."
                    });
                }

                // VALIDACIÓN 3: Verificar que no exista ya un festivo en esa fecha
                var festivoExistente = await _db.festivo
                    .FirstOrDefaultAsync(f => f.fecha == festivo.fecha);

                if (festivoExistente != null)
                {
                    return BadRequest(new
                    {
                        error = "FECHA_DUPLICADA",
                        message = $"Ya existe un festivo registrado para la fecha {festivo.fecha:yyyy-MM-dd}: '{festivoExistente.nombre}' (ID: {festivoExistente.festivoid}, Estado: {festivoExistente.estado}).",
                        constraint = "uq_fecha_festivo",
                        festivo_existente_id = festivoExistente.festivoid,
                        festivo_existente_nombre = festivoExistente.nombre,
                        festivo_existente_estado = festivoExistente.estado
                    });
                }

                // Establecer valores por defecto
                if (string.IsNullOrWhiteSpace(festivo.estado))
                    festivo.estado = "ACT";

                _db.festivo.Add(festivo);
                await _db.SaveChangesAsync();

                return CreatedAtAction(nameof(GetById), new { id = festivo.festivoid }, festivo);
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;

                if (innerMessage.Contains("uq_fecha_festivo"))
                {
                    return BadRequest(new
                    {
                        error = "FECHA_DUPLICADA",
                        message = $"Ya existe un festivo registrado para la fecha {festivo.fecha:yyyy-MM-dd}.",
                        constraint = "uq_fecha_festivo"
                    });
                }

                return StatusCode(503, new
                {
                    error = "ERROR_BASE_DATOS",
                    message = "Error al guardar en la base de datos.",
                    details = innerMessage
                });
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

        [HttpPut("UpdateFestivo/{id:long}")]
        public async Task<IActionResult> UpdateFestivo(long id, [FromBody] Festivo festivo)
        {
            try
            {
                if (id != festivo.festivoid)
                    return BadRequest(new
                    {
                        error = "ID_NO_COINCIDE",
                        message = $"El id de la URL ({id}) no coincide con el id del cuerpo ({festivo.festivoid})."
                    });

                var existing = await _db.festivo.FindAsync(id);
                if (existing == null)
                    return NotFound(new
                    {
                        error = "FESTIVO_NO_ENCONTRADO",
                        message = $"No existe un festivo con el id {id}."
                    });

                // VALIDACIÓN 1: Nombre no puede estar vacío
                if (string.IsNullOrWhiteSpace(festivo.nombre))
                {
                    return BadRequest(new
                    {
                        error = "NOMBRE_REQUERIDO",
                        message = "El nombre del festivo es requerido y no puede estar vacío."
                    });
                }

                // VALIDACIÓN 2: Verificar que no exista otro festivo en esa fecha (excepto el actual)
                var festivoExistente = await _db.festivo
                    .FirstOrDefaultAsync(f => f.fecha == festivo.fecha && f.festivoid != id);

                if (festivoExistente != null)
                {
                    return BadRequest(new
                    {
                        error = "FECHA_DUPLICADA",
                        message = $"Ya existe otro festivo registrado para la fecha {festivo.fecha:yyyy-MM-dd}: '{festivoExistente.nombre}' (ID: {festivoExistente.festivoid}, Estado: {festivoExistente.estado}).",
                        constraint = "uq_fecha_festivo",
                        festivo_existente_id = festivoExistente.festivoid,
                        festivo_existente_nombre = festivoExistente.nombre,
                        festivo_existente_estado = festivoExistente.estado
                    });
                }

                // Actualizamos campos
                existing.fecha = festivo.fecha;
                existing.nombre = festivo.nombre;
                existing.estado = festivo.estado;

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Festivo actualizado correctamente",
                    festivo_id = existing.festivoid,
                    nombre = existing.nombre,
                    fecha = existing.fecha
                });
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;

                if (innerMessage.Contains("uq_fecha_festivo"))
                {
                    return BadRequest(new
                    {
                        error = "FECHA_DUPLICADA",
                        message = $"Ya existe un festivo registrado para la fecha {festivo.fecha:yyyy-MM-dd}.",
                        constraint = "uq_fecha_festivo"
                    });
                }

                return StatusCode(503, new
                {
                    error = "ERROR_BASE_DATOS",
                    message = "Error al guardar en la base de datos.",
                    details = innerMessage
                });
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

        // Cambia el estado de la entidad a INA (soft delete)
        [HttpDelete("DeleteFestivo/{id:long}")]
        public async Task<IActionResult> DeleteFestivo(long id)
        {
            try
            {
                var festivo = await _db.festivo.FindAsync(id);
                if (festivo == null)
                    return NotFound(new
                    {
                        error = "FESTIVO_NO_ENCONTRADO",
                        message = $"No existe un festivo con el id {id}."
                    });

                if (festivo.estado == "INA")
                {
                    return BadRequest(new
                    {
                        error = "FESTIVO_YA_INACTIVO",
                        message = $"El festivo '{festivo.nombre}' ({festivo.fecha:yyyy-MM-dd}) ya está inactivo."
                    });
                }

                festivo.estado = "INA";
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Festivo inactivado correctamente",
                    festivo_id = festivo.festivoid,
                    nombre = festivo.nombre,
                    fecha = festivo.fecha
                });
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
    }
}

