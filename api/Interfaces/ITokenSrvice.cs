using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Models;

namespace api.Interfaces
{
    public interface ITokenService
    {
        Task<string> CreateToken(User user);
    }
}