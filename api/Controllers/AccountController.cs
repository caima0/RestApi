using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Account;
using api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using static api.Interfaces.ITokenSrvice;

namespace api.Controllers
{
    [Route("api/account")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly ITokenService _tokenService;
        public AccountController(UserManager<User> userManager, ITokenService tokenService)
        {
            _userManager=userManager;
            _tokenService=tokenService;
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
             try{
                   if(!ModelState.IsValid)
                    return BadRequest(ModelState);
                   var appUser= new User
                   {
                     UserName = registerDto.Username,
                     Email = registerDto.Email
                   };

                    var createUser= await _userManager.CreateAsync(appUser, registerDto.Password ?? string.Empty);

                    if(createUser.Succeeded)
                    {
                        var roleResult = await _userManager.AddToRoleAsync(appUser, "User");
                        if(roleResult.Succeeded)
                        {
                            return Ok(new NewUserDto
                            {
                                UserName = appUser.UserName,
                                Email = appUser.Email,
                                Token = _tokenService.CreateToken(appUser)
                            });
                        }
                        else
                        {
                            return StatusCode(500, roleResult.Errors);
                        }
                    }
                    else 
                    {
                        return StatusCode(500, createUser.Errors);
                    }
             } catch(Exception e)
             {
                return StatusCode(500, e.Message);
             }
        }
        
    }

}