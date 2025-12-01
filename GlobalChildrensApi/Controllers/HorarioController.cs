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
    public class HorarioController : ControllerBase
    {
        private readonly AppDbContext _db;

        public HorarioController(AppDbContext db)
        {
            _db = db;
        }

        // Obtener todos los horarios activos por jornada
        [HttpGet("GetAllHorariosByJornadaId/{jornadaId:long}")]
        public async Task<ActionResult<IEnumerable<Horario>>> GetAllHorariosByJornadaId(long jornadaId)
        {
            try
            {
                // VALIDACIÓN 1: Verificar que la jornada existe y está activa
                var jornada = await _db.jornada.FindAsync(jornadaId);
                if (jornada == null)
                {
                    return NotFound(new
                    {
                        error = "JORNADA_NO_ENCONTRADA",
                        message = $"No existe una jornada con el id {jornadaId}."
                    });
                }

                if (jornada.estado != "ACT")
                {
                    return BadRequest(new
                    {
                        error = "JORNADA_INACTIVA",
                        message = $"La jornada '{jornada.descripcion}' existe pero no está activa (Estado = {jornada.estado})."
                    });
                }

                var horarios = await _db.horario
                    .Where(h => h.jornadaid == jornadaId && h.estado == "ACT")
                    .OrderBy(h => h.minutos_por_unidad)
                    .ToListAsync();

                return Ok(horarios);
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

        // Crear un nuevo horario
        [HttpPost("CreateHorario")]
        public async Task<ActionResult<Horario>> CreateHorario([FromBody] Horario horario)
        {
            try
            {
                // VALIDACIÓN 1: minutos_por_unidad debe ser mayor a 0
                if (horario.minutos_por_unidad <= 0)
                {
                    return BadRequest(new
                    {
                        error = "MINUTOS_INVALIDOS",
                        message = $"Los minutos por unidad deben ser mayor a 0. Valor recibido: {horario.minutos_por_unidad}.",
                        constraint = "chk_minutos_por_unidad"
                    });
                }

                // VALIDACIÓN 2: Verificar que la jornada existe y está activa
                var jornada = await _db.jornada.FindAsync(horario.jornadaid);
                if (jornada == null)
                {
                    return NotFound(new
                    {
                        error = "JORNADA_NO_ENCONTRADA",
                        message = $"No existe una jornada con el id {horario.jornadaid}."
                    });
                }

                if (jornada.estado != "ACT")
                {
                    return BadRequest(new
                    {
                        error = "JORNADA_INACTIVA",
                        message = $"La jornada '{jornada.descripcion}' existe pero no está activa (Estado = {jornada.estado})."
                    });
                }

                // VALIDACIÓN 3: Descripción no puede exceder 200 caracteres
                if (!string.IsNullOrWhiteSpace(horario.descripcion) && horario.descripcion.Length > 200)
                {
                    return BadRequest(new
                    {
                        error = "DESCRIPCION_MUY_LARGA",
                        message = $"La descripción no puede exceder 200 caracteres. Longitud actual: {horario.descripcion.Length}."
                    });
                }

                // VALIDACIÓN 4: Verificar que no exista un horario con los mismos minutos y jornada
                var horarioExistente = await _db.horario
                    .FirstOrDefaultAsync(h => h.minutos_por_unidad == horario.minutos_por_unidad 
                                           && h.jornadaid == horario.jornadaid);

                if (horarioExistente != null)
                {
                    return BadRequest(new
                    {
                        error = "HORARIO_DUPLICADO",
                        message = $"Ya existe un horario con {horario.minutos_por_unidad} minutos por unidad para la jornada '{jornada.descripcion}' (ID: {horarioExistente.horarioid}, Estado: {horarioExistente.estado}).",
                        constraint = "uq_horario_minuto_jornada",
                        horario_existente_id = horarioExistente.horarioid,
                        horario_existente_estado = horarioExistente.estado
                    });
                }

                // Establecer valores por defecto
                if (string.IsNullOrWhiteSpace(horario.estado))
                    horario.estado = "ACT";

                horario.fecha_creacion = DateTime.UtcNow;

                _db.horario.Add(horario);
                await _db.SaveChangesAsync();

                return CreatedAtAction(nameof(GetAllHorariosByJornadaId), 
                    new { jornadaId = horario.jornadaid }, 
                    horario);
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;

                if (innerMessage.Contains("uq_horario_minuto_jornada"))
                {
                    return BadRequest(new
                    {
                        error = "HORARIO_DUPLICADO",
                        message = $"Ya existe un horario con {horario.minutos_por_unidad} minutos por unidad para esta jornada.",
                        constraint = "uq_horario_minuto_jornada"
                    });
                }

                if (innerMessage.Contains("horario_jornadaid_fkey"))
                {
                    return BadRequest(new
                    {
                        error = "JORNADA_NO_ENCONTRADA",
                        message = $"La jornada con id {horario.jornadaid} no existe.",
                        constraint = "horario_jornadaid_fkey"
                    });
                }

                if (innerMessage.Contains("chk_minutos_por_unidad"))
                {
                    return BadRequest(new
                    {
                        error = "MINUTOS_INVALIDOS",
                        message = $"Los minutos por unidad deben ser mayor a 0.",
                        constraint = "chk_minutos_por_unidad"
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

        // Actualizar un horario existente
        [HttpPut("UpdateHorario/{id:long}")]
        public async Task<IActionResult> UpdateHorario(long id, [FromBody] Horario horario)
        {
            try
            {
                if (id != horario.horarioid)
                    return BadRequest(new
                    {
                        error = "ID_NO_COINCIDE",
                        message = $"El id de la URL ({id}) no coincide con el id del cuerpo ({horario.horarioid})."
                    });

                var existing = await _db.horario.FindAsync(id);
                if (existing == null)
                    return NotFound(new
                    {
                        error = "HORARIO_NO_ENCONTRADO",
                        message = $"No existe un horario con el id {id}."
                    });

                // VALIDACIÓN 1: minutos_por_unidad debe ser mayor a 0
                if (horario.minutos_por_unidad <= 0)
                {
                    return BadRequest(new
                    {
                        error = "MINUTOS_INVALIDOS",
                        message = $"Los minutos por unidad deben ser mayor a 0. Valor recibido: {horario.minutos_por_unidad}.",
                        constraint = "chk_minutos_por_unidad"
                    });
                }

                // VALIDACIÓN 2: Verificar que la jornada existe y está activa
                var jornada = await _db.jornada.FindAsync(horario.jornadaid);
                if (jornada == null)
                {
                    return NotFound(new
                    {
                        error = "JORNADA_NO_ENCONTRADA",
                        message = $"No existe una jornada con el id {horario.jornadaid}."
                    });
                }

                if (jornada.estado != "ACT")
                {
                    return BadRequest(new
                    {
                        error = "JORNADA_INACTIVA",
                        message = $"La jornada '{jornada.descripcion}' existe pero no está activa (Estado = {jornada.estado})."
                    });
                }

                // VALIDACIÓN 3: Descripción no puede exceder 200 caracteres
                if (!string.IsNullOrWhiteSpace(horario.descripcion) && horario.descripcion.Length > 200)
                {
                    return BadRequest(new
                    {
                        error = "DESCRIPCION_MUY_LARGA",
                        message = $"La descripción no puede exceder 200 caracteres. Longitud actual: {horario.descripcion.Length}."
                    });
                }

                // VALIDACIÓN 4: Verificar que no exista otro horario con los mismos minutos y jornada (excepto el actual)
                var horarioExistente = await _db.horario
                    .FirstOrDefaultAsync(h => h.minutos_por_unidad == horario.minutos_por_unidad 
                                           && h.jornadaid == horario.jornadaid 
                                           && h.horarioid != id);

                if (horarioExistente != null)
                {
                    return BadRequest(new
                    {
                        error = "HORARIO_DUPLICADO",
                        message = $"Ya existe otro horario con {horario.minutos_por_unidad} minutos por unidad para la jornada '{jornada.descripcion}' (ID: {horarioExistente.horarioid}, Estado: {horarioExistente.estado}).",
                        constraint = "uq_horario_minuto_jornada",
                        horario_existente_id = horarioExistente.horarioid,
                        horario_existente_estado = horarioExistente.estado
                    });
                }

                // Actualizamos campos
                existing.minutos_por_unidad = horario.minutos_por_unidad;
                existing.descripcion = horario.descripcion;
                existing.estado = horario.estado;
                existing.jornadaid = horario.jornadaid;

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Horario actualizado correctamente",
                    horario_id = existing.horarioid,
                    minutos_por_unidad = existing.minutos_por_unidad,
                    jornada_id = existing.jornadaid,
                    jornada_descripcion = jornada.descripcion
                });
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;

                if (innerMessage.Contains("uq_horario_minuto_jornada"))
                {
                    return BadRequest(new
                    {
                        error = "HORARIO_DUPLICADO",
                        message = $"Ya existe un horario con {horario.minutos_por_unidad} minutos por unidad para esta jornada.",
                        constraint = "uq_horario_minuto_jornada"
                    });
                }

                if (innerMessage.Contains("horario_jornadaid_fkey"))
                {
                    return BadRequest(new
                    {
                        error = "JORNADA_NO_ENCONTRADA",
                        message = $"La jornada con id {horario.jornadaid} no existe.",
                        constraint = "horario_jornadaid_fkey"
                    });
                }

                if (innerMessage.Contains("chk_minutos_por_unidad"))
                {
                    return BadRequest(new
                    {
                        error = "MINUTOS_INVALIDOS",
                        message = $"Los minutos por unidad deben ser mayor a 0.",
                        constraint = "chk_minutos_por_unidad"
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

        // Inactivar un horario (soft delete)
        [HttpDelete("InactivateHorario/{id:long}")]
        public async Task<IActionResult> InactivateHorario(long id)
        {
            try
            {
                var horario = await _db.horario.FindAsync(id);
                if (horario == null)
                    return NotFound(new
                    {
                        error = "HORARIO_NO_ENCONTRADO",
                        message = $"No existe un horario con el id {id}."
                    });

                if (horario.estado == "INA")
                {
                    return BadRequest(new
                    {
                        error = "HORARIO_YA_INACTIVO",
                        message = $"El horario con {horario.minutos_por_unidad} minutos por unidad ya está inactivo."
                    });
                }

                horario.estado = "INA";
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Horario inactivado correctamente",
                    horario_id = horario.horarioid,
                    minutos_por_unidad = horario.minutos_por_unidad,
                    jornada_id = horario.jornadaid
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

