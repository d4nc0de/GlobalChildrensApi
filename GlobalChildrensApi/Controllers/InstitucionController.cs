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

        [HttpGet("ObtenerInstituciones")]
        public async Task<ActionResult<IEnumerable<Institucion>>> GetAll()
        {
            try
            {
                var instituciones = await _db.institucion
                .Where(i => i.estado == "ACT")
                .OrderBy(i => i.institucionid)
                .ToListAsync();

                return Ok(instituciones);
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

        [HttpGet("ObtenerInstitucionesById/{id:long}")]
        public async Task<ActionResult<Institucion>> GetInstitucionById(long id)
        {
            try
            {
                var institucion = await _db.institucion
                .FirstOrDefaultAsync(i => i.institucionid == id);

                if (institucion == null || institucion.estado != "ACT")
                    return NotFound($"No existe una institucion activa con el id {id}.");

                return Ok(institucion);
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

        [HttpPost("CrearInstitucion")]
        public async Task<ActionResult<Institucion>> CreateInstitucion([FromBody] Institucion institucion)
        {
            try
            {
                _db.institucion.Add(institucion);
                await _db.SaveChangesAsync();

                return CreatedAtAction(nameof(GetInstitucionById), new { id = institucion.institucionid }, institucion);
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

        [HttpPut("ActualizarInstitucion/{id:long}")]
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
                        message = $"No existe una institucion con el id {id}."
                    });


                // Actualizamos campos
                existing.codigo = institucion.codigo;
                existing.nombre = institucion.nombre;
                existing.estado = institucion.estado;

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Institucion actualizada correctamente"
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

        //Cambia el estado de la entidad a INA y activo a false
        [HttpDelete("InactivarAula/{id:long}")]
        public async Task<IActionResult> InactivateInstitucion(long id)
        {
            try
            {
                var institucion = await _db.institucion.FindAsync(id);
                if (institucion == null || institucion.estado != "ACT")
                    return NotFound(new
                    {
                        message = $"No existe un aula activa con el id {id}."
                    });

                institucion.estado = "INA";
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Aula inactivada correctamente"
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

