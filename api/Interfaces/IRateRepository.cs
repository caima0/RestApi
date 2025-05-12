using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Rate;
using api.Models;

namespace api.Interfaces
{
    public interface IRateRepository
    {
        Task<List<Rate>> GetAllAsync();
        Task<Rate?> GetByIdAsync(int id);
        Task<Rate> CreateAsync(Rate currencieModel);
        Task<Rate?> UpdateAsync (int id, UpdateCurencieReaquestDto rateDto);
        Task<Rate?> DeleteAsync(int id);
        Task UpdateRatesFromNBPAsync(List<ResponseItem> responseItems);
    }
}