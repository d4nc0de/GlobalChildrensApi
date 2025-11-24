using GlobalChildrensApi.Data;
using GlobalChildrensApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GlobalChildrensApi.Controllers
{
    //base
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize] // Usa el JWT de Supabase
    public class SedeController : ControllerBase
    {
        private readonly AppDbContext _db;

        public SedeController(AppDbContext db)
        {
            _db = db;
        }


        [HttpGet("ObtenerSedesActivas")]
        public async Task<ActionResult<IEnumerable<Sede>>> GetAll()
        {
            try
            {
                var sedes = await _db.sede
                .Where(s => s.estado == "ACT")
                .OrderBy(s => s.sedeid)
                .ToListAsync();

                return Ok(sedes);
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

        [HttpGet("ObtenerSedePorId/{id:long}")]
        public async Task<ActionResult<Sede>> GetById(long id)
        {
            try
            {
                var sede = await _db.sede
                .FirstOrDefaultAsync(s => s.sedeid == id);

                if (sede == null)
                    return NotFound($"No existe una sede con el id {id}.");

                if (sede.estado != "ACT")
                    return BadRequest($"La sede con id {id} existe pero no está activa (Estado = {sede.estado}).");

                return Ok(sede);
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

        [HttpPost("CrearSede")]
        public async Task<ActionResult<Sede>> Create([FromBody] Sede sede)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sede.estado))
                    sede.estado = "ACT";


                _db.sede.Add(sede);
                await _db.SaveChangesAsync();

                return CreatedAtAction(nameof(GetById), new { id = sede.sedeid }, sede);
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

        [HttpPut("ActualizarSede/{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] Sede sede)
        {
            try
            {
                if (id != sede.sedeid)
                    return BadRequest("El id de la URL no coincide con el id del cuerpo.");

                var existing = await _db.sede.FindAsync(id);
                if (existing == null)
                    return NotFound($"No existe una sede con el id {id}.");

                // Actualizamos campos simples
                existing.nombre = sede.nombre;
                existing.direccion = sede.direccion;
                existing.es_principal = sede.es_principal;
                existing.estado = sede.estado;
                existing.institucionid = sede.institucionid;

                await _db.SaveChangesAsync();

                return Ok("Sede actualizada correctamente");
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

        //Cambia el estado de la entidad a INA
        [HttpDelete("InactivarSede/{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var sede = await _db.sede.FindAsync(id);
                if (sede == null)
                    return NotFound($"No existe una sede con el id {id}.");

                sede.estado = "INA";
                await _db.SaveChangesAsync();

                return Ok("Sede Inacvitada correctamente");
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
