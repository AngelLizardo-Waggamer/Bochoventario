using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;
using inventory_service.Controllers;
using inventory_service.Data;
using inventory_service.Models;

namespace inventory_service.Tests
{
    public class HelperMethodsTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly InventoryController _controller;

        public HelperMethodsTests()
        {
            // Configurar base de datos en memoria
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            
            // Seed data inicial
            SeedDatabase();
            
            _controller = new InventoryController(_context);
        }

        private void SeedDatabase()
        {
            var roles = new List<Rol>
            {
                new Rol { IdRol = 1, NombreRol = "Administrador" },
                new Rol { IdRol = 2, NombreRol = "Gestor" },
                new Rol { IdRol = 3, NombreRol = "Lector" }
            };
            _context.Roles.AddRange(roles);

            var usuarios = new List<Usuario>
            {
                new Usuario
                {
                    IdUsuario = 1,
                    IdRol = 1,
                    NombreUsuario = "admin",
                    PasswordHash = "hash",
                    NombreCompleto = "Administrador"
                },
                new Usuario
                {
                    IdUsuario = 2,
                    IdRol = 2,
                    NombreUsuario = "gestor",
                    PasswordHash = "hash",
                    NombreCompleto = "Gestor"
                },
                new Usuario
                {
                    IdUsuario = 3,
                    IdRol = 3,
                    NombreUsuario = "lector",
                    PasswordHash = "hash",
                    NombreCompleto = "Lector"
                }
            };
            _context.Usuarios.AddRange(usuarios);

            _context.SaveChanges();
        }

        private void SetupUserClaims(List<Claim> claims)
        {
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        // Método helper para invocar métodos privados usando reflexión
        private T InvokePrivateMethod<T>(string methodName, params object[] parameters)
        {
            var method = typeof(InventoryController).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            return (T)method!.Invoke(_controller, parameters)!;
        }

        private async Task<(bool, Usuario?, string?)> InvokeValidateUserPermissionsAsync()
        {
            var method = typeof(InventoryController).GetMethod("ValidateUserPermissions", BindingFlags.NonPublic | BindingFlags.Instance);
            var task = (Task<(bool, Usuario?, string?)>)method!.Invoke(_controller, null)!;
            return await task;
        }

        [Fact]
        public void GetUserIdFromToken_ConClaimNameIdentifier_RetornaUserId()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "123")
            };
            SetupUserClaims(claims);

            // Act
            var userId = InvokePrivateMethod<int?>("GetUserIdFromToken");

            // Assert
            Assert.NotNull(userId);
            Assert.Equal(123, userId.Value);
        }

        [Fact]
        public void GetUserIdFromToken_ConClaimSub_RetornaUserId()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim("sub", "456")
            };
            SetupUserClaims(claims);

            // Act
            var userId = InvokePrivateMethod<int?>("GetUserIdFromToken");

            // Assert
            Assert.NotNull(userId);
            Assert.Equal(456, userId.Value);
        }

        [Fact]
        public void GetUserIdFromToken_ConClaimUserId_RetornaUserId()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim("userId", "789")
            };
            SetupUserClaims(claims);

            // Act
            var userId = InvokePrivateMethod<int?>("GetUserIdFromToken");

            // Assert
            Assert.NotNull(userId);
            Assert.Equal(789, userId.Value);
        }

        [Fact]
        public void GetUserIdFromToken_SinClaimValido_RetornaNull()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim("otherClaim", "value")
            };
            SetupUserClaims(claims);

            // Act
            var userId = InvokePrivateMethod<int?>("GetUserIdFromToken");

            // Assert
            Assert.Null(userId);
        }

        [Fact]
        public async Task ValidateUserPermissions_UsuarioAdministrador_RetornaValido()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1")
            };
            SetupUserClaims(claims);

            // Act
            var (isValid, usuario, errorMessage) = await InvokeValidateUserPermissionsAsync();

            // Assert
            Assert.True(isValid);
            Assert.NotNull(usuario);
            Assert.Equal(1, usuario.IdRol);
            Assert.Equal("admin", usuario.NombreUsuario);
            Assert.Null(errorMessage);
        }

        [Fact]
        public async Task ValidateUserPermissions_UsuarioGestor_RetornaValido()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "2")
            };
            SetupUserClaims(claims);

            // Act
            var (isValid, usuario, errorMessage) = await InvokeValidateUserPermissionsAsync();

            // Assert
            Assert.True(isValid);
            Assert.NotNull(usuario);
            Assert.Equal(2, usuario.IdRol);
            Assert.Equal("gestor", usuario.NombreUsuario);
            Assert.Null(errorMessage);
        }

        [Fact]
        public async Task ValidateUserPermissions_UsuarioLector_RetornaInvalido()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "3")
            };
            SetupUserClaims(claims);

            // Act
            var (isValid, usuario, errorMessage) = await InvokeValidateUserPermissionsAsync();

            // Assert
            Assert.False(isValid);
            Assert.Null(usuario);
            Assert.NotNull(errorMessage);
            Assert.Contains("no tiene permisos suficientes", errorMessage);
        }

        [Fact]
        public async Task ValidateUserPermissions_UsuarioNoExiste_RetornaInvalido()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "999")
            };
            SetupUserClaims(claims);

            // Act
            var (isValid, usuario, errorMessage) = await InvokeValidateUserPermissionsAsync();

            // Assert
            Assert.False(isValid);
            Assert.Null(usuario);
            Assert.NotNull(errorMessage);
            Assert.Contains("Usuario con ID 999 no encontrado", errorMessage);
        }

        [Fact]
        public async Task ValidateUserPermissions_SinUserId_RetornaInvalido()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim("otherClaim", "value")
            };
            SetupUserClaims(claims);

            // Act
            var (isValid, usuario, errorMessage) = await InvokeValidateUserPermissionsAsync();

            // Assert
            Assert.False(isValid);
            Assert.Null(usuario);
            Assert.NotNull(errorMessage);
            Assert.Contains("No se pudo obtener el ID del usuario del token JWT", errorMessage);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
