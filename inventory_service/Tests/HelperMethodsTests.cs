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

        private (bool, int?, string?) InvokeValidateUserPermissions()
        {
            var method = typeof(InventoryController).GetMethod("ValidateUserPermissions", BindingFlags.NonPublic | BindingFlags.Instance);
            return ((bool, int?, string?))method!.Invoke(_controller, null)!;
        }

        [Fact]
        public void GetUserIdFromToken_ConClaimIdUsuario_RetornaUserId()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim("id_usuario", "123")
            };
            SetupUserClaims(claims);

            // Act
            var userId = InvokePrivateMethod<int?>("GetUserIdFromToken");

            // Assert
            Assert.NotNull(userId);
            Assert.Equal(123, userId.Value);
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
        public void GetUsernameFromToken_ConClaimNombreUsuario_RetornaUsername()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim("nombre_usuario", "admin")
            };
            SetupUserClaims(claims);

            // Act
            var username = InvokePrivateMethod<string?>("GetUsernameFromToken");

            // Assert
            Assert.NotNull(username);
            Assert.Equal("admin", username);
        }

        [Fact]
        public void GetUsernameFromToken_SinClaim_RetornaNull()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim("id_usuario", "1")
            };
            SetupUserClaims(claims);

            // Act
            var username = InvokePrivateMethod<string?>("GetUsernameFromToken");

            // Assert
            Assert.Null(username);
        }

        [Fact]
        public void GetRoleIdFromToken_ConClaimIdRol_RetornaRoleId()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim("id_rol", "1")
            };
            SetupUserClaims(claims);

            // Act
            var roleId = InvokePrivateMethod<int?>("GetRoleIdFromToken");

            // Assert
            Assert.NotNull(roleId);
            Assert.Equal(1, roleId.Value);
        }

        [Fact]
        public void GetRoleIdFromToken_SinClaim_RetornaNull()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim("id_usuario", "1")
            };
            SetupUserClaims(claims);

            // Act
            var roleId = InvokePrivateMethod<int?>("GetRoleIdFromToken");

            // Assert
            Assert.Null(roleId);
        }

        [Fact]
        public void ValidateUserPermissions_UsuarioAdministrador_RetornaValido()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim("id_usuario", "1"),
                new Claim("nombre_usuario", "admin"),
                new Claim("id_rol", "1")
            };
            SetupUserClaims(claims);

            // Act
            var (isValid, userId, errorMessage) = InvokeValidateUserPermissions();

            // Assert
            Assert.True(isValid);
            Assert.NotNull(userId);
            Assert.Equal(1, userId.Value);
            Assert.Null(errorMessage);
        }

        [Fact]
        public void ValidateUserPermissions_UsuarioGestor_RetornaValido()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim("id_usuario", "2"),
                new Claim("nombre_usuario", "gestor"),
                new Claim("id_rol", "2")
            };
            SetupUserClaims(claims);

            // Act
            var (isValid, userId, errorMessage) = InvokeValidateUserPermissions();

            // Assert
            Assert.True(isValid);
            Assert.NotNull(userId);
            Assert.Equal(2, userId.Value);
            Assert.Null(errorMessage);
        }

        [Fact]
        public void ValidateUserPermissions_UsuarioLector_RetornaInvalido()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim("id_usuario", "3"),
                new Claim("nombre_usuario", "lector"),
                new Claim("id_rol", "3")
            };
            SetupUserClaims(claims);

            // Act
            var (isValid, userId, errorMessage) = InvokeValidateUserPermissions();

            // Assert
            Assert.False(isValid);
            Assert.Null(userId);
            Assert.NotNull(errorMessage);
            Assert.Contains("no tiene permisos suficientes", errorMessage);
        }

        [Fact]
        public void ValidateUserPermissions_SinUserId_RetornaInvalido()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim("nombre_usuario", "admin"),
                new Claim("id_rol", "1")
            };
            SetupUserClaims(claims);

            // Act
            var (isValid, userId, errorMessage) = InvokeValidateUserPermissions();

            // Assert
            Assert.False(isValid);
            Assert.Null(userId);
            Assert.NotNull(errorMessage);
            Assert.Contains("No se pudo obtener el ID del usuario del token JWT", errorMessage);
        }

        [Fact]
        public void ValidateUserPermissions_SinUsername_RetornaInvalido()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim("id_usuario", "1"),
                new Claim("id_rol", "1")
            };
            SetupUserClaims(claims);

            // Act
            var (isValid, userId, errorMessage) = InvokeValidateUserPermissions();

            // Assert
            Assert.False(isValid);
            Assert.Null(userId);
            Assert.NotNull(errorMessage);
            Assert.Contains("No se pudo obtener el nombre de usuario del token JWT", errorMessage);
        }

        [Fact]
        public void ValidateUserPermissions_SinRoleId_RetornaInvalido()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim("id_usuario", "1"),
                new Claim("nombre_usuario", "admin")
            };
            SetupUserClaims(claims);

            // Act
            var (isValid, userId, errorMessage) = InvokeValidateUserPermissions();

            // Assert
            Assert.False(isValid);
            Assert.Null(userId);
            Assert.NotNull(errorMessage);
            Assert.Contains("No se pudo obtener el rol del usuario del token JWT", errorMessage);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
