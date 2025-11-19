using Microsoft.AspNetCore.Mvc;
using UserDataAPI.Models;
using UserDataAPI.Services;

namespace UserDataAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly UserService _userService;

    public UserController(UserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> Get()
    {
        var users = await _userService.GetAsync();
        return Ok(users);
    }

    [HttpGet("{id:length(24)}")]
    public async Task<ActionResult<User>> Get(string id)
    {
        var user = await _userService.GetAsync(id);
        if (user is null)
            return NotFound();
        return Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<User>> Post([FromBody] User newUser)
    {
        await _userService.CreateAsync(newUser);
        return CreatedAtAction(nameof(Get), new { id = newUser.Id }, newUser);
    }

    [HttpPut("{id:length(24)}")]
    public async Task<IActionResult> Put(string id, [FromBody] User updatedUser)
    {
        var existing = await _userService.GetAsync(id);
        if (existing is null)
            return NotFound();

        var updated = await _userService.UpdateAsync(id, updatedUser);
        if (!updated)
            return StatusCode(500, "Update failed.");

        return NoContent();
    }

    [HttpDelete("{id:length(24)}")]
    public async Task<IActionResult> Delete(string id)
    {
        var existing = await _userService.GetAsync(id);
        if (existing is null)
            return NotFound();

        var deleted = await _userService.RemoveAsync(id);
        if (!deleted)
            return StatusCode(500, "Delete failed.");

        return NoContent();
    }
}
