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
    public class JornadaController : ControllerBase
    {
        private readonly AppDbContext _db;

        public JornadaController(AppDbContext db)
        {
            _db = db;
        }

        // Obtener todas las jornadas activas
        [HttpGet("GetAllJornadas")]
        public async Task<ActionResult<IEnumerable<Jornada>>> GetAllJornadas()
        {
            try
            {
                var jornadas = await _db.jornada
                    .Where(j => j.estado == "ACT")
                    .OrderBy(j => j.codigo)
                    .ToListAsync();

                return Ok(jornadas);
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

        // Obtener jornada por ID
        [HttpGet("GetJornada/{id:long}")]
        public async Task<ActionResult<Jornada>> GetJornada(long id)
        {
            try
            {
                var jornada = await _db.jornada.FirstOrDefaultAsync(j => j.jornadaid == id);

                if (jornada == null)
                    return NotFound(new
                    {
                        error = "JORNADA_NO_ENCONTRADA",
                        message = $"No existe una jornada con el id {id}."
                    });

                if (jornada.estado != "ACT")
                    return BadRequest(new
                    {
                        error = "JORNADA_INACTIVA",
                        message = $"La jornada '{jornada.descripcion}' existe pero no está activa (Estado = {jornada.estado})."
                    });

                return Ok(jornada);
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

        // Crear una nueva jornada
        [HttpPost("CreateJornada")]
        public async Task<ActionResult<Jornada>> CreateJornada([FromBody] Jornada jornada)
        {
            try
            {
                // VALIDACIÓN 1: Código no puede estar vacío
                if (string.IsNullOrWhiteSpace(jornada.codigo))
                {
                    return BadRequest(new
                    {
                        error = "CODIGO_REQUERIDO",
                        message = "El código de la jornada es requerido y no puede estar vacío."
                    });
                }

                // VALIDACIÓN 2: Descripción no puede estar vacía
                if (string.IsNullOrWhiteSpace(jornada.descripcion))
                {
                    return BadRequest(new
                    {
                        error = "DESCRIPCION_REQUERIDA",
                        message = "La descripción de la jornada es requerida y no puede estar vacía."
                    });
                }

                // VALIDACIÓN 3: Código no puede exceder 20 caracteres
                if (jornada.codigo.Length > 20)
                {
                    return BadRequest(new
                    {
                        error = "CODIGO_MUY_LARGO",
                        message = $"El código no puede exceder 20 caracteres. Longitud actual: {jornada.codigo.Length}."
                    });
                }

                // VALIDACIÓN 4: Descripción no puede exceder 100 caracteres
                if (jornada.descripcion.Length > 100)
                {
                    return BadRequest(new
                    {
                        error = "DESCRIPCION_MUY_LARGA",
                        message = $"La descripción no puede exceder 100 caracteres. Longitud actual: {jornada.descripcion.Length}."
                    });
                }

                // VALIDACIÓN 5: Verificar que no exista ya una jornada con ese código
                var jornadaExistente = await _db.jornada
                    .FirstOrDefaultAsync(j => j.codigo.ToUpper() == jornada.codigo.ToUpper());

                if (jornadaExistente != null)
                {
                    return BadRequest(new
                    {
                        error = "CODIGO_DUPLICADO",
                        message = $"Ya existe una jornada con el código '{jornada.codigo}': '{jornadaExistente.descripcion}' (ID: {jornadaExistente.jornadaid}, Estado: {jornadaExistente.estado}).",
                        constraint = "uq_codigo_jornada",
                        jornada_existente_id = jornadaExistente.jornadaid,
                        jornada_existente_descripcion = jornadaExistente.descripcion,
                        jornada_existente_estado = jornadaExistente.estado
                    });
                }

                // Establecer valores por defecto
                if (string.IsNullOrWhiteSpace(jornada.estado))
                    jornada.estado = "ACT";

                jornada.fecha_creacion = DateTime.UtcNow;

                _db.jornada.Add(jornada);
                await _db.SaveChangesAsync();

                return CreatedAtAction(nameof(GetJornada), new { id = jornada.jornadaid }, jornada);
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;

                if (innerMessage.Contains("uq_codigo_jornada"))
                {
                    return BadRequest(new
                    {
                        error = "CODIGO_DUPLICADO",
                        message = $"Ya existe una jornada con el código '{jornada.codigo}'.",
                        constraint = "uq_codigo_jornada"
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

        // Actualizar una jornada existente
        [HttpPut("UpdateJornada/{id:long}")]
        public async Task<IActionResult> UpdateJornada(long id, [FromBody] Jornada jornada)
        {
            try
            {
                if (id != jornada.jornadaid)
                    return BadRequest(new
                    {
                        error = "ID_NO_COINCIDE",
                        message = $"El id de la URL ({id}) no coincide con el id del cuerpo ({jornada.jornadaid})."
                    });

                var existing = await _db.jornada.FindAsync(id);
                if (existing == null)
                    return NotFound(new
                    {
                        error = "JORNADA_NO_ENCONTRADA",
                        message = $"No existe una jornada con el id {id}."
                    });

                // VALIDACIÓN 1: Código no puede estar vacío
                if (string.IsNullOrWhiteSpace(jornada.codigo))
                {
                    return BadRequest(new
                    {
                        error = "CODIGO_REQUERIDO",
                        message = "El código de la jornada es requerido y no puede estar vacío."
                    });
                }

                // VALIDACIÓN 2: Descripción no puede estar vacía
                if (string.IsNullOrWhiteSpace(jornada.descripcion))
                {
                    return BadRequest(new
                    {
                        error = "DESCRIPCION_REQUERIDA",
                        message = "La descripción de la jornada es requerida y no puede estar vacía."
                    });
                }

                // VALIDACIÓN 3: Código no puede exceder 20 caracteres
                if (jornada.codigo.Length > 20)
                {
                    return BadRequest(new
                    {
                        error = "CODIGO_MUY_LARGO",
                        message = $"El código no puede exceder 20 caracteres. Longitud actual: {jornada.codigo.Length}."
                    });
                }

                // VALIDACIÓN 4: Descripción no puede exceder 100 caracteres
                if (jornada.descripcion.Length > 100)
                {
                    return BadRequest(new
                    {
                        error = "DESCRIPCION_MUY_LARGA",
                        message = $"La descripción no puede exceder 100 caracteres. Longitud actual: {jornada.descripcion.Length}."
                    });
                }

                // VALIDACIÓN 5: Verificar que no exista otra jornada con ese código (excepto la actual)
                var jornadaExistente = await _db.jornada
                    .FirstOrDefaultAsync(j => j.codigo.ToUpper() == jornada.codigo.ToUpper() && j.jornadaid != id);

                if (jornadaExistente != null)
                {
                    return BadRequest(new
                    {
                        error = "CODIGO_DUPLICADO",
                        message = $"Ya existe otra jornada con el código '{jornada.codigo}': '{jornadaExistente.descripcion}' (ID: {jornadaExistente.jornadaid}, Estado: {jornadaExistente.estado}).",
                        constraint = "uq_codigo_jornada",
                        jornada_existente_id = jornadaExistente.jornadaid,
                        jornada_existente_descripcion = jornadaExistente.descripcion,
                        jornada_existente_estado = jornadaExistente.estado
                    });
                }

                // Actualizamos campos
                existing.codigo = jornada.codigo;
                existing.descripcion = jornada.descripcion;
                existing.estado = jornada.estado;

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Jornada actualizada correctamente",
                    jornada_id = existing.jornadaid,
                    codigo = existing.codigo,
                    descripcion = existing.descripcion
                });
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;

                if (innerMessage.Contains("uq_codigo_jornada"))
                {
                    return BadRequest(new
                    {
                        error = "CODIGO_DUPLICADO",
                        message = $"Ya existe una jornada con el código '{jornada.codigo}'.",
                        constraint = "uq_codigo_jornada"
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

        // Inactivar una jornada (soft delete)
        [HttpDelete("InactivateJornada/{id:long}")]
        public async Task<IActionResult> InactivateJornada(long id)
        {
            try
            {
                var jornada = await _db.jornada.FindAsync(id);
                if (jornada == null)
                    return NotFound(new
                    {
                        error = "JORNADA_NO_ENCONTRADA",
                        message = $"No existe una jornada con el id {id}."
                    });

                if (jornada.estado == "INA")
                {
                    return BadRequest(new
                    {
                        error = "JORNADA_YA_INACTIVA",
                        message = $"La jornada '{jornada.descripcion}' (código: {jornada.codigo}) ya está inactiva."
                    });
                }

                jornada.estado = "INA";
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Jornada inactivada correctamente",
                    jornada_id = jornada.jornadaid,
                    codigo = jornada.codigo,
                    descripcion = jornada.descripcion
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

