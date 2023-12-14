using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

[Route("api/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
    private static List<User> _users = new List<User>
    {
        new User { Id = 1, Name = "John Doe", Age = 25 },
        new User { Id = 2, Name = "Jane Doe", Age = 30 }
    };

    [HttpGet]
    public ActionResult<IEnumerable<User>> Get()
    {
        return Ok(_users);
    }

    [HttpGet("{id}")]
    public ActionResult<User> Get(int id)
    {
        var user = _users.Find(u => u.Id == id);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    [HttpPost]
    public ActionResult<User> Post([FromBody] User newUser)
    {
        newUser.Id = _users.Count + 1;
        _users.Add(newUser);

        return CreatedAtAction(nameof(Get), new { id = newUser.Id }, newUser);
    }

    [HttpPut("{id}")]
    public ActionResult Put(int id, [FromBody] User updatedUser)
    {
        var existingUser = _users.Find(u => u.Id == id);

        if (existingUser == null)
        {
            return NotFound();
        }

        existingUser.Name = updatedUser.Name;
        existingUser.Age = updatedUser.Age;

        return NoContent();
    }

    [HttpDelete("{id}")]
    public ActionResult Delete(int id)
    {
        var user = _users.Find(u => u.Id == id);

        if (user == null)
        {
            return NotFound();
        }

        _users.Remove(user);

        return NoContent();
    }
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
}
