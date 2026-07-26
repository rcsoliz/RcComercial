namespace RcComercial.Tests.Infraestructura;

[CollectionDefinition("BaseDatos")]
public class ColeccionBaseDatos : ICollectionFixture<PostgresContainerFixture>;
