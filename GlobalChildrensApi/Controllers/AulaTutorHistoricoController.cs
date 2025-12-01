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
    public class AulaTutorHistoricoController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AulaTutorHistoricoController(AppDbContext db)
        {
            _db = db;
        }

        // Obtener todos los registros de historial activos
        [HttpGet("GetAll")]
        public async Task<ActionResult<IEnumerable<AulaTutorHistorico>>> GetAll()
        {
            try
            {
                var historiales = await _db.aulatutorhistorico
                    .Where(h => h.estado == "ACT")
                    .OrderByDescending(h => h.fecha_inicio)
                    .ToListAsync();

                return Ok(historiales);
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
        public async Task<ActionResult<AulaTutorHistorico>> GetById(long id)
        {
            try
            {
                var historial = await _db.aulatutorhistorico.FirstOrDefaultAsync(h => h.aulatutorhistoricoid == id);

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

        // Obtener historial de tutores de un aula específica
        [HttpGet("GetByAula/{aulaid:long}")]
        public async Task<ActionResult<IEnumerable<AulaTutorHistorico>>> GetByAula(long aulaid)
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

                var historiales = await _db.aulatutorhistorico
                    .Where(h => h.aulaid == aulaid && h.estado == "ACT")
                    .OrderByDescending(h => h.fecha_inicio)
                    .ToListAsync();

                return Ok(historiales);
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

        // Obtener tutor actual de un aula (el que tiene fecha_fin null)
        [HttpGet("GetTutorActual/{aulaid:long}")]
        public async Task<ActionResult<AulaTutorHistorico>> GetTutorActual(long aulaid)
        {
            try
            {
                var tutorActual = await _db.aulatutorhistorico
                    .FirstOrDefaultAsync(h => h.aulaid == aulaid 
                                           && h.fecha_fin == null 
                                           && h.estado == "ACT");

                if (tutorActual == null)
                {
                    return NotFound(new
                    {
                        error = "TUTOR_ACTUAL_NO_ENCONTRADO",
                        message = $"El aula con id {aulaid} no tiene un tutor activo asignado actualmente."
                    });
                }

                return Ok(tutorActual);
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

        // Obtener historial de aulas de un tutor específico
        [HttpGet("GetByTutor/{tutorid:long}")]
        public async Task<ActionResult<IEnumerable<AulaTutorHistorico>>> GetByTutor(long tutorid)
        {
            try
            {
                var historiales = await _db.aulatutorhistorico
                    .Where(h => h.tutorid == tutorid && h.estado == "ACT")
                    .OrderByDescending(h => h.fecha_inicio)
                    .ToListAsync();

                return Ok(historiales);
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
        public async Task<ActionResult<AulaTutorHistorico>> Create([FromBody] AulaTutorHistorico historial)
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
                        constraint = "aulatutorhistorico_aulaid_fkey"
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
                        constraint = "chk_fecha_fin_aula_tutor"
                    });
                }

                // VALIDACIÓN 3: Verificar constraint único (aulaid + tutorid + fecha_inicio)
                var registroExistente = await _db.aulatutorhistorico
                    .FirstOrDefaultAsync(h => h.aulaid == historial.aulaid 
                                           && h.tutorid == historial.tutorid 
                                           && h.fecha_inicio == historial.fecha_inicio);

                if (registroExistente != null)
                {
                    return BadRequest(new
                    {
                        error = "REGISTRO_DUPLICADO",
                        message = $"Ya existe un registro para el aula {historial.aulaid} con el tutor {historial.tutorid} en la fecha {historial.fecha_inicio:yyyy-MM-dd} (ID: {registroExistente.aulatutorhistoricoid}, Estado: {registroExistente.estado}).",
                        constraint = "uq_aula_tutor_historico",
                        registro_existente_id = registroExistente.aulatutorhistoricoid,
                        registro_existente_estado = registroExistente.estado
                    });
                }

                // VALIDACIÓN 4: Si está creando un tutor "activo" (fecha_fin = null),
                // cerrar cualquier otro tutor activo de la misma aula
                if (!historial.fecha_fin.HasValue)
                {
                    var tutorActual = await _db.aulatutorhistorico
                        .Where(h => h.aulaid == historial.aulaid 
                                 && h.fecha_fin == null
                                 && h.estado == "ACT")
                        .ToListAsync();

                    foreach (var registro in tutorActual)
                    {
                        registro.fecha_fin = historial.fecha_inicio.AddDays(-1); // Cerrar el día anterior
                        if (string.IsNullOrWhiteSpace(registro.motivo_cambio))
                        {
                            registro.motivo_cambio = "Cambio de tutor automático";
                        }
                    }

                    // Actualizar el tutorid del aula también
                    aula.tutorid = historial.tutorid;
                }

                // Establecer valores por defecto
                if (string.IsNullOrWhiteSpace(historial.estado))
                    historial.estado = "ACT";

                _db.aulatutorhistorico.Add(historial);
                await _db.SaveChangesAsync();

                return CreatedAtAction(nameof(GetById), new { id = historial.aulatutorhistoricoid }, historial);
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;

                if (innerMessage.Contains("aulatutorhistorico_tutorid_fkey"))
                {
                    return BadRequest(new
                    {
                        error = "TUTOR_NO_EXISTE",
                        message = $"El tutor con ID {historial.tutorid} no existe en el sistema.",
                        constraint = "aulatutorhistorico_tutorid_fkey"
                    });
                }
                else if (innerMessage.Contains("uq_aula_tutor_historico"))
                {
                    return BadRequest(new
                    {
                        error = "REGISTRO_DUPLICADO",
                        message = $"Ya existe un registro para esta combinación de aula, tutor y fecha de inicio.",
                        constraint = "uq_aula_tutor_historico"
                    });
                }
                else if (innerMessage.Contains("chk_fecha_fin_aula_tutor"))
                {
                    return BadRequest(new
                    {
                        error = "FECHA_FIN_INVALIDA",
                        message = "La fecha fin debe ser mayor que la fecha inicio.",
                        constraint = "chk_fecha_fin_aula_tutor"
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
        public async Task<IActionResult> Update(long id, [FromBody] AulaTutorHistorico historial)
        {
            try
            {
                if (id != historial.aulatutorhistoricoid)
                    return BadRequest(new
                    {
                        error = "ID_NO_COINCIDE",
                        message = $"El id de la URL ({id}) no coincide con el id del cuerpo ({historial.aulatutorhistoricoid})."
                    });

                var existing = await _db.aulatutorhistorico.FindAsync(id);
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
                        constraint = "aulatutorhistorico_aulaid_fkey"
                    });
                }

                // VALIDACIÓN 2: Validar que fecha_fin > fecha_inicio (si fecha_fin no es null)
                if (historial.fecha_fin.HasValue && historial.fecha_fin.Value <= historial.fecha_inicio)
                {
                    return BadRequest(new
                    {
                        error = "FECHA_FIN_INVALIDA",
                        message = $"La fecha fin ({historial.fecha_fin:yyyy-MM-dd}) debe ser mayor que la fecha inicio ({historial.fecha_inicio:yyyy-MM-dd}).",
                        constraint = "chk_fecha_fin_aula_tutor"
                    });
                }

                // VALIDACIÓN 3: Verificar constraint único (excepto el registro actual)
                var registroExistente = await _db.aulatutorhistorico
                    .FirstOrDefaultAsync(h => h.aulaid == historial.aulaid 
                                           && h.tutorid == historial.tutorid 
                                           && h.fecha_inicio == historial.fecha_inicio
                                           && h.aulatutorhistoricoid != id);

                if (registroExistente != null)
                {
                    return BadRequest(new
                    {
                        error = "REGISTRO_DUPLICADO",
                        message = $"Ya existe otro registro para el aula {historial.aulaid} con el tutor {historial.tutorid} en la fecha {historial.fecha_inicio:yyyy-MM-dd} (ID: {registroExistente.aulatutorhistoricoid}, Estado: {registroExistente.estado}).",
                        constraint = "uq_aula_tutor_historico",
                        registro_existente_id = registroExistente.aulatutorhistoricoid,
                        registro_existente_estado = registroExistente.estado
                    });
                }

                // Actualizamos campos
                existing.fecha_inicio = historial.fecha_inicio;
                existing.fecha_fin = historial.fecha_fin;
                existing.motivo_cambio = historial.motivo_cambio;
                existing.estado = historial.estado;
                existing.aulaid = historial.aulaid;
                existing.tutorid = historial.tutorid;

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Historial de tutor actualizado correctamente",
                    historial_id = existing.aulatutorhistoricoid,
                    aula_id = existing.aulaid,
                    tutor_id = existing.tutorid,
                    fecha_inicio = existing.fecha_inicio,
                    fecha_fin = existing.fecha_fin
                });
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;

                if (innerMessage.Contains("aulatutorhistorico_tutorid_fkey"))
                {
                    return BadRequest(new
                    {
                        error = "TUTOR_NO_EXISTE",
                        message = $"El tutor con ID {historial.tutorid} no existe en el sistema.",
                        constraint = "aulatutorhistorico_tutorid_fkey"
                    });
                }
                else if (innerMessage.Contains("uq_aula_tutor_historico"))
                {
                    return BadRequest(new
                    {
                        error = "REGISTRO_DUPLICADO",
                        message = $"Ya existe un registro para esta combinación de aula, tutor y fecha de inicio.",
                        constraint = "uq_aula_tutor_historico"
                    });
                }
                else if (innerMessage.Contains("chk_fecha_fin_aula_tutor"))
                {
                    return BadRequest(new
                    {
                        error = "FECHA_FIN_INVALIDA",
                        message = "La fecha fin debe ser mayor que la fecha inicio.",
                        constraint = "chk_fecha_fin_aula_tutor"
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

        // Cerrar un tutor activo (poner fecha_fin)
        [HttpPatch("CerrarTutor/{id:long}")]
        public async Task<IActionResult> CerrarTutor(long id, [FromBody] CerrarTutorRequest request)
        {
            try
            {
                var historial = await _db.aulatutorhistorico.FindAsync(id);
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
                        error = "TUTOR_YA_CERRADO",
                        message = $"Este tutor ya fue retirado el {historial.fecha_fin:yyyy-MM-dd}."
                    });
                }

                // Validar que fecha_fin > fecha_inicio
                if (request.fecha_fin <= historial.fecha_inicio)
                {
                    return BadRequest(new
                    {
                        error = "FECHA_FIN_INVALIDA",
                        message = $"La fecha fin ({request.fecha_fin:yyyy-MM-dd}) debe ser mayor que la fecha inicio ({historial.fecha_inicio:yyyy-MM-dd}).",
                        constraint = "chk_fecha_fin_aula_tutor"
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
                    message = "Tutor retirado del aula correctamente",
                    historial_id = historial.aulatutorhistoricoid,
                    aula_id = historial.aulaid,
                    tutor_id = historial.tutorid,
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
                var historial = await _db.aulatutorhistorico.FindAsync(id);
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
                    historial_id = historial.aulatutorhistoricoid
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

    // Clase auxiliar para el endpoint CerrarTutor
    public class CerrarTutorRequest
    {
        public DateTime fecha_fin { get; set; }
        public string? motivo_cambio { get; set; }
    }
}

