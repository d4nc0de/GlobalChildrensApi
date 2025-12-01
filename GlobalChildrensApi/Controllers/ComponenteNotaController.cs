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
    public class ComponenteNotaController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ComponenteNotaController(AppDbContext db)
        {
            _db = db;
        }

        // Obtener todos los componentes de nota activos
        [HttpGet("GetAllComponenteNota")]
        public async Task<ActionResult<IEnumerable<ComponenteNota>>> GetAllComponenteNota()
        {
            try
            {
                var componentes = await _db.componentenota
                    .Where(c => c.estado == "ACT" && c.activo == true)
                    .OrderBy(c => c.nombre)
                    .ToListAsync();

                return Ok(componentes);
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

        // Obtener componente por ID
        [HttpGet("GetById/{id:long}")]
        public async Task<ActionResult<ComponenteNota>> GetById(long id)
        {
            try
            {
                var componente = await _db.componentenota.FirstOrDefaultAsync(c => c.componentenotaid == id);

                if (componente == null)
                    return NotFound(new
                    {
                        error = "COMPONENTE_NO_ENCONTRADO",
                        message = $"No existe un componente de nota con el id {id}."
                    });

                if (componente.estado != "ACT")
                    return BadRequest(new
                    {
                        error = "COMPONENTE_INACTIVO",
                        message = $"El componente '{componente.nombre}' existe pero no está activo (Estado = {componente.estado})."
                    });

                return Ok(componente);
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

        // Obtener componentes por rango de porcentaje
        [HttpGet("GetByPorcentajeRange")]
        public async Task<ActionResult<IEnumerable<ComponenteNota>>> GetByPorcentajeRange(
            [FromQuery] decimal porcentajeMin,
            [FromQuery] decimal porcentajeMax)
        {
            try
            {
                if (porcentajeMin < 0 || porcentajeMax > 100 || porcentajeMin > porcentajeMax)
                {
                    return BadRequest(new
                    {
                        error = "RANGO_INVALIDO",
                        message = "El rango de porcentaje debe estar entre 0 y 100, y el mínimo no puede ser mayor que el máximo."
                    });
                }

                var componentes = await _db.componentenota
                    .Where(c => c.porcentaje >= porcentajeMin 
                             && c.porcentaje <= porcentajeMax 
                             && c.estado == "ACT" 
                             && c.activo == true)
                    .OrderBy(c => c.porcentaje)
                    .ToListAsync();

                return Ok(componentes);
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
        public async Task<ActionResult<ComponenteNota>> Create([FromBody] ComponenteNota componente)
        {
            try
            {
                // VALIDACIÓN 1: Nombre no puede estar vacío
                if (string.IsNullOrWhiteSpace(componente.nombre))
                {
                    return BadRequest(new
                    {
                        error = "NOMBRE_REQUERIDO",
                        message = "El nombre del componente es requerido y no puede estar vacío."
                    });
                }

                // VALIDACIÓN 2: Porcentaje debe estar entre 0 y 100
                if (componente.porcentaje < 0 || componente.porcentaje > 100)
                {
                    return BadRequest(new
                    {
                        error = "PORCENTAJE_INVALIDO",
                        message = $"El porcentaje debe estar entre 0 y 100. Valor recibido: {componente.porcentaje}",
                        constraint = "chk_porcentaje"
                    });
                }

                // VALIDACIÓN 3: Verificar constraint único (nombre + porcentaje)
                var componenteExistente = await _db.componentenota
                    .FirstOrDefaultAsync(c => c.nombre == componente.nombre 
                                           && c.porcentaje == componente.porcentaje);

                if (componenteExistente != null)
                {
                    return BadRequest(new
                    {
                        error = "COMPONENTE_DUPLICADO",
                        message = $"Ya existe un componente con el nombre '{componente.nombre}' y porcentaje {componente.porcentaje}% (ID: {componenteExistente.componentenotaid}, Estado: {componenteExistente.estado}).",
                        constraint = "uq_componente_nombre_porcentaje",
                        componente_existente_id = componenteExistente.componentenotaid,
                        componente_existente_estado = componenteExistente.estado
                    });
                }

                // Establecer valores por defecto
                if (string.IsNullOrWhiteSpace(componente.estado))
                    componente.estado = "ACT";

                if (!componente.activo)
                    componente.activo = true;

                _db.componentenota.Add(componente);
                await _db.SaveChangesAsync();

                return CreatedAtAction(nameof(GetById), new { id = componente.componentenotaid }, componente);
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;

                if (innerMessage.Contains("uq_componente_nombre_porcentaje"))
                {
                    return BadRequest(new
                    {
                        error = "COMPONENTE_DUPLICADO",
                        message = $"Ya existe un componente con el nombre '{componente.nombre}' y porcentaje {componente.porcentaje}%.",
                        constraint = "uq_componente_nombre_porcentaje"
                    });
                }
                else if (innerMessage.Contains("chk_porcentaje"))
                {
                    return BadRequest(new
                    {
                        error = "PORCENTAJE_INVALIDO",
                        message = "El porcentaje debe estar entre 0 y 100.",
                        constraint = "chk_porcentaje"
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
        public async Task<IActionResult> Update(long id, [FromBody] ComponenteNota componente)
        {
            try
            {
                if (id != componente.componentenotaid)
                    return BadRequest(new
                    {
                        error = "ID_NO_COINCIDE",
                        message = $"El id de la URL ({id}) no coincide con el id del cuerpo ({componente.componentenotaid})."
                    });

                var existing = await _db.componentenota.FindAsync(id);
                if (existing == null)
                    return NotFound(new
                    {
                        error = "COMPONENTE_NO_ENCONTRADO",
                        message = $"No existe un componente de nota con el id {id}."
                    });

                // VALIDACIÓN 1: Nombre no puede estar vacío
                if (string.IsNullOrWhiteSpace(componente.nombre))
                {
                    return BadRequest(new
                    {
                        error = "NOMBRE_REQUERIDO",
                        message = "El nombre del componente es requerido y no puede estar vacío."
                    });
                }

                // VALIDACIÓN 2: Porcentaje debe estar entre 0 y 100
                if (componente.porcentaje < 0 || componente.porcentaje > 100)
                {
                    return BadRequest(new
                    {
                        error = "PORCENTAJE_INVALIDO",
                        message = $"El porcentaje debe estar entre 0 y 100. Valor recibido: {componente.porcentaje}",
                        constraint = "chk_porcentaje"
                    });
                }

                // VALIDACIÓN 3: Verificar constraint único (excepto el componente actual)
                var componenteExistente = await _db.componentenota
                    .FirstOrDefaultAsync(c => c.nombre == componente.nombre 
                                           && c.porcentaje == componente.porcentaje
                                           && c.componentenotaid != id);

                if (componenteExistente != null)
                {
                    return BadRequest(new
                    {
                        error = "COMPONENTE_DUPLICADO",
                        message = $"Ya existe otro componente con el nombre '{componente.nombre}' y porcentaje {componente.porcentaje}% (ID: {componenteExistente.componentenotaid}, Estado: {componenteExistente.estado}).",
                        constraint = "uq_componente_nombre_porcentaje",
                        componente_existente_id = componenteExistente.componentenotaid,
                        componente_existente_estado = componenteExistente.estado
                    });
                }

                // Actualizamos campos
                existing.nombre = componente.nombre;
                existing.porcentaje = componente.porcentaje;
                existing.activo = componente.activo;
                existing.estado = componente.estado;

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Componente de nota actualizado correctamente",
                    componente_id = existing.componentenotaid,
                    nombre = existing.nombre,
                    porcentaje = existing.porcentaje
                });
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;

                if (innerMessage.Contains("uq_componente_nombre_porcentaje"))
                {
                    return BadRequest(new
                    {
                        error = "COMPONENTE_DUPLICADO",
                        message = $"Ya existe un componente con el nombre '{componente.nombre}' y porcentaje {componente.porcentaje}%.",
                        constraint = "uq_componente_nombre_porcentaje"
                    });
                }
                else if (innerMessage.Contains("chk_porcentaje"))
                {
                    return BadRequest(new
                    {
                        error = "PORCENTAJE_INVALIDO",
                        message = "El porcentaje debe estar entre 0 y 100.",
                        constraint = "chk_porcentaje"
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

        // Inactivar componente
        [HttpDelete("Delete/{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var componente = await _db.componentenota.FindAsync(id);
                if (componente == null)
                    return NotFound(new
                    {
                        error = "COMPONENTE_NO_ENCONTRADO",
                        message = $"No existe un componente de nota con el id {id}."
                    });

                if (componente.estado == "INA" && !componente.activo)
                {
                    return BadRequest(new
                    {
                        error = "COMPONENTE_YA_INACTIVO",
                        message = $"El componente '{componente.nombre}' ya está inactivo."
                    });
                }

                componente.estado = "INA";
                componente.activo = false;
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Componente de nota inactivado correctamente",
                    componente_id = componente.componentenotaid,
                    nombre = componente.nombre,
                    porcentaje = componente.porcentaje
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

