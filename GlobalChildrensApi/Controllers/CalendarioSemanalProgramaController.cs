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
    public class CalendarioSemanalProgramaController : ControllerBase
    {
        private readonly AppDbContext _db;

        public CalendarioSemanalProgramaController(AppDbContext db)
        {
            _db = db;
        }

        // Obtener todas las semanas activas
        [HttpGet("GetAll")]
        public async Task<ActionResult<IEnumerable<CalendarioSemanalPrograma>>> GetAll()
        {
            try
            {
                var semanas = await _db.calendariosemanalprograma
                    .Where(c => c.estado == "ACT")
                    .OrderBy(c => c.anio)
                    .ThenBy(c => c.numero_semana)
                    .ToListAsync();

                return Ok(semanas);
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

        // Obtener semana por ID
        [HttpGet("GetById/{id:long}")]
        public async Task<ActionResult<CalendarioSemanalPrograma>> GetById(long id)
        {
            try
            {
                var semana = await _db.calendariosemanalprograma
                    .FirstOrDefaultAsync(c => c.calendariosemanalprogramaid == id);

                if (semana == null)
                    return NotFound(new
                    {
                        error = "SEMANA_NO_ENCONTRADA",
                        message = $"No existe una semana con el id {id}."
                    });

                return Ok(semana);
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

        // Obtener semanas por programa ID (SOLICITADO)
        [HttpGet("GetSemanaPorProgramaId/{programaid:long}")]
        public async Task<ActionResult<IEnumerable<CalendarioSemanalPrograma>>> GetSemanaPorProgramaId(long programaid)
        {
            try
            {
                var semanas = await _db.calendariosemanalprograma
                    .Where(c => c.programaid == programaid && c.estado == "ACT")
                    .OrderBy(c => c.anio)
                    .ThenBy(c => c.numero_semana)
                    .ToListAsync();

                return Ok(semanas);
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

        // Obtener semana por fecha (SOLICITADO)
        [HttpGet("GetSemanaByDate")]
        public async Task<ActionResult<CalendarioSemanalPrograma>> GetSemanaByDate(
            [FromQuery] DateTime fecha,
            [FromQuery] long? programaid = null)
        {
            try
            {
                var query = _db.calendariosemanalprograma
                    .Where(c => c.fecha_inicio <= fecha.Date 
                             && c.fecha_fin >= fecha.Date 
                             && c.estado == "ACT");

                if (programaid.HasValue)
                {
                    query = query.Where(c => c.programaid == programaid.Value);
                }

                var semana = await query.FirstOrDefaultAsync();

                if (semana == null)
                {
                    var mensaje = programaid.HasValue
                        ? $"No existe una semana activa para la fecha {fecha:yyyy-MM-dd} en el programa {programaid}."
                        : $"No existe una semana activa para la fecha {fecha:yyyy-MM-dd}.";

                    return NotFound(new
                    {
                        error = "SEMANA_NO_ENCONTRADA",
                        message = mensaje,
                        fecha_consultada = fecha.Date,
                        programa_id = programaid
                    });
                }

                return Ok(semana);
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

        // Obtener semanas de un año específico
        [HttpGet("GetByAnio/{anio:int}")]
        public async Task<ActionResult<IEnumerable<CalendarioSemanalPrograma>>> GetByAnio(int anio, [FromQuery] long? programaid = null)
        {
            try
            {
                if (anio < 2000 || anio > 2100)
                {
                    return BadRequest(new
                    {
                        error = "ANIO_INVALIDO",
                        message = $"El año debe estar entre 2000 y 2100. Valor recibido: {anio}",
                        constraint = "chk_anio"
                    });
                }

                var query = _db.calendariosemanalprograma
                    .Where(c => c.anio == anio && c.estado == "ACT");

                if (programaid.HasValue)
                {
                    query = query.Where(c => c.programaid == programaid.Value);
                }

                var semanas = await query
                    .OrderBy(c => c.numero_semana)
                    .ToListAsync();

                return Ok(semanas);
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

        // Crear semana (SOLICITADO)
        [HttpPost("CreateSemana")]
        public async Task<ActionResult<CalendarioSemanalPrograma>> CreateSemana([FromBody] CalendarioSemanalPrograma semana)
        {
            try
            {
                // VALIDACIÓN 1: Año debe estar entre 2000 y 2100
                if (semana.anio < 2000 || semana.anio > 2100)
                {
                    return BadRequest(new
                    {
                        error = "ANIO_INVALIDO",
                        message = $"El año debe estar entre 2000 y 2100. Valor recibido: {semana.anio}",
                        constraint = "chk_anio"
                    });
                }

                // VALIDACIÓN 2: Número de semana debe estar entre 1 y 53
                if (semana.numero_semana < 1 || semana.numero_semana > 53)
                {
                    return BadRequest(new
                    {
                        error = "NUMERO_SEMANA_INVALIDO",
                        message = $"El número de semana debe estar entre 1 y 53. Valor recibido: {semana.numero_semana}",
                        constraint = "chk_numero_semana"
                    });
                }

                // VALIDACIÓN 3: Fecha fin debe ser mayor que fecha inicio
                if (semana.fecha_fin <= semana.fecha_inicio)
                {
                    return BadRequest(new
                    {
                        error = "FECHA_FIN_INVALIDA",
                        message = $"La fecha fin ({semana.fecha_fin:yyyy-MM-dd}) debe ser mayor que la fecha inicio ({semana.fecha_inicio:yyyy-MM-dd}).",
                        constraint = "chk_fecha_fin_calendario"
                    });
                }

                // VALIDACIÓN 4: Verificar que el programa exista
                var programa = await _db.programa.FindAsync(semana.programaid);
                if (programa == null)
                {
                    return BadRequest(new
                    {
                        error = "PROGRAMA_NO_EXISTE",
                        message = $"El programa con ID {semana.programaid} no existe en el sistema.",
                        constraint = "calendariosemanalprograma_programaid_fkey"
                    });
                }

                // VALIDACIÓN 5: Verificar constraint único (programaid + anio + numero_semana)
                var semanaExistente = await _db.calendariosemanalprograma
                    .FirstOrDefaultAsync(c => c.programaid == semana.programaid 
                                           && c.anio == semana.anio 
                                           && c.numero_semana == semana.numero_semana);

                if (semanaExistente != null)
                {
                    return BadRequest(new
                    {
                        error = "SEMANA_DUPLICADA",
                        message = $"Ya existe la semana {semana.numero_semana} del año {semana.anio} para el programa {semana.programaid} (ID: {semanaExistente.calendariosemanalprogramaid}, Estado: {semanaExistente.estado}).",
                        constraint = "uq_calendario_programa_anio_semana",
                        semana_existente_id = semanaExistente.calendariosemanalprogramaid,
                        semana_existente_estado = semanaExistente.estado
                    });
                }

                // VALIDACIÓN 6: Verificar que no haya solapamiento de fechas con otras semanas del mismo programa
                var solapamiento = await _db.calendariosemanalprograma
                    .Where(c => c.programaid == semana.programaid 
                             && c.estado == "ACT"
                             && ((c.fecha_inicio <= semana.fecha_fin && c.fecha_fin >= semana.fecha_inicio)))
                    .FirstOrDefaultAsync();

                if (solapamiento != null)
                {
                    return BadRequest(new
                    {
                        error = "FECHAS_SOLAPADAS",
                        message = $"Las fechas ({semana.fecha_inicio:yyyy-MM-dd} - {semana.fecha_fin:yyyy-MM-dd}) se solapan con otra semana del programa: Semana {solapamiento.numero_semana} del {solapamiento.anio} ({solapamiento.fecha_inicio:yyyy-MM-dd} - {solapamiento.fecha_fin:yyyy-MM-dd}).",
                        semana_solapada_id = solapamiento.calendariosemanalprogramaid,
                        semana_solapada_numero = solapamiento.numero_semana,
                        semana_solapada_anio = solapamiento.anio
                    });
                }

                // Establecer valores por defecto
                if (string.IsNullOrWhiteSpace(semana.estado))
                    semana.estado = "ACT";

                _db.calendariosemanalprograma.Add(semana);
                await _db.SaveChangesAsync();

                return CreatedAtAction(nameof(GetById), new { id = semana.calendariosemanalprogramaid }, semana);
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;

                if (innerMessage.Contains("uq_calendario_programa_anio_semana"))
                {
                    return BadRequest(new
                    {
                        error = "SEMANA_DUPLICADA",
                        message = $"Ya existe la semana {semana.numero_semana} del año {semana.anio} para este programa.",
                        constraint = "uq_calendario_programa_anio_semana"
                    });
                }
                else if (innerMessage.Contains("chk_anio"))
                {
                    return BadRequest(new
                    {
                        error = "ANIO_INVALIDO",
                        message = "El año debe estar entre 2000 y 2100.",
                        constraint = "chk_anio"
                    });
                }
                else if (innerMessage.Contains("chk_numero_semana"))
                {
                    return BadRequest(new
                    {
                        error = "NUMERO_SEMANA_INVALIDO",
                        message = "El número de semana debe estar entre 1 y 53.",
                        constraint = "chk_numero_semana"
                    });
                }
                else if (innerMessage.Contains("chk_fecha_fin_calendario"))
                {
                    return BadRequest(new
                    {
                        error = "FECHA_FIN_INVALIDA",
                        message = "La fecha fin debe ser mayor que la fecha inicio.",
                        constraint = "chk_fecha_fin_calendario"
                    });
                }
                else if (innerMessage.Contains("calendariosemanalprograma_programaid_fkey"))
                {
                    return BadRequest(new
                    {
                        error = "PROGRAMA_NO_EXISTE",
                        message = $"El programa con ID {semana.programaid} no existe en el sistema.",
                        constraint = "calendariosemanalprograma_programaid_fkey"
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
        public async Task<IActionResult> Update(long id, [FromBody] CalendarioSemanalPrograma semana)
        {
            try
            {
                if (id != semana.calendariosemanalprogramaid)
                    return BadRequest(new
                    {
                        error = "ID_NO_COINCIDE",
                        message = $"El id de la URL ({id}) no coincide con el id del cuerpo ({semana.calendariosemanalprogramaid})."
                    });

                var existing = await _db.calendariosemanalprograma.FindAsync(id);
                if (existing == null)
                    return NotFound(new
                    {
                        error = "SEMANA_NO_ENCONTRADA",
                        message = $"No existe una semana con el id {id}."
                    });

                // Validaciones (mismas que en Create)
                if (semana.anio < 2000 || semana.anio > 2100)
                {
                    return BadRequest(new
                    {
                        error = "ANIO_INVALIDO",
                        message = $"El año debe estar entre 2000 y 2100. Valor recibido: {semana.anio}",
                        constraint = "chk_anio"
                    });
                }

                if (semana.numero_semana < 1 || semana.numero_semana > 53)
                {
                    return BadRequest(new
                    {
                        error = "NUMERO_SEMANA_INVALIDO",
                        message = $"El número de semana debe estar entre 1 y 53. Valor recibido: {semana.numero_semana}",
                        constraint = "chk_numero_semana"
                    });
                }

                if (semana.fecha_fin <= semana.fecha_inicio)
                {
                    return BadRequest(new
                    {
                        error = "FECHA_FIN_INVALIDA",
                        message = $"La fecha fin ({semana.fecha_fin:yyyy-MM-dd}) debe ser mayor que la fecha inicio ({semana.fecha_inicio:yyyy-MM-dd}).",
                        constraint = "chk_fecha_fin_calendario"
                    });
                }

                // Verificar constraint único (excepto el registro actual)
                var semanaExistente = await _db.calendariosemanalprograma
                    .FirstOrDefaultAsync(c => c.programaid == semana.programaid 
                                           && c.anio == semana.anio 
                                           && c.numero_semana == semana.numero_semana
                                           && c.calendariosemanalprogramaid != id);

                if (semanaExistente != null)
                {
                    return BadRequest(new
                    {
                        error = "SEMANA_DUPLICADA",
                        message = $"Ya existe otra semana {semana.numero_semana} del año {semana.anio} para el programa {semana.programaid}.",
                        constraint = "uq_calendario_programa_anio_semana"
                    });
                }

                // Verificar solapamiento (excepto el registro actual)
                var solapamiento = await _db.calendariosemanalprograma
                    .Where(c => c.programaid == semana.programaid 
                             && c.estado == "ACT"
                             && c.calendariosemanalprogramaid != id
                             && ((c.fecha_inicio <= semana.fecha_fin && c.fecha_fin >= semana.fecha_inicio)))
                    .FirstOrDefaultAsync();

                if (solapamiento != null)
                {
                    return BadRequest(new
                    {
                        error = "FECHAS_SOLAPADAS",
                        message = $"Las fechas se solapan con la semana {solapamiento.numero_semana} del {solapamiento.anio}.",
                        semana_solapada_id = solapamiento.calendariosemanalprogramaid
                    });
                }

                // Actualizamos campos
                existing.anio = semana.anio;
                existing.numero_semana = semana.numero_semana;
                existing.fecha_inicio = semana.fecha_inicio;
                existing.fecha_fin = semana.fecha_fin;
                existing.estado = semana.estado;
                existing.programaid = semana.programaid;

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Semana actualizada correctamente",
                    semana_id = existing.calendariosemanalprogramaid,
                    anio = existing.anio,
                    numero_semana = existing.numero_semana,
                    fecha_inicio = existing.fecha_inicio,
                    fecha_fin = existing.fecha_fin
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

        // Inactivar semana
        [HttpDelete("Delete/{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var semana = await _db.calendariosemanalprograma.FindAsync(id);
                if (semana == null)
                    return NotFound(new
                    {
                        error = "SEMANA_NO_ENCONTRADA",
                        message = $"No existe una semana con el id {id}."
                    });

                if (semana.estado == "INA")
                {
                    return BadRequest(new
                    {
                        error = "SEMANA_YA_INACTIVA",
                        message = $"La semana {semana.numero_semana} del año {semana.anio} ya está inactiva."
                    });
                }

                semana.estado = "INA";
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Semana inactivada correctamente",
                    semana_id = semana.calendariosemanalprogramaid,
                    anio = semana.anio,
                    numero_semana = semana.numero_semana
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

