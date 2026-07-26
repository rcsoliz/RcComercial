namespace RcComercial.Application.Categorias;

public record CategoriaDto(Guid Id, string Nombre, Guid? PadreId, bool Activo);
