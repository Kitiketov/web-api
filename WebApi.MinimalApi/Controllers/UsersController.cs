using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
    private readonly IUserRepository userRepository;
    private readonly IMapper mapper;

    public UsersController(IUserRepository userRepository, IMapper mapper)
    {
        this.userRepository = userRepository;
        this.mapper = mapper;
    }

    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);

        if (user is null)
            return NotFound();

        return Ok(mapper.Map<UserDto>(user));
    }

    [HttpPost]
    public IActionResult CreateUser([FromBody] UserCreateDto? userCreate)
    {
        if (userCreate is null)
            return BadRequest();
        
        if (!string.IsNullOrEmpty(userCreate.Login))
        {
            if (userCreate.Login.Any(symbol => !char.IsLetterOrDigit(symbol)))
                ModelState.AddModelError(
                    "Login",
                    "Логин должен состоять только из букв и цифр.");
        }

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }
        var createdUserEntity = userRepository.Insert(mapper.Map<UserEntity>(userCreate));
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUserEntity.Id },
            createdUserEntity.Id);
    }
}