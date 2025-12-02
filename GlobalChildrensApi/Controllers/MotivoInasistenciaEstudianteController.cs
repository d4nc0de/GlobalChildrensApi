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
    public class MotivoInasistenciaEstudianteController : ControllerBase
    {
        private readonly AppDbContext _db;

        public MotivoInasistenciaEstudianteController(AppDbContext db)
        {
            _db = db;
        }

        // Obtener todos los motivos de inasistencia activos
        [HttpGet("GetAllMotivoInasistencia")]
        public async Task<ActionResult<IEnumerable<MotivoInasistenciaEstudiante>>> GetAllMotivoInasistencia()
        {
            try
            {
                var motivos = await _db.motivoinasistenciaestudiante
                    .Where(m => m.estado == "ACT")
                    .OrderBy(m => m.codigo)
                    .ToListAsync();

                return Ok(motivos);
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

        // Crear un nuevo motivo de inasistencia
        [HttpPost("CreateMotivoInasistencia")]
        public async Task<ActionResult<MotivoInasistenciaEstudiante>> CreateMotivoInasistencia([FromBody] MotivoInasistenciaEstudiante motivo)
        {
            try
            {
                // VALIDACIÓN 1: Código no puede estar vacío
                if (string.IsNullOrWhiteSpace(motivo.codigo))
                {
                    return BadRequest(new
                    {
                        error = "CODIGO_REQUERIDO",
                        message = "El código del motivo de inasistencia es requerido y no puede estar vacío."
                    });
                }

                // VALIDACIÓN 2: Descripción no puede estar vacía
                if (string.IsNullOrWhiteSpace(motivo.descripcion))
                {
                    return BadRequest(new
                    {
                        error = "DESCRIPCION_REQUERIDA",
                        message = "La descripción del motivo de inasistencia es requerida y no puede estar vacía."
                    });
                }

                // VALIDACIÓN 3: Código no puede exceder 30 caracteres
                if (motivo.codigo.Length > 30)
                {
                    return BadRequest(new
                    {
                        error = "CODIGO_MUY_LARGO",
                        message = $"El código no puede exceder 30 caracteres. Longitud actual: {motivo.codigo.Length}."
                    });
                }

                // VALIDACIÓN 4: Descripción no puede exceder 200 caracteres
                if (motivo.descripcion.Length > 200)
                {
                    return BadRequest(new
                    {
                        error = "DESCRIPCION_MUY_LARGA",
                        message = $"La descripción no puede exceder 200 caracteres. Longitud actual: {motivo.descripcion.Length}."
                    });
                }

                // VALIDACIÓN 5: Verificar que no exista ya un motivo con ese código
                var motivoExistente = await _db.motivoinasistenciaestudiante
                    .FirstOrDefaultAsync(m => m.codigo.ToUpper() == motivo.codigo.ToUpper());

                if (motivoExistente != null)
                {
                    return BadRequest(new
                    {
                        error = "CODIGO_DUPLICADO",
                        message = $"Ya existe un motivo de inasistencia con el código '{motivo.codigo}': '{motivoExistente.descripcion}' (ID: {motivoExistente.motivoinasistenciaestudianteid}, Estado: {motivoExistente.estado}).",
                        constraint = "uq_codigo_motivo_inasistencia",
                        motivo_existente_id = motivoExistente.motivoinasistenciaestudianteid,
                        motivo_existente_descripcion = motivoExistente.descripcion,
                        motivo_existente_estado = motivoExistente.estado
                    });
                }

                // Establecer valores por defecto
                if (string.IsNullOrWhiteSpace(motivo.estado))
                    motivo.estado = "ACT";

                motivo.fecha_creacion = DateTime.UtcNow;

                _db.motivoinasistenciaestudiante.Add(motivo);
                await _db.SaveChangesAsync();

                return CreatedAtAction(nameof(GetAllMotivoInasistencia), 
                    new { id = motivo.motivoinasistenciaestudianteid }, 
                    motivo);
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;

                if (innerMessage.Contains("uq_codigo_motivo_inasistencia"))
                {
                    return BadRequest(new
                    {
                        error = "CODIGO_DUPLICADO",
                        message = $"Ya existe un motivo de inasistencia con el código '{motivo.codigo}'.",
                        constraint = "uq_codigo_motivo_inasistencia"
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

        // Actualizar un motivo de inasistencia existente
        [HttpPut("UpdateMotivoInasistencia/{id:long}")]
        public async Task<IActionResult> UpdateMotivoInasistencia(long id, [FromBody] MotivoInasistenciaEstudiante motivo)
        {
            try
            {
                if (id != motivo.motivoinasistenciaestudianteid)
                    return BadRequest(new
                    {
                        error = "ID_NO_COINCIDE",
                        message = $"El id de la URL ({id}) no coincide con el id del cuerpo ({motivo.motivoinasistenciaestudianteid})."
                    });

                var existing = await _db.motivoinasistenciaestudiante.FindAsync(id);
                if (existing == null)
                    return NotFound(new
                    {
                        error = "MOTIVO_NO_ENCONTRADO",
                        message = $"No existe un motivo de inasistencia con el id {id}."
                    });

                // VALIDACIÓN 1: Código no puede estar vacío
                if (string.IsNullOrWhiteSpace(motivo.codigo))
                {
                    return BadRequest(new
                    {
                        error = "CODIGO_REQUERIDO",
                        message = "El código del motivo de inasistencia es requerido y no puede estar vacío."
                    });
                }

                // VALIDACIÓN 2: Descripción no puede estar vacía
                if (string.IsNullOrWhiteSpace(motivo.descripcion))
                {
                    return BadRequest(new
                    {
                        error = "DESCRIPCION_REQUERIDA",
                        message = "La descripción del motivo de inasistencia es requerida y no puede estar vacía."
                    });
                }

                // VALIDACIÓN 3: Código no puede exceder 30 caracteres
                if (motivo.codigo.Length > 30)
                {
                    return BadRequest(new
                    {
                        error = "CODIGO_MUY_LARGO",
                        message = $"El código no puede exceder 30 caracteres. Longitud actual: {motivo.codigo.Length}."
                    });
                }

                // VALIDACIÓN 4: Descripción no puede exceder 200 caracteres
                if (motivo.descripcion.Length > 200)
                {
                    return BadRequest(new
                    {
                        error = "DESCRIPCION_MUY_LARGA",
                        message = $"La descripción no puede exceder 200 caracteres. Longitud actual: {motivo.descripcion.Length}."
                    });
                }

                // VALIDACIÓN 5: Verificar que no exista otro motivo con ese código (excepto el actual)
                var motivoExistente = await _db.motivoinasistenciaestudiante
                    .FirstOrDefaultAsync(m => m.codigo.ToUpper() == motivo.codigo.ToUpper() && m.motivoinasistenciaestudianteid != id);

                if (motivoExistente != null)
                {
                    return BadRequest(new
                    {
                        error = "CODIGO_DUPLICADO",
                        message = $"Ya existe otro motivo de inasistencia con el código '{motivo.codigo}': '{motivoExistente.descripcion}' (ID: {motivoExistente.motivoinasistenciaestudianteid}, Estado: {motivoExistente.estado}).",
                        constraint = "uq_codigo_motivo_inasistencia",
                        motivo_existente_id = motivoExistente.motivoinasistenciaestudianteid,
                        motivo_existente_descripcion = motivoExistente.descripcion,
                        motivo_existente_estado = motivoExistente.estado
                    });
                }

                // Actualizamos campos
                existing.codigo = motivo.codigo;
                existing.descripcion = motivo.descripcion;
                existing.estado = motivo.estado;

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Motivo de inasistencia actualizado correctamente",
                    motivo_id = existing.motivoinasistenciaestudianteid,
                    codigo = existing.codigo,
                    descripcion = existing.descripcion
                });
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;

                if (innerMessage.Contains("uq_codigo_motivo_inasistencia"))
                {
                    return BadRequest(new
                    {
                        error = "CODIGO_DUPLICADO",
                        message = $"Ya existe un motivo de inasistencia con el código '{motivo.codigo}'.",
                        constraint = "uq_codigo_motivo_inasistencia"
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

        // Inactivar un motivo de inasistencia (soft delete)
        [HttpDelete("InactivateMotivoInasistencia/{id:long}")]
        public async Task<IActionResult> InactivateMotivoInasistencia(long id)
        {
            try
            {
                var motivo = await _db.motivoinasistenciaestudiante.FindAsync(id);
                if (motivo == null)
                    return NotFound(new
                    {
                        error = "MOTIVO_NO_ENCONTRADO",
                        message = $"No existe un motivo de inasistencia con el id {id}."
                    });

                if (motivo.estado == "INA")
                {
                    return BadRequest(new
                    {
                        error = "MOTIVO_YA_INACTIVO",
                        message = $"El motivo de inasistencia '{motivo.descripcion}' (código: {motivo.codigo}) ya está inactivo."
                    });
                }

                motivo.estado = "INA";
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Motivo de inasistencia inactivado correctamente",
                    motivo_id = motivo.motivoinasistenciaestudianteid,
                    codigo = motivo.codigo,
                    descripcion = motivo.descripcion
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

