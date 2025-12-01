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
    public class HorarioDetalleController : ControllerBase
    {
        private readonly AppDbContext _db;

        public HorarioDetalleController(AppDbContext db)
        {
            _db = db;
        }

        // Obtener todos los detalles de horario activos por horario
        [HttpGet("GetAllDetalleHorarioByHorarioId/{horarioId:long}")]
        public async Task<ActionResult<IEnumerable<HorarioDetalle>>> GetAllDetalleHorarioByHorarioId(long horarioId)
        {
            try
            {
                // VALIDACIÓN 1: Verificar que el horario existe y está activo
                var horario = await _db.horario.FindAsync(horarioId);
                if (horario == null)
                {
                    return NotFound(new
                    {
                        error = "HORARIO_NO_ENCONTRADO",
                        message = $"No existe un horario con el id {horarioId}."
                    });
                }

                if (horario.estado != "ACT")
                {
                    return BadRequest(new
                    {
                        error = "HORARIO_INACTIVO",
                        message = $"El horario con {horario.minutos_por_unidad} minutos por unidad existe pero no está activo (Estado = {horario.estado})."
                    });
                }

                var detalles = await _db.horariodetalle
                    .Where(hd => hd.horarioid == horarioId && hd.estado == "ACT")
                    .OrderBy(hd => hd.dia_semana)
                    .ThenBy(hd => hd.hora_inicio)
                    .ToListAsync();

                return Ok(detalles);
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

        // Crear un nuevo detalle de horario
        [HttpPost("CreateDetalleHorario")]
        public async Task<ActionResult<HorarioDetalle>> CreateDetalleHorario([FromBody] HorarioDetalle detalle)
        {
            try
            {
                // VALIDACIÓN 1: dia_semana debe estar entre 1 y 7
                if (detalle.dia_semana < 1 || detalle.dia_semana > 7)
                {
                    return BadRequest(new
                    {
                        error = "DIA_SEMANA_INVALIDO",
                        message = $"El día de la semana debe estar entre 1 (Lunes) y 7 (Domingo). Valor recibido: {detalle.dia_semana}.",
                        constraint = "chk_dia_semana_detalle"
                    });
                }

                // VALIDACIÓN 2: hora_fin debe ser mayor que hora_inicio
                if (detalle.hora_fin <= detalle.hora_inicio)
                {
                    return BadRequest(new
                    {
                        error = "HORA_FIN_INVALIDA",
                        message = $"La hora fin ({detalle.hora_fin}) debe ser mayor que la hora inicio ({detalle.hora_inicio}).",
                        constraint = "chk_hora_fin_detalle"
                    });
                }

                // VALIDACIÓN 3: unidades debe ser mayor a 0
                if (detalle.unidades <= 0)
                {
                    return BadRequest(new
                    {
                        error = "UNIDADES_INVALIDAS",
                        message = $"Las unidades deben ser mayor a 0. Valor recibido: {detalle.unidades}.",
                        constraint = "chk_unidades"
                    });
                }

                // VALIDACIÓN 4: Verificar que el horario existe y está activo
                var horario = await _db.horario.FindAsync(detalle.horarioid);
                if (horario == null)
                {
                    return NotFound(new
                    {
                        error = "HORARIO_NO_ENCONTRADO",
                        message = $"No existe un horario con el id {detalle.horarioid}."
                    });
                }

                if (horario.estado != "ACT")
                {
                    return BadRequest(new
                    {
                        error = "HORARIO_INACTIVO",
                        message = $"El horario con {horario.minutos_por_unidad} minutos por unidad existe pero no está activo (Estado = {horario.estado})."
                    });
                }

                // VALIDACIÓN 5: Verificar que no exista un detalle con la misma combinación
                var detalleExistente = await _db.horariodetalle
                    .FirstOrDefaultAsync(hd => hd.horarioid == detalle.horarioid
                                            && hd.dia_semana == detalle.dia_semana
                                            && hd.hora_inicio == detalle.hora_inicio
                                            && hd.hora_fin == detalle.hora_fin);

                if (detalleExistente != null)
                {
                    string[] diasSemana = { "", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo" };
                    string diaNombre = diasSemana[detalle.dia_semana];

                    return BadRequest(new
                    {
                        error = "DETALLE_DUPLICADO",
                        message = $"Ya existe un detalle de horario para el día {diaNombre} de {detalle.hora_inicio} a {detalle.hora_fin} en este horario (ID: {detalleExistente.horariodetalleid}, Estado: {detalleExistente.estado}).",
                        constraint = "uq_horario_dia_hora",
                        detalle_existente_id = detalleExistente.horariodetalleid,
                        detalle_existente_estado = detalleExistente.estado
                    });
                }

                // VALIDACIÓN 6: Verificar que las unidades calculadas coincidan con los minutos por unidad
                var minutosCalculados = (int)(detalle.hora_fin - detalle.hora_inicio).TotalMinutes;
                var unidadesCalculadas = minutosCalculados / horario.minutos_por_unidad;

                if (detalle.unidades != unidadesCalculadas)
                {
                    return BadRequest(new
                    {
                        error = "UNIDADES_NO_COINCIDEN",
                        message = $"Las unidades especificadas ({detalle.unidades}) no coinciden con las calculadas ({unidadesCalculadas}). " +
                                  $"Minutos totales: {minutosCalculados}, Minutos por unidad: {horario.minutos_por_unidad}.",
                        sugerencia = $"Ajuste las unidades a {unidadesCalculadas} o modifique el rango horario."
                    });
                }

                // Establecer valores por defecto
                if (string.IsNullOrWhiteSpace(detalle.estado))
                    detalle.estado = "ACT";

                detalle.fecha_creacion = DateTime.UtcNow;

                _db.horariodetalle.Add(detalle);
                await _db.SaveChangesAsync();

                return CreatedAtAction(nameof(GetAllDetalleHorarioByHorarioId),
                    new { horarioId = detalle.horarioid },
                    detalle);
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;

                if (innerMessage.Contains("uq_horario_dia_hora"))
                {
                    string[] diasSemana = { "", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo" };
                    string diaNombre = detalle.dia_semana >= 1 && detalle.dia_semana <= 7 ? diasSemana[detalle.dia_semana] : detalle.dia_semana.ToString();

                    return BadRequest(new
                    {
                        error = "DETALLE_DUPLICADO",
                        message = $"Ya existe un detalle de horario para el día {diaNombre} de {detalle.hora_inicio} a {detalle.hora_fin} en este horario.",
                        constraint = "uq_horario_dia_hora"
                    });
                }

                if (innerMessage.Contains("horariodetalle_horarioid_fkey"))
                {
                    return BadRequest(new
                    {
                        error = "HORARIO_NO_ENCONTRADO",
                        message = $"El horario con id {detalle.horarioid} no existe.",
                        constraint = "horariodetalle_horarioid_fkey"
                    });
                }

                if (innerMessage.Contains("chk_dia_semana_detalle"))
                {
                    return BadRequest(new
                    {
                        error = "DIA_SEMANA_INVALIDO",
                        message = "El día de la semana debe estar entre 1 y 7.",
                        constraint = "chk_dia_semana_detalle"
                    });
                }

                if (innerMessage.Contains("chk_hora_fin_detalle"))
                {
                    return BadRequest(new
                    {
                        error = "HORA_FIN_INVALIDA",
                        message = "La hora fin debe ser mayor que la hora inicio.",
                        constraint = "chk_hora_fin_detalle"
                    });
                }

                if (innerMessage.Contains("chk_unidades"))
                {
                    return BadRequest(new
                    {
                        error = "UNIDADES_INVALIDAS",
                        message = "Las unidades deben ser mayor a 0.",
                        constraint = "chk_unidades"
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

        // Actualizar un detalle de horario existente
        [HttpPut("UpdateDetalleHorario/{id:long}")]
        public async Task<IActionResult> UpdateDetalleHorario(long id, [FromBody] HorarioDetalle detalle)
        {
            try
            {
                if (id != detalle.horariodetalleid)
                    return BadRequest(new
                    {
                        error = "ID_NO_COINCIDE",
                        message = $"El id de la URL ({id}) no coincide con el id del cuerpo ({detalle.horariodetalleid})."
                    });

                var existing = await _db.horariodetalle.FindAsync(id);
                if (existing == null)
                    return NotFound(new
                    {
                        error = "DETALLE_NO_ENCONTRADO",
                        message = $"No existe un detalle de horario con el id {id}."
                    });

                // VALIDACIÓN 1: dia_semana debe estar entre 1 y 7
                if (detalle.dia_semana < 1 || detalle.dia_semana > 7)
                {
                    return BadRequest(new
                    {
                        error = "DIA_SEMANA_INVALIDO",
                        message = $"El día de la semana debe estar entre 1 (Lunes) y 7 (Domingo). Valor recibido: {detalle.dia_semana}.",
                        constraint = "chk_dia_semana_detalle"
                    });
                }

                // VALIDACIÓN 2: hora_fin debe ser mayor que hora_inicio
                if (detalle.hora_fin <= detalle.hora_inicio)
                {
                    return BadRequest(new
                    {
                        error = "HORA_FIN_INVALIDA",
                        message = $"La hora fin ({detalle.hora_fin}) debe ser mayor que la hora inicio ({detalle.hora_inicio}).",
                        constraint = "chk_hora_fin_detalle"
                    });
                }

                // VALIDACIÓN 3: unidades debe ser mayor a 0
                if (detalle.unidades <= 0)
                {
                    return BadRequest(new
                    {
                        error = "UNIDADES_INVALIDAS",
                        message = $"Las unidades deben ser mayor a 0. Valor recibido: {detalle.unidades}.",
                        constraint = "chk_unidades"
                    });
                }

                // VALIDACIÓN 4: Verificar que el horario existe y está activo
                var horario = await _db.horario.FindAsync(detalle.horarioid);
                if (horario == null)
                {
                    return NotFound(new
                    {
                        error = "HORARIO_NO_ENCONTRADO",
                        message = $"No existe un horario con el id {detalle.horarioid}."
                    });
                }

                if (horario.estado != "ACT")
                {
                    return BadRequest(new
                    {
                        error = "HORARIO_INACTIVO",
                        message = $"El horario con {horario.minutos_por_unidad} minutos por unidad existe pero no está activo (Estado = {horario.estado})."
                    });
                }

                // VALIDACIÓN 5: Verificar que no exista otro detalle con la misma combinación (excepto el actual)
                var detalleExistente = await _db.horariodetalle
                    .FirstOrDefaultAsync(hd => hd.horarioid == detalle.horarioid
                                            && hd.dia_semana == detalle.dia_semana
                                            && hd.hora_inicio == detalle.hora_inicio
                                            && hd.hora_fin == detalle.hora_fin
                                            && hd.horariodetalleid != id);

                if (detalleExistente != null)
                {
                    string[] diasSemana = { "", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo" };
                    string diaNombre = diasSemana[detalle.dia_semana];

                    return BadRequest(new
                    {
                        error = "DETALLE_DUPLICADO",
                        message = $"Ya existe otro detalle de horario para el día {diaNombre} de {detalle.hora_inicio} a {detalle.hora_fin} en este horario (ID: {detalleExistente.horariodetalleid}, Estado: {detalleExistente.estado}).",
                        constraint = "uq_horario_dia_hora",
                        detalle_existente_id = detalleExistente.horariodetalleid,
                        detalle_existente_estado = detalleExistente.estado
                    });
                }

                // VALIDACIÓN 6: Verificar que las unidades calculadas coincidan con los minutos por unidad
                var minutosCalculados = (int)(detalle.hora_fin - detalle.hora_inicio).TotalMinutes;
                var unidadesCalculadas = minutosCalculados / horario.minutos_por_unidad;

                if (detalle.unidades != unidadesCalculadas)
                {
                    return BadRequest(new
                    {
                        error = "UNIDADES_NO_COINCIDEN",
                        message = $"Las unidades especificadas ({detalle.unidades}) no coinciden con las calculadas ({unidadesCalculadas}). " +
                                  $"Minutos totales: {minutosCalculados}, Minutos por unidad: {horario.minutos_por_unidad}.",
                        sugerencia = $"Ajuste las unidades a {unidadesCalculadas} o modifique el rango horario."
                    });
                }

                // Actualizamos campos
                existing.dia_semana = detalle.dia_semana;
                existing.hora_inicio = detalle.hora_inicio;
                existing.hora_fin = detalle.hora_fin;
                existing.unidades = detalle.unidades;
                existing.estado = detalle.estado;
                existing.horarioid = detalle.horarioid;

                await _db.SaveChangesAsync();

                string[] dias = { "", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo" };
                return Ok(new
                {
                    message = "Detalle de horario actualizado correctamente",
                    detalle_id = existing.horariodetalleid,
                    dia_semana = dias[existing.dia_semana],
                    hora_inicio = existing.hora_inicio,
                    hora_fin = existing.hora_fin,
                    unidades = existing.unidades,
                    horario_id = existing.horarioid
                });
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;

                if (innerMessage.Contains("uq_horario_dia_hora"))
                {
                    string[] diasSemana = { "", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo" };
                    string diaNombre = detalle.dia_semana >= 1 && detalle.dia_semana <= 7 ? diasSemana[detalle.dia_semana] : detalle.dia_semana.ToString();

                    return BadRequest(new
                    {
                        error = "DETALLE_DUPLICADO",
                        message = $"Ya existe un detalle de horario para el día {diaNombre} de {detalle.hora_inicio} a {detalle.hora_fin} en este horario.",
                        constraint = "uq_horario_dia_hora"
                    });
                }

                if (innerMessage.Contains("horariodetalle_horarioid_fkey"))
                {
                    return BadRequest(new
                    {
                        error = "HORARIO_NO_ENCONTRADO",
                        message = $"El horario con id {detalle.horarioid} no existe.",
                        constraint = "horariodetalle_horarioid_fkey"
                    });
                }

                if (innerMessage.Contains("chk_dia_semana_detalle"))
                {
                    return BadRequest(new
                    {
                        error = "DIA_SEMANA_INVALIDO",
                        message = "El día de la semana debe estar entre 1 y 7.",
                        constraint = "chk_dia_semana_detalle"
                    });
                }

                if (innerMessage.Contains("chk_hora_fin_detalle"))
                {
                    return BadRequest(new
                    {
                        error = "HORA_FIN_INVALIDA",
                        message = "La hora fin debe ser mayor que la hora inicio.",
                        constraint = "chk_hora_fin_detalle"
                    });
                }

                if (innerMessage.Contains("chk_unidades"))
                {
                    return BadRequest(new
                    {
                        error = "UNIDADES_INVALIDAS",
                        message = "Las unidades deben ser mayor a 0.",
                        constraint = "chk_unidades"
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

        // Inactivar un detalle de horario (soft delete)
        [HttpDelete("InactivateDetalleHorario/{id:long}")]
        public async Task<IActionResult> InactivateDetalleHorario(long id)
        {
            try
            {
                var detalle = await _db.horariodetalle.FindAsync(id);
                if (detalle == null)
                    return NotFound(new
                    {
                        error = "DETALLE_NO_ENCONTRADO",
                        message = $"No existe un detalle de horario con el id {id}."
                    });

                if (detalle.estado == "INA")
                {
                    string[] diasSemana = { "", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo" };
                    string diaNombre = diasSemana[detalle.dia_semana];

                    return BadRequest(new
                    {
                        error = "DETALLE_YA_INACTIVO",
                        message = $"El detalle de horario del día {diaNombre} de {detalle.hora_inicio} a {detalle.hora_fin} ya está inactivo."
                    });
                }

                detalle.estado = "INA";
                await _db.SaveChangesAsync();

                string[] dias = { "", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo" };
                return Ok(new
                {
                    message = "Detalle de horario inactivado correctamente",
                    detalle_id = detalle.horariodetalleid,
                    dia_semana = dias[detalle.dia_semana],
                    hora_inicio = detalle.hora_inicio,
                    hora_fin = detalle.hora_fin,
                    horario_id = detalle.horarioid
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

