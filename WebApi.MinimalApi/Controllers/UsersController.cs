using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
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

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        
        var createdUserEntity = userRepository.Insert(mapper.Map<UserEntity>(userCreate));
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUserEntity.Id },
            createdUserEntity.Id);
    }
    
    [HttpPut("{userId}")]
    public IActionResult UpdateUser(Guid userId, [FromBody] UserUpdateDto? userUpdate)
    {
        if (userUpdate is null | userId.Equals(Guid.Empty))
            return BadRequest();

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var userEntity = mapper.Map(
            userUpdate,
            new UserEntity(userId));
        
        userRepository.UpdateOrInsert(userEntity, out var isInserted);

        if (isInserted)
        {
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId },
                userId);
        }
        return NoContent();
    }
    
    [HttpPatch("{userId}")]
    public IActionResult PartiallyUpdateUser(Guid userId, [FromBody] JsonPatchDocument<UserUpdateDto>? patchDoc)
    {
        if (userId.Equals(Guid.Empty))
            return NotFound();
        
        if (patchDoc is null)
            return BadRequest();
        
        var userEntity = userRepository.FindById(userId);
        
        if (userEntity is null)
            return NotFound();
        
        var updateDto = mapper.Map<UserUpdateDto>(userEntity);
        
        patchDoc.ApplyTo(updateDto, ModelState);
        
        if (!TryValidateModel(updateDto))
            return UnprocessableEntity(ModelState);
        
        mapper.Map(updateDto, userEntity);

        userRepository.Update(userEntity);

        return NoContent();
    }
    
    [HttpDelete("{userId}")]
    public IActionResult DeleteUser(Guid userId)
    {
        var userEntity = userRepository.FindById(userId);
        
        if (userEntity is null)
            return NotFound();

        userRepository.Delete(userId);

        return NoContent();
    }
}