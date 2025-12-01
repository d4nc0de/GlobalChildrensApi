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
    public class AsistenciaEstudianteController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AsistenciaEstudianteController(AppDbContext db)
        {
            _db = db;
        }

        // Obtener todas las asistencias activas
        [HttpGet("GetAllAsistenciaEstudiante")]
        public async Task<ActionResult<IEnumerable<AsistenciaEstudiante>>> GetAllAsistenciaEstudiante()
        {
            try
            {
                var asistencias = await _db.asistenciaestudiante
                    .Where(a => a.estado == "ACT")
                    .OrderByDescending(a => a.fecha_creacion)
                    .ToListAsync();

                return Ok(new
                {
                    total = asistencias.Count,
                    asistencias = asistencias
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

        // Obtener asistencia por ID
        [HttpGet("GetById/{id:long}")]
        public async Task<ActionResult<AsistenciaEstudiante>> GetById(long id)
        {
            try
            {
                var asistencia = await _db.asistenciaestudiante
                    .FirstOrDefaultAsync(a => a.asistenciaestudianteid == id);

                if (asistencia == null)
                    return NotFound(new
                    {
                        error = "ASISTENCIA_NO_ENCONTRADA",
                        message = $"No existe una asistencia con el id {id}."
                    });

                if (asistencia.estado != "ACT")
                    return BadRequest(new
                    {
                        error = "ASISTENCIA_INACTIVA",
                        message = $"La asistencia con id {id} existe pero no está activa (Estado = {asistencia.estado})."
                    });

                return Ok(asistencia);
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

        // Obtener asistencias de un estudiante específico
        [HttpGet("GetByEstudiante/{estudianteid:long}")]
        public async Task<ActionResult<IEnumerable<AsistenciaEstudiante>>> GetByEstudiante(long estudianteid)
        {
            try
            {
                var estudiante = await _db.estudiante.FindAsync(estudianteid);
                if (estudiante == null)
                {
                    return NotFound(new
                    {
                        error = "ESTUDIANTE_NO_ENCONTRADO",
                        message = $"No existe un estudiante con el id {estudianteid}."
                    });
                }

                var asistencias = await _db.asistenciaestudiante
                    .Where(a => a.estudianteid == estudianteid && a.estado == "ACT")
                    .OrderByDescending(a => a.fecha_creacion)
                    .ToListAsync();

                return Ok(new
                {
                    estudiante_id = estudianteid,
                    total_registros = asistencias.Count,
                    asistencias = asistencias
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

        // Obtener asistencias de una sesión de clase específica
        [HttpGet("GetBySesion/{sesionclaseid:long}")]
        public async Task<ActionResult<IEnumerable<AsistenciaEstudiante>>> GetBySesion(long sesionclaseid)
        {
            try
            {
                var asistencias = await _db.asistenciaestudiante
                    .Where(a => a.sesionclaseid == sesionclaseid && a.estado == "ACT")
                    .OrderBy(a => a.estudianteid)
                    .ToListAsync();

                return Ok(new
                {
                    sesion_clase_id = sesionclaseid,
                    total_registros = asistencias.Count,
                    asistencias = asistencias
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

        // Obtener estadísticas de asistencia de un estudiante
        [HttpGet("GetEstadisticas/{estudianteid:long}")]
        public async Task<ActionResult<object>> GetEstadisticas(long estudianteid)
        {
            try
            {
                var estudiante = await _db.estudiante.FindAsync(estudianteid);
                if (estudiante == null)
                {
                    return NotFound(new
                    {
                        error = "ESTUDIANTE_NO_ENCONTRADO",
                        message = $"No existe un estudiante con el id {estudianteid}."
                    });
                }

                var asistencias = await _db.asistenciaestudiante
                    .Where(a => a.estudianteid == estudianteid && a.estado == "ACT")
                    .ToListAsync();

                var totalSesiones = asistencias.Count;
                var asistenciasPresentes = asistencias.Count(a => a.asistio);
                var inasistencias = asistencias.Count(a => !a.asistio);
                var inasistenciasJustificadas = asistencias.Count(a => !a.asistio && a.justificada);
                var inasistenciasInjustificadas = asistencias.Count(a => !a.asistio && !a.justificada);
                
                var porcentajeAsistencia = totalSesiones > 0 
                    ? Math.Round((double)asistenciasPresentes / totalSesiones * 100, 2) 
                    : 0;

                return Ok(new
                {
                    estudiante_id = estudianteid,
                    total_sesiones = totalSesiones,
                    asistencias = asistenciasPresentes,
                    inasistencias = inasistencias,
                    inasistencias_justificadas = inasistenciasJustificadas,
                    inasistencias_injustificadas = inasistenciasInjustificadas,
                    porcentaje_asistencia = porcentajeAsistencia
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

        [HttpPost("Create")]
        public async Task<ActionResult<AsistenciaEstudiante>> Create([FromBody] AsistenciaEstudiante asistencia)
        {
            try
            {
                // VALIDACIÓN 1: Verificar que el estudiante exista y esté activo
                var estudiante = await _db.estudiante.FindAsync(asistencia.estudianteid);
                if (estudiante == null)
                {
                    return BadRequest(new
                    {
                        error = "ESTUDIANTE_NO_EXISTE",
                        message = $"El estudiante con ID {asistencia.estudianteid} no existe en el sistema.",
                        constraint = "asistenciaestudiante_estudianteid_fkey"
                    });
                }

                if (!estudiante.activo || estudiante.estado != "ACT")
                {
                    return BadRequest(new
                    {
                        error = "ESTUDIANTE_INACTIVO",
                        message = $"El estudiante no está activo."
                    });
                }

                // VALIDACIÓN 2: Verificar que no exista ya una asistencia para este estudiante en esta sesión
                var asistenciaExistente = await _db.asistenciaestudiante
                    .FirstOrDefaultAsync(a => a.sesionclaseid == asistencia.sesionclaseid 
                                && a.estudianteid == asistencia.estudianteid);

                if (asistenciaExistente != null)
                {
                    return BadRequest(new
                    {
                        error = "ASISTENCIA_DUPLICADA",
                        message = $"Ya existe un registro de asistencia para el estudiante {asistencia.estudianteid} en la sesión {asistencia.sesionclaseid} (ID: {asistenciaExistente.asistenciaestudianteid}, Estado: {asistenciaExistente.estado}).",
                        constraint = "uq_asistencia_sesion_estudiante",
                        asistencia_existente_id = asistenciaExistente.asistenciaestudianteid,
                        asistencia_existente_estado = asistenciaExistente.estado
                    });
                }

                // VALIDACIÓN 3: Si no asistió y tiene motivo de inasistencia, debe estar justificada
                if (!asistencia.asistio && asistencia.motivoinasistenciaestudianteid.HasValue && !asistencia.justificada)
                {
                    asistencia.justificada = true; // Automáticamente se marca como justificada si tiene motivo
                }

                // VALIDACIÓN 4: Si asistió, no puede tener motivo de inasistencia ni estar justificada
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
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;

                if (innerMessage.Contains("asistenciaestudiante_sesionclaseid_fkey"))
                {
                    return BadRequest(new
                    {
                        error = "SESION_NO_EXISTE",
                        message = $"La sesión de clase con ID {asistencia.sesionclaseid} no existe en el sistema.",
                        constraint = "asistenciaestudiante_sesionclaseid_fkey"
                    });
                }
                else if (innerMessage.Contains("asistenciaestudiante_motivoinasistenciaestudianteid_fkey"))
                {
                    return BadRequest(new
                    {
                        error = "MOTIVO_NO_EXISTE",
                        message = $"El motivo de inasistencia con ID {asistencia.motivoinasistenciaestudianteid} no existe en el sistema.",
                        constraint = "asistenciaestudiante_motivoinasistenciaestudianteid_fkey"
                    });
                }
                else if (innerMessage.Contains("uq_asistencia_sesion_estudiante"))
                {
                    return BadRequest(new
                    {
                        error = "ASISTENCIA_DUPLICADA",
                        message = "Ya existe un registro de asistencia para este estudiante en esta sesión de clase.",
                        constraint = "uq_asistencia_sesion_estudiante"
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
        public async Task<IActionResult> Update(long id, [FromBody] AsistenciaEstudiante asistencia)
        {
            try
            {
                if (id != asistencia.asistenciaestudianteid)
                    return BadRequest(new
                    {
                        error = "ID_NO_COINCIDE",
                        message = $"El id de la URL ({id}) no coincide con el id del cuerpo ({asistencia.asistenciaestudianteid})."
                    });

                var existing = await _db.asistenciaestudiante.FindAsync(id);
                if (existing == null)
                    return NotFound(new
                    {
                        error = "ASISTENCIA_NO_ENCONTRADA",
                        message = $"No existe una asistencia con el id {id}."
                    });

                // VALIDACIÓN 1: Verificar que el estudiante exista
                var estudiante = await _db.estudiante.FindAsync(asistencia.estudianteid);
                if (estudiante == null)
                {
                    return BadRequest(new
                    {
                        error = "ESTUDIANTE_NO_EXISTE",
                        message = $"El estudiante con ID {asistencia.estudianteid} no existe en el sistema.",
                        constraint = "asistenciaestudiante_estudianteid_fkey"
                    });
                }

                // VALIDACIÓN 2: Verificar que no exista otra asistencia para este estudiante en esta sesión
                var asistenciaExistente = await _db.asistenciaestudiante
                    .FirstOrDefaultAsync(a => a.sesionclaseid == asistencia.sesionclaseid 
                                && a.estudianteid == asistencia.estudianteid
                                && a.asistenciaestudianteid != id);

                if (asistenciaExistente != null)
                {
                    return BadRequest(new
                    {
                        error = "ASISTENCIA_DUPLICADA",
                        message = $"Ya existe otro registro de asistencia para el estudiante {asistencia.estudianteid} en la sesión {asistencia.sesionclaseid} (ID: {asistenciaExistente.asistenciaestudianteid}, Estado: {asistenciaExistente.estado}).",
                        constraint = "uq_asistencia_sesion_estudiante",
                        asistencia_existente_id = asistenciaExistente.asistenciaestudianteid,
                        asistencia_existente_estado = asistenciaExistente.estado
                    });
                }

                // VALIDACIÓN 3: Si no asistió y tiene motivo de inasistencia, debe estar justificada
                if (!asistencia.asistio && asistencia.motivoinasistenciaestudianteid.HasValue && !asistencia.justificada)
                {
                    asistencia.justificada = true;
                }

                // VALIDACIÓN 4: Si asistió, no puede tener motivo de inasistencia ni estar justificada
                if (asistencia.asistio)
                {
                    asistencia.motivoinasistenciaestudianteid = null;
                    asistencia.justificada = false;
                }

                // Actualizamos campos
                existing.asistio = asistencia.asistio;
                existing.observacion = asistencia.observacion;
                existing.justificada = asistencia.justificada;
                existing.estado = asistencia.estado;
                existing.sesionclaseid = asistencia.sesionclaseid;
                existing.estudianteid = asistencia.estudianteid;
                existing.motivoinasistenciaestudianteid = asistencia.motivoinasistenciaestudianteid;

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Asistencia actualizada correctamente",
                    asistencia_id = existing.asistenciaestudianteid,
                    estudiante_id = existing.estudianteid,
                    sesion_clase_id = existing.sesionclaseid,
                    asistio = existing.asistio,
                    justificada = existing.justificada
                });
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;

                if (innerMessage.Contains("asistenciaestudiante_sesionclaseid_fkey"))
                {
                    return BadRequest(new
                    {
                        error = "SESION_NO_EXISTE",
                        message = $"La sesión de clase con ID {asistencia.sesionclaseid} no existe en el sistema.",
                        constraint = "asistenciaestudiante_sesionclaseid_fkey"
                    });
                }
                else if (innerMessage.Contains("asistenciaestudiante_motivoinasistenciaestudianteid_fkey"))
                {
                    return BadRequest(new
                    {
                        error = "MOTIVO_NO_EXISTE",
                        message = $"El motivo de inasistencia con ID {asistencia.motivoinasistenciaestudianteid} no existe en el sistema.",
                        constraint = "asistenciaestudiante_motivoinasistenciaestudianteid_fkey"
                    });
                }
                else if (innerMessage.Contains("uq_asistencia_sesion_estudiante"))
                {
                    return BadRequest(new
                    {
                        error = "ASISTENCIA_DUPLICADA",
                        message = "Ya existe un registro de asistencia para este estudiante en esta sesión de clase.",
                        constraint = "uq_asistencia_sesion_estudiante"
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

        // Inactivar asistencia
        [HttpDelete("Delete/{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var asistencia = await _db.asistenciaestudiante.FindAsync(id);
                if (asistencia == null)
                    return NotFound(new
                    {
                        error = "ASISTENCIA_NO_ENCONTRADA",
                        message = $"No existe una asistencia con el id {id}."
                    });

                if (asistencia.estado == "INA")
                {
                    return BadRequest(new
                    {
                        error = "ASISTENCIA_YA_INACTIVA",
                        message = "Esta asistencia ya está inactiva."
                    });
                }

                asistencia.estado = "INA";
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Asistencia inactivada correctamente",
                    asistencia_id = asistencia.asistenciaestudianteid
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

