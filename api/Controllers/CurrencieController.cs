using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Dtos.Rate;
using api.Interfaces;
using api.Mappers;
using api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers
{
    [Route("api/Rate")]
    [ApiController]
    public class CurrencieController : ControllerBase
    {
        private readonly ApplicationDBContex  _apiclient;
        private readonly IRateRepository _rateRepo;
        public CurrencieController(ApplicationDBContex  apiclient, IRateRepository rateRepo)
        {
            _rateRepo=rateRepo;
            _apiclient= apiclient;
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var Rates = await _rateRepo.GetAllAsync();
            var RateDto=Rates.Select(s=>s.ToRateDto());

            return Ok(Rates);
        }     
        [HttpGet("{id}")]
            public async Task<IActionResult> GetById([FromRoute] int id)
            {
                var rate =await _rateRepo.GetByIdAsync(id);

                if(rate == null)
                {
                    return NotFound();
                }
                return Ok(rate.ToRateDto());
            }
        [HttpPost]
         public async Task<IActionResult> Create([FromBody] CreateCurencieReaquestDto RateDto)
        {
             var currencieModel = RateDto.ToRateFromCreateDTO();
            await _rateRepo.CreateAsync(currencieModel);
             return CreatedAtAction(nameof(GetById), new {id = currencieModel.Id}, currencieModel.ToRateDto());

        }
        [HttpPut]
        [Route("{id}")]
        public  async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateCurencieReaquestDto  updateDto)
        {
            var currencieModel =await _rateRepo.UpdateAsync(id,updateDto);

            if(currencieModel==null)
            {
                 return NotFound();
            }


            return Ok(currencieModel.ToRateDto());

        }
        [HttpDelete]
        [Route("{id}")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
             var currencieModel=await _rateRepo.DeleteAsync(id);

             if(currencieModel ==null)
             {
               return NotFound();
             }

             return NoContent();
              
        }


           
    }
}