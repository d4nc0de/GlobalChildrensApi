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
    public class InstitucionController : ControllerBase
    {
        private readonly AppDbContext _db;

        public InstitucionController(AppDbContext db)
        {
            _db = db;
        }

        // Obtener todas las instituciones activas (SOLICITADO)
        [HttpGet("GetAllInstituciones")]
        public async Task<ActionResult<IEnumerable<Institucion>>> GetAllInstituciones()
        {
            try
            {
                var instituciones = await _db.institucion
                    .Where(i => i.estado == "ACT")
                    .OrderBy(i => i.nombre)
                    .ToListAsync();

                return Ok(new
                {
                    total = instituciones.Count,
                    instituciones = instituciones
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

        // Obtener institución por ID (SOLICITADO)
        [HttpGet("GetInstitucionById/{id:long}")]
        public async Task<ActionResult<Institucion>> GetInstitucionById(long id)
        {
            try
            {
                var institucion = await _db.institucion.FirstOrDefaultAsync(i => i.institucionid == id);

                if (institucion == null)
                    return NotFound(new
                    {
                        error = "INSTITUCION_NO_ENCONTRADA",
                        message = $"No existe una institución con el id {id}."
                    });

                if (institucion.estado != "ACT")
                    return BadRequest(new
                    {
                        error = "INSTITUCION_INACTIVA",
                        message = $"La institución con id {id} existe pero no está activa (Estado = {institucion.estado})."
                    });

                return Ok(institucion);
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

        // Obtener institución por código
        [HttpGet("GetByCodigo/{codigo}")]
        public async Task<ActionResult<Institucion>> GetByCodigo(string codigo)
        {
            try
            {
                var institucion = await _db.institucion
                    .FirstOrDefaultAsync(i => i.codigo == codigo && i.estado == "ACT");

                if (institucion == null)
                {
                    return NotFound(new
                    {
                        error = "INSTITUCION_NO_ENCONTRADA",
                        message = $"No existe una institución con el código '{codigo}'."
                    });
                }

                return Ok(institucion);
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

        // Crear institución (SOLICITADO)
        [HttpPost("CreateInstitucion")]
        public async Task<ActionResult<Institucion>> CreateInstitucion([FromBody] Institucion institucion)
        {
            try
            {
                // VALIDACIÓN 1: Código no vacío
                if (string.IsNullOrWhiteSpace(institucion.codigo))
                {
                    return BadRequest(new
                    {
                        error = "CODIGO_VACIO",
                        message = "El código de la institución no puede estar vacío."
                    });
                }

                // VALIDACIÓN 2: Nombre no vacío
                if (string.IsNullOrWhiteSpace(institucion.nombre))
                {
                    return BadRequest(new
                    {
                        error = "NOMBRE_VACIO",
                        message = "El nombre de la institución no puede estar vacío."
                    });
                }

                // VALIDACIÓN 3: Código único
                var codigoExiste = await _db.institucion
                    .AnyAsync(i => i.codigo == institucion.codigo);

                if (codigoExiste)
                {
                    return BadRequest(new
                    {
                        error = "CODIGO_DUPLICADO",
                        message = $"Ya existe una institución con el código '{institucion.codigo}'.",
                        constraint = "uq_codigo_institucion"
                    });
                }

                // VALIDACIÓN 4: Longitud del código (máx 50 caracteres)
                if (institucion.codigo.Length > 50)
                {
                    return BadRequest(new
                    {
                        error = "CODIGO_MUY_LARGO",
                        message = $"El código no puede tener más de 50 caracteres. Longitud actual: {institucion.codigo.Length}."
                    });
                }

                // VALIDACIÓN 5: Longitud del nombre (máx 100 caracteres)
                if (institucion.nombre.Length > 100)
                {
                    return BadRequest(new
                    {
                        error = "NOMBRE_MUY_LARGO",
                        message = $"El nombre no puede tener más de 100 caracteres. Longitud actual: {institucion.nombre.Length}."
                    });
                }

                // Establecer valores por defecto
                if (string.IsNullOrWhiteSpace(institucion.estado))
                    institucion.estado = "ACT";

                _db.institucion.Add(institucion);
                await _db.SaveChangesAsync();

                return CreatedAtAction(nameof(GetInstitucionById), new { id = institucion.institucionid }, institucion);
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;

                if (innerMessage.Contains("uq_codigo_institucion"))
                {
                    return BadRequest(new
                    {
                        error = "CODIGO_DUPLICADO",
                        message = $"Ya existe una institución con el código '{institucion.codigo}'.",
                        constraint = "uq_codigo_institucion"
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

        // Actualizar institución (SOLICITADO)
        [HttpPut("UpdateInstitucion/{id:long}")]
        public async Task<IActionResult> UpdateInstitucion(long id, [FromBody] Institucion institucion)
        {
            try
            {
                if (id != institucion.institucionid)
                    return BadRequest(new
                    {
                        error = "ID_NO_COINCIDE",
                        message = $"El id de la URL ({id}) no coincide con el id del cuerpo ({institucion.institucionid})."
                    });

                var existing = await _db.institucion.FindAsync(id);
                if (existing == null)
                    return NotFound(new
                    {
                        error = "INSTITUCION_NO_ENCONTRADA",
                        message = $"No existe una institución con el id {id}."
                    });

                // VALIDACIÓN 1: Código no vacío
                if (string.IsNullOrWhiteSpace(institucion.codigo))
                {
                    return BadRequest(new
                    {
                        error = "CODIGO_VACIO",
                        message = "El código de la institución no puede estar vacío."
                    });
                }

                // VALIDACIÓN 2: Nombre no vacío
                if (string.IsNullOrWhiteSpace(institucion.nombre))
                {
                    return BadRequest(new
                    {
                        error = "NOMBRE_VACIO",
                        message = "El nombre de la institución no puede estar vacío."
                    });
                }

                // VALIDACIÓN 3: Código único (si cambió)
                if (existing.codigo != institucion.codigo)
                {
                    var codigoExiste = await _db.institucion
                        .AnyAsync(i => i.codigo == institucion.codigo && i.institucionid != id);

                    if (codigoExiste)
                    {
                        return BadRequest(new
                        {
                            error = "CODIGO_DUPLICADO",
                            message = $"Ya existe otra institución con el código '{institucion.codigo}'.",
                            constraint = "uq_codigo_institucion"
                        });
                    }
                }

                // VALIDACIÓN 4: Longitud del código (máx 50 caracteres)
                if (institucion.codigo.Length > 50)
                {
                    return BadRequest(new
                    {
                        error = "CODIGO_MUY_LARGO",
                        message = $"El código no puede tener más de 50 caracteres. Longitud actual: {institucion.codigo.Length}."
                    });
                }

                // VALIDACIÓN 5: Longitud del nombre (máx 100 caracteres)
                if (institucion.nombre.Length > 100)
                {
                    return BadRequest(new
                    {
                        error = "NOMBRE_MUY_LARGO",
                        message = $"El nombre no puede tener más de 100 caracteres. Longitud actual: {institucion.nombre.Length}."
                    });
                }

                // Actualizamos campos
                existing.codigo = institucion.codigo;
                existing.nombre = institucion.nombre;
                existing.estado = institucion.estado;

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Institución actualizada correctamente",
                    institucion_id = existing.institucionid,
                    codigo = existing.codigo,
                    nombre = existing.nombre,
                    estado = existing.estado
                });
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;

                if (innerMessage.Contains("uq_codigo_institucion"))
                {
                    return BadRequest(new
                    {
                        error = "CODIGO_DUPLICADO",
                        message = $"Ya existe una institución con el código '{institucion.codigo}'.",
                        constraint = "uq_codigo_institucion"
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

        // Inactivar institución (SOLICITADO)
        [HttpDelete("InactivateInstitucion/{id:long}")]
        public async Task<IActionResult> InactivateInstitucion(long id)
        {
            try
            {
                var institucion = await _db.institucion.FindAsync(id);
                if (institucion == null)
                    return NotFound(new
                    {
                        error = "INSTITUCION_NO_ENCONTRADA",
                        message = $"No existe una institución con el id {id}."
                    });

                if (institucion.estado == "INA")
                {
                    return BadRequest(new
                    {
                        error = "INSTITUCION_YA_INACTIVA",
                        message = $"La institución con id {id} ya está inactiva."
                    });
                }

                // VALIDACIÓN: Verificar si la institución tiene sedes asociadas actualmente
                var sedesAsociadas = await _db.sede
                    .Where(s => s.institucionid == id && s.estado == "ACT")
                    .ToListAsync();

                if (sedesAsociadas.Any())
                {
                    var nombresSedes = string.Join(", ", sedesAsociadas.Select(s => $"'{s.nombre}'"));
                    
                    return BadRequest(new
                    {
                        error = "INSTITUCION_TIENE_SEDES",
                        message = $"No se puede inactivar la institución porque tiene {sedesAsociadas.Count} sede(s) activa(s) asociada(s): {nombresSedes}. Debe inactivar las sedes primero.",
                        sedes_asociadas = sedesAsociadas.Select(s => new
                        {
                            sede_id = s.sedeid,
                            nombre = s.nombre,
                            direccion = s.direccion
                        })
                    });
                }

                institucion.estado = "INA";
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Institución inactivada correctamente",
                    institucion_id = institucion.institucionid,
                    codigo = institucion.codigo,
                    nombre = institucion.nombre
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

