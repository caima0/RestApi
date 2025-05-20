using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Account;
using api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static api.Interfaces.ITokenSrvice;

namespace api.Controllers
{
    [Route("api/account")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly ITokenService _tokenService;
        private readonly SignInManager<User> _signInManager;
        public AccountController(UserManager<User> userManager, ITokenService tokenService, SignInManager<User> signInManager)
        {
            _userManager=userManager;
            _tokenService=tokenService;
            _signInManager=signInManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetAccounts()
        {
            try
            {
                var users = await _userManager.Users.ToListAsync();
                var userDtos = users.Select(u => new NewUserDto
                {
                    UserName = u.UserName,
                    Email = u.Email,
                    Token = _tokenService.CreateToken(u)
                }).ToList();

                return Ok(userDtos);
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpGet("{username}")]
        public async Task<IActionResult> GetAccount(string username)
        {
            try
            {
                var user = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName == username.ToLower());
                if (user == null) return NotFound("User not found");

                return Ok(new NewUserDto
                {
                    UserName = user.UserName,
                    Email = user.Email,
                    Token = _tokenService.CreateToken(user)
                });
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpPut("{username}")]
        public async Task<IActionResult> UpdateAccount(string username, [FromBody] UpdateAccountDto updateDto)
        {
            try
            {
                var user = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName == username.ToLower());
                if (user == null) return NotFound("User not found");

                if (!string.IsNullOrEmpty(updateDto.Email))
                    user.Email = updateDto.Email;

                if (!string.IsNullOrEmpty(updateDto.NewPassword))
                {
                    var result = await _userManager.RemovePasswordAsync(user);
                    if (!result.Succeeded)
                        return BadRequest(result.Errors);

                    result = await _userManager.AddPasswordAsync(user, updateDto.NewPassword);
                    if (!result.Succeeded)
                        return BadRequest(result.Errors);
                }

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                    return BadRequest(updateResult.Errors);

                return Ok(new NewUserDto
                {
                    UserName = user.UserName,
                    Email = user.Email,
                    Token = _tokenService.CreateToken(user)
                });
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpDelete("{username}")]
        public async Task<IActionResult> DeleteAccount(string username)
        {
            try
            {
                var user = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName == username.ToLower());
                if (user == null) return NotFound("User not found");

                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                    return BadRequest(result.Errors);

                return Ok("Account deleted successfully");
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName == loginDto.Username.ToLower());

            if (user == null) return Unauthorized("Invalid username");

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);

            if (!result.Succeeded) return Unauthorized("Username not found and/or password incorrect");

            return Ok(
                new NewUserDto
                {
                    UserName = user.UserName,
                    Email = user.Email,
                    Token = _tokenService.CreateToken(user)
                }
            );
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