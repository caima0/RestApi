using System.Threading.Tasks;
using api.Controllers;
using api.Dtos.Account;
using api.Interfaces;
using api.Models;
using MockQueryable.Moq;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Linq;

public class AccountControllerTests
{
    private readonly Mock<UserManager<User>> _mockUserManager;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly Mock<SignInManager<User>> _mockSignInManager;
    private readonly AccountController _controller;

    public AccountControllerTests()
    {
        var userStoreMock = new Mock<IUserStore<User>>();
        _mockUserManager = new Mock<UserManager<User>>(userStoreMock.Object, null, null, null, null, null, null, null, null);
        _mockTokenService = new Mock<ITokenService>();
        _mockSignInManager = new Mock<SignInManager<User>>(
            _mockUserManager.Object,
            Mock.Of<Microsoft.AspNetCore.Http.IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<User>>(),
            null, null, null, null);

        _controller = new AccountController(_mockUserManager.Object, _mockTokenService.Object, _mockSignInManager.Object);
    }

    [Fact]
    public async Task GetAccount_ReturnsOkResult_WhenUserExists()
    {
        var username = "testuser";
        var user = new User { UserName = username, Email = "testuser@example.com" };
        var users = new List<User> { user }.AsQueryable().BuildMockDbSet();

        _mockUserManager.Setup(um => um.Users).Returns(users.Object);
        _mockUserManager.Setup(um => um.GetRolesAsync(It.IsAny<User>())).ReturnsAsync(new List<string> { "User" });
        _mockTokenService.Setup(ts => ts.CreateToken(It.IsAny<User>())).ReturnsAsync("sample-token");

        var result = await _controller.GetAccount(username);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var userDto = Assert.IsType<NewUserDto>(okResult.Value);
        Assert.Equal(username, userDto.UserName);
        Assert.Equal("testuser@example.com", userDto.Email);
        Assert.Equal("sample-token", userDto.Token);
        Assert.Contains("User", userDto.Roles);
    }

    [Fact]
    public async Task GetAccount_ReturnsNotFound_WhenUserDoesNotExist()
    {
        var username = "nonexistentuser";
        var users = new List<User>().AsQueryable().BuildMockDbSet();

        _mockUserManager.Setup(um => um.Users).Returns(users.Object);

        var result = await _controller.GetAccount(username);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("User not found", notFoundResult.Value);
    }
}
