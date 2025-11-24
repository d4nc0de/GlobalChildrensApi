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
    public class AulaController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AulaController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("ObtenerAulasActivas")]
        public async Task<ActionResult<IEnumerable<Aula>>> GetAll()
        {
            try
            {
                var aulas = await _db.aula
                .Where(a => a.estado == "ACT" && a.activo == true)
                .OrderBy(a => a.aulaid)
                .ToListAsync();

                return Ok(aulas);
            }
            catch (Exception ex)
            {
                return StatusCode(503, new
                {
                    message = "No fue posible comunicarse con la base de datos. Intenta de nuevo.",
                    error = ex.Message
                });
            }
        }

        [HttpGet("ObtenerAulaPorId/{id:long}")]
        public async Task<ActionResult<Aula>> GetById(long id)
        {
            try
            {
                var aula = await _db.aula
                .FirstOrDefaultAsync(a => a.aulaid == id);

                if (aula == null)
                    return NotFound($"No existe un aula con el id {id}.");

                if (aula.estado != "ACT")
                    return BadRequest($"El aula con id {id} existe pero no está activa (Estado = {aula.estado}).");

                return Ok(aula);
            }
            catch (Exception ex)
            {
                return StatusCode(503, new
                {
                    message = "No fue posible comunicarse con la base de datos. Intenta de nuevo.",
                    error = ex.Message
                });
            }
        }

        [HttpPost("CrearAula")]
        public async Task<ActionResult<Aula>> Create([FromBody] Aula aula)
        {
            try
            {
                // Validar que el grado sea válido (4, 5, 9, 10)
                if (aula.grado != 4 && aula.grado != 5 && aula.grado != 9 && aula.grado != 10)
                {
                    return BadRequest("El grado debe ser 4, 5, 9 o 10.");
                }

                // Validar que el cupo máximo sea mayor a 0
                if (aula.cupo_maximo <= 0)
                {
                    return BadRequest("El cupo máximo debe ser mayor a 0.");
                }

                // Establecer valores por defecto si no se proporcionan
                if (string.IsNullOrWhiteSpace(aula.estado))
                    aula.estado = "ACT";

                if (!aula.activo)
                    aula.activo = true;

                _db.aula.Add(aula);
                await _db.SaveChangesAsync();

                return CreatedAtAction(nameof(GetById), new { id = aula.aulaid }, aula);
            }
            catch (Exception ex)
            {
                return StatusCode(503, new
                {
                    message = "No fue posible comunicarse con la base de datos. Intenta de nuevo.",
                    error = ex.Message
                });
            }
        }

        [HttpPut("ActualizarAula/{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] Aula aula)
        {
            try
            {
                if (id != aula.aulaid)
                    return BadRequest("El id de la URL no coincide con el id del cuerpo.");

                // Validar que el grado sea válido (4, 5, 9, 10)
                if (aula.grado != 4 && aula.grado != 5 && aula.grado != 9 && aula.grado != 10)
                {
                    return BadRequest("El grado debe ser 4, 5, 9 o 10.");
                }

                // Validar que el cupo máximo sea mayor a 0
                if (aula.cupo_maximo <= 0)
                {
                    return BadRequest("El cupo máximo debe ser mayor a 0.");
                }

                var existing = await _db.aula.FindAsync(id);
                if (existing == null)
                    return NotFound($"No existe un aula con el id {id}.");

                // Actualizamos campos
                existing.grado = aula.grado;
                existing.nombre = aula.nombre;
                existing.cupo_maximo = aula.cupo_maximo;
                existing.activo = aula.activo;
                existing.estado = aula.estado;
                existing.sedeid = aula.sedeid;
                existing.programaid = aula.programaid;
                existing.jornadaid = aula.jornadaid;
                existing.tutorid = aula.tutorid;

                await _db.SaveChangesAsync();

                return Ok("Aula actualizada correctamente");
            }
            catch (Exception ex)
            {
                return StatusCode(503, new
                {
                    message = "No fue posible comunicarse con la base de datos. Intenta de nuevo.",
                    error = ex.Message
                });
            }
        }

        //Cambia el estado de la entidad a INA y activo a false
        [HttpDelete("InactivarAula/{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var aula = await _db.aula.FindAsync(id);
                if (aula == null)
                    return NotFound($"No existe un aula con el id {id}.");

                aula.estado = "INA";
                aula.activo = false;
                await _db.SaveChangesAsync();

                return Ok("Aula inactivada correctamente");
            }
            catch (Exception ex)
            {
                return StatusCode(503, new
                {
                    message = "No fue posible comunicarse con la base de datos. Intenta de nuevo.",
                    error = ex.Message
                });
            }
        }
    }
}

