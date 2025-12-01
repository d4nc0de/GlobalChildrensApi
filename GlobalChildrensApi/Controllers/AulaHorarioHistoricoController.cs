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
    public class AulaHorarioHistoricoController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AulaHorarioHistoricoController(AppDbContext db)
        {
            _db = db;
        }

        // Obtener todos los registros de historial activos
        [HttpGet("GetAll")]
        public async Task<ActionResult<IEnumerable<AulaHorarioHistorico>>> GetAll()
        {
            try
            {
                var historiales = await _db.aulahorariohistorico
                    .Where(h => h.estado == "ACT")
                    .OrderByDescending(h => h.fecha_inicio)
                    .ToListAsync();

                return Ok(new
                {
                    total = historiales.Count,
                    historiales = historiales
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

        // Obtener historial por ID
        [HttpGet("GetById/{id:long}")]
        public async Task<ActionResult<AulaHorarioHistorico>> GetById(long id)
        {
            try
            {
                var historial = await _db.aulahorariohistorico.FirstOrDefaultAsync(h => h.aulahorariohistoricoid == id);

                if (historial == null)
                    return NotFound(new
                    {
                        error = "HISTORIAL_NO_ENCONTRADO",
                        message = $"No existe un registro de historial con el id {id}."
                    });

                return Ok(historial);
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

        // Obtener historial de horarios de un aula específica
        [HttpGet("GetByAula/{aulaid:long}")]
        public async Task<ActionResult<IEnumerable<AulaHorarioHistorico>>> GetByAula(long aulaid)
        {
            try
            {
                // Verificar que el aula exista
                var aula = await _db.aula.FindAsync(aulaid);
                if (aula == null)
                {
                    return NotFound(new
                    {
                        error = "AULA_NO_ENCONTRADA",
                        message = $"No existe un aula con el id {aulaid}."
                    });
                }

                var historiales = await _db.aulahorariohistorico
                    .Where(h => h.aulaid == aulaid && h.estado == "ACT")
                    .OrderByDescending(h => h.fecha_inicio)
                    .ToListAsync();

                return Ok(new
                {
                    aula_id = aulaid,
                    aula_nombre = aula.nombre,
                    aula_grado = aula.grado,
                    total_registros = historiales.Count,
                    historiales = historiales
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

        // Obtener horario actual de un aula (el que tiene fecha_fin null)
        [HttpGet("GetHorarioActual/{aulaid:long}")]
        public async Task<ActionResult<AulaHorarioHistorico>> GetHorarioActual(long aulaid)
        {
            try
            {
                var horarioActual = await _db.aulahorariohistorico
                    .FirstOrDefaultAsync(h => h.aulaid == aulaid 
                                           && h.fecha_fin == null 
                                           && h.estado == "ACT");

                if (horarioActual == null)
                {
                    return NotFound(new
                    {
                        error = "HORARIO_ACTUAL_NO_ENCONTRADO",
                        message = $"El aula con id {aulaid} no tiene un horario activo asignado actualmente."
                    });
                }

                return Ok(horarioActual);
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

        [HttpPost("Create")]
        public async Task<ActionResult<AulaHorarioHistorico>> Create([FromBody] AulaHorarioHistorico historial)
        {
            try
            {
                // VALIDACIÓN 1: Verificar que el aula exista
                var aula = await _db.aula.FindAsync(historial.aulaid);
                if (aula == null)
                {
                    return BadRequest(new
                    {
                        error = "AULA_NO_EXISTE",
                        message = $"El aula con ID {historial.aulaid} no existe en el sistema.",
                        constraint = "aulahorariohistorico_aulaid_fkey"
                    });
                }

                if (!aula.activo || aula.estado != "ACT")
                {
                    return BadRequest(new
                    {
                        error = "AULA_INACTIVA",
                        message = $"El aula '{aula.nombre}' (grado {aula.grado}º) no está activa."
                    });
                }

                // VALIDACIÓN 2: Validar que fecha_fin > fecha_inicio (si fecha_fin no es null)
                if (historial.fecha_fin.HasValue && historial.fecha_fin.Value <= historial.fecha_inicio)
                {
                    return BadRequest(new
                    {
                        error = "FECHA_FIN_INVALIDA",
                        message = $"La fecha fin ({historial.fecha_fin:yyyy-MM-dd}) debe ser mayor que la fecha inicio ({historial.fecha_inicio:yyyy-MM-dd}).",
                        constraint = "chk_fecha_fin_aula_horario"
                    });
                }

                // VALIDACIÓN 3: Verificar constraint único (aulaid + horarioid + fecha_inicio)
                var registroExistente = await _db.aulahorariohistorico
                    .FirstOrDefaultAsync(h => h.aulaid == historial.aulaid 
                                           && h.horarioid == historial.horarioid 
                                           && h.fecha_inicio == historial.fecha_inicio);

                if (registroExistente != null)
                {
                    return BadRequest(new
                    {
                        error = "REGISTRO_DUPLICADO",
                        message = $"Ya existe un registro para el aula {historial.aulaid} con el horario {historial.horarioid} en la fecha {historial.fecha_inicio:yyyy-MM-dd} (ID: {registroExistente.aulahorariohistoricoid}, Estado: {registroExistente.estado}).",
                        constraint = "uq_aula_horario_historico",
                        registro_existente_id = registroExistente.aulahorariohistoricoid,
                        registro_existente_estado = registroExistente.estado
                    });
                }

                // VALIDACIÓN 4: Si está creando un horario "activo" (fecha_fin = null),
                // cerrar cualquier otro horario activo de la misma aula
                if (!historial.fecha_fin.HasValue)
                {
                    var horarioActual = await _db.aulahorariohistorico
                        .Where(h => h.aulaid == historial.aulaid 
                                 && h.fecha_fin == null
                                 && h.estado == "ACT")
                        .ToListAsync();

                    foreach (var registro in horarioActual)
                    {
                        registro.fecha_fin = historial.fecha_inicio.AddDays(-1); // Cerrar el día anterior
                        if (string.IsNullOrWhiteSpace(registro.motivo_cambio))
                        {
                            registro.motivo_cambio = "Cambio de horario automático";
                        }
                    }
                }

                // Establecer valores por defecto
                if (string.IsNullOrWhiteSpace(historial.estado))
                    historial.estado = "ACT";

                _db.aulahorariohistorico.Add(historial);
                await _db.SaveChangesAsync();

                return CreatedAtAction(nameof(GetById), new { id = historial.aulahorariohistoricoid }, historial);
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;

                if (innerMessage.Contains("aulahorariohistorico_horarioid_fkey"))
                {
                    return BadRequest(new
                    {
                        error = "HORARIO_NO_EXISTE",
                        message = $"El horario con ID {historial.horarioid} no existe en el sistema.",
                        constraint = "aulahorariohistorico_horarioid_fkey"
                    });
                }
                else if (innerMessage.Contains("uq_aula_horario_historico"))
                {
                    return BadRequest(new
                    {
                        error = "REGISTRO_DUPLICADO",
                        message = $"Ya existe un registro para esta combinación de aula, horario y fecha de inicio.",
                        constraint = "uq_aula_horario_historico"
                    });
                }
                else if (innerMessage.Contains("chk_fecha_fin_aula_horario"))
                {
                    return BadRequest(new
                    {
                        error = "FECHA_FIN_INVALIDA",
                        message = "La fecha fin debe ser mayor que la fecha inicio.",
                        constraint = "chk_fecha_fin_aula_horario"
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

        [HttpPut("Update/{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] AulaHorarioHistorico historial)
        {
            try
            {
                if (id != historial.aulahorariohistoricoid)
                    return BadRequest(new
                    {
                        error = "ID_NO_COINCIDE",
                        message = $"El id de la URL ({id}) no coincide con el id del cuerpo ({historial.aulahorariohistoricoid})."
                    });

                var existing = await _db.aulahorariohistorico.FindAsync(id);
                if (existing == null)
                    return NotFound(new
                    {
                        error = "HISTORIAL_NO_ENCONTRADO",
                        message = $"No existe un registro de historial con el id {id}."
                    });

                // VALIDACIÓN 1: Verificar que el aula exista
                var aula = await _db.aula.FindAsync(historial.aulaid);
                if (aula == null)
                {
                    return BadRequest(new
                    {
                        error = "AULA_NO_EXISTE",
                        message = $"El aula con ID {historial.aulaid} no existe en el sistema.",
                        constraint = "aulahorariohistorico_aulaid_fkey"
                    });
                }

                // VALIDACIÓN 2: Validar que fecha_fin > fecha_inicio (si fecha_fin no es null)
                if (historial.fecha_fin.HasValue && historial.fecha_fin.Value <= historial.fecha_inicio)
                {
                    return BadRequest(new
                    {
                        error = "FECHA_FIN_INVALIDA",
                        message = $"La fecha fin ({historial.fecha_fin:yyyy-MM-dd}) debe ser mayor que la fecha inicio ({historial.fecha_inicio:yyyy-MM-dd}).",
                        constraint = "chk_fecha_fin_aula_horario"
                    });
                }

                // VALIDACIÓN 3: Verificar constraint único (excepto el registro actual)
                var registroExistente = await _db.aulahorariohistorico
                    .FirstOrDefaultAsync(h => h.aulaid == historial.aulaid 
                                           && h.horarioid == historial.horarioid 
                                           && h.fecha_inicio == historial.fecha_inicio
                                           && h.aulahorariohistoricoid != id);

                if (registroExistente != null)
                {
                    return BadRequest(new
                    {
                        error = "REGISTRO_DUPLICADO",
                        message = $"Ya existe otro registro para el aula {historial.aulaid} con el horario {historial.horarioid} en la fecha {historial.fecha_inicio:yyyy-MM-dd} (ID: {registroExistente.aulahorariohistoricoid}, Estado: {registroExistente.estado}).",
                        constraint = "uq_aula_horario_historico",
                        registro_existente_id = registroExistente.aulahorariohistoricoid,
                        registro_existente_estado = registroExistente.estado
                    });
                }

                // Actualizamos campos
                existing.fecha_inicio = historial.fecha_inicio;
                existing.fecha_fin = historial.fecha_fin;
                existing.motivo_cambio = historial.motivo_cambio;
                existing.estado = historial.estado;
                existing.aulaid = historial.aulaid;
                existing.horarioid = historial.horarioid;

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Historial de horario actualizado correctamente",
                    historial_id = existing.aulahorariohistoricoid,
                    aula_id = existing.aulaid,
                    horario_id = existing.horarioid,
                    fecha_inicio = existing.fecha_inicio,
                    fecha_fin = existing.fecha_fin
                });
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;

                if (innerMessage.Contains("aulahorariohistorico_horarioid_fkey"))
                {
                    return BadRequest(new
                    {
                        error = "HORARIO_NO_EXISTE",
                        message = $"El horario con ID {historial.horarioid} no existe en el sistema.",
                        constraint = "aulahorariohistorico_horarioid_fkey"
                    });
                }
                else if (innerMessage.Contains("uq_aula_horario_historico"))
                {
                    return BadRequest(new
                    {
                        error = "REGISTRO_DUPLICADO",
                        message = $"Ya existe un registro para esta combinación de aula, horario y fecha de inicio.",
                        constraint = "uq_aula_horario_historico"
                    });
                }
                else if (innerMessage.Contains("chk_fecha_fin_aula_horario"))
                {
                    return BadRequest(new
                    {
                        error = "FECHA_FIN_INVALIDA",
                        message = "La fecha fin debe ser mayor que la fecha inicio.",
                        constraint = "chk_fecha_fin_aula_horario"
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

        // Cerrar un horario activo (poner fecha_fin)
        [HttpPatch("CerrarHorario/{id:long}")]
        public async Task<IActionResult> CerrarHorario(long id, [FromBody] CerrarHorarioRequest request)
        {
            try
            {
                var historial = await _db.aulahorariohistorico.FindAsync(id);
                if (historial == null)
                    return NotFound(new
                    {
                        error = "HISTORIAL_NO_ENCONTRADO",
                        message = $"No existe un registro de historial con el id {id}."
                    });

                if (historial.fecha_fin.HasValue)
                {
                    return BadRequest(new
                    {
                        error = "HORARIO_YA_CERRADO",
                        message = $"Este horario ya fue cerrado el {historial.fecha_fin:yyyy-MM-dd}."
                    });
                }

                // Validar que fecha_fin > fecha_inicio
                if (request.fecha_fin <= historial.fecha_inicio)
                {
                    return BadRequest(new
                    {
                        error = "FECHA_FIN_INVALIDA",
                        message = $"La fecha fin ({request.fecha_fin:yyyy-MM-dd}) debe ser mayor que la fecha inicio ({historial.fecha_inicio:yyyy-MM-dd}).",
                        constraint = "chk_fecha_fin_aula_horario"
                    });
                }

                historial.fecha_fin = request.fecha_fin;
                if (!string.IsNullOrWhiteSpace(request.motivo_cambio))
                {
                    historial.motivo_cambio = request.motivo_cambio;
                }

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Horario cerrado correctamente",
                    historial_id = historial.aulahorariohistoricoid,
                    fecha_inicio = historial.fecha_inicio,
                    fecha_fin = historial.fecha_fin,
                    motivo_cambio = historial.motivo_cambio
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

        // Inactivar un registro de historial
        [HttpDelete("Delete/{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var historial = await _db.aulahorariohistorico.FindAsync(id);
                if (historial == null)
                    return NotFound(new
                    {
                        error = "HISTORIAL_NO_ENCONTRADO",
                        message = $"No existe un registro de historial con el id {id}."
                    });

                if (historial.estado == "INA")
                {
                    return BadRequest(new
                    {
                        error = "HISTORIAL_YA_INACTIVO",
                        message = "Este registro de historial ya está inactivo."
                    });
                }

                historial.estado = "INA";
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Registro de historial inactivado correctamente",
                    historial_id = historial.aulahorariohistoricoid
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

    // Clase auxiliar para el endpoint CerrarHorario
    public class CerrarHorarioRequest
    {
        public DateTime fecha_fin { get; set; }
        public string? motivo_cambio { get; set; }
    }
}

