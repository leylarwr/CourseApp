using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.Datos;
using API.Entities;
using Microsoft.AspNetCore.Mvc;
using API.Interfaces;
using API.DTOs;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        private readonly DataContext context;
        private readonly ITokenService tokenService;

        public AccountController(DataContext context, ITokenService tokenService)
        {
            this.context = context;
            this.tokenService = tokenService;
        }

        [HttpPost("register")]

        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
        {
            if (await UserExists (registerDto.UserName)) return BadRequest("Username is taken");

            using var hmac = new HMACSHA512();
            var user = new AppUser
            {

             UserName = registerDto.UserName.ToLower(),
             PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password)),  
             PasswordSalt = hmac.Key 
            };

            this.context.Users.Add(user);
            await this.context.SaveChangesAsync();

            return new UserDto
            {
                UserName = user.UserName,
                Token = this.tokenService.CreateToken(user)
            };

        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(DTOs.LoginDto loginDto)
        {
           var user = await this.context.Users
                .SingleOrDefaultAsync(x => x.UserName == loginDto.UserName); 
            
            if (user == null) return Unauthorized("Invalid Username");

            using var hmac = new HMACSHA512(user.PasswordSalt);

            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

            for (int i = 0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != user.PasswordHash[i]) return Unauthorized("Invalid Password");
            }
            
            return new UserDto
            {
                UserName = user.UserName,
                Token = this.tokenService.CreateToken(user)
            };
        }

        private async Task<bool> UserExists(string username)
        {
            return await this.context.Users.AnyAsync(x => x.UserName == username.ToLower());
        }
    }
}