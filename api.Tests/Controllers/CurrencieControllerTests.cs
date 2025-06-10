using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using api.Controllers;
using api.Data;
using api.Dtos.Rate;
using api.Interfaces;
using api.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace api.Tests.Controllers
{
    public class CurrencieControllerTests
    {
        private readonly Mock<IRateRepository> _mockRateRepo;
        private readonly Mock<INBPClient> _mockNbpClient;
        private readonly CurrencieController _controller;

        public CurrencieControllerTests()
        {
            _mockRateRepo = new Mock<IRateRepository>();
            _mockNbpClient = new Mock<INBPClient>();
            _controller = new CurrencieController(null, _mockRateRepo.Object, _mockNbpClient.Object);
        }

        [Fact]
        public async Task GetAll_ShouldReturnAllRates()
        {
            var expectedRates = new List<Rate>
            {
                new Rate { Code = "USD", Currency = "US Dollar", Bid = 1.0, Ask = 1.1 },
                new Rate { Code = "EUR", Currency = "Euro", Bid = 0.85, Ask = 0.95 }
            };
            _mockRateRepo.Setup(repo => repo.GetAllAsync())
                .ReturnsAsync(expectedRates);

            var result = await _controller.GetAll();

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedRates = okResult.Value.Should().BeAssignableTo<IEnumerable<RateDto>>().Subject;
            returnedRates.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetByCode_WithValidCode_ShouldReturnRate()
        {
            var expectedRate = new Rate { Code = "USD", Currency = "US Dollar", Bid = 1.0, Ask = 1.1 };
            _mockRateRepo.Setup(repo => repo.GetByCodeAsync("USD"))
                .ReturnsAsync(expectedRate);

            var result = await _controller.GetByCode("USD");


            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedRate = okResult.Value.Should().BeAssignableTo<RateDto>().Subject;
            returnedRate.Code.Should().Be("USD");
        }

        [Fact]
        public async Task GetByCode_WithInvalidCode_ShouldReturnNotFound()
        {
            _mockRateRepo.Setup(repo => repo.GetByCodeAsync("INVALID"))
                .ReturnsAsync((Rate)null);

            var result = await _controller.GetByCode("INVALID");

            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task Create_WithValidData_ShouldCreateRate()
        {
            var createDto = new CreateCurencieReaquestDto
            {
                Code = "USD",
                Currency = "US Dollar",
                Bid = 1.0,
                Ask = 1.1
            };

            var expectedRate = new Rate
            {
                Code = createDto.Code,
                Currency = createDto.Currency,
                Bid = createDto.Bid,
                Ask = createDto.Ask
            };

            _mockRateRepo.Setup(repo => repo.CreateAsync(It.IsAny<Rate>()))
                .ReturnsAsync(expectedRate);

            var result = await _controller.Create(createDto);

            var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdResult.ActionName.Should().Be(nameof(_controller.GetByCode));
            createdResult.RouteValues["code"].Should().Be(createDto.Code);
        }

        [Fact]
        public async Task Update_WithValidData_ShouldUpdateRate()
        {
            var updateDto = new UpdateCurencieReaquestDto
            {
                Currency = "Updated Dollar",
                Code = "USD",
                Bid = 1.1,
                Ask = 1.2
            };

            var existingRate = new Rate
            {
                Code = "USD",
                Currency = "US Dollar",
                Bid = 1.0,
                Ask = 1.1
            };

            _mockRateRepo.Setup(repo => repo.UpdateAsync("USD", updateDto))
                .ReturnsAsync(existingRate);

            var result = await _controller.Update("USD", updateDto);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedRate = okResult.Value.Should().BeAssignableTo<RateDto>().Subject;
            returnedRate.Code.Should().Be("USD");
        }

        [Fact]
        public async Task Update_WithInvalidCode_ShouldReturnNotFound()
        {
            var updateDto = new UpdateCurencieReaquestDto
            {
                Currency = "Updated Dollar",
                Code = "USD",
                Bid = 1.1,
                Ask = 1.2
            };

            _mockRateRepo.Setup(repo => repo.UpdateAsync("INVALID", updateDto))
                .ReturnsAsync((Rate)null);

            var result = await _controller.Update("INVALID", updateDto);

            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task Delete_WithValidCode_ShouldDeleteRate()
        {
            var existingRate = new Rate
            {
                Code = "USD",
                Currency = "US Dollar",
                Bid = 1.0,
                Ask = 1.1
            };

            _mockRateRepo.Setup(repo => repo.DeleteAsync("USD"))
                .ReturnsAsync(existingRate);

            var result = await _controller.Delete("USD");

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task Delete_WithInvalidCode_ShouldReturnNotFound()
        {
            _mockRateRepo.Setup(repo => repo.DeleteAsync("INVALID"))
                .ReturnsAsync((Rate)null);

            var result = await _controller.Delete("INVALID");

            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task UpdateFromNBP_ShouldUpdateRates()
        {

            var responseItems = new List<ResponseItem>
            {
                new ResponseItem
                {
                    Table = "A",
                    No = "123",
                    TradingDate = "2024-01-01",
                    EffectiveDate = "2024-01-01",
                    Rates = new List<Rate>
                    {
                        new Rate { Code = "USD", Currency = "US Dollar", Bid = 1.0, Ask = 1.1 },
                        new Rate { Code = "EUR", Currency = "Euro", Bid = 0.85, Ask = 0.95 }
                    }
                }
            };

            _mockNbpClient.Setup(client => client.GetExchangeRatesAsync())
                .ReturnsAsync(responseItems);

            _mockRateRepo.Setup(repo => repo.UpdateRatesFromNBPAsync(responseItems))
                .Returns(Task.CompletedTask);

            var result = await _controller.UpdateFromNBP();

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<object>().Subject;
            response.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateFromNBP_WhenExceptionOccurs_ShouldReturnError()
        {
            _mockNbpClient.Setup(client => client.GetExchangeRatesAsync())
                .ThrowsAsync(new Exception("NBP API error"));

            var result = await _controller.UpdateFromNBP();

            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }
    }
} 