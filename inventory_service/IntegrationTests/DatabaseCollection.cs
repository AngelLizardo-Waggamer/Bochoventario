using Xunit;

namespace inventory_service.IntegrationTests
{
    /// <summary>
    /// Define una colección de tests que comparten el mismo DatabaseFixture.
    /// Esto asegura que todos los tests de integración usen la misma instancia
    /// del contenedor MySQL, reduciendo el tiempo de ejecución drásticamente.
    /// </summary>
    [CollectionDefinition("Database collection")]
    public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
    {
        // Esta clase no tiene código, solo sirve como definición de colección
        // para que xUnit agrupe los tests
    }
}
