using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Dtos.Rate;
using api.Interfaces;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Repository
{
    public class RateRepository: IRateRepository
    {
        private readonly ApplicationDBContex _apiclient;
        public RateRepository(ApplicationDBContex apiclient)
        {
             _apiclient = apiclient;
        }

        public async Task<Rate> CreateAsync(Rate currencieModel)
        {
            await _apiclient.Rates.AddAsync(currencieModel);
            await _apiclient.SaveChangesAsync();
            return currencieModel;
        }

        public async Task<Rate?> DeleteAsync(int id)
        {
            var currencieModel = await _apiclient.Rates.FirstOrDefaultAsync(x => x.Id == id);

            if (currencieModel==null)
            {
                 return null;
            }
            _apiclient.Rates.Remove(currencieModel);
            await _apiclient.SaveChangesAsync();
            return currencieModel;
        }

        public async Task<List<Rate>> GetAllAsync()
        {
           return  await _apiclient.Rates.ToListAsync();
        }

        public async Task<Rate?> GetByIdAsync(int id)
        {
            return await _apiclient.Rates.FindAsync(id);
        }

        public async Task<Rate?> UpdateAsync(int id, UpdateCurencieReaquestDto rateDto)
        {
            var existingCurrencie = await _apiclient.Rates.FirstOrDefaultAsync(x=>x.Id==id);
            if (existingCurrencie==null)
            {
                return null;
            }
            existingCurrencie.Currency = rateDto.Currency;
            existingCurrencie.Code = rateDto.Code;
            existingCurrencie.Bid = rateDto.Bid;
            existingCurrencie.Ask = rateDto.Ask;

            await _apiclient.SaveChangesAsync();
            return existingCurrencie;
        }

        public async Task UpdateRatesFromNBPAsync(List<ResponseItem> responseItems)
        {
            // Delete all existing rates and response items
            _apiclient.Rates.RemoveRange(await _apiclient.Rates.ToListAsync());
            _apiclient.ResponseItem.RemoveRange(await _apiclient.ResponseItem.ToListAsync());
            await _apiclient.SaveChangesAsync();

            // Add new data
            foreach (var responseItem in responseItems)
            {
                await _apiclient.ResponseItem.AddAsync(responseItem);
            }
            await _apiclient.SaveChangesAsync();
        }
    }
}