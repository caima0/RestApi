using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
   [Route("api/UserDTo")]
   [ApiController]

   public class AuthController: ControllerBase
   {
    private readonly IUserMockInterface _userUserMockService;
    public AuthController(IUserMockInterface userMockInterface)
    {
        _userUserMockService=userMockInterface;
    }

    [HttpPost("register")]
     public IActionResult Register([FromBody] UserDTo user)
    {
        if(_userUserMockService.Register(user))
            return Ok(new {messahe="User Registered"});

        return BadRequest(new {messahe="User already exists"});
        
    }

    [HttpPost("login")]
     public IActionResult Login([FromBody] UserDTo user)
     {
            var token = _userUserMockService.Authenticate(user);
            if(token == null)
                return Unauthorized(new {message ="Invalid username or password."});
            return Ok(new {token});
     }

     [HttpGet("secure")]
     [Authorize]
     public IActionResult Secure()
     {
           return Unauthorized(new {message ="This is a secure endpoint."});
     }

   }
}