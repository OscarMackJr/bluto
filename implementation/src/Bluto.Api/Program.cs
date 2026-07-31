using Bluto.Api.Mapping;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddBlutoMappingApi();

var app = builder.Build();
app.UseAuthorization();
app.MapBlutoMappingApi();
app.Run();

public partial class Program;
