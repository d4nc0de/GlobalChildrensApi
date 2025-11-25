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
    public class PersonaController : ControllerBase
    {
        private readonly AppDbContext _db;

        public PersonaController(AppDbContext db)
        {
            _db = db;
        }


        [HttpGet("ObtenerPersonasActivas")]
        public async Task<ActionResult<IEnumerable<Persona>>> GetAll()
        {
            try
            {
                var personas = await _db.persona
                .Where(s => s.estado == "ACT")
                .OrderBy(s => s.personaid)
                .ToListAsync();

                return Ok(personas);
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

        [HttpGet("ObtenerPersonaPorId/{id:long}")]
        public async Task<ActionResult<Persona>> GetById(long id)
        {
            try
            {
                var persona = await _db.persona
                .FirstOrDefaultAsync(s => s.personaid == id);

                if (persona == null)
                    return NotFound($"No existe una persona con el id {id}.");

                if (persona.estado != "ACT")
                    return BadRequest($"La persona con id: {id} existe pero no está activa (Estado = {persona.estado}).");

                return Ok(persona);
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

        [HttpPost("CrearPersona")]
        public async Task<ActionResult<Sede>> Create([FromBody] Persona persona)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(persona.estado))
                    persona.estado = "ACT";


                _db.persona.Add(persona);
                await _db.SaveChangesAsync();

                return CreatedAtAction(nameof(GetById), new { id = persona.personaid }, persona);
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

        [HttpPut("ActualizarPersona/{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] Persona persona)
        {
            try
            {
                if (id != persona.personaid)
                    return BadRequest("El id de la URL no coincide con el id del cuerpo.");

                var existing = await _db.persona.FindAsync(id);
                if (existing == null)
                    return NotFound($"No existe una persona con el id {id}.");

                // Actualizamos campos simples
                existing.numero_documento = persona.numero_documento;
                existing.nombres = persona.nombres;
                existing.apellidos = persona.apellidos;
                existing.correo = persona.correo;
                existing.activo = persona.activo;
                existing.estado = persona.estado;
                existing.tipodocumentoid = persona.tipodocumentoid;
                existing.rolid = persona.rolid;
                existing.usuarioId = persona.usuarioId;

                await _db.SaveChangesAsync();

                return Ok("Persona actualizada correctamente");
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
        [HttpDelete("InactivarPersona/{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var persona = await _db.persona.FindAsync(id);
                if (persona == null)
                    return NotFound($"No existe una persona con el id {id}.");

                persona.estado = "INA";
                await _db.SaveChangesAsync();

                return Ok("Persona Inactivada correctamente");
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
