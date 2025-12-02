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
    public class MotivoNoClaseController : ControllerBase
    {
        private readonly AppDbContext _db;

        public MotivoNoClaseController(AppDbContext db)
        {
            _db = db;
        }

        // Obtener todos los motivos de no clase activos
        [HttpGet("GetAllMotivoNoClase")]
        public async Task<ActionResult<IEnumerable<MotivoNoClase>>> GetAllMotivoNoClase()
        {
            try
            {
                var motivos = await _db.motivonoclase
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

        // Crear un nuevo motivo de no clase
        [HttpPost("CreateMotivoNoClase")]
        public async Task<ActionResult<MotivoNoClase>> CreateMotivoNoClase([FromBody] MotivoNoClase motivo)
        {
            try
            {
                // VALIDACIÓN 1: Código no puede estar vacío
                if (string.IsNullOrWhiteSpace(motivo.codigo))
                {
                    return BadRequest(new
                    {
                        error = "CODIGO_REQUERIDO",
                        message = "El código del motivo de no clase es requerido y no puede estar vacío."
                    });
                }

                // VALIDACIÓN 2: Descripción no puede estar vacía
                if (string.IsNullOrWhiteSpace(motivo.descripcion))
                {
                    return BadRequest(new
                    {
                        error = "DESCRIPCION_REQUERIDA",
                        message = "La descripción del motivo de no clase es requerida y no puede estar vacía."
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
                var motivoExistente = await _db.motivonoclase
                    .FirstOrDefaultAsync(m => m.codigo.ToUpper() == motivo.codigo.ToUpper());

                if (motivoExistente != null)
                {
                    return BadRequest(new
                    {
                        error = "CODIGO_DUPLICADO",
                        message = $"Ya existe un motivo de no clase con el código '{motivo.codigo}': '{motivoExistente.descripcion}' (ID: {motivoExistente.motivonoclaseid}, Estado: {motivoExistente.estado}).",
                        constraint = "uq_codigo_motivo_no_clase",
                        motivo_existente_id = motivoExistente.motivonoclaseid,
                        motivo_existente_descripcion = motivoExistente.descripcion,
                        motivo_existente_estado = motivoExistente.estado
                    });
                }

                // Establecer valores por defecto
                if (string.IsNullOrWhiteSpace(motivo.estado))
                    motivo.estado = "ACT";

                motivo.fecha_creacion = DateTime.UtcNow;

                _db.motivonoclase.Add(motivo);
                await _db.SaveChangesAsync();

                return CreatedAtAction(nameof(GetAllMotivoNoClase), 
                    new { id = motivo.motivonoclaseid }, 
                    motivo);
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;

                if (innerMessage.Contains("uq_codigo_motivo_no_clase"))
                {
                    return BadRequest(new
                    {
                        error = "CODIGO_DUPLICADO",
                        message = $"Ya existe un motivo de no clase con el código '{motivo.codigo}'.",
                        constraint = "uq_codigo_motivo_no_clase"
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

        // Actualizar un motivo de no clase existente
        [HttpPut("UpdateMotivoNoClase/{id:long}")]
        public async Task<IActionResult> UpdateMotivoNoClase(long id, [FromBody] MotivoNoClase motivo)
        {
            try
            {
                if (id != motivo.motivonoclaseid)
                    return BadRequest(new
                    {
                        error = "ID_NO_COINCIDE",
                        message = $"El id de la URL ({id}) no coincide con el id del cuerpo ({motivo.motivonoclaseid})."
                    });

                var existing = await _db.motivonoclase.FindAsync(id);
                if (existing == null)
                    return NotFound(new
                    {
                        error = "MOTIVO_NO_ENCONTRADO",
                        message = $"No existe un motivo de no clase con el id {id}."
                    });

                // VALIDACIÓN 1: Código no puede estar vacío
                if (string.IsNullOrWhiteSpace(motivo.codigo))
                {
                    return BadRequest(new
                    {
                        error = "CODIGO_REQUERIDO",
                        message = "El código del motivo de no clase es requerido y no puede estar vacío."
                    });
                }

                // VALIDACIÓN 2: Descripción no puede estar vacía
                if (string.IsNullOrWhiteSpace(motivo.descripcion))
                {
                    return BadRequest(new
                    {
                        error = "DESCRIPCION_REQUERIDA",
                        message = "La descripción del motivo de no clase es requerida y no puede estar vacía."
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
                var motivoExistente = await _db.motivonoclase
                    .FirstOrDefaultAsync(m => m.codigo.ToUpper() == motivo.codigo.ToUpper() && m.motivonoclaseid != id);

                if (motivoExistente != null)
                {
                    return BadRequest(new
                    {
                        error = "CODIGO_DUPLICADO",
                        message = $"Ya existe otro motivo de no clase con el código '{motivo.codigo}': '{motivoExistente.descripcion}' (ID: {motivoExistente.motivonoclaseid}, Estado: {motivoExistente.estado}).",
                        constraint = "uq_codigo_motivo_no_clase",
                        motivo_existente_id = motivoExistente.motivonoclaseid,
                        motivo_existente_descripcion = motivoExistente.descripcion,
                        motivo_existente_estado = motivoExistente.estado
                    });
                }

                // Actualizamos campos
                existing.codigo = motivo.codigo;
                existing.descripcion = motivo.descripcion;
                existing.permite_reposicion = motivo.permite_reposicion;
                existing.estado = motivo.estado;

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Motivo de no clase actualizado correctamente",
                    motivo_id = existing.motivonoclaseid,
                    codigo = existing.codigo,
                    descripcion = existing.descripcion,
                    permite_reposicion = existing.permite_reposicion
                });
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;

                if (innerMessage.Contains("uq_codigo_motivo_no_clase"))
                {
                    return BadRequest(new
                    {
                        error = "CODIGO_DUPLICADO",
                        message = $"Ya existe un motivo de no clase con el código '{motivo.codigo}'.",
                        constraint = "uq_codigo_motivo_no_clase"
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

        // Inactivar un motivo de no clase (soft delete)
        [HttpDelete("InactivateMotivoNoClase/{id:long}")]
        public async Task<IActionResult> InactivateMotivoNoClase(long id)
        {
            try
            {
                var motivo = await _db.motivonoclase.FindAsync(id);
                if (motivo == null)
                    return NotFound(new
                    {
                        error = "MOTIVO_NO_ENCONTRADO",
                        message = $"No existe un motivo de no clase con el id {id}."
                    });

                if (motivo.estado == "INA")
                {
                    return BadRequest(new
                    {
                        error = "MOTIVO_YA_INACTIVO",
                        message = $"El motivo de no clase '{motivo.descripcion}' (código: {motivo.codigo}) ya está inactivo."
                    });
                }

                motivo.estado = "INA";
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Motivo de no clase inactivado correctamente",
                    motivo_id = motivo.motivonoclaseid,
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

