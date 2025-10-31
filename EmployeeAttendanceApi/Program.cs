using EmployeeAttendanceApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register your service as Singleton (model loaded once)
var modelPath = Path.Combine(builder.Environment.ContentRootPath, "faceNet.onnx");
builder.Services.AddSingleton<FaceRecognitionService>(sp =>
    new FaceRecognitionService(modelPath,
        sp.GetRequiredService<ILogger<FaceRecognitionService>>()));
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
