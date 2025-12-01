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
    public class PeriodoEvaluacionController : ControllerBase
    {
        private readonly AppDbContext _db;

        public PeriodoEvaluacionController(AppDbContext db)
        {
            _db = db;
        }

        // Obtener todos los períodos de evaluación activos (SOLICITADO)
        [HttpGet("GetAllPeriodoEvaluacion")]
        public async Task<ActionResult<IEnumerable<PeriodoEvaluacion>>> GetAllPeriodoEvaluacion()
        {
            try
            {
                var periodos = await _db.periodoevaluacion
                    .Where(p => p.estado == "ACT")
                    .OrderBy(p => p.orden)
                    .ThenBy(p => p.fecha_inicio)
                    .ToListAsync();

                return Ok(periodos);
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

        // Obtener período de evaluación por ID
        [HttpGet("GetById/{id:long}")]
        public async Task<ActionResult<PeriodoEvaluacion>> GetById(long id)
        {
            try
            {
                var periodo = await _db.periodoevaluacion.FirstOrDefaultAsync(p => p.periodoevaluacionid == id);

                if (periodo == null)
                    return NotFound(new
                    {
                        error = "PERIODO_NO_ENCONTRADO",
                        message = $"No existe un período de evaluación con el id {id}."
                    });

                if (periodo.estado != "ACT")
                    return BadRequest(new
                    {
                        error = "PERIODO_INACTIVO",
                        message = $"El período de evaluación con id {id} existe pero no está activo (Estado = {periodo.estado})."
                    });

                return Ok(periodo);
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

        // Obtener período actual (el que contiene la fecha de hoy)
        [HttpGet("GetPeriodoActual")]
        public async Task<ActionResult<PeriodoEvaluacion>> GetPeriodoActual()
        {
            try
            {
                var hoy = DateTime.UtcNow.Date;
                var periodoActual = await _db.periodoevaluacion
                    .Where(p => p.estado == "ACT" 
                        && p.fecha_inicio.Date <= hoy 
                        && p.fecha_fin.Date >= hoy)
                    .FirstOrDefaultAsync();

                if (periodoActual == null)
                {
                    return NotFound(new
                    {
                        error = "PERIODO_ACTUAL_NO_ENCONTRADO",
                        message = $"No hay un período de evaluación activo que contenga la fecha actual ({hoy:yyyy-MM-dd})."
                    });
                }

                return Ok(periodoActual);
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

        // Obtener período por fecha específica
        [HttpGet("GetByFecha")]
        public async Task<ActionResult<PeriodoEvaluacion>> GetByFecha([FromQuery] DateTime fecha)
        {
            try
            {
                var fechaBusqueda = fecha.Date;
                var periodo = await _db.periodoevaluacion
                    .Where(p => p.estado == "ACT" 
                        && p.fecha_inicio.Date <= fechaBusqueda 
                        && p.fecha_fin.Date >= fechaBusqueda)
                    .FirstOrDefaultAsync();

                if (periodo == null)
                {
                    return NotFound(new
                    {
                        error = "PERIODO_NO_ENCONTRADO",
                        message = $"No hay un período de evaluación activo que contenga la fecha {fechaBusqueda:yyyy-MM-dd}."
                    });
                }

                return Ok(periodo);
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

        // Crear período de evaluación
        [HttpPost("Create")]
        public async Task<ActionResult<PeriodoEvaluacion>> Create([FromBody] PeriodoEvaluacion periodo)
        {
            try
            {
                // VALIDACIÓN 1: Nombre no vacío
                if (string.IsNullOrWhiteSpace(periodo.nombre))
                {
                    return BadRequest(new
                    {
                        error = "NOMBRE_VACIO",
                        message = "El nombre del período de evaluación no puede estar vacío."
                    });
                }

                // VALIDACIÓN 2: Longitud del nombre (máx 100 caracteres)
                if (periodo.nombre.Length > 100)
                {
                    return BadRequest(new
                    {
                        error = "NOMBRE_MUY_LARGO",
                        message = $"El nombre no puede tener más de 100 caracteres. Longitud actual: {periodo.nombre.Length}."
                    });
                }

                // VALIDACIÓN 3: Fecha fin > fecha inicio
                if (periodo.fecha_fin.Date <= periodo.fecha_inicio.Date)
                {
                    return BadRequest(new
                    {
                        error = "FECHA_FIN_INVALIDA",
                        message = "La fecha de fin debe ser mayor que la fecha de inicio.",
                        constraint = "chk_fecha_fin",
                        fecha_inicio = periodo.fecha_inicio.Date.ToString("yyyy-MM-dd"),
                        fecha_fin = periodo.fecha_fin.Date.ToString("yyyy-MM-dd")
                    });
                }

                // VALIDACIÓN 4: Orden debe ser positivo
                if (periodo.orden <= 0)
                {
                    return BadRequest(new
                    {
                        error = "ORDEN_INVALIDO",
                        message = "El orden debe ser un número positivo mayor a 0."
                    });
                }

                // VALIDACIÓN 5: Fechas únicas (no duplicadas)
                var periodoExistente = await _db.periodoevaluacion
                    .AnyAsync(p => p.fecha_inicio.Date == periodo.fecha_inicio.Date 
                        && p.fecha_fin.Date == periodo.fecha_fin.Date);

                if (periodoExistente)
                {
                    return BadRequest(new
                    {
                        error = "FECHAS_DUPLICADAS",
                        message = $"Ya existe un período de evaluación con las mismas fechas (Inicio: {periodo.fecha_inicio:yyyy-MM-dd}, Fin: {periodo.fecha_fin:yyyy-MM-dd}).",
                        constraint = "uq_fecha_periodo"
                    });
                }

                // VALIDACIÓN 6: Verificar solapamiento de fechas con otros períodos activos
                var periodosSolapados = await _db.periodoevaluacion
                    .Where(p => p.estado == "ACT" 
                        && ((p.fecha_inicio.Date <= periodo.fecha_fin.Date && p.fecha_fin.Date >= periodo.fecha_inicio.Date)))
                    .ToListAsync();

                if (periodosSolapados.Any())
                {
                    var detalles = periodosSolapados.Select(p => $"'{p.nombre}' ({p.fecha_inicio:yyyy-MM-dd} a {p.fecha_fin:yyyy-MM-dd})");
                    return BadRequest(new
                    {
                        error = "FECHAS_SOLAPADAS",
                        message = $"Las fechas se solapan con {periodosSolapados.Count} período(s) existente(s): {string.Join(", ", detalles)}.",
                        periodos_solapados = periodosSolapados.Select(p => new
                        {
                            periodo_id = p.periodoevaluacionid,
                            nombre = p.nombre,
                            fecha_inicio = p.fecha_inicio.Date.ToString("yyyy-MM-dd"),
                            fecha_fin = p.fecha_fin.Date.ToString("yyyy-MM-dd")
                        })
                    });
                }

                // Establecer valores por defecto
                if (string.IsNullOrWhiteSpace(periodo.estado))
                    periodo.estado = "ACT";

                _db.periodoevaluacion.Add(periodo);
                await _db.SaveChangesAsync();

                return CreatedAtAction(nameof(GetById), new { id = periodo.periodoevaluacionid }, periodo);
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;

                if (innerMessage.Contains("uq_fecha_periodo"))
                {
                    return BadRequest(new
                    {
                        error = "FECHAS_DUPLICADAS",
                        message = $"Ya existe un período con las mismas fechas (Inicio: {periodo.fecha_inicio:yyyy-MM-dd}, Fin: {periodo.fecha_fin:yyyy-MM-dd}).",
                        constraint = "uq_fecha_periodo"
                    });
                }
                else if (innerMessage.Contains("chk_fecha_fin"))
                {
                    return BadRequest(new
                    {
                        error = "FECHA_FIN_INVALIDA",
                        message = "La fecha de fin debe ser mayor que la fecha de inicio.",
                        constraint = "chk_fecha_fin"
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

        // Actualizar período de evaluación
        [HttpPut("Update/{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] PeriodoEvaluacion periodo)
        {
            try
            {
                if (id != periodo.periodoevaluacionid)
                    return BadRequest(new
                    {
                        error = "ID_NO_COINCIDE",
                        message = $"El id de la URL ({id}) no coincide con el id del cuerpo ({periodo.periodoevaluacionid})."
                    });

                var existing = await _db.periodoevaluacion.FindAsync(id);
                if (existing == null)
                    return NotFound(new
                    {
                        error = "PERIODO_NO_ENCONTRADO",
                        message = $"No existe un período de evaluación con el id {id}."
                    });

                // VALIDACIÓN 1: Nombre no vacío
                if (string.IsNullOrWhiteSpace(periodo.nombre))
                {
                    return BadRequest(new
                    {
                        error = "NOMBRE_VACIO",
                        message = "El nombre del período de evaluación no puede estar vacío."
                    });
                }

                // VALIDACIÓN 2: Longitud del nombre (máx 100 caracteres)
                if (periodo.nombre.Length > 100)
                {
                    return BadRequest(new
                    {
                        error = "NOMBRE_MUY_LARGO",
                        message = $"El nombre no puede tener más de 100 caracteres. Longitud actual: {periodo.nombre.Length}."
                    });
                }

                // VALIDACIÓN 3: Fecha fin > fecha inicio
                if (periodo.fecha_fin.Date <= periodo.fecha_inicio.Date)
                {
                    return BadRequest(new
                    {
                        error = "FECHA_FIN_INVALIDA",
                        message = "La fecha de fin debe ser mayor que la fecha de inicio.",
                        constraint = "chk_fecha_fin",
                        fecha_inicio = periodo.fecha_inicio.Date.ToString("yyyy-MM-dd"),
                        fecha_fin = periodo.fecha_fin.Date.ToString("yyyy-MM-dd")
                    });
                }

                // VALIDACIÓN 4: Orden debe ser positivo
                if (periodo.orden <= 0)
                {
                    return BadRequest(new
                    {
                        error = "ORDEN_INVALIDO",
                        message = "El orden debe ser un número positivo mayor a 0."
                    });
                }

                // VALIDACIÓN 5: Fechas únicas (si cambiaron)
                if (existing.fecha_inicio.Date != periodo.fecha_inicio.Date || existing.fecha_fin.Date != periodo.fecha_fin.Date)
                {
                    var periodoExistente = await _db.periodoevaluacion
                        .AnyAsync(p => p.periodoevaluacionid != id 
                            && p.fecha_inicio.Date == periodo.fecha_inicio.Date 
                            && p.fecha_fin.Date == periodo.fecha_fin.Date);

                    if (periodoExistente)
                    {
                        return BadRequest(new
                        {
                            error = "FECHAS_DUPLICADAS",
                            message = $"Ya existe otro período con las mismas fechas (Inicio: {periodo.fecha_inicio:yyyy-MM-dd}, Fin: {periodo.fecha_fin:yyyy-MM-dd}).",
                            constraint = "uq_fecha_periodo"
                        });
                    }

                    // VALIDACIÓN 6: Verificar solapamiento con otros períodos (excepto él mismo)
                    var periodosSolapados = await _db.periodoevaluacion
                        .Where(p => p.periodoevaluacionid != id 
                            && p.estado == "ACT" 
                            && ((p.fecha_inicio.Date <= periodo.fecha_fin.Date && p.fecha_fin.Date >= periodo.fecha_inicio.Date)))
                        .ToListAsync();

                    if (periodosSolapados.Any())
                    {
                        var detalles = periodosSolapados.Select(p => $"'{p.nombre}' ({p.fecha_inicio:yyyy-MM-dd} a {p.fecha_fin:yyyy-MM-dd})");
                        return BadRequest(new
                        {
                            error = "FECHAS_SOLAPADAS",
                            message = $"Las fechas se solapan con {periodosSolapados.Count} período(s) existente(s): {string.Join(", ", detalles)}.",
                            periodos_solapados = periodosSolapados.Select(p => new
                            {
                                periodo_id = p.periodoevaluacionid,
                                nombre = p.nombre,
                                fecha_inicio = p.fecha_inicio.Date.ToString("yyyy-MM-dd"),
                                fecha_fin = p.fecha_fin.Date.ToString("yyyy-MM-dd")
                            })
                        });
                    }
                }

                // Actualizamos campos
                existing.nombre = periodo.nombre;
                existing.fecha_inicio = periodo.fecha_inicio;
                existing.fecha_fin = periodo.fecha_fin;
                existing.orden = periodo.orden;
                existing.estado = periodo.estado;

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Período de evaluación actualizado correctamente",
                    periodo_id = existing.periodoevaluacionid,
                    nombre = existing.nombre,
                    fecha_inicio = existing.fecha_inicio.Date.ToString("yyyy-MM-dd"),
                    fecha_fin = existing.fecha_fin.Date.ToString("yyyy-MM-dd"),
                    orden = existing.orden,
                    estado = existing.estado
                });
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;

                if (innerMessage.Contains("uq_fecha_periodo"))
                {
                    return BadRequest(new
                    {
                        error = "FECHAS_DUPLICADAS",
                        message = $"Ya existe un período con las mismas fechas (Inicio: {periodo.fecha_inicio:yyyy-MM-dd}, Fin: {periodo.fecha_fin:yyyy-MM-dd}).",
                        constraint = "uq_fecha_periodo"
                    });
                }
                else if (innerMessage.Contains("chk_fecha_fin"))
                {
                    return BadRequest(new
                    {
                        error = "FECHA_FIN_INVALIDA",
                        message = "La fecha de fin debe ser mayor que la fecha de inicio.",
                        constraint = "chk_fecha_fin"
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

        // Inactivar período de evaluación
        [HttpDelete("Inactivate/{id:long}")]
        public async Task<IActionResult> Inactivate(long id)
        {
            try
            {
                var periodo = await _db.periodoevaluacion.FindAsync(id);
                if (periodo == null)
                    return NotFound(new
                    {
                        error = "PERIODO_NO_ENCONTRADO",
                        message = $"No existe un período de evaluación con el id {id}."
                    });

                if (periodo.estado == "INA")
                {
                    return BadRequest(new
                    {
                        error = "PERIODO_YA_INACTIVO",
                        message = $"El período de evaluación con id {id} ya está inactivo."
                    });
                }

                periodo.estado = "INA";
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Período de evaluación inactivado correctamente",
                    periodo_id = periodo.periodoevaluacionid,
                    nombre = periodo.nombre
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

